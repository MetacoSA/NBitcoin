#if !NOSOCKET
using NBitcoin.Protocol.Behaviors;
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

	public class NodeDisconnectReason
	{
		public string Reason
		{
			get;
			set;
		}
		public Exception Exception
		{
			get;
			set;
		}
	}


	public delegate void NodeEventHandler(Node node);
	public delegate void NodeEventMessageIncoming(Node node, IncomingMessage message);
	public delegate void NodeStateEventHandler(Node node, NodeState oldState);
	public class Node : IDisposable
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
#if NOTRACESOURCE
			internal
#else
			public
#endif
 TraceCorrelation TraceCorrelation
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

			EventLoopMessageListener<IncomingMessage> _MessageListener;

			bool _IsDisposed;
			public void Dispose()
			{
				if(!_IsDisposed)
				{
					Utils.SafeCloseSocket(Socket);
					if(_MessageListener != null)
					{
						Node.MessageProducer.RemoveMessageListener(_MessageListener);
						_MessageListener.Dispose();
						_MessageListener = null;
					}
					_IsDisposed = true;
					foreach(var behavior in _Node.Behaviors)
					{
						behavior.Detach();
					}
				}
			}

			void MessageReceived(IncomingMessage message)
			{
				var version = message.Message.Payload as VersionPayload;
				if(version != null && Node.State == NodeState.HandShaked)
				{
					if((uint)message.Node.Version >= 70002)
						message.Node.SendMessage(new RejectPayload()
						{
							Code = RejectCode.DUPLICATE
						});
				}
				Node.OnMessageReceived(message);
			}

			public void BeginListen()
			{
				new Thread(() =>
				{
					using(TraceCorrelation.Open(false))
					{
						NodeServerTrace.Information("Listening");
						_MessageListener = new EventLoopMessageListener<IncomingMessage>(MessageReceived);
						Node.MessageProducer.AddMessageListener(_MessageListener);
						try
						{
							while(!Cancel.Token.IsCancellationRequested)
							{
								PerformanceCounter counter;
								var message = Message.ReadNext(Socket, Node.Network, Node.Version, Cancel.Token, out counter);

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
								Node.DisconnectReason = new NodeDisconnectReason()
								{
									Reason = "Unexpected exception while connecting to socket",
									Exception = ex
								};
							}
						}
						NodeServerTrace.Information("Stop listening");
						if(Node.State != NodeState.Failed)
							Node.State = NodeState.Offline;

						Dispose();

						_Cancel.Cancel();
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


		internal readonly NodeConnection _Connection;


		public static Node ConnectToLocal(Network network,
								ProtocolVersion myVersion = ProtocolVersion.PROTOCOL_VERSION,
								bool isRelay = true,
								CancellationToken cancellation = default(CancellationToken))
		{
			return Connect(network, Utils.ParseIpEndpoint("localhost", network.DefaultPort), myVersion, isRelay, cancellation);
		}

		public static Node Connect(Network network,
								 string endpoint,
								 ProtocolVersion myVersion = ProtocolVersion.PROTOCOL_VERSION,
								bool isRelay = true,
								CancellationToken cancellation = default(CancellationToken))
		{
			return Connect(network, Utils.ParseIpEndpoint(endpoint, network.DefaultPort), myVersion, isRelay, cancellation);
		}
		public static Node Connect(Network network,
								 IPEndPoint endpoint,
								 ProtocolVersion myVersion = ProtocolVersion.PROTOCOL_VERSION,
								bool isRelay = true,
								CancellationToken cancellation = default(CancellationToken))
		{
			var peer = new Peer(PeerOrigin.Manual, new NetworkAddress()
			{
				Time = DateTimeOffset.UtcNow,
				Endpoint = endpoint
			});

			VersionPayload version = new VersionPayload()
			{
				Nonce = 12345,
				UserAgent = VersionPayload.GetNBitcoinUserAgent(),
				Version = myVersion,
				StartHeight = 0,
				Timestamp = DateTimeOffset.UtcNow,
				AddressReceiver = peer.NetworkAddress.Endpoint,
				AddressFrom = new IPEndPoint(IPAddress.Parse("0.0.0.0").MapToIPv6(), network.DefaultPort),
				Relay = isRelay
			};
			return new Node(peer, network, version, cancellation);
		}

		internal Node(Peer peer, Network network, VersionPayload myVersion, CancellationToken cancellation)
		{
			Inbound = false;
			_Behaviors = new BehaviorsCollection(this);
			_MyVersion = myVersion;
			Version = _MyVersion.Version;
			Network = network;
			_Peer = peer;
			LastSeen = peer.NetworkAddress.Time;

			var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
#if !NOIPDUALMODE
			socket.DualMode = true;
#endif
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
					DisconnectReason = new NodeDisconnectReason()
					{
						Reason = "Unexpected exception while connecting to socket",
						Exception = ex
					};
					throw;
				}
				InitDefaultBehaviors();
				_Connection.BeginListen();
			}
		}
		internal Node(Peer peer, Network network, VersionPayload myVersion, Socket socket, VersionPayload peerVersion)
		{
			Inbound = true;
			_Behaviors = new BehaviorsCollection(this);
			_MyVersion = myVersion;
			Network = network;
			_Peer = peer;
			_Connection = new NodeConnection(this, socket);
			_PeerVersion = peerVersion;
			Version = peerVersion.Version;
			LastSeen = peer.NetworkAddress.Time;
			TraceCorrelation.LogInside(() =>
			{
				NodeServerTrace.Information("Connected to advertised node " + _Peer.NetworkAddress.Endpoint);
				State = NodeState.Connected;
			});
			InitDefaultBehaviors();
			_Connection.BeginListen();
		}

		public bool Inbound
		{
			get;
			private set;
		}

		public event NodeEventMessageIncoming MessageReceived;
		protected void OnMessageReceived(IncomingMessage message)
		{
			var messageReceived = MessageReceived;
			if(messageReceived != null)
				messageReceived(this, message);
		}

		private void InitDefaultBehaviors()
		{
			_Behaviors.Add(new PingPongBehavior());
		}

		private readonly BehaviorsCollection _Behaviors;
		public BehaviorsCollection Behaviors
		{
			get
			{
				return _Behaviors;
			}
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
#if NOTRACESOURCE
		internal
#else
		public
#endif
 TraceCorrelation TraceCorrelation
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
			message.Magic = Network.Magic;
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

		public TPayload ReceiveMessage<TPayload>(TimeSpan timeout) where TPayload : Payload
		{
			var source = new CancellationTokenSource();
			source.CancelAfter(timeout);
			return ReceiveMessage<TPayload>(source.Token);
		}



		public TPayload ReceiveMessage<TPayload>(CancellationToken cancellationToken = default(CancellationToken)) where TPayload : Payload
		{
			using(var listener = new NodeListener(this))
			{
				return listener.ReceivePayload<TPayload>(cancellationToken);
			}
		}

		private readonly VersionPayload _MyVersion;
		public VersionPayload MyVersion
		{
			get
			{
				return _MyVersion;
			}
		}

		VersionPayload _PeerVersion;
		public VersionPayload PeerVersion
		{
			get
			{
				return _PeerVersion;
			}
		}

		public void VersionHandshake(CancellationToken cancellationToken = default(CancellationToken))
		{
			using(var listener = CreateListener()
									.Where(p => p.Message.Payload is VersionPayload ||
												p.Message.Payload is RejectPayload ||
												p.Message.Payload is VerAckPayload))
			{
				using(TraceCorrelation.Open())
				{
					SendMessage(MyVersion);

					var payload = listener.ReceivePayload<Payload>(cancellationToken);
					if(payload is RejectPayload)
					{
						throw new InvalidOperationException("Handshake rejected : " + ((RejectPayload)payload).Reason);
					}
					var version = (VersionPayload)payload;
					_PeerVersion = version;
					Version = version.Version;
					if(!version.AddressReceiver.Address.Equals(MyVersion.AddressFrom.Address))
					{
						NodeServerTrace.Warning("Different external address detected by the node " + version.AddressReceiver.Address + " instead of " + MyVersion.AddressFrom.Address);
					}
					if(version.Version < ProtocolVersion.MIN_PEER_PROTO_VERSION)
					{
						NodeServerTrace.Warning("Outdated version " + version.Version + " disconnecting");
						Disconnect();
						return;
					}
					SendMessage(new VerAckPayload());
					listener.ReceivePayload<VerAckPayload>(cancellationToken);
					State = NodeState.HandShaked;
				}
			}
		}




		public void RespondToHandShake(CancellationToken cancellation = default(CancellationToken))
		{
			using(TraceCorrelation.Open())
			{
				var listener = new PollMessageListener<IncomingMessage>();
				using(MessageProducer.AddMessageListener(listener))
				{
					NodeServerTrace.Information("Responding to handshake");
					SendMessage(MyVersion);
					listener.ReceiveMessage().AssertPayload<VerAckPayload>();
					SendMessage(new VerAckPayload());
					_State = NodeState.HandShaked;
				}
			}
		}

		public void Disconnect()
		{
			Disconnect(null, null);
		}
		public void Disconnect(string reason, Exception exception = null)
		{
			if(State == NodeState.Offline)
				return;
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
				if(DisconnectReason == null)
					DisconnectReason = new NodeDisconnectReason()
					{
						Reason = reason,
						Exception = exception
					};
			}
		}

		public NodeDisconnectReason DisconnectReason
		{
			get;
			private set;
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

		public ConcurrentChain GetChain(uint256 hashStop = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			ConcurrentChain chain = new ConcurrentChain(Network);
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
				using(var listener = this.CreateListener().OfType<HeadersPayload>())
				{
					while(true)
					{
						SendMessage(new GetHeadersPayload()
						{
							BlockLocators = currentTip.GetLocator(),
							HashStop = hashStop
						});
						var headers = listener.ReceivePayload<HeadersPayload>(cancellationToken);
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
		}

		public IEnumerable<ChainedBlock> SynchronizeChain(ChainBase chain, uint256 hashStop = null, CancellationToken cancellationToken = default(CancellationToken))
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
			var genesis = new ChainedBlock(Network.GetGenesis().Header, 0);
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

		public IEnumerable<Block> GetBlocks(IEnumerable<ChainedBlock> blocks, CancellationToken cancellationToken = default(CancellationToken))
		{
			return GetBlocks(blocks.Select(c => c.HashBlock), cancellationToken);
		}

		public IEnumerable<Block> GetBlocks(IEnumerable<uint256> neededBlocks, CancellationToken cancellationToken = default(CancellationToken))
		{
			AssertState(NodeState.HandShaked, cancellationToken);
			using(TraceCorrelation.Open())
			{
				NodeServerTrace.Information("Downloading blocks");
				int simultaneous = 70;
				using(var listener = CreateListener()
									.OfType<BlockPayload>())
				{
					foreach(var invs in neededBlocks
										.Select(b => new InventoryVector()
											{
												Type = InventoryType.MSG_BLOCK,
												Hash = b
											})
										.Partition(() => simultaneous))
					{

						var remaining = new Queue<uint256>(invs.Select(k => k.Hash));
						this.SendMessage(new GetDataPayload(invs.ToArray()));

						int maxQueued = 0;
						while(remaining.Count != 0)
						{
							var block = listener.ReceivePayload<BlockPayload>(cancellationToken).Object;
							maxQueued = Math.Max(listener.MessageQueue.Count, maxQueued);
							if(remaining.Peek() == block.GetHash())
							{
								remaining.Dequeue();
								yield return block;
							}
						}
						if(maxQueued < 10)
							simultaneous *= 2;
						else
							simultaneous /= 2;
						simultaneous = Math.Max(10, simultaneous);
						simultaneous = Math.Min(10000, simultaneous);
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

		public Network Network
		{
			get;
			set;
		}

		#region IDisposable Members

		public void Dispose()
		{
			Disconnect("Node disposed");
		}

		#endregion

		/// <summary>
		/// Emit a ping and wait the pong
		/// </summary>
		/// <param name="cancellation"></param>
		/// <returns>Latency</returns>
		public TimeSpan PingPong(CancellationToken cancellation = default(CancellationToken))
		{
			using(var listener = CreateListener().OfType<PongPayload>())
			{
				var ping = new PingPayload()
				{
					Nonce = RandomUtils.GetUInt64()
				};
				var before = DateTimeOffset.UtcNow;
				SendMessage(ping);

				while(listener.ReceivePayload<PongPayload>(cancellation).Nonce != ping.Nonce)
				{
				}
				var after = DateTimeOffset.UtcNow;
				return after - before;
			}
		}
	}
}
#endif