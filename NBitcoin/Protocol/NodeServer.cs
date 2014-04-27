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
			int internalPort = -1,
			int externalPort = -1)
		{
			internalPort = internalPort == -1 ? network.DefaultPort : internalPort;
			externalPort = externalPort == -1 ? internalPort : externalPort;
			_LocalEndpoint = new IPEndPoint(IPAddress.Parse("0.0.0.0").MapToIPv6(), internalPort);
			_ExternalEndpoint = new IPEndPoint(_LocalEndpoint.Address, externalPort);
			_Network = network;
			_Version = version;
			_HardCodedNodes = network.SeedNodes.Select(n => new Node(n, this, NodeOrigin.HardSeed)).ToArray();
			_MessageListener = new EventLoopMessageListener<IncomingMessage>(ProcessMessage);
			_MessageProducer.AddMessageListener(_MessageListener);
		}

		private readonly EventLoopMessageListener<IncomingMessage> _MessageListener;

		private readonly NodeRepository _NodeRepository = new NodeRepository();
		public NodeRepository NodeRepository
		{
			get
			{
				return _NodeRepository;
			}
		}

		public Node[] GetDNSNodes()
		{
			var nodes = Network.DNSSeeds
							.SelectMany(s =>
							{
								try
								{
									return s.GetAddressNodes();
								}
								catch(Exception ex)
								{
									ProtocolTrace.ErrorWhileRetrievingDNSSeedIp(s.Name, ex);
									return new IPAddress[0];
								}
							})
							.Select(s => new Node(new NetworkAddress()
							{
								Endpoint = new IPEndPoint(s, Network.DefaultPort),
								Time = Utils.UnixTimeToDateTime(0)
							}, this, NodeOrigin.DNSSeed)).ToArray();

			_DNSNodes = nodes;
			//Utils.Shuffle(_DNSNodes);
			return nodes;
		}

		//public Node[] DiscoverNodes()
		//{
		//	return null;
		//}

		//Node[] _DiscoveredNodes = new Node[0];
		//public Node[] DiscoveredNodes
		//{
		//	get
		//	{
		//		return _DiscoveredNodes;
		//	}
		//}

		Node[] _DNSNodes = new Node[0];
		public Node[] DNSNodes
		{
			get
			{
				return _DNSNodes;
			}
		}

		Node[] _HardCodedNodes = new Node[0];
		public Node[] HardCodedNodes
		{
			get
			{
				return _HardCodedNodes.ToArray();
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
		TraceCorrelation listenerTrace = new TraceCorrelation();


		public void Listen()
		{
			if(socket != null)
				throw new InvalidOperationException("Already listening");
			ProtocolTrace.Trace.TraceTransfer(0, "transfer", listenerTrace.Activity);
			using(listenerTrace.Open())
			{
				ProtocolTrace.Trace.TraceEvent(TraceEventType.Start, 0, "Protocol server trying to listen on " + LocalEndpoint);
				try
				{
					socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
					socket.Bind(LocalEndpoint);
					socket.Listen(8);
					ProtocolTrace.Information("Listening...");
					BeginAccept();
				}
				catch(Exception ex)
				{
					ProtocolTrace.Error("Error while opening the Protocol server", ex);
					throw;
				}
			}
		}

		private void BeginAccept()
		{
			if(isDisposed)
				return;
			ProtocolTrace.Information("Accepting connection...");
			socket.BeginAccept(EndAccept, null);
		}
		private void EndAccept(IAsyncResult ar)
		{
			using(listenerTrace.Open())
			{
				try
				{
					var client = socket.EndAccept(ar);
					if(isDisposed)
						return;
					ProtocolTrace.Information("Client connection accepted : " + client.RemoteEndPoint);
					//Trace.CorrelationManager.ActivityId
					//RPCTrace.Information("New client connected");
				}
				catch(Exception ex)
				{
					if(isDisposed)
						return;
					ProtocolTrace.Error("Error while accepting connection ", ex);
					Thread.Sleep(3000);
					BeginAccept();
				}
			}
		}

		public IPAddress GetMyExternalIP()
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
							 ProtocolTrace.Warning("can't resolve ip of " + site.DNS + " using hardcoded one " + site.IP, ex);
						 }
						 WebClient client = new WebClient();
						 var page = client.DownloadString("http://" + ip);
						 var match = Regex.Match(page, "[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}");
						 return match.Value;
					 });
			 }).ToArray();


			Task.WaitAny(tasks);
			try
			{
				var result = tasks.First(t => t.IsCompleted && !t.IsFaulted).Result;
				ProtocolTrace.ExternalIpRecieved(result);
				return IPAddress.Parse(result);
			}
			catch(InvalidOperationException)
			{
				ProtocolTrace.ExternalIpFailed(tasks.Select(t => t.Exception).FirstOrDefault());
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
		}

		bool trustedExternal = false;
		public void ChangeExternalAddress(IPAddress iPAddress, bool trusted)
		{
			if(trusted)
			{
				_ExternalEndpoint = new IPEndPoint(iPAddress, _ExternalEndpoint.Port);
				trustedExternal = true;
			}
			else
			{
				if(trustedExternal)
					return;
				_ExternalEndpoint = new IPEndPoint(iPAddress, _ExternalEndpoint.Port);
				trustedExternal = false;
			}
		}

		internal void ExternalAddressDetected(IPAddress iPAddress)
		{
			if(!ExternalEndpoint.Address.IsRoutable())
			{
				ProtocolTrace.Information("New externalAddress detected " + iPAddress);
				_ExternalEndpoint = new IPEndPoint(iPAddress, ExternalEndpoint.Port);
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

		Dictionary<IPEndPoint, Node> _Nodes = new Dictionary<IPEndPoint, Node>();
		private Node GetNodeByEndpoint(IPEndPoint endpoint)
		{
			if(endpoint.AddressFamily != AddressFamily.InterNetworkV6)
				endpoint = new IPEndPoint(endpoint.Address.MapToIPv6(), endpoint.Port);

			Node result = null;
			if(_Nodes.TryGetValue(endpoint, out result))
				return result;
			var address = NodeRepository.GetAddress(endpoint);
			if(address == null)
				address = new NetworkAddress()
				{
					Endpoint = endpoint,
					Time = Utils.UnixTimeToDateTime(0)
				};
			var node = new Node(address, this, NodeOrigin.Manually);
			node.MessageProducer.AddMessageListener(_MessageListener);
			lock(_Nodes)
			{
				_Nodes.Add(node.Endpoint, node);
			}
			return node;
		}

		void ProcessMessage(IncomingMessage message)
		{
			var trace = new TraceCorrelation();
			ProtocolTrace.Trace.TraceTransfer(0, "transfer", trace.Activity);
			using(trace.Open())
			{
				//ProtocolTrace.Trace.TraceEvent(TraceEventType.Start,0, "Processing inbound message
			}
		}

		#region IDisposable Members

		bool isDisposed;
		public void Dispose()
		{
			if(!isDisposed)
			{
				_MessageListener.Dispose();
				isDisposed = true;
			}
		}

		#endregion
	}
}
