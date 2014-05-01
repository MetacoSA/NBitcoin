using Mono.Nat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
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

		public NodeServer(Network network, ProtocolVersion version = ProtocolVersion.PROTOCOL_VERSION,
			int internalPort = -1)
		{
			internalPort = internalPort == -1 ? network.DefaultPort : internalPort;
			_LocalEndpoint = new IPEndPoint(IPAddress.Parse("0.0.0.0").MapToIPv6(), internalPort);
			_Network = network;
			_ExternalEndpoint = new IPEndPoint(_LocalEndpoint.Address, Network.DefaultPort);
			_Version = version;
			_MessageListener = new EventLoopMessageListener<IncomingMessage>(ProcessMessage);
			_MessageProducer.AddMessageListener(_MessageListener);

			_Trace = new TraceCorrelation(NodeServerTrace.Trace, "Node server listening on " + LocalEndpoint);
		}

		public IDisposable RegisterBlockRepository(BlockRepository repository)
		{
			var listener = new NewThreadMessageListener<IncomingMessage>((m) =>
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
			return _MessageProducer.AddMessageListener(listener);
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
		public bool AllowLocalPeers
		{
			get
			{
				return PeerTable.AllowLocalPeers;
			}
			set
			{
				PeerTable.AllowLocalPeers = value;
			}
		}

		private readonly EventLoopMessageListener<IncomingMessage> _MessageListener;

		private void PopulateTableWithHardNodes()
		{
			PeerTable.UpdatePeers(Network.SeedNodes.Select(n => new Peer(PeerOrigin.HardSeed, n)).ToArray());
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

			PeerTable.UpdatePeers(peers);
		}

		private readonly PeerTable _PeerTable = new PeerTable();
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
					socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
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
						new {IP = "74.208.43.192", DNS = "www.showmyip.com"}
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
				NodeServerTrace.ExternalIpRecieved(result);
				return IPAddress.Parse(result);
			}
			catch(InvalidOperationException)
			{
				NodeServerTrace.ExternalIpFailed(tasks.Select(t => t.Exception).FirstOrDefault());
				throw new WebException("Impossible to detect extenal ip");
			}
		}

		private readonly MessageProducer<IncomingMessage> _MessageProducer = new MessageProducer<IncomingMessage>();
		internal MessageProducer<IncomingMessage> MessageProducer
		{
			get
			{
				return _MessageProducer;
			}
		}

		IPEndPoint _ExternalEndpoint;
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
				if(!iPAddress.Equals(_ExternalEndpoint.Address))
				{
					NodeServerTrace.Information("New externalAddress detected " + iPAddress);
					ExternalEndpoint = new IPEndPoint(iPAddress, ExternalEndpoint.Port);
				}
			}
		}

		public Node GetNodeByHostName(string hostname, int port = -1)
		{
			if(port == -1)
				port = Network.DefaultPort;
			var ip = Dns.GetHostAddresses(hostname).First();
			var endpoint = new IPEndPoint(ip, port);
			return GetNodeByEndpoint(endpoint);
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		Dictionary<IPEndPoint, Node> _Nodes = new Dictionary<IPEndPoint, Node>();
		public Node GetNodeByEndpoint(IPEndPoint endpoint)
		{
			lock(_Nodes)
			{
				endpoint = Utils.EnsureIPv6(endpoint);

				Node result = null;
				if(_Nodes.TryGetValue(endpoint, out result))
					return result;
				var peer = PeerTable.GetPeer(endpoint);
				if(peer == null)
					peer = new Peer(PeerOrigin.Manual, new NetworkAddress()
					{

						Endpoint = endpoint,
						Time = Utils.UnixTimeToDateTime(0)
					});

				return AddNode(peer);
			}
		}
		private Node GetNodeByPeer(Peer peer)
		{
			lock(_Nodes)
			{
				Node result = null;
				if(_Nodes.TryGetValue(peer.NetworkAddress.Endpoint, out result))
					return result;
			}
			return AddNode(peer);

		}

		private Node AddNode(Peer peer)
		{
			try
			{
				var node = new Node(peer, this);
				return AddNode(node);
			}
			catch(Exception)
			{
				return null;
			}
		}

		private Node AddNode(Node node)
		{
			lock(_Nodes)
			{
				if(node.State < NodeState.Connected)
					return null;
				node.MessageProducer.AddMessageListener(_MessageListener);
				_Nodes.Add(node.Peer.NetworkAddress.Endpoint, node);
			}
			return node;
		}

		internal void RemoveNode(Node node)
		{
			lock(_Nodes)
			{
				if(_Nodes.Remove(node.Peer.NetworkAddress.Endpoint))
				{
					node.MessageProducer.RemoveMessageListener(_MessageListener);
				}
			}
		}
		void ProcessMessage(IncomingMessage message)
		{
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
			bool isAdvertised = message.Node != null && message.Node.State == NodeState.HandShaked;
			if(isAdvertised)
			{
				if(message.Message.Payload is AddrPayload)
				{
					PeerTable.UpdatePeers(message.AssertPayload<AddrPayload>().Addresses.Select(a => new Peer(PeerOrigin.Addr, a)));
				}
				if(message.Message.Payload is GetAddrPayload)
				{
					var existingPeers = PeerTable.GetActivePeers(1000)
							 .Where(p => p.Origin != PeerOrigin.DNSSeed && p.Origin != PeerOrigin.HardSeed)
							 .Select(p => p.NetworkAddress).ToArray();
					message.Node.SendMessage(new AddrPayload(existingPeers));
				}
			}

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

					if(IsConnectedTo(remoteEndpoint))
					{
						NodeServerTrace.Warning("Ongoing connection with " + remoteEndpoint + " detected, aborting the incoming connection attempt");
						Utils.SafeCloseSocket(message.Socket);
						return;
					}

					var node = new Node(new Peer(PeerOrigin.Advertised, new NetworkAddress()
					{
						Endpoint = remoteEndpoint,
						Time = DateTimeOffset.UtcNow
					}), this, message.Socket, version);

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
						node.RespondToHandShake(cancel.Token);
						AddNode(node);
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


		}

		public bool IsConnectedTo(IPEndPoint endpoint)
		{
			lock(_Nodes)
			{
				return _Nodes.ContainsKey(endpoint);
			}
		}


		#region IDisposable Members

		bool isDisposed;
		public void Dispose()
		{
			if(!isDisposed)
			{
				isDisposed = true;

				_MessageListener.Dispose();
				Task[] tasks = null;
				lock(_Nodes)
				{
					tasks = _Nodes.Values.ToArray().Select(n => Task.Factory.StartNew(() => n.Disconnect())).ToArray();
				}
				try
				{
					Task.WaitAll(tasks);
				}
				finally
				{
					if(_UPnPLease != null)
					{
						_UPnPLease.Dispose();
					}
					if(socket != null)
					{
						Utils.SafeCloseSocket(socket);
						socket = null;
					}
				}
			}
		}

		#endregion

		public VersionPayload CreateVersionPayload(Peer peer, IPEndPoint me, ProtocolVersion? version)
		{
			if(!me.Address.IsRoutable(AllowLocalPeers))
			{
				me = new IPEndPoint(ExternalEndpoint.Address, me.Port);
			}
			me = Utils.EnsureIPv6(me);
			return new VersionPayload()
					{
						Nonce = Nonce,
						UserAgent = UserAgent,
						Version = version == null ? Version : version.Value,
						StartHeight = 0,
						Timestamp = DateTimeOffset.UtcNow,
						AddressReciever = peer.NetworkAddress.Endpoint,
						AddressFrom = me
					};
		}

		string _UserAgent;
		public string UserAgent
		{
			get
			{
				if(_UserAgent == null)
				{
					var version = this.GetType().Assembly.GetName().Version;
					_UserAgent = "/NBitcoin:" + version.Major + "." + version.MajorRevision + "." + version.Minor + "/";
				}
				return _UserAgent;
			}
		}

		static Random _RandNonce = new Random();
		ulong _Nonce;
		public ulong Nonce
		{
			get
			{
				if(_Nonce == 0)
				{
					lock(_RandNonce)
					{
						var bytes = new byte[8];
						_RandNonce.NextBytes(bytes);
						_Nonce = BitConverter.ToUInt64(bytes, 0);
					}
				}
				return _Nonce;
			}
			set
			{
				_Nonce = value;
			}
		}


		public int CountPeerRequired()
		{
			return Math.Max(0, 990 - PeerTable.CountUsed(true));
		}

		public void DiscoverNodes()
		{
			TraceCorrelation traceCorrelation = new TraceCorrelation(NodeServerTrace.Trace, "Discovering nodes");
			List<Task> tasks = new List<Task>();
			using(traceCorrelation.Open())
			{
				int simultaneous = 20;
				while(CountPeerRequired() != 0)
				{
					NodeServerTrace.PeerTableRemainingPeerToGet(CountPeerRequired());
					CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(40));
					var peers = PeerTable.GetActivePeers(simultaneous);
					if(peers.Length == 0)
					{
						PopulateTableWithDNSNodes();
						PopulateTableWithHardNodes();
						peers = PeerTable.GetActivePeers(simultaneous);
					}


					tasks.AddRange(peers
						.Select(p => Task.Factory.StartNew(() =>
						{
							var n = GetNodeByPeer(p);
							if(n == null)
								return;
							try
							{
								if(n.State < NodeState.HandShaked)
									n.VersionHandshake(cancellation.Token);
								n.SendMessage(new GetAddrPayload());
								while(!cancellation.IsCancellationRequested)
								{
									n.RecieveMessage<AddrPayload>(cancellation.Token);
								}
							}
							finally
							{
								n.Disconnect();
							}
						}, cancellation.Token)).ToArray());

					while(CountPeerRequired() != 0)
					{
						if(cancellation.IsCancellationRequested)
							break;
						Thread.Sleep(2000);
						NodeServerTrace.PeerTableRemainingPeerToGet(CountPeerRequired());
					}
					if(!cancellation.IsCancellationRequested)
						cancellation.Cancel();
				}
				NodeServerTrace.Trace.TraceInformation("Peer table is now full");
				try
				{
					Task.WaitAll(tasks.ToArray());
				}
				catch(AggregateException ex)
				{
					if(!ex.InnerExceptions.All(i => i is OperationCanceledException))
						throw;
				}
			}
		}






	}
}
