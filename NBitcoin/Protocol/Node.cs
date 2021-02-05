#if !NOSOCKET
using NBitcoin.Protocol.Behaviors;
using NBitcoin.Protocol.Filters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin.Logging;

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

	public class NodeRequirement
	{
		public uint? MinVersion
		{
			get;
			set;
		}

		public ProtocolCapabilities MinProtocolCapabilities
		{
			get; set;
		}

		public NodeServices RequiredServices
		{
			get;
			set;
		}

		public int? MinStartHeight
		{
			get;
			set;
		}

		public bool SupportSPV
		{
			get;
			set;
		}

		public virtual bool Check(VersionPayload version, ProtocolCapabilities capabilities)
		{
#pragma warning disable CS0618 // Type or member is obsolete
			if (!Check(version))
#pragma warning restore CS0618 // Type or member is obsolete
				return false;
			if (capabilities.PeerTooOld)
				return false;
			if (MinProtocolCapabilities is null)
				return true;

			if (SupportSPV)
			{
				if (capabilities.SupportNodeBloom && ((version.Services & NodeServices.NODE_BLOOM) == 0))
					return false;
			}

			if (MinStartHeight is { } minStartHeight)
			{
				if (version.StartHeight < minStartHeight)
					return false;
			}

			return capabilities.IsSupersetOf(MinProtocolCapabilities);
		}

		[Obsolete("Use Check(VersionPayload, ProtocolCapabilities capabilities) instead")]
		public virtual bool Check(VersionPayload version)
		{
			if (MinVersion != null)
			{
				if (version.Version < MinVersion.Value)
					return false;
			}
			if ((RequiredServices & version.Services) != RequiredServices)
			{
				return false;
			}
			return true;
		}
	}

	public class SynchronizeChainOptions
	{
		/// <summary>
		/// Location until which synchronization should be stopped (default: null)
		/// </summary>
		public uint256 HashStop
		{
			get; set;
		}
		/// <summary>
		/// Skip PoW check
		/// </summary>
		public bool SkipPoWCheck
		{
			get; set;
		}
		/// <summary>
		/// Strip headers from the retrieved chain
		/// </summary>
		public bool StripHeaders
		{
			get; set;
		}
	}

	public delegate void NodeEventHandler(Node node);
	public delegate void NodeEventMessageIncoming(Node node, IncomingMessage message);
	public delegate void NodeStateEventHandler(Node node, NodeState oldState);
	public class Node : IDisposable
	{
		internal class SentMessage
		{
			public Payload Payload;
			public TaskCompletionSource<bool> Completion;
			public Guid ActivityId;
		}
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
			readonly Socket _Socket;
			public Socket Socket
			{
				get
				{
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

			public NodeConnection(Node node, Socket socket)
			{
				_Node = node;
				_Socket = socket;
				_Disconnected = new ManualResetEvent(false);
				_Cancel = new CancellationTokenSource();
			}

			internal bool IsVerbose => Logs.NodeServer.IsEnabled(LogLevel.Trace);


			internal BlockingCollection<SentMessage> Messages = new BlockingCollection<SentMessage>(new ConcurrentQueue<SentMessage>());
			public void BeginListen()
			{
				new Thread(() =>
				{
					SentMessage processing = null;
					Exception unhandledException = null;

					ManualResetEvent ar = new ManualResetEvent(false);
					SocketAsyncEventArgs evt = new SocketAsyncEventArgs();
					evt.SocketFlags = SocketFlags.None;
					evt.Completed += (a, b) =>
					{
						Utils.SafeSet(ar);
					};
					try

					{
						foreach (var kv in Messages.GetConsumingEnumerable(Cancel.Token))
						{
							processing = kv;
							var payload = kv.Payload;
							var message = new Message();
							message.Magic = _Node.Network.Magic;
							message.Payload = payload;
							if (payload is GetHeadersPayload getHeaders)
							{
								getHeaders.Version = Node.Version;
							}
							if (payload is GetBlocksPayload getBlocks)
							{
								getBlocks.Version = Node.Version;
							}
							if (IsVerbose)
							{
								Logs.NodeServer.LogTrace("Sending message {message}", message);
							}
							var addrV2Support = Node.PreferAddressV2
									? NetworkAddress.AddrV2Format
									: 0;
							MemoryStream ms = new MemoryStream();
							message.ReadWrite(new BitcoinStream(ms, true)
							{
								ProtocolVersion = Node.Version | addrV2Support,
								TransactionOptions = Node.SupportedTransactionOptions,
								ConsensusFactory = Node.Network.Consensus.ConsensusFactory
							});
							var bytes = ms.ToArrayEfficient();
							evt.SetBuffer(bytes, 0, bytes.Length);
							_Node.Counter.AddWritten(bytes.Length);
							ar.Reset();
							if (!Socket.SendAsync(evt))
								Utils.SafeSet(ar);
							WaitHandle.WaitAny(new WaitHandle[] { ar, Cancel.Token.WaitHandle }, -1);
							if (!Cancel.Token.IsCancellationRequested)
							{
								if (evt.SocketError != SocketError.Success)
									throw new SocketException((int)evt.SocketError);
								processing.Completion.SetResult(true);
								processing = null;
							}
						}
					}
					catch (OperationCanceledException)
					{
					}
					catch (Exception ex)
					{
						unhandledException = ex;
					}
					finally
					{
						evt?.Dispose();
						ar?.Dispose();
					}

					if (processing != null)
						Messages.Add(processing);

					foreach (var pending in Messages)
					{
						if (IsVerbose)
						{
							Logs.NodeServer.LogTrace("The connection cancelled before the message was sent");
						}
						pending.Completion.SetException(new OperationCanceledException("The peer has been disconnected"));
					}
					Messages = new BlockingCollection<SentMessage>(new ConcurrentQueue<SentMessage>());
					Logs.NodeServer.LogInformation("Stop sending");
					Cleanup(unhandledException);
				}).Start();
				new Thread(() =>
				{
					_ListenerThreadId = Thread.CurrentThread.ManagedThreadId;


					using (Logs.NodeServer.BeginScope("Thread scope {ThreadId}", _ListenerThreadId))
					{
						Logs.NodeServer.LogInformation("Start Listening");

						Exception unhandledException = null;
						try
						{
							var stream = new NetworkStream(Socket, false);
							while (!Cancel.Token.IsCancellationRequested)
							{
								PerformanceCounter counter;

								var message = Message.ReadNext(stream, Node.Network, Node.Version, Cancel.Token, out counter);

								if (IsVerbose)
									Logs.NodeServer.LogTrace("Receiving message: {command} ({payload})", message.Command, message.Payload);

								Node.LastSeen = DateTimeOffset.UtcNow;
								Node.Counter.Add(counter);
								Node.OnMessageReceived(new IncomingMessage()
								{
									Message = message,
									Socket = Socket,
									Length = counter.ReadenBytes,
									Node = Node
								});
							}
						}
						catch (OperationCanceledException)
						{
						}
						catch (Exception ex)
						{
							unhandledException = ex;
						}
						Logs.NodeServer.LogInformation("Stop listening");
						Cleanup(unhandledException);
					}
				}).Start();
			}

			int _CleaningUp;
			public int _ListenerThreadId;
			private void Cleanup(Exception unhandledException)
			{
				if (Interlocked.CompareExchange(ref _CleaningUp, 1, 0) == 1)
					return;
				if (!Cancel.IsCancellationRequested)
				{
					Logs.NodeServer.LogError(default, unhandledException, "Connection to server stopped unexpectedly");
					Node.DisconnectReason = new NodeDisconnectReason()
					{
						Reason = "Unexpected exception while connecting to socket",
						Exception = unhandledException
					};
					Node.State = NodeState.Failed;
				}

				if (Node.State != NodeState.Failed)
					Node.State = NodeState.Offline;

				_Cancel.Cancel();
				Utils.SafeCloseSocket(Socket);
				_Disconnected.Set(); //Set before behavior detach to prevent deadlock
				foreach (var behavior in _Node.Behaviors)
				{
					try
					{
						behavior.Detach();
					}
					catch (Exception ex)
					{
						Logs.NodeServer.LogError(default, ex, "Error while detaching behavior {behaviour}", behavior.GetType().FullName);
					}
				}
			}

		}

		public DateTimeOffset ConnectedAt
		{
			get;
			private set;
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
				Logs.NodeServer.LogInformation("State changed from {stateFrom} to {stateTo}", _State, value);

				var previous = _State;
				_State = value;
				if (previous != _State)
				{
					OnStateChanged(previous);
					if (value == NodeState.Failed || value == NodeState.Offline)
					{
						Logs.NodeServer.LogInformation("Communication closed");

						OnDisconnected();
					}
				}
			}
		}

		public event NodeStateEventHandler StateChanged;
		private void OnStateChanged(NodeState previous)
		{
			var stateChanged = StateChanged;
			if (stateChanged != null)
			{
				foreach (var handler in stateChanged.GetInvocationList().Cast<NodeStateEventHandler>())
				{
					try
					{
						handler.DynamicInvoke(this, previous);
					}
					catch (TargetInvocationException ex)
					{
						Logs.NodeServer.LogError(default, ex.InnerException, "Error while StateChanged event raised");
					}
				}
			}
		}

		private readonly NodeFiltersCollection _Filters = new NodeFiltersCollection();
		public NodeFiltersCollection Filters
		{
			get
			{
				return _Filters;
			}
		}

		public event NodeEventMessageIncoming MessageReceived;
		protected void OnMessageReceived(IncomingMessage message)
		{
			var version = message.Message.Payload as VersionPayload;
			if (version != null && State == NodeState.HandShaked)
			{
				if (message.Node.ProtocolCapabilities.SupportReject)
					message.Node.SendMessageAsync(new RejectPayload()
					{
						Code = RejectCode.DUPLICATE
					});
			}
			if (version != null)
			{
				TimeOffset = DateTimeOffset.Now - version.Timestamp;
				if ((version.Services & NodeServices.NODE_WITNESS) != 0)
					_SupportedTransactionOptions |= TransactionOptions.Witness;
			}
			var havewitness = message.Message.Payload as HaveWitnessPayload;
			if (havewitness != null)
				_SupportedTransactionOptions |= TransactionOptions.Witness;

			var last = new ActionFilter((m, n) =>
			{
				MessageProducer.PushMessage(m);
				var messageReceived = MessageReceived;
				if (messageReceived != null)
				{
					foreach (var handler in messageReceived.GetInvocationList().Cast<NodeEventMessageIncoming>())
					{
						try
						{
							handler.DynamicInvoke(this, m);
						}
						catch (TargetInvocationException ex)
						{
							Logs.NodeServer.LogError(default, ex.InnerException, "Error while OnMessageReceived event raised");
							UncaughtException?.Invoke(this, ex.InnerException);
						}
					}
				}
			});

			var enumerator = Filters.Concat(new[] { last }).GetEnumerator();
			FireFilters(enumerator, message);
		}

		private void OnSendingMessage(Payload payload, Action final)
		{
			var enumerator = Filters.Concat(new[] { new ActionFilter(null, (n, p, a) => final()) }).GetEnumerator();
			FireFilters(enumerator, payload);
		}

		private void FireFilters(IEnumerator<INodeFilter> enumerator, Payload payload)
		{
			if (enumerator.MoveNext())
			{
				var filter = enumerator.Current;
				try
				{
					filter.OnSendingMessage(this, payload, () => FireFilters(enumerator, payload));
				}
				catch (Exception ex)
				{
					Logs.NodeServer.LogError(default, ex.InnerException, "Unhandled exception raised by a node filter (OnSendingMessage)");

					UncaughtException?.Invoke(this, ex);
				}
			}
		}

		public delegate void NodeExceptionDelegate(Node sender, Exception ex);
		public event NodeExceptionDelegate UncaughtException;

		private void FireFilters(IEnumerator<INodeFilter> enumerator, IncomingMessage message)
		{
			if (enumerator.MoveNext())
			{
				var filter = enumerator.Current;
				try
				{
					filter.OnReceivingMessage(message, () => FireFilters(enumerator, message));
				}
				catch (Exception ex)
				{
					Logs.NodeServer.LogError(default, ex.InnerException, "Unhandled exception raised by a node filter (OnReceivingMessage)");

					UncaughtException?.Invoke(this, ex);
				}
			}
		}

		public event NodeEventHandler Disconnected;
		private void OnDisconnected()
		{
			var disconnected = Disconnected;
			if (disconnected != null)
			{
				foreach (var handler in disconnected.GetInvocationList().Cast<NodeEventHandler>())
				{
					try
					{
						handler.DynamicInvoke(this);
					}
					catch (TargetInvocationException ex)
					{
						Logs.NodeServer.LogError(default, ex.InnerException, "Error while Disconnected event raised");
					}
				}
			}
		}


		internal readonly NodeConnection _Connection;



		/// <summary>
		/// Connect to a random node on the network
		/// </summary>
		/// <param name="network">The network to connect to</param>
		/// <param name="addrman">The addrman used for finding peers</param>
		/// <param name="parameters">The parameters used by the found node</param>
		/// <param name="connectedEndpoints">The already connected endpoints, the new endpoint will be select outside of existing groups</param>
		/// <returns></returns>
		public static Node Connect(Network network, AddressManager addrman, NodeConnectionParameters parameters = null, EndPoint[] connectedEndpoints = null)
		{
			parameters = parameters ?? new NodeConnectionParameters();
			AddressManagerBehavior.SetAddrman(parameters, addrman);
			return Connect(network, parameters, connectedEndpoints);
		}

		/// <summary>
		/// Connect to a random node on the network
		/// </summary>
		/// <param name="network">The network to connect to</param>
		/// <param name="parameters">The parameters used by the found node, use AddressManagerBehavior.GetAddrman for finding peers</param>
		/// <param name="connectedEndpoints">The already connected endpoints, the new endpoint will be select outside of existing groups</param>
		/// <param name="getGroup">Group selector, by default NBicoin.IpExtensions.GetGroup</param>
		/// <returns></returns>
		public static Node Connect(Network network, NodeConnectionParameters parameters = null, EndPoint[] connectedEndpoints = null, Func<EndPoint, byte[]> getGroup = null)
		{
			getGroup = getGroup ?? new Func<EndPoint, byte[]>((a) => a.GetGroup());
			connectedEndpoints = connectedEndpoints ?? new IPEndPoint[0];
			parameters = parameters ?? new NodeConnectionParameters();
			var addrmanBehavior = parameters.TemplateBehaviors.FindOrCreate(() => new AddressManagerBehavior(new AddressManager()));
			var addrman = AddressManagerBehavior.GetAddrman(parameters);
			DateTimeOffset start = DateTimeOffset.UtcNow;
			while (true)
			{
				parameters.ConnectCancellation.ThrowIfCancellationRequested();
				if (addrman.Count == 0 || DateTimeOffset.UtcNow - start > TimeSpan.FromSeconds(60))
				{
					addrmanBehavior.DiscoverPeers(network, parameters);
					start = DateTimeOffset.UtcNow;
				}
				NetworkAddress addr = null;
				int groupFail = 0;
				while (true)
				{
					if (groupFail > 50)
					{
						parameters.ConnectCancellation.WaitHandle.WaitOne((int)TimeSpan.FromSeconds(60).TotalMilliseconds);
						break;
					}
					addr = addrman.Select();
					if (addr is null)
					{
						parameters.ConnectCancellation.WaitHandle.WaitOne(1000);
						break;
					}
					if (!addr.Endpoint.IsValid())
						continue;
					var groupExist = connectedEndpoints.Any(a => getGroup(a).SequenceEqual(getGroup(addr.Endpoint)));
					if (groupExist)
					{
						groupFail++;
						continue;
					}
					break;
				}
				if (addr is null)
					continue;
				try
				{
					var timeout = new CancellationTokenSource(5000);
					var param2 = parameters.Clone();
					using (var cts = CancellationTokenSource.CreateLinkedTokenSource(parameters.ConnectCancellation, timeout.Token))
					{
						param2.ConnectCancellation = cts.Token;
						var node = Node.Connect(network, addr.Endpoint, param2);
						return node;
					}
				}
				catch (OperationCanceledException ex)
				{
					if (ex.CancellationToken == parameters.ConnectCancellation)
						throw;
				}
				catch (SocketException)
				{
					parameters.ConnectCancellation.WaitHandle.WaitOne(500);
				}
			}
		}

		/// <summary>
		/// Connect to the node of this machine
		/// </summary>
		/// <param name="network"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static Node ConnectToLocal(Network network,
								NodeConnectionParameters parameters)
		{
			return Connect(network, Utils.ParseEndpoint("localhost", network.DefaultPort), parameters);
		}

		public static Node ConnectToLocal(Network network,
								uint? myVersion = null,
								bool isRelay = true,
								CancellationToken cancellation = default(CancellationToken))
		{
			return ConnectToLocal(network, new NodeConnectionParameters()
			{
				ConnectCancellation = cancellation,
				IsRelay = isRelay,
				Version = myVersion
			});
		}

		public static Node Connect(Network network,
								 string endpoint, NodeConnectionParameters parameters)
		{
			return Connect(network, Utils.ParseEndpoint(endpoint, network.DefaultPort), parameters);
		}

		public static Node Connect(Network network,
								 string endpoint,
								 uint? myVersion = null,
								bool isRelay = true,
								CancellationToken cancellation = default(CancellationToken))
		{
			return Connect(network, Utils.ParseEndpoint(endpoint, network.DefaultPort), myVersion, isRelay, cancellation);
		}

		public static Node Connect(Network network,
							 NetworkAddress endpoint,
							 NodeConnectionParameters parameters)
		{
			return ConnectAsync(network, endpoint?.Endpoint, endpoint, parameters).GetAwaiter().GetResult();
		}

		public static Node Connect(Network network,
							 EndPoint endpoint,
							 NodeConnectionParameters parameters)
		{
			return ConnectAsync(network, endpoint, parameters).GetAwaiter().GetResult();
		}

		public static Task<Node> ConnectAsync(Network network, EndPoint endpoint, NodeConnectionParameters parameters = null)
		{
			return ConnectAsync(network, endpoint, null, parameters);
		}
		public static Task<Node> ConnectAsync(Network network, string endpoint, NodeConnectionParameters parameters = null)
		{
			if (network is null)
				throw new ArgumentNullException(nameof(network));
			return ConnectAsync(network, Utils.ParseEndpoint(endpoint, network.DefaultPort), null, parameters);
		}

		public static async Task<Node> ConnectAsync(Network network, EndPoint endpoint, NetworkAddress peer, NodeConnectionParameters parameters)
		{
			if (endpoint is null && peer is null)
				throw new ArgumentNullException(nameof(endpoint));
			if (network is null)
				throw new ArgumentNullException(nameof(network));
			if (endpoint is null)
			{
				endpoint = peer.Endpoint;
			}

			if (endpoint is null)
				throw new ArgumentNullException(nameof(endpoint));

			parameters = parameters ?? new NodeConnectionParameters();
			var addrman = AddressManagerBehavior.GetAddrman(parameters);

			var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			parameters.SocketSettings.SetSocketProperties(socket);
			try
			{
				await parameters.EndpointConnector.ConnectSocket(socket, endpoint, parameters, parameters.ConnectCancellation).ConfigureAwait(false);
				var expectedPeerEndpoint = (endpoint as IPEndPoint) ?? endpoint.AsOnionCatIPEndpoint() ?? (socket.RemoteEndPoint as IPEndPoint);
				if (peer is null)
				{
					peer = new NetworkAddress(expectedPeerEndpoint)
					{
						Time = DateTimeOffset.UtcNow,
					};
				}
				else if (!expectedPeerEndpoint.MapToIPv6Ex().Equals(peer.Endpoint))
				{
					throw new ArgumentException("The peer's endpoint that you provided is different from the endpoint eventually connected to");
				}
			}
			catch (OperationCanceledException)
			{
				Utils.SafeCloseSocket(socket);
				Logs.NodeServer.LogInformation("Connection to node cancelled");
				if (addrman != null && peer != null && peer.Endpoint != null)
					addrman.Attempt(peer);
				throw;
			}
			catch (Exception ex)
			{
				Utils.SafeCloseSocket(socket);
				Logs.NodeServer.LogError(default, ex, "Error connecting to the remote endpoint");
				if (addrman != null && peer != null && peer.Endpoint != null)
					addrman.Attempt(peer);
				throw;
			}

			Node node = new Node(peer, network, parameters, socket, null);
			return node;
		}

		public static Node Connect(Network network,
								 EndPoint endpoint,
								 uint? myVersion = null,
								bool isRelay = true,
								CancellationToken cancellation = default(CancellationToken))
		{
			return Connect(network, endpoint, new NodeConnectionParameters()
			{
				ConnectCancellation = cancellation,
				IsRelay = isRelay,
				Version = myVersion,
				Services = NodeServices.Nothing,
			});
		}

		private void SetVersion(uint version)
		{
			Version = version;
			ProtocolCapabilities = Network.Consensus.ConsensusFactory.GetProtocolCapabilities(version);
		}

		internal Node(NetworkAddress peer, Network network, NodeConnectionParameters parameters, Socket socket, VersionPayload peerVersion)
		{
			_RemoteSocketEndpoint = peer.Endpoint;
			if (peer.Endpoint is IPEndPoint ip)
			{
				_RemoteSocketAddress = ip.Address;
				_RemoteSocketPort = ip.Port;
			}
			_Peer = peer;
			Inbound = peerVersion != null;
			Network = network;
			_Behaviors = new NodeBehaviorsCollection(this);
			_MyVersion = parameters.CreateVersion(peer.Endpoint, network);
			if (peerVersion is null)
				SetVersion((uint)_MyVersion.Version);
			_Connection = new NodeConnection(this, socket);
			_PeerVersion = peerVersion;
			if (peerVersion != null)
			{
				SetVersion(Math.Min(_MyVersion.Version, _PeerVersion.Version));
			}

			LastSeen = peer.Time;
			ConnectedAt = DateTimeOffset.UtcNow;
			Logs.NodeServer.LogInformation("Connected to node {endpoint} (inbound: {inbound})", _Peer.Endpoint, Inbound);
			State = NodeState.Connected;

			InitDefaultBehaviors(parameters);
			_Connection.BeginListen();
		}

		IPAddress _RemoteSocketAddress;
		public IPAddress RemoteSocketAddress
		{
			get
			{
				return _RemoteSocketAddress;
			}
		}

		EndPoint _RemoteSocketEndpoint;
		public EndPoint RemoteSocketEndpoint
		{
			get
			{
				return _RemoteSocketEndpoint;
			}
		}

		int _RemoteSocketPort;
		public int RemoteSocketPort
		{
			get
			{
				return _RemoteSocketPort;
			}
		}

		public bool Inbound
		{
			get;
			private set;
		}

		private void InitDefaultBehaviors(NodeConnectionParameters parameters)
		{
			Advertize = parameters.Advertize;
			PreferredTransactionOptions = parameters.PreferredTransactionOptions;
			_Behaviors.DelayAttach = true;
			foreach (var behavior in parameters.TemplateBehaviors)
			{
				_Behaviors.Add(behavior.Clone());
			}
			_Behaviors.DelayAttach = false;
		}

		private readonly NodeBehaviorsCollection _Behaviors;
		public NodeBehaviorsCollection Behaviors
		{
			get
			{
				return _Behaviors;
			}
		}

		private readonly NetworkAddress _Peer;
		public NetworkAddress Peer
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

		public TimeSpan? TimeOffset
		{
			get;
			private set;
		}

		/// <summary>
		/// Send a message to the peer asynchronously
		/// </summary>
		/// <param name="payload">The payload to send</param>
		/// <param name="System.OperationCanceledException">The node has been disconnected</param>
		public Task SendMessageAsync(Payload payload)
		{
			if (payload is null)
				throw new ArgumentNullException(nameof(payload));

#if NO_RCA
			TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
#else
			TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
			if (!IsConnected)
			{
				completion.SetException(new OperationCanceledException("The peer has been disconnected"));
				return completion.Task;
			}

			var activity = Guid.NewGuid();

			Action final = () =>
			{
				_Connection.Messages.Add(new SentMessage()
				{
					Payload = payload,
					ActivityId = activity,
					Completion = completion
				});
			};
			OnSendingMessage(payload, final);
			return completion.Task;
		}

		/// <summary>
		/// Send a message to the peer synchronously
		/// </summary>
		/// <param name="payload">The payload to send</param>
		/// <exception cref="System.ArgumentNullException">Payload is null</exception>
		/// <param name="System.OperationCanceledException">The node has been disconnected, or the cancellation token has been set to canceled</param>
		public void SendMessage(Payload payload, CancellationToken cancellation = default(CancellationToken))
		{
			try
			{
				SendMessageAsync(payload).Wait(cancellation);
			}
			catch (AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
				throw;
			}
		}

		private PerformanceCounter _Counter;
		public PerformanceCounter Counter
		{
			get
			{
				if (_Counter is null)
					_Counter = new PerformanceCounter();
				return _Counter;
			}
		}

		/// <summary>
		/// The negociated protocol version (minimum of supported version between MyVersion and the PeerVersion)
		/// </summary>
		public uint Version
		{
			get;
			internal set;
		}

		public ProtocolCapabilities ProtocolCapabilities
		{
			get;
			internal set;
		}

		public bool IsConnected
		{
			get
			{
				return State == NodeState.Connected || State == NodeState.HandShaked;
			}
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
			using (var listener = new NodeListener(this))
			{
				return listener.ReceivePayload<TPayload>(cancellationToken);
			}
		}

		/// <summary>
		/// Send addr unsolicited message of the AddressFrom peer when passing to Handshaked state
		/// </summary>
		public bool Advertize
		{
			get;
			set;
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
			VersionHandshake(null, cancellationToken);
		}
		public void VersionHandshake(NodeRequirement requirements, CancellationToken cancellationToken = default(CancellationToken))
		{
			requirements = requirements ?? new NodeRequirement();
			using (var listener = CreateListener()
									.Where(p => p.Message.Payload is VersionPayload ||
												p.Message.Payload is RejectPayload ||
												p.Message.Payload is VerAckPayload))
			{

				SendMessageAsync(MyVersion);
				var payload = listener.ReceivePayload<Payload>(cancellationToken);
				if (payload is RejectPayload)
				{
					throw new ProtocolException("Handshake rejected : " + ((RejectPayload)payload).Reason);
				}
				var version = (VersionPayload)payload;
				_PeerVersion = version;
				SetVersion(Math.Min(MyVersion.Version, version.Version));

				var receiverAddress = version.AddressReceiver.GetStringAddress();
				var addressFrom = MyVersion.AddressFrom.GetStringAddress();
				if (receiverAddress != addressFrom)
					Logs.NodeServer.LogWarning($"Different external address detected by the node {receiverAddress} instead of {addressFrom}");

				if (ProtocolCapabilities.PeerTooOld)
				{
					Logs.NodeServer.LogWarning("Outdated version {version} disconnecting", version.Version);
					Disconnect("Outdated version");
					return;
				}

				if (!requirements.Check(version, ProtocolCapabilities))
				{
					Disconnect("The peer does not support the required services requirement");
					return;
				}

				// As a cortesy we do not send sendaddr to nodes that do not support it.
				if (ProtocolCapabilities.SupportAddrv2)
				{
					// Signal ADDRv2 support (BIP155).
					SendMessageAsync(new SendAddrV2Payload());
				}

				SendMessageAsync(new VerAckPayload());

				listener.ReceivePayload<VerAckPayload>(cancellationToken);

				State = NodeState.HandShaked;

				if (Advertize)
				{
					if (MyVersion.AddressFrom is IPEndPoint iPEndPoint && !iPEndPoint.Address.IsRoutable(true))
						return;

					SendMessageAsync(new AddrPayload(new NetworkAddress(MyVersion.AddressFrom)
					{
						Time = DateTimeOffset.UtcNow
					}));
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="cancellation"></param>
		public void RespondToHandShake(CancellationToken cancellation = default(CancellationToken))
		{
			using (var list = CreateListener().Where(m => m.Message.Payload is VerAckPayload || m.Message.Payload is RejectPayload))
			{
				Logs.NodeServer.LogInformation("Responding to handshake");

				SendMessageAsync(MyVersion);
				var message = list.ReceiveMessage(cancellation);
				var reject = message.Message.Payload as RejectPayload;
				if (reject != null)
					throw new ProtocolException("Version rejected " + reject.Code + " : " + reject.Reason);
				SendMessageAsync(new VerAckPayload());
				State = NodeState.HandShaked;

				// As a courtesy we do not send sendaddr to nodes that do not support it.
				if (ProtocolCapabilities.SupportAddrv2)
				{
					// Signal ADDRv2 support (BIP155).
					SendMessageAsync(new SendAddrV2Payload());
				}
			}
		}

		public void Disconnect()
		{
			Disconnect(null, null);
		}

		int _Disconnecting;

		public void Disconnect(string reason, Exception exception = null)
		{
			DisconnectAsync(reason, exception);
			AssertNoListeningThread();
			_Connection.Disconnected.WaitOne();
		}

		private void AssertNoListeningThread()
		{
			if (_Connection._ListenerThreadId == Thread.CurrentThread.ManagedThreadId)
				throw new InvalidOperationException("Using Disconnect on this thread would result in a deadlock, use DisconnectAsync instead");
		}
		public void DisconnectAsync()
		{
			DisconnectAsync(null, null);
		}
		public void DisconnectAsync(string reason, Exception exception = null)
		{
			if (!IsConnected)
				return;
			if (Interlocked.CompareExchange(ref _Disconnecting, 1, 0) == 1)
				return;

			Logs.NodeServer.LogInformation("Disconnection request {reason}", reason);

			State = NodeState.Disconnecting;
			_Connection.Cancel.Cancel();
			if (DisconnectReason is null)
				DisconnectReason = new NodeDisconnectReason()
				{
					Reason = reason,
					Exception = exception
				};
		}

		TransactionOptions _PreferredTransactionOptions = TransactionOptions.All;

		/// <summary>
		/// Transaction options we would like
		/// </summary>
		public TransactionOptions PreferredTransactionOptions
		{
			get
			{
				return _PreferredTransactionOptions;
			}
			set
			{
				_PreferredTransactionOptions = value;
			}
		}

		TransactionOptions _SupportedTransactionOptions = TransactionOptions.None;
		/// <summary>
		/// Transaction options supported by the peer
		/// </summary>
		public TransactionOptions SupportedTransactionOptions
		{
			get
			{
				return _SupportedTransactionOptions;
			}
		}


		/// <summary>
		/// Transaction options supported by the peer
		/// </summary>
		public bool PreferAddressV2
		{
			get;
			internal set;
		}

		/// <summary>
		/// Transaction options we prefer and which is also supported by peer
		/// </summary>
		public TransactionOptions ActualTransactionOptions
		{
			get
			{
				return PreferredTransactionOptions & SupportedTransactionOptions;
			}
		}

		public NodeDisconnectReason DisconnectReason
		{
			get;
			private set;
		}

		public override string ToString()
		{
			return String.Format("{0} ({1})", State, Peer.Endpoint);
		}

		private Socket Socket
		{
			get
			{
				return _Connection.Socket;
			}
		}

		internal TimeSpan PollHeaderDelay = TimeSpan.FromMinutes(1.0);


		/// <summary>
		/// Get the chain of headers from the peer (thread safe)
		/// </summary>
		/// <param name="options">The synchronization chain options</param>
		/// <param name="cancellationToken"></param>
		/// <returns>The chain of headers</returns>
		public ConcurrentChain GetChain(SynchronizeChainOptions options, CancellationToken cancellationToken = default(CancellationToken))
		{
			ConcurrentChain chain = new ConcurrentChain(Network);
			SynchronizeChain(chain, options, cancellationToken);
			return chain;
		}

		/// <summary>
		/// Get the chain of block hashes from the peer (thread safe)
		/// </summary>
		/// <param name="hashStop">Location until which synchronization should be stopped (default: null)</param>
		/// <param name="cancellationToken"></param>
		/// <returns>The chain of headers</returns>
		public SlimChain GetSlimChain(uint256 hashStop = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			SlimChain chain = new SlimChain(Network.GenesisHash);
			SynchronizeSlimChain(chain, hashStop, cancellationToken);
			return chain;
		}


		/// <summary>
		/// Get the chain of headers from the peer (thread safe)
		/// </summary>
		/// <param name="hashStop">The highest block wanted</param>
		/// <param name="cancellationToken"></param>
		/// <returns>The chain of headers</returns>
		public ConcurrentChain GetChain(uint256 hashStop = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return GetChain(new SynchronizeChainOptions() { HashStop = hashStop }, cancellationToken);
		}

		public IEnumerable<ChainedBlock> GetHeadersFromFork(ChainedBlock currentTip,
														SynchronizeChainOptions options,
														CancellationToken cancellationToken = default(CancellationToken))
		{
			AssertState(NodeState.HandShaked, cancellationToken);
			options = options ?? new SynchronizeChainOptions();

			Logs.NodeServer.LogInformation("Building chain");

			using (var listener = this.CreateListener().OfType<HeadersPayload>())
			{
				int acceptMaxReorgDepth = 0;
				while (true)
				{
					//Get before last so, at the end, we should only receive 1 header equals to this one (so we will not have race problems with concurrent GetChains)
					var awaited = currentTip.Previous is null ? currentTip.GetLocator() : currentTip.Previous.GetLocator();
					SendMessageAsync(new GetHeadersPayload()
					{
						BlockLocators = awaited,
						HashStop = options.HashStop
					});

					while (true)
					{
						bool isOurs = false;
						HeadersPayload headers = null;

						using (var headersCancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
						{
							headersCancel.CancelAfter(PollHeaderDelay);
							try
							{
								headers = listener.ReceivePayload<HeadersPayload>(headersCancel.Token);
							}
							catch (OperationCanceledException)
							{
								acceptMaxReorgDepth += 6;
								if (cancellationToken.IsCancellationRequested)
									throw;
								break; //Send a new GetHeaders
							}
						}
						if (headers.Headers.Count == 0 && PeerVersion.StartHeight == 0 && currentTip.HashBlock == Network.GenesisHash) //In the special case where the remote node is at height 0 as well as us, then the headers count will be 0
							yield break;
						if (headers.Headers.Count == 1 && headers.Headers[0].GetHash() == currentTip.HashBlock)
							yield break;
						foreach (var header in headers.Headers)
						{
							var h = header.GetHash();
							if (h == currentTip.HashBlock)
								continue;

							//The previous headers request timeout, this can arrive in case of big reorg
							if (header.HashPrevBlock != currentTip.HashBlock)
							{
								int reorgDepth = 0;
								var tempCurrentTip = currentTip;
								while (reorgDepth != acceptMaxReorgDepth && tempCurrentTip != null && header.HashPrevBlock != tempCurrentTip.HashBlock)
								{
									reorgDepth++;
									tempCurrentTip = tempCurrentTip.Previous;
								}
								if (reorgDepth != acceptMaxReorgDepth && tempCurrentTip != null)
									currentTip = tempCurrentTip;
							}

							if (header.HashPrevBlock == currentTip.HashBlock)
							{
								isOurs = true;
								currentTip = new ChainedBlock(header, h, currentTip);
								if (options.StripHeaders)
									currentTip.StripHeader();
								if (!options.SkipPoWCheck)
								{
									if (!currentTip.Validate(Network))
										throw new ProtocolException("An header which does not pass proof of work verification has been received");
								}
								yield return currentTip;
								if (currentTip.HashBlock == options.HashStop)
									yield break;
							}
							else
								break; //Not our headers, continue receive
						}
						if (isOurs)
							break;  //Go ask for next header
					}
				}
			}
		}

		public IEnumerable<ChainedBlock> GetHeadersFromFork(ChainedBlock currentTip,
														uint256 hashStop = null,
														CancellationToken cancellationToken = default(CancellationToken))
		{
			return GetHeadersFromFork(currentTip, new SynchronizeChainOptions() { HashStop = hashStop }, cancellationToken);
		}

		/// <summary>
		/// Synchronize a given Chain to the tip of this node if its height is higher. (Thread safe)
		/// </summary>
		/// <param name="chain">The chain to synchronize</param>
		/// <param name="options">The synchronisation options</param>
		/// <param name="cancellationToken"></param>
		/// <returns>The chain of block retrieved</returns>
		public IEnumerable<ChainedBlock> SynchronizeChain(ChainBase chain, SynchronizeChainOptions options, CancellationToken cancellationToken = default(CancellationToken))
		{
			options = options ?? new SynchronizeChainOptions();
			var oldTip = chain.Tip;
			var headers = GetHeadersFromFork(oldTip, options, cancellationToken).ToList();
			if (headers.Count == 0)
				return new ChainedBlock[0];
			var newTip = headers[headers.Count - 1];
			if (newTip.Height <= oldTip.Height)
				throw new ProtocolException("No tip should have been received older than the local one");
			chain.SetTip(newTip);
			return headers;
		}

		/// <summary>
		/// Synchronize a given Chain to the tip of this node if its height is higher. (Thread safe)
		/// </summary>
		/// <param name="chain">The chain to synchronize</param>
		/// <param name="hashStop">The location until which it synchronize</param>
		/// <param name="cancellationToken"></param>
		/// <returns>The chain of block retrieved</returns>
		public IEnumerable<ChainedBlock> SynchronizeChain(ChainBase chain, uint256 hashStop = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return SynchronizeChain(chain, new SynchronizeChainOptions() { HashStop = hashStop }, cancellationToken);
		}

		/// <summary>
		/// Synchronize a given SlimChain to the tip of this node if its height is higher.
		/// </summary>
		/// <param name="chain">The chain to synchronize</param>
		/// <param name="hashStop">The location until which it synchronize</param>
		/// <param name="cancellationToken"></param>
		/// <returns>Task which finish when complete</returns>
		public void SynchronizeSlimChain(SlimChain chain, uint256 hashStop = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (chain is null)
				throw new ArgumentNullException(nameof(chain));
			AssertState(NodeState.HandShaked, cancellationToken);

			Logs.NodeServer.LogInformation("Building chain");

			using (var listener = this.CreateListener().OfType<HeadersPayload>())
			{
				while (true)
				{
					var currentTip = chain.TipBlock;

					//Get before last so, at the end, we should only receive 1 header equals to this one (so we will not have race problems with concurrent GetChains)
					var awaited = currentTip.Previous is null ? chain.GetLocator(currentTip.Height) : chain.GetLocator(currentTip.Height - 1);
					if (awaited is null)
						continue;
					SendMessageAsync(new GetHeadersPayload()
					{
						BlockLocators = awaited,
						HashStop = hashStop
					});

					while (true)
					{
						bool isOurs = false;
						HeadersPayload headers = null;

						using (var headersCancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
						{
							headersCancel.CancelAfter(PollHeaderDelay);
							try
							{
								headers = listener.ReceivePayload<HeadersPayload>(headersCancel.Token);
							}
							catch (OperationCanceledException)
							{
								if (cancellationToken.IsCancellationRequested)
									throw;
								break; //Send a new GetHeaders
							}
						}
						if (headers.Headers.Count == 0 && PeerVersion.StartHeight == 0 && currentTip.Hash == Network.GenesisHash) //In the special case where the remote node is at height 0 as well as us, then the headers count will be 0
							return;
						if (headers.Headers.Count == 1 && headers.Headers[0].GetHash() == currentTip.Hash)
							return;
						foreach (var header in headers.Headers)
						{
							var h = header.GetHash();
							if (h == currentTip.Hash)
								continue;

							if (header.HashPrevBlock == currentTip.Hash)
							{
								isOurs = true;
								currentTip = new SlimChainedBlock(h, currentTip.Hash, currentTip.Height + 1);
								chain.TrySetTip(currentTip.Hash, currentTip.Previous);
								if (currentTip.Hash == hashStop)
									return;
							}
							else if (chain.TrySetTip(h, header.HashPrevBlock))
							{
								currentTip = chain.TipBlock;
							}
							else
								break;
						}
						if (isOurs)
							break;  //Go ask for next header
					}
				}
			}
		}

		public IEnumerable<Block> GetBlocks(SynchronizeChainOptions synchronizeChainOptions, CancellationToken cancellationToken = default(CancellationToken))
		{
			var genesis = new ChainedBlock(Network.GetGenesis().Header, 0);
			return GetBlocksFromFork(genesis, synchronizeChainOptions, cancellationToken);
		}

		public IEnumerable<Block> GetBlocks(uint256 hashStop = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return GetBlocks(new SynchronizeChainOptions() { HashStop = hashStop });
		}

		public IEnumerable<Block> GetBlocksFromFork(ChainedBlock currentTip, SynchronizeChainOptions synchronizeChainOptions, CancellationToken cancellationToken = default(CancellationToken))
		{
			using (var listener = CreateListener())
			{
				SendMessageAsync(new GetBlocksPayload()
				{
					BlockLocators = currentTip.GetLocator(),
				});

				var headers = GetHeadersFromFork(currentTip, synchronizeChainOptions, cancellationToken);

				foreach (var block in GetBlocks(headers.Select(b => b.HashBlock), cancellationToken))
				{
					yield return block;
				}
			}
		}

		public IEnumerable<Block> GetBlocksFromFork(ChainedBlock currentTip, uint256 hashStop = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return GetBlocksFromFork(currentTip, new SynchronizeChainOptions() { HashStop = hashStop });
		}

		public IEnumerable<Block> GetBlocks(IEnumerable<ChainedBlock> blocks, CancellationToken cancellationToken = default(CancellationToken))
		{
			return GetBlocks(blocks.Select(c => c.HashBlock), cancellationToken);
		}

		public IEnumerable<Block> GetBlocks(IEnumerable<uint256> neededBlocks, CancellationToken cancellationToken = default(CancellationToken))
		{
			AssertState(NodeState.HandShaked, cancellationToken);

			int simultaneous = 70;
			using (var listener = CreateListener()
								.OfType<BlockPayload>())
			{
				foreach (var invs in neededBlocks
									.Select(b => new InventoryVector()
									{
										Type = AddSupportedOptions(InventoryType.MSG_BLOCK),
										Hash = b
									})
									.Partition(() => simultaneous))
				{

					var remaining = new Queue<uint256>(invs.Select(k => k.Hash));
					SendMessageAsync(new GetDataPayload(invs.ToArray()));

					int maxQueued = 0;
					while (remaining.Count != 0)
					{
						var block = listener.ReceivePayload<BlockPayload>(cancellationToken).Object;
						maxQueued = Math.Max(listener.MessageQueue.Count, maxQueued);
						if (remaining.Peek() == block.GetHash())
						{
							remaining.Dequeue();
							yield return block;
						}
					}
					if (maxQueued < 10)
						simultaneous *= 2;
					else
						simultaneous /= 2;
					simultaneous = Math.Max(10, simultaneous);
					simultaneous = Math.Min(10000, simultaneous);
				}
			}
		}

		/// <summary>
		/// Create a listener that will queue messages until disposed
		/// </summary>
		/// <returns>The listener</returns>
		/// <exception cref="System.InvalidOperationException">Thrown if used on the listener's thread, as it would result in a deadlock</exception>
		public NodeListener CreateListener()
		{
			AssertNoListeningThread();
			return new NodeListener(this);
		}


		private void AssertState(NodeState nodeState, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (nodeState == NodeState.HandShaked && State == NodeState.Connected)
				this.VersionHandshake(cancellationToken);
			if (nodeState != State)
				throw new InvalidOperationException("Invalid Node state, needed=" + nodeState + ", current= " + State);
		}

		public uint256[] GetMempool(CancellationToken cancellationToken = default(CancellationToken))
		{
			AssertState(NodeState.HandShaked);
			using (var listener = CreateListener().OfType<InvPayload>())
			{
				this.SendMessageAsync(new MempoolPayload());
				var invs = listener.ReceivePayload<InvPayload>(cancellationToken).Inventory.Select(i => i.Hash).ToList();
				var result = invs;
				while (invs.Count == InvPayload.MAX_INV_SZ)
				{
					invs = listener.ReceivePayload<InvPayload>(cancellationToken).Inventory.Select(i => i.Hash).ToList();
					result.AddRange(invs);
				}
				return result.ToArray();
			}
		}

		/// <summary>
		/// Retrieve transactions from the mempool
		/// </summary>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>Transactions in the mempool</returns>
		public Transaction[] GetMempoolTransactions(CancellationToken cancellationToken = default(CancellationToken))
		{
			return GetMempoolTransactions(GetMempool(), cancellationToken);
		}

		/// <summary>
		/// Retrieve transactions from the mempool by ids
		/// </summary>
		/// <param name="txIds">Transaction ids to retrieve</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>The transactions, if a transaction is not found, then it is not returned in the array.</returns>
		public Transaction[] GetMempoolTransactions(uint256[] txIds, CancellationToken cancellationToken = default(CancellationToken))
		{
			AssertState(NodeState.HandShaked);
			if (txIds.Length == 0)
				return new Transaction[0];
			List<Transaction> result = new List<Transaction>();
			using (var listener = CreateListener().Where(m => m.Message.Payload is TxPayload || m.Message.Payload is NotFoundPayload))
			{
				foreach (var batch in txIds.Partition(500))
				{
					this.SendMessageAsync(new GetDataPayload(batch.Select(txid => new InventoryVector()
					{
						Type = AddSupportedOptions(InventoryType.MSG_TX),
						Hash = txid
					}).ToArray()));
					try
					{
						List<Transaction> batchResult = new List<NBitcoin.Transaction>();
						while (batchResult.Count < batch.Count)
						{
							CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10.0));
							using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token))
							{
								var payload = listener.ReceivePayload<Payload>(cts.Token);
								if (payload is NotFoundPayload)
									batchResult.Add(null);
								else
									batchResult.Add(((TxPayload)payload).Object);
							}
						}
						result.AddRange(batchResult);
					}
					catch (OperationCanceledException)
					{
						if (cancellationToken.IsCancellationRequested)
						{
							throw;
						}
					}
				}
			}
			return result.Where(r => r != null).ToArray();
		}

		/// <summary>
		/// Add supported option to the input inventory type
		/// </summary>
		/// <param name="inventoryType">Inventory type (like MSG_TX)</param>
		/// <returns>Inventory type with options (MSG_TX | MSG_WITNESS_FLAG)</returns>
		public InventoryType AddSupportedOptions(InventoryType inventoryType)
		{
			if ((ActualTransactionOptions & TransactionOptions.Witness) != 0)
				inventoryType |= InventoryType.MSG_WITNESS_FLAG;
			return inventoryType;
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
			using (var listener = CreateListener().OfType<PongPayload>())
			{
				var ping = new PingPayload()
				{
					Nonce = RandomUtils.GetUInt64()
				};
				var before = DateTimeOffset.UtcNow;
				SendMessageAsync(ping);

				while (listener.ReceivePayload<PongPayload>(cancellation).Nonce != ping.Nonce)
				{
				}
				var after = DateTimeOffset.UtcNow;
				return after - before;
			}
		}
	}
}
#endif
