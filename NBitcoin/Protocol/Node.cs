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
				_State = value;
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

		public Chain BuildChain(CancellationToken cancellationToken = default(CancellationToken))
		{
			var ms = new MemoryStream();
			Chain chain = new Chain(NodeServer.Network, new StreamObjectStream<ChainChange>(ms));
			return BuildChain(chain, cancellationToken);
		}

		public Chain BuildChain(Chain chain, CancellationToken cancellationToken = default(CancellationToken))
		{
			AssertState(NodeState.HandShaked, cancellationToken);
			using(TraceCorrelation.Open())
			{
				NodeServerTrace.Information("Building chain");
				if(FullVersion.StartHeight <= chain.Height)
				{
					NodeServerTrace.Information("Local chain already ahead");
					return chain;
				}

				while(chain.Height < FullVersion.StartHeight)
				{
					NodeServerTrace.Information("Chain progress : " + chain.Height + "/" + FullVersion.StartHeight);
					SendMessage(new GetHeadersPayload()
					{
						BlockLocators = chain.Tip.GetLocator()
					});
					var headers = this.RecieveMessage<HeadersPayload>(cancellationToken);
					foreach(var header in headers.Headers)
					{
						var prev = chain.GetBlock(header.HashPrevBlock);
						if(prev == null || prev.Height != chain.Height)
						{
							NodeServerTrace.Error("Block Header received out of order " + header.GetHash(), null);
							throw new InvalidOperationException("Block Header received out of order");
						}
						var chained = chain.GetOrAdd(header);
						chain.SetTip(chained);
					}
				}
			}
			return chain;
		}

		public IEnumerable<Block> GetBlocks(CancellationToken cancellationToken = default(CancellationToken))
		{
			Chain chain = new Chain(NodeServer.Network);
			return GetBlocks(chain, cancellationToken);
		}
		public IEnumerable<Block> GetBlocks(Chain chain, CancellationToken cancellationToken = default(CancellationToken))
		{
			AssertState(NodeState.HandShaked, cancellationToken);
			using(TraceCorrelation.Open())
			{
				NodeServerTrace.Information("Downloading blocks");
				if(FullVersion.StartHeight <= chain.Height)
				{
					NodeServerTrace.Information("Local chain already ahead");
					yield break;
				}

				int simultaneous = 70;
				PerformanceSnapshot lastSpeed = null;
				using(var listener = CreateListener()
									.Where(inc => inc.Message.Payload is InvPayload || inc.Message.Payload is BlockPayload))
				{
					while(chain.Height < FullVersion.StartHeight)
					{
						SendMessage(new GetBlocksPayload()
						{
							BlockLocators = chain.Tip.GetLocator(),
						});
						InvPayload blocks = listener.ReceivePayload<InvPayload>(cancellationToken);
						foreach(var invs in blocks
											.Inventory
											.Where(i => i.Type == InventoryType.MSG_BLOCK)
											.Partition(() => simultaneous))
						{
							NodeServerTrace.Information("Download progress : " + chain.Height + "/" + FullVersion.StartHeight + " (" + lastSpeed + " look ahead " + simultaneous + ")");
							var begin = Counter.Snapshot();


							this.SendMessage(new GetDataPayload(invs.ToArray()));

							List<Block> downloaded = new List<Block>();
							while(downloaded.Count != invs.Count)
							{
								var block = listener.ReceivePayload<BlockPayload>(cancellationToken);
								downloaded.Add(block.Object);
							}

							while(downloaded.Count != 0)
							{
								var nextBlock = downloaded
												.Select(b => new
													{
														Block = b,
														Prev = chain.GetBlock(b.Header.HashPrevBlock)
													})
												.Where(b => b.Prev != null)
												.FirstOrDefault();
								if(nextBlock == null)
								{
									downloaded.Clear();
									continue;
								}
								var hash = nextBlock.Block.Header.GetHash();
								if(chain.GetBlock(hash) != null)
								{
									continue;
								}
								yield return nextBlock.Block;
								chain.SetTip(new ChainedBlock(nextBlock.Block.Header, hash, nextBlock.Prev));
								downloaded.Remove(nextBlock.Block);
							}
							var end = Counter.Snapshot();
							lastSpeed = end - begin;
						}
					}
				}
			}
		}

		private NodeListener CreateListener()
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
	}
}
