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
		public bool AdvertizeMyself
		{
			get;
			set;
		}

		public NodeServer(Network network, ProtocolVersion version = ProtocolVersion.PROTOCOL_VERSION,
			int internalPort = -1)
		{
			AdvertizeMyself = false;
			internalPort = internalPort == -1 ? network.DefaultPort : internalPort;
			_LocalEndpoint = new IPEndPoint(IPAddress.Parse("0.0.0.0").MapToIPv6(), internalPort);
			_Network = network;
			_ExternalEndpoint = new IPEndPoint(_LocalEndpoint.Address, Network.DefaultPort);
			_Version = version;
			var listener = new EventLoopMessageListener<IncomingMessage>(ProcessMessage);
			_MessageProducer.AddMessageListener(listener);
			OwnResource(listener);
			RegisterPeerTableRepository(_PeerTable);
			_Nodes = new NodeSet();
			_Nodes.NodeAdded += _Nodes_NodeAdded;
			_Nodes.NodeRemoved += _Nodes_NodeRemoved;
			_Nodes.MessageProducer.AddMessageListener(listener);
			_Trace = new TraceCorrelation(NodeServerTrace.Trace, "Node server listening on " + LocalEndpoint);
		}


		public event NodeServerNodeEventHandler NodeRemoved;
		public event NodeServerNodeEventHandler NodeAdded;
		public event NodeServerMessageEventHandler MessageReceived;

		void _Nodes_NodeRemoved(NodeSet sender, Node node)
		{
			var removed = NodeRemoved;
			if(removed != null)
				removed(this, node);
		}

		void _Nodes_NodeAdded(NodeSet sender, Node node)
		{
			var added = NodeAdded;
			if(added != null)
				added(this, node);
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


		private void PopulateTableWithHardNodes()
		{
			_InternalMessageProducer.PushMessages(Network.SeedNodes.Select(n => new Peer(PeerOrigin.HardSeed, n)).ToArray());
		}
		private void PopulateTableWithDNSNodes()
		{
			var peers = Network.DNSSeeds
							.SelectMany(s =>
							{
								try
								{
									return s.GetAddressNodes();
								}
								catch(Exception ex)
								{
									NodeServerTrace.ErrorWhileRetrievingDNSSeedIp(s.Name, ex);
									return new IPAddress[0];
								}
							})
							.Select(s => new Peer(PeerOrigin.DNSSeed, new NetworkAddress()
							{
								Endpoint = new IPEndPoint(s, Network.DefaultPort),
								Time = Utils.UnixTimeToDateTime(0)
							})).ToArray();

			_InternalMessageProducer.PushMessages(peers);
		}

		readonly PeerTable _PeerTable = new PeerTable();
		public PeerTable PeerTable
		{
			get
			{
				return _PeerTable;
			}
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
			if(isDisposed)
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
					if(isDisposed)
						return;
					NodeServerTrace.Information("Client connection accepted : " + client.RemoteEndPoint);
					var cancel = new CancellationTokenSource();
					cancel.CancelAfter(TimeSpan.FromSeconds(10));
					var message = Message.ReadNext(client, Network, Version, cancel.Token);
					_MessageProducer.PushMessage(new IncomingMessage()
					{
						Socket = client,
						Message = message,
						Node = null,
					});
				}
				catch(OperationCanceledException ex)
				{
					NodeServerTrace.Error("The remote connecting failed to send a message within 10 seconds, dropping connection", ex);
				}
				catch(Exception ex)
				{
					if(isDisposed)
						return;
					if(client == null)
					{
						NodeServerTrace.Error("Error while accepting connection ", ex);
						Thread.Sleep(3000);
					}
					else
					{
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

		public Node GetNodeByHostName(string hostname, int port = -1, CancellationToken cancellation = default(CancellationToken))
		{
			if(port == -1)
				port = Network.DefaultPort;
			var ip = Dns.GetHostAddresses(hostname).First();
			var endpoint = new IPEndPoint(ip, port);
			return GetNodeByEndpoint(endpoint, cancellation);
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly NodeSet _Nodes;

		public Node GetNodeByEndpoint(IPEndPoint endpoint, CancellationToken cancellation = default(CancellationToken))
		{
			lock(_Nodes)
			{
				endpoint = Utils.EnsureIPv6(endpoint);

				var node = _Nodes.GetNodeByEndpoint(endpoint);
				if(node != null)
					return node;
				var peer = PeerTable.GetPeer(endpoint);
				if(peer == null)
					peer = new Peer(PeerOrigin.Manual, new NetworkAddress()
					{

						Endpoint = endpoint,
						Time = Utils.UnixTimeToDateTime(0)
					});

				return AddNode(peer, cancellation);
			}
		}

		public Node GetNodeByEndpoint(string endpoint)
		{
			IPEndPoint ip = Utils.ParseIpEndpoint(endpoint, Network.DefaultPort);
			return GetNodeByEndpoint(ip);
		}

		public Node GetNodeByPeer(Peer peer, CancellationToken cancellation = default(CancellationToken))
		{
			var node = _Nodes.GetNodeByPeer(peer);
			if(node != null)
				return node;
			return AddNode(peer, cancellation);

		}

		private Node AddNode(Peer peer, CancellationToken cancellationToken)
		{
			try
			{
				var node = new Node(peer, Network, CreateVersionPayload(peer, ExternalEndpoint, Version), cancellationToken);
				return AddNode(node);
			}
			catch(Exception)
			{
				return null;
			}
		}

		private Node AddNode(Node node)
		{
			if(node.State < NodeState.Connected)
				return null;
			return _Nodes.AddNode(node);
		}

		internal void RemoveNode(Node node)
		{
			_Nodes.RemoveNode(node);
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

					var peer = new Peer(PeerOrigin.Advertised, new NetworkAddress()
					{
						Endpoint = remoteEndpoint,
						Time = DateTimeOffset.UtcNow
					});
					var node = new Node(peer, Network, CreateVersionPayload(peer, ExternalEndpoint, Version), message.Socket, version);

					if(connectedToSelf)
					{
						node.SendMessage(CreateVersionPayload(node.Peer, ExternalEndpoint, Version));
						NodeServerTrace.ConnectionToSelfDetected();
						node.Disconnect();
						return;
					}

					CancellationTokenSource cancel = new CancellationTokenSource();
					cancel.CancelAfter(TimeSpan.FromSeconds(10.0));
					try
					{
						AddNode(node);
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


		public bool IsConnectedTo(IPEndPoint endpoint)
		{
			return _Nodes.Contains(endpoint);
		}

		ConcurrentDictionary<Node, Node> _ConnectedNodes = new ConcurrentDictionary<Node, Node>();

		public bool AdvertiseMyself()
		{
			if(IsListening && ExternalEndpoint.Address.IsRoutable(AllowLocalPeers))
			{
				NodeServerTrace.Information("Advertizing myself");
				foreach(var node in _ConnectedNodes)
				{
					node.Value.SendMessage(new AddrPayload(new NetworkAddress()
					{
						Ago = TimeSpan.FromSeconds(0),
						Endpoint = ExternalEndpoint
					}));
				}
				return true;
			}
			else
				return false;

		}

		List<IDisposable> _Resources = new List<IDisposable>();
		IDisposable OwnResource(IDisposable resource)
		{
			if(isDisposed)
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

		bool isDisposed;
		public void Dispose()
		{
			if(!isDisposed)
			{
				isDisposed = true;

				lock(_Resources)
				{
					foreach(var resource in _Resources)
						resource.Dispose();
				}
				try
				{
					_Nodes.DisconnectAll();
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

		public VersionPayload CreateVersionPayload(Peer peer, IPEndPoint myExternal, ProtocolVersion? version)
		{
			myExternal = Utils.EnsureIPv6(myExternal);
			return new VersionPayload()
					{
						Nonce = Nonce,
						UserAgent = UserAgent,
						Version = version == null ? Version : version.Value,
						StartHeight = 0,
						Timestamp = DateTimeOffset.UtcNow,
						AddressReceiver = peer.NetworkAddress.Endpoint,
						AddressFrom = myExternal,
						Relay = IsRelay
					};
		}

		public bool IsRelay
		{
			get;
			set;
		}

		string _UserAgent;
		public string UserAgent
		{
			get
			{
				if(_UserAgent == null)
				{
					_UserAgent = VersionPayload.GetNBitcoinUserAgent();
				}
				return _UserAgent;
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





		/// <summary>
		/// Fill the PeerTable with fresh addresses
		/// </summary>
		public void DiscoverPeers(int peerToFind = 990)
		{
			TraceCorrelation traceCorrelation = new TraceCorrelation(NodeServerTrace.Trace, "Discovering nodes");
			List<Task> tasks = new List<Task>();
			using(traceCorrelation.Open())
			{
				while(CountPeerRequired(peerToFind) != 0)
				{
					NodeServerTrace.PeerTableRemainingPeerToGet(CountPeerRequired(peerToFind));
					var peers = PeerTable.GetActivePeers(1000);
					if(peers.Length == 0)
					{
						PopulateTableWithDNSNodes();
						PopulateTableWithHardNodes();
						peers = PeerTable.GetActivePeers(1000);
					}


					CancellationTokenSource peerTableFull = new CancellationTokenSource();
					NodeSet connected = new NodeSet();
					try
					{
						Parallel.ForEach(peers, new ParallelOptions()
						{
							MaxDegreeOfParallelism = 5,
							CancellationToken = peerTableFull.Token,
						}, p =>
						{
							CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(40));
							Node n = null;
							try
							{
								n = GetNodeByPeer(p, cancellation.Token);
								if(n.State < NodeState.HandShaked)
								{
									connected.AddNode(n);
									n.VersionHandshake(cancellation.Token);
								}
								n.SendMessage(new GetAddrPayload());
								Thread.Sleep(2000);
							}
							catch(Exception)
							{
								if(n != null)
									n.Disconnect();
							}
							if(CountPeerRequired(peerToFind) == 0)
								peerTableFull.Cancel();
							else
								NodeServerTrace.Information("Need " + CountPeerRequired(peerToFind) + " more peers");
						});
					}
					catch(OperationCanceledException)
					{
					}
					finally
					{
						connected.DisconnectAll();
					}
				}
				NodeServerTrace.Trace.TraceInformation("Peer table is now full");
			}
		}

		int CountPeerRequired(int peerToFind)
		{
			return Math.Max(0, peerToFind - PeerTable.CountUsed(true));
		}

		public NodeSet CreateNodeSet(int size)
		{
			if(size > 1000)
				throw new ArgumentOutOfRangeException("size", "size should be less than 1000");
			TraceCorrelation trace = new TraceCorrelation(NodeServerTrace.Trace, "Creating node set of size " + size);
			NodeSet set = new NodeSet();
			using(trace.Open())
			{
				while(set.Count() < size)
				{
					var peerToGet = size - set.Count();
					var activePeers = PeerTable.GetActivePeers(1000);
					activePeers = activePeers.Where(p => !set.Contains(p.NetworkAddress.Endpoint)).ToArray();
					if(activePeers.Length < peerToGet)
					{
						DiscoverPeers(size);
						continue;
					}
					NodeServerTrace.Information("Need " + peerToGet + " more nodes");

					BlockingCollection<Node> handshakedNodes = new BlockingCollection<Node>(peerToGet);
					CancellationTokenSource handshakedFull = new CancellationTokenSource();

					try
					{
						Parallel.ForEach(activePeers,
							new ParallelOptions()
							{
								MaxDegreeOfParallelism = 10,
								CancellationToken = handshakedFull.Token
							}, p =>
						{
							if(set.Contains(p.NetworkAddress.Endpoint))
								return;
							Node node = null;
							try
							{
								node = GetNodeByPeer(p, handshakedFull.Token);
								node.VersionHandshake(handshakedFull.Token);
								if(node != null && node.State != NodeState.HandShaked)
									node.Disconnect();
								if(!handshakedNodes.TryAdd(node))
								{
									handshakedFull.Cancel();
									node.Disconnect();
								}
								else
								{
									var remaining = (size - set.Count() - handshakedNodes.Count);
									if(remaining == 0)
									{
										handshakedFull.Cancel();
									}
									else
										NodeServerTrace.Information("Need " + remaining + " more nodes");
								}
							}
							catch(Exception)
							{
								if(node != null)
									node.Disconnect();
							}
						});
					}
					catch(OperationCanceledException)
					{
					}
					set.AddNodes(handshakedNodes.ToArray());
				}
			}
			return set;
		}

		public IDisposable RegisterPeerTableRepository(PeerTableRepository peerTableRepository)
		{
			var poll = new EventLoopMessageListener<object>(o =>
			{
				var message = o as IncomingMessage;
				if(message != null)
				{
					if(message.Message.Payload is AddrPayload)
					{
						peerTableRepository.WritePeers(((AddrPayload)message.Message.Payload).Addresses
														.Where(a => a.Endpoint.Address.IsRoutable(AllowLocalPeers))
														.Select(a => new Peer(PeerOrigin.Addr, a)));
					}
				}
				var peer = o as Peer;
				if(peer != null)
				{
					if(peer.NetworkAddress.Endpoint.Address.IsRoutable(AllowLocalPeers))
						peerTableRepository.WritePeer(peer);
				}
			});

			if(peerTableRepository != _PeerTable)
			{
				_InternalMessageProducer.PushMessages(peerTableRepository.GetPeers());
			}
			return new CompositeDisposable(AllMessages.AddMessageListener(poll), _InternalMessageProducer.AddMessageListener(poll), OwnResource(poll));
		}

#if !NOFILEIO
		public IDisposable RegisterBlockRepository(BlockRepository repository)
		{
			var listener = new EventLoopMessageListener<IncomingMessage>((m) =>
			{
				if(m.Node != null)
				{
					if(m.Message.Payload is HeadersPayload)
					{
						foreach(var header in ((HeadersPayload)m.Message.Payload).Headers)
						{
							repository.WriteBlockHeader(header);
						}
					}
					if(m.Message.Payload is BlockPayload)
					{
						repository.WriteBlock(((BlockPayload)m.Message.Payload).Object);
					}
				}
			});
			return new CompositeDisposable(AllMessages.AddMessageListener(listener), OwnResource(listener));
		}
#endif
		public Node GetLocalNode()
		{
			return GetNodeByEndpoint(new IPEndPoint(IPAddress.Loopback, ExternalEndpoint.Port));
		}
	}
}
#endif