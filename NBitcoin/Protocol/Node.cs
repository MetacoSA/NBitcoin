using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public enum NodeState : int
	{
		Failed,
		Offline,
		Disconnecting,
		Connected,
		HandShaked
	}


	public delegate void NodeEventHandler(Node node);
	public delegate void NodeStateEventHandler(Node node, NodeState oldState);
	public class Node
	{
		public class NodeConnection
		{
			private readonly Node _Node;
			public Node Node
			{
				get
				{
					return _Node;
				}
			}
			private readonly Socket _Socket;
			public Socket Socket
			{
				get
				{
					if(_IsDisposed)
					{
						throw new InvalidOperationException("Connection disposed");
					}
					return _Socket;
				}
			}
			private readonly ManualResetEvent _Disconnected;
			public ManualResetEvent Disconnected
			{
				get
				{
					return _Disconnected;
				}
			}
			private readonly CancellationTokenSource _Cancel;
			public CancellationTokenSource Cancel
			{
				get
				{
					return _Cancel;
				}
			}
			public TraceCorrelation TraceCorrelation
			{
				get
				{
					return Node.TraceCorrelation;
				}
			}

			public NodeConnection(Node node, Socket socket)
			{
				_Node = node;
				_Socket = socket;
				_Disconnected = new ManualResetEvent(false);
				_Cancel = new CancellationTokenSource();
			}

			EventLoopMessageListener<IncomingMessage> _PingListener;

			bool _IsDisposed;
			public void Dispose()
			{
				if(!_IsDisposed)
				{
					Utils.SafeCloseSocket(Socket);
					if(_PingListener != null)
					{
						Node.MessageProducer.RemoveMessageListener(_PingListener);
						_PingListener.Dispose();
						_PingListener = null;
					}
					_IsDisposed = true;
				}
			}

			void MessageReceived(IncomingMessage message)
			{
				var ping = message.Message.Payload as PingPayload;
				if(ping != null)
				{
					message.Node.SendMessage(ping.CreatePong());
				}
			}

			public void BeginListen()
			{
				new Thread(() =>
				{
					using(TraceCorrelation.Open(false))
					{
						NodeServerTrace.Information("Listening");
						_PingListener = new EventLoopMessageListener<IncomingMessage>(MessageReceived);
						Node.MessageProducer.AddMessageListener(_PingListener);
						try
						{
							while(!Cancel.Token.IsCancellationRequested)
							{
								PerformanceCounter counter;
								var message = Message.ReadNext(Socket, Node.NodeServer.Network, Node.Version, Cancel.Token, out counter);

								Node.LastSeen = DateTimeOffset.UtcNow;
								Node.MessageProducer.PushMessage(new IncomingMessage()
								{
									Message = message,
									Socket = Socket,
									Node = Node
								});
								Node.Counter.Add(counter);
							}
						}
						catch(OperationCanceledException)
						{
						}
						catch(Exception ex)
						{
							if(Node.State != NodeState.Disconnecting)
							{
								Node.State = NodeState.Failed;
								NodeServerTrace.Error("Connection to server stopped unexpectedly", ex);
							}
						}
						NodeServerTrace.Information("Stop listening");
						if(Node.State != NodeState.Failed)
							Node.State = NodeState.Offline;
						Dispose();
						_Disconnected.Set();
					}
				}).Start();
			}

		}


		volatile NodeState _State = NodeState.Offline;
		public NodeState State
		{
			get
			{
				return _State;
			}
			private set
			{
				TraceCorrelation.LogInside(() => NodeServerTrace.Information("State changed from " + _State + " to " + value));
				var previous = _State;
				_State = value;
				if(previous != _State)
				{
					OnStateChanged(previous);
				}

				if(value == NodeState.Failed)
				{
					var peer = Peer.Clone();
					peer.NetworkAddress.ZeroTime();
					NodeServer._InternalMessageProducer.PushMessage(peer);
				}
				if(value == NodeState.Failed || value == NodeState.Offline || value == NodeState.Disconnecting)
				{
					NodeServer.RemoveNode(this);
				}
				if(value == NodeState.Failed || value == NodeState.Offline)
				{
					TraceCorrelation.LogInside(() => NodeServerTrace.Trace.TraceEvent(TraceEventType.Stop, 0, "Communication closed"));
				}

				if(value == NodeState.Offline || value == NodeState.Failed)
				{
					OnDisconnected();
				}
			}
		}

		public event NodeStateEventHandler StateChanged;
		private void OnStateChanged(NodeState previous)
		{
			if(StateChanged != null)
			{
				try
				{
					StateChanged(this, previous);
				}
				catch(Exception ex)
				{
					TraceCorrelation.LogInside(() => NodeServerTrace.Error("Error while Disconnected event raised", ex));
				}
			}
		}

		public event NodeEventHandler Disconnected;
		private void OnDisconnected()
		{
			if(Disconnected != null)
			{
				try
				{
					Disconnected(this);
				}
				catch(Exception ex)
				{
					TraceCorrelation.LogInside(() => NodeServerTrace.Error("Error while Disconnected event raised", ex));
				}
			}
		}

		private readonly NodeServer _NodeServer;
		public NodeServer NodeServer
		{
			get
			{
				return _NodeServer;
			}
		}


		internal readonly NodeConnection _Connection;




		internal Node(Peer peer, NodeServer nodeServer, CancellationToken cancellation)
		{
			Version = nodeServer.Version;
			_NodeServer = nodeServer;
			_Peer = peer;
			LastSeen = peer.NetworkAddress.Time;

			var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			_Connection = new NodeConnection(this, socket);
			using(TraceCorrelation.Open())
			{
				try
				{
					var ar = socket.BeginConnect(Peer.NetworkAddress.Endpoint, null, null);
					WaitHandle.WaitAny(new WaitHandle[] { ar.AsyncWaitHandle, cancellation.WaitHandle });
					cancellation.ThrowIfCancellationRequested();
					socket.EndConnect(ar);
					State = NodeState.Connected;
					NodeServerTrace.Information("Outbound connection successfull");
				}
				catch(OperationCanceledException)
				{
					Utils.SafeCloseSocket(socket);
					NodeServerTrace.Information("Connection to node cancelled");
					State = NodeState.Offline;
					return;
				}
				catch(Exception ex)
				{
					Utils.SafeCloseSocket(socket);
					NodeServerTrace.Error("Error connecting to the remote endpoint ", ex);
					State = NodeState.Failed;
					throw;
				}
				_Connection.BeginListen();
			}
		}
		internal Node(Peer peer, NodeServer nodeServer, Socket socket, VersionPayload version)
		{
			_Peer = peer;
			_NodeServer = nodeServer;
			_Connection = new NodeConnection(this, socket);
			_FullVersion = version;
			Version = version.Version;
			LastSeen = peer.NetworkAddress.Time;
			TraceCorrelation.LogInside(() =>
			{
				NodeServerTrace.Information("Connected to advertised node " + _Peer.NetworkAddress.Endpoint);
				State = NodeState.Connected;
			});
			_Connection.BeginListen();
		}

		private readonly Peer _Peer;
		public Peer Peer
		{
			get
			{
				return _Peer;
			}
		}

		public DateTimeOffset LastSeen
		{
			get;
			private set;
		}

		TraceCorrelation _TraceCorrelation = null;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TraceCorrelation TraceCorrelation
		{
			get
			{
				if(_TraceCorrelation == null)
				{
					_TraceCorrelation = new TraceCorrelation(NodeServerTrace.Trace, "Communication with " + Peer.NetworkAddress.Endpoint.ToString());
				}
				return _TraceCorrelation;
			}
		}
		public void SendMessage(Payload payload)
		{
			var message = new Message();
			message.Magic = NodeServer.Network.Magic;
			message.UpdatePayload(payload, Version);
			TraceCorrelation.LogInside(() => NodeServerTrace.Verbose("Sending message " + message));

			var bytes = message.ToBytes();
			Counter.AddWritten(bytes.LongLength);
			var result = _Connection.Socket.Send(bytes);
		}

		private PerformanceCounter _Counter;
		public PerformanceCounter Counter
		{
			get
			{
				if(_Counter == null)
					_Counter = new PerformanceCounter();
				return _Counter;
			}
		}

		public ProtocolVersion Version
		{
			get;
			private set;
		}

		private readonly MessageProducer<IncomingMessage> _MessageProducer = new MessageProducer<IncomingMessage>();
		public MessageProducer<IncomingMessage> MessageProducer
		{
			get
			{
				return _MessageProducer;
			}
		}

		public TPayload RecieveMessage<TPayload>(TimeSpan timeout) where TPayload : Payload
		{
			var source = new CancellationTokenSource();
			source.CancelAfter(timeout);
			return RecieveMessage<TPayload>(source.Token);
		}



		public TPayload RecieveMessage<TPayload>(CancellationToken cancellationToken = default(CancellationToken)) where TPayload : Payload
		{
			using(var listener = new NodeListener(this))
			{
				return listener.ReceivePayload<TPayload>(cancellationToken);
			}
		}


		VersionPayload _FullVersion;
		public VersionPayload FullVersion
		{
			get
			{
				return _FullVersion;
			}
		}

		public void VersionHandshake(CancellationToken cancellationToken = default(CancellationToken))
		{
			var listener = new PollMessageListener<IncomingMessage>();
			using(MessageProducer.AddMessageListener(listener))
			{
				using(TraceCorrelation.Open())
				{
					var myVersion = CreateVersionPayload();
					SendMessage(myVersion);

					var version = listener.RecieveMessage(cancellationToken).AssertPayload<VersionPayload>();
					_FullVersion = version;
					Version = version.Version;
					if(version.Nonce == NodeServer.Nonce)
					{
						NodeServerTrace.ConnectionToSelfDetected();
						Disconnect();
						throw new InvalidOperationException("Impossible to connect to self");
					}

					if(!version.AddressReciever.Address.Equals(ExternalEndpoint.Address))
					{
						NodeServerTrace.Warning("Different external address detected by the node " + version.AddressReciever.Address + " instead of " + ExternalEndpoint.Address);
					}
					NodeServer.ExternalAddressDetected(version.AddressReciever.Address);
					if(version.Version < ProtocolVersion.MIN_PEER_PROTO_VERSION)
					{
						NodeServerTrace.Warning("Outdated version " + version.Version + " disconnecting");
						Disconnect();
						return;
					}
					SendMessage(new VerAckPayload());
					listener.RecieveMessage(cancellationToken).AssertPayload<VerAckPayload>();
					State = NodeState.HandShaked;
					if(NodeServer.AdvertizeMyself)
						AdvertiseMyself();
				}
			}
		}

		public bool AdvertiseMyself()
		{
			if(NodeServer.IsListening && NodeServer.ExternalEndpoint.Address.IsRoutable(NodeServer.AllowLocalPeers))
			{
				TraceCorrelation.LogInside(() => NodeServerTrace.Information("Advertizing myself"));
				SendMessage(new AddrPayload(new NetworkAddress()
				{
					Ago = TimeSpan.FromSeconds(0),
					Endpoint = NodeServer.ExternalEndpoint
				}));
				return true;
			}
			else
				return false;

		}


		public void RespondToHandShake(CancellationToken cancellation = default(CancellationToken))
		{
			using(TraceCorrelation.Open())
			{
				var listener = new PollMessageListener<IncomingMessage>();
				using(MessageProducer.AddMessageListener(listener))
				{
					NodeServerTrace.Information("Responding to handshake");
					SendMessage(CreateVersionPayload());
					listener.RecieveMessage().AssertPayload<VerAckPayload>();
					SendMessage(new VerAckPayload());
					_State = NodeState.HandShaked;
					if(NodeServer.AdvertizeMyself)
						AdvertiseMyself();
				}
			}
		}

		private VersionPayload CreateVersionPayload()
		{
			return NodeServer.CreateVersionPayload(Peer, ExternalEndpoint, Version);
		}
		public IPEndPoint ExternalEndpoint
		{
			get
			{
				var me = (IPEndPoint)_Connection.Socket.LocalEndPoint;
				if(!me.Address.IsRoutable(NodeServer.AllowLocalPeers))
				{
					me = new IPEndPoint(NodeServer.ExternalEndpoint.Address, me.Port);
				}
				return me;
			}
		}

		public void Disconnect()
		{
			if(State < NodeState.Connected)
			{
				_Connection.Disconnected.WaitOne();
				return;
			}
			using(TraceCorrelation.Open())
			{
				NodeServerTrace.Information("Disconnection request");
				State = NodeState.Disconnecting;
				_Connection.Cancel.Cancel();
				_Connection.Disconnected.WaitOne();
			}
		}


		public override string ToString()
		{
			return State + " (" + Peer.NetworkAddress.Endpoint + ") " + Peer.Origin;
		}



		public Socket Socket
		{
			get
			{
				return _Connection.Socket;
			}
		}

		public Chain GetChain(uint256 hashStop = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var ms = new MemoryStream();
			Chain chain = new Chain(NodeServer.Network, new StreamObjectStream<ChainChange>(ms));
			SynchronizeChain(chain, hashStop, cancellationToken);
			return chain;
		}
		public IEnumerable<ChainedBlock> GetHeadersFromFork(ChainedBlock currentTip,
														uint256 hashStop = null,
														CancellationToken cancellationToken = default(CancellationToken))
		{
			AssertState(NodeState.HandShaked, cancellationToken);
			using(TraceCorrelation.Open())
			{
				NodeServerTrace.Information("Building chain");
				while(true)
				{
					SendMessage(new GetHeadersPayload()
					{
						BlockLocators = currentTip.GetLocator(),
						HashStop = hashStop
					});
					var headers = this.RecieveMessage<HeadersPayload>(cancellationToken);
					if(headers.Headers.Count == 0)
						break;
					foreach(var header in headers.Headers)
					{
						var prev = currentTip.FindAncestorOrSelf(header.HashPrevBlock);
						if(prev == null)
						{
							NodeServerTrace.Error("Block Header received out of order " + header.GetHash(), null);
							throw new InvalidOperationException("Block Header received out of order");
						}
						currentTip = new ChainedBlock(header, header.GetHash(), prev);
						yield return currentTip;
					}
					if(currentTip.HashBlock == hashStop)
						break;
				}
			}
		}

		public IEnumerable<ChainedBlock> SynchronizeChain(Chain chain, uint256 hashStop = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			List<ChainedBlock> headers = new List<ChainedBlock>();
			foreach(var header in GetHeadersFromFork(chain.Tip, hashStop, cancellationToken))
			{
				chain.SetTip(header);
				headers.Add(header);
			}
			return headers;
		}

		public IEnumerable<Block> GetBlocks(uint256 hashStop = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var genesis = new ChainedBlock(NodeServer.Network.GetGenesis().Header, 0);
			return GetBlocksFromFork(genesis, hashStop, cancellationToken);
		}


		public IEnumerable<Block> GetBlocksFromFork(ChainedBlock currentTip, uint256 hashStop = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			using(var listener = CreateListener())
			{
				SendMessage(new GetBlocksPayload()
				{
					BlockLocators = currentTip.GetLocator(),
				});

				var headers = GetHeadersFromFork(currentTip, hashStop, cancellationToken);

				foreach(var block in GetBlocks(headers.Select(b => b.HashBlock), cancellationToken))
				{
					yield return block;
				}
			}
			//GetBlocks(neededBlocks.ToEnumerable(false).Select(e => e.HashBlock), cancellationToken);
		}
		public IEnumerable<Block> GetBlocks(IEnumerable<uint256> neededBlocks, CancellationToken cancellationToken = default(CancellationToken))
		{
			AssertState(NodeState.HandShaked, cancellationToken);
			using(TraceCorrelation.Open())
			{
				NodeServerTrace.Information("Downloading blocks");
				int simultaneous = 70;
				PerformanceSnapshot lastSpeed = null;
				using(var listener = CreateListener()
									.Where(inc => inc.Message.Payload is InvPayload || inc.Message.Payload is BlockPayload))
				{
					foreach(var invs in neededBlocks
										.Select(b => new InventoryVector()
											{
												Type = InventoryType.MSG_BLOCK,
												Hash = b
											})
										.Partition(simultaneous))
					{
						NodeServerTrace.Information("Speed " + lastSpeed);
						var begin = Counter.Snapshot();

						var invsByHash = invs.ToDictionary(k => k.Hash);

						this.SendMessage(new GetDataPayload(invs.ToArray()));

						Block[] downloadedBlocks = new Block[invs.Count];
						while(invsByHash.Count != 0)
						{
							var block = listener.ReceivePayload<BlockPayload>(cancellationToken).Object;
							var thisHash = block.GetHash();
							if(invsByHash.ContainsKey(thisHash))
							{
								downloadedBlocks[invs.IndexOf(invsByHash[thisHash])] = block;
								invsByHash.Remove(thisHash);
							}
						}
						var end = Counter.Snapshot();
						lastSpeed = end - begin;

						foreach(var downloadedBlock in downloadedBlocks)
						{
							yield return downloadedBlock;
						}
					}

				}
			}
		}

		public NodeListener CreateListener()
		{
			return new NodeListener(this);
		}


		private void AssertState(NodeState nodeState, CancellationToken cancellationToken = default(CancellationToken))
		{
			if(nodeState == NodeState.HandShaked && State == NodeState.Connected)
				this.VersionHandshake(cancellationToken);
			if(nodeState != State)
				throw new InvalidOperationException("Invalid Node state, needed=" + nodeState + ", current= " + State);
		}

		public uint256[] GetMempool(CancellationToken cancellationToken = default(CancellationToken))
		{
			AssertState(NodeState.HandShaked);
			using(var listener = CreateListener().OfType<InvPayload>())
			{
				this.SendMessage(new MempoolPayload());
				return listener.ReceivePayload<InvPayload>(cancellationToken).Inventory.Select(i => i.Hash).ToArray();
			}
		}

		public Transaction[] GetMempoolTransactions(CancellationToken cancellationToken = default(CancellationToken))
		{
			return GetMempoolTransactions(GetMempool(), cancellationToken);
		}

		public Transaction[] GetMempoolTransactions(uint256[] txIds, CancellationToken cancellationToken = default(CancellationToken))
		{
			AssertState(NodeState.HandShaked);
			if(txIds.Length == 0)
				return new Transaction[0];
			List<Transaction> result = new List<Transaction>();
			using(var listener = CreateListener().OfType<TxPayload>())
			{
				this.SendMessage(new GetDataPayload(txIds.Select(txid => new InventoryVector()
				{
					Type = InventoryType.MSG_TX,
					Hash = txid
				}).ToArray()));
				try
				{
					while(result.Count < txIds.Length)
					{
						CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2.0));
						result.Add(listener.ReceivePayload<TxPayload>(
							CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token).Token).Object);

					}
				}
				catch(OperationCanceledException)
				{
					if(cancellationToken.IsCancellationRequested)
					{
						throw;
					}
				}
			}
			return result.ToArray();
		}
	}
}
