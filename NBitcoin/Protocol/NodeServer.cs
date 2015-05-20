#if !NOSOCKET
#if !NOUPNP
using Mono.Nat;
#endif
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin.BitcoinCore;

namespace NBitcoin.Protocol
{
	public delegate void NodeServerNodeEventHandler(NodeServer sender, Node node);
	public delegate void NodeServerMessageEventHandler(NodeServer sender, IncomingMessage message);
	public class NodeServer : IDisposable
	{
		private readonly Network _Network;
		public Network Network
		{
			get
			{
				return _Network;
			}
		}
		private readonly ProtocolVersion _Version;
		public ProtocolVersion Version
		{
			get
			{
				return _Version;
			}
		}

		/// <summary>
		/// The parameters that will be cloned and applied for each node connecting to the NodeServer
		/// </summary>
		public NodeConnectionParameters InboundNodeConnectionParameters
		{
			get;
			set;
		}

		public NodeServer(Network network, ProtocolVersion version = ProtocolVersion.PROTOCOL_VERSION,
			int internalPort = -1)
		{
			AllowLocalPeers = true;
			InboundNodeConnectionParameters = new NodeConnectionParameters();
			internalPort = internalPort == -1 ? network.DefaultPort : internalPort;
			_LocalEndpoint = new IPEndPoint(IPAddress.Parse("0.0.0.0").MapToIPv6(), internalPort);
			_Network = network;
			_ExternalEndpoint = new IPEndPoint(_LocalEndpoint.Address, Network.DefaultPort);
			_Version = version;
			var listener = new EventLoopMessageListener<IncomingMessage>(ProcessMessage);
			_MessageProducer.AddMessageListener(listener);
			OwnResource(listener);
			_ConnectedNodes = new NodesCollection();
			_ConnectedNodes.Added += _Nodes_NodeAdded;
			_ConnectedNodes.Removed += _Nodes_NodeRemoved;
			_ConnectedNodes.MessageProducer.AddMessageListener(listener);
			_Trace = new TraceCorrelation(NodeServerTrace.Trace, "Node server listening on " + LocalEndpoint);
		}


		public event NodeServerNodeEventHandler NodeRemoved;
		public event NodeServerNodeEventHandler NodeAdded;
		public event NodeServerMessageEventHandler MessageReceived;

		void _Nodes_NodeRemoved(object sender, NodeEventArgs node)
		{
			var removed = NodeRemoved;
			if(removed != null)
				removed(this, node.Node);
		}

		void _Nodes_NodeAdded(object sender, NodeEventArgs node)
		{
			var added = NodeAdded;
			if(added != null)
				added(this, node.Node);
		}


		int[] bitcoinPorts;
		int[] BitcoinPorts
		{
			get
			{
				if(bitcoinPorts == null)
				{
					bitcoinPorts = Enumerable.Range(Network.DefaultPort, 10).ToArray();
				}
				return bitcoinPorts;
			}
		}

		TimeSpan _NATLeasePeriod = TimeSpan.FromMinutes(10.0);
		/// <summary>
		/// When using DetectExternalEndpoint, UPNP will open ports on the gateway for a fixed amount of time before renewing
		/// </summary>
		public TimeSpan NATLeasePeriod
		{
			get
			{
				return _NATLeasePeriod;
			}
			set
			{
				_NATLeasePeriod = value;
			}
		}


		string _NATRuleName = "NBitcoin Node Server";
		public string NATRuleName
		{
			get
			{
				return _NATRuleName;
			}
			set
			{
				_NATRuleName = value;
			}
		}
#if !NOUPNP
		UPnPLease _UPnPLease;
		public UPnPLease DetectExternalEndpoint(CancellationToken cancellation = default(CancellationToken))
		{
			if(_UPnPLease != null)
			{
				_UPnPLease.Dispose();
				_UPnPLease = null;
			}
			var lease = new UPnPLease(BitcoinPorts, LocalEndpoint.Port, NATRuleName);
			lease.LeasePeriod = NATLeasePeriod;
			if(lease.DetectExternalEndpoint(cancellation))
			{
				_UPnPLease = lease;
				ExternalEndpoint = _UPnPLease.ExternalEndpoint;
				return lease;
			}
			else
			{
				using(lease.Trace.Open())
				{
					NodeServerTrace.Information("No UPNP device found, try to use external web services to deduce external address");
					try
					{
						var ip = GetMyExternalIP(cancellation);
						if(ip != null)
							ExternalEndpoint = new IPEndPoint(ip, ExternalEndpoint.Port);
					}
					catch(Exception ex)
					{
						NodeServerTrace.Error("Could not use web service to deduce external address", ex);
					}
				}
				return null;
			}
		}
#endif
		public bool AllowLocalPeers
		{
			get;
			set;
		}

		private IPEndPoint _LocalEndpoint;
		public IPEndPoint LocalEndpoint
		{
			get
			{
				return _LocalEndpoint;
			}
		}

		Socket socket;
		TraceCorrelation _Trace;

		public bool IsListening
		{
			get
			{
				return socket != null;
			}
		}

		public void Listen()
		{
			if(socket != null)
				throw new InvalidOperationException("Already listening");
			using(_Trace.Open())
			{
				try
				{
					socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
#if !NOIPDUALMODE
					socket.DualMode = true;
#endif

					socket.Bind(LocalEndpoint);
					socket.Listen(8);
					NodeServerTrace.Information("Listening...");
					BeginAccept();
				}
				catch(Exception ex)
				{
					NodeServerTrace.Error("Error while opening the Protocol server", ex);
					throw;
				}
			}
		}

		private void BeginAccept()
		{
			if(_Cancel.IsCancellationRequested)
				return;
			NodeServerTrace.Information("Accepting connection...");
			socket.BeginAccept(EndAccept, null);
		}
		private void EndAccept(IAsyncResult ar)
		{
			using(_Trace.Open())
			{
				Socket client = null;
				try
				{
					client = socket.EndAccept(ar);
					if(_Cancel.IsCancellationRequested)
						return;
					NodeServerTrace.Information("Client connection accepted : " + client.RemoteEndPoint);
					var cancel = CancellationTokenSource.CreateLinkedTokenSource(_Cancel.Token);
					cancel.CancelAfter(TimeSpan.FromSeconds(10));
					while(!_Cancel.IsCancellationRequested)
					{
						var message = Message.ReadNext(client, Network, Version, cancel.Token);
						_MessageProducer.PushMessage(new IncomingMessage()
						{
							Socket = client,
							Message = message,
							Node = null,
						});
						if(message.Payload is VersionPayload)
							break;
						else
							NodeServerTrace.Error("The first message of the remote peer did not contained a Version payload", null);
					}
				}
				catch(OperationCanceledException ex)
				{
					if(ex.CancellationToken != _Cancel.Token)
					{
						Utils.SafeCloseSocket(client);
						NodeServerTrace.Error("The remote connecting failed to send a message within 10 seconds, dropping connection", ex);
					}
				}
				catch(Exception ex)
				{
					if(_Cancel.IsCancellationRequested)
						return;
					if(client == null)
					{
						NodeServerTrace.Error("Error while accepting connection ", ex);
						Thread.Sleep(3000);
					}
					else
					{
						Utils.SafeCloseSocket(client);
						NodeServerTrace.Error("Invalid message received from the remote connecting node", ex);
					}
				}
				BeginAccept();
			}
		}

		public IPAddress GetMyExternalIP(CancellationToken cancellation = default(CancellationToken))
		{

			var tasks = new[]{
						new {IP = "91.198.22.70", DNS ="checkip.dyndns.org"}, 
						new {IP = "209.68.27.16", DNS = "www.ipchicken.com"}
			 }.Select(site =>
			 {
				 return Task.Run(() =>
					 {
						 var ip = IPAddress.Parse(site.IP);
						 try
						 {
							 ip = Dns.GetHostAddresses(site.DNS).First();
						 }
						 catch(Exception ex)
						 {
							 NodeServerTrace.Warning("can't resolve ip of " + site.DNS + " using hardcoded one " + site.IP, ex);
						 }
						 WebClient client = new WebClient();
						 var page = client.DownloadString("http://" + ip);
						 var match = Regex.Match(page, "[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}");
						 return match.Value;
					 });
			 }).ToArray();


			Task.WaitAny(tasks, cancellation);
			try
			{
				var result = tasks.First(t => t.IsCompleted && !t.IsFaulted).Result;
				NodeServerTrace.ExternalIpReceived(result);
				return IPAddress.Parse(result);
			}
			catch(InvalidOperationException)
			{
				NodeServerTrace.ExternalIpFailed(tasks.Select(t => t.Exception).FirstOrDefault());
				throw new WebException("Impossible to detect extenal ip");
			}
		}

		internal readonly MessageProducer<IncomingMessage> _MessageProducer = new MessageProducer<IncomingMessage>();
		internal readonly MessageProducer<object> _InternalMessageProducer = new MessageProducer<object>();

		MessageProducer<IncomingMessage> _AllMessages = new MessageProducer<IncomingMessage>();
		public MessageProducer<IncomingMessage> AllMessages
		{
			get
			{
				return _AllMessages;
			}
		}

		volatile IPEndPoint _ExternalEndpoint;
		public IPEndPoint ExternalEndpoint
		{
			get
			{
				return _ExternalEndpoint;
			}
			set
			{
				_ExternalEndpoint = Utils.EnsureIPv6(value);
			}
		}


		internal void ExternalAddressDetected(IPAddress iPAddress)
		{
			if(!ExternalEndpoint.Address.IsRoutable(AllowLocalPeers) && iPAddress.IsRoutable(AllowLocalPeers))
			{
				NodeServerTrace.Information("New externalAddress detected " + iPAddress);
				ExternalEndpoint = new IPEndPoint(iPAddress, ExternalEndpoint.Port);
			}
		}

		void ProcessMessage(IncomingMessage message)
		{
			AllMessages.PushMessage(message);
			TraceCorrelation trace = null;
			if(message.Node != null)
			{
				trace = message.Node.TraceCorrelation;
			}
			else
			{
				trace = new TraceCorrelation(NodeServerTrace.Trace, "Processing inbound message " + message.Message);
			}
			using(trace.Open(false))
			{
				ProcessMessageCore(message);
			}
		}

		private void ProcessMessageCore(IncomingMessage message)
		{
			if(message.Message.Payload is VersionPayload)
			{
				var version = message.AssertPayload<VersionPayload>();
				var connectedToSelf = version.Nonce == Nonce;
				if(message.Node != null && connectedToSelf)
				{
					NodeServerTrace.ConnectionToSelfDetected();
					message.Node.Disconnect();
					return;
				}

				if(message.Node == null)
				{
					var remoteEndpoint = version.AddressFrom;
					if(!remoteEndpoint.Address.IsRoutable(AllowLocalPeers))
					{
						//Send his own endpoint
						remoteEndpoint = new IPEndPoint(((IPEndPoint)message.Socket.RemoteEndPoint).Address, Network.DefaultPort);
					}

					var peer = new NetworkAddress()
					{
						Endpoint = remoteEndpoint,
						Time = DateTimeOffset.UtcNow
					};
					var node = new Node(peer, Network, CreateNodeConnectionParameters(), message.Socket, version);

					if(connectedToSelf)
					{
						node.SendMessage(CreateNodeConnectionParameters().CreateVersion(node.Peer.Endpoint, Network));
						NodeServerTrace.ConnectionToSelfDetected();
						node.Disconnect();
						return;
					}

					CancellationTokenSource cancel = new CancellationTokenSource();
					cancel.CancelAfter(TimeSpan.FromSeconds(10.0));
					try
					{
						ConnectedNodes.Add(node);
						node.StateChanged += node_StateChanged;
						node.RespondToHandShake(cancel.Token);
					}
					catch(OperationCanceledException ex)
					{
						NodeServerTrace.Error("The remote node did not respond fast enough (10 seconds) to the handshake completion, dropping connection", ex);
						node.Disconnect();
						throw;
					}
					catch(Exception)
					{
						node.Disconnect();
						throw;
					}
				}
			}

			var messageReceived = MessageReceived;
			if(messageReceived != null)
				messageReceived(this, message);
		}

		void node_StateChanged(Node node, NodeState oldState)
		{
			if(node.State == NodeState.Disconnecting ||
				node.State == NodeState.Failed ||
				node.State == NodeState.Offline)
				ConnectedNodes.Remove(node);
		}

		private readonly NodesCollection _ConnectedNodes = new NodesCollection();
		public NodesCollection ConnectedNodes
		{
			get
			{
				return _ConnectedNodes;
			}
		}


		List<IDisposable> _Resources = new List<IDisposable>();
		IDisposable OwnResource(IDisposable resource)
		{
			if(_Cancel.IsCancellationRequested)
			{
				resource.Dispose();
				return Scope.Nothing;
			}
			return new Scope(() =>
			{
				lock(_Resources)
				{
					_Resources.Add(resource);
				}
			}, () =>
			{
				lock(_Resources)
				{
					_Resources.Remove(resource);
				}
			});
		}
		#region IDisposable Members

		CancellationTokenSource _Cancel = new CancellationTokenSource();
		public void Dispose()
		{
			if(!_Cancel.IsCancellationRequested)
			{
				_Cancel.Cancel();

				lock(_Resources)
				{
					foreach(var resource in _Resources)
						resource.Dispose();
				}
				try
				{
					_ConnectedNodes.DisconnectAll();
				}
				finally
				{
#if !NOUPNP
					if(_UPnPLease != null)
					{
						_UPnPLease.Dispose();
					}
#endif
					if(socket != null)
					{
						Utils.SafeCloseSocket(socket);
						socket = null;
					}
				}
			}
		}

		#endregion

		internal NodeConnectionParameters CreateNodeConnectionParameters()
		{
			var myExternal = Utils.EnsureIPv6(ExternalEndpoint);
			var param2 = InboundNodeConnectionParameters.Clone();
			param2.Nonce = Nonce;
			param2.Version = Version;
			param2.AddressFrom = myExternal;
			return param2;
		}

		public bool IsRelay
		{
			get
			{
				return InboundNodeConnectionParameters.IsRelay;
			}
			set
			{
				InboundNodeConnectionParameters.IsRelay = value;
			}
		}

		public string UserAgent
		{
			get
			{
				return InboundNodeConnectionParameters.UserAgent;
			}
			set
			{
				InboundNodeConnectionParameters.UserAgent = value;
			}
		}

		ulong _Nonce;
		public ulong Nonce
		{
			get
			{
				if(_Nonce == 0)
				{
					_Nonce = RandomUtils.GetUInt64();
				}
				return _Nonce;
			}
			set
			{
				_Nonce = value;
			}
		}



		public bool IsConnectedTo(IPEndPoint endpoint)
		{
			return _ConnectedNodes.FindByEndpoint(endpoint) != null;
		}

		public Node FindOrConnect(IPEndPoint endpoint)
		{
			while(true)
			{
				var node = _ConnectedNodes.FindByEndpoint(endpoint);
				if(node != null)
					return node;
				node = Node.Connect(Network, endpoint, CreateNodeConnectionParameters());
				node.StateChanged += node_StateChanged;
				if(!_ConnectedNodes.Add(node))
				{
					node.Disconnect();
				}
				else
					return node;
			}
		}
	}
}
#endif