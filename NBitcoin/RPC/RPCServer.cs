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

namespace NBitcoin.RPC
{
	public class RPCServer
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
		private readonly int _Port;
		public int Port
		{
			get
			{
				return _Port;
			}
		}
		public RPCServer(Network network, ProtocolVersion version = ProtocolVersion.PROTOCOL_VERSION, int port = -1)
		{
			_Port = port == -1 ? network.DefaultPort : port;
			_Network = network;
			_Version = version;
			_HardCodedNodes = network.SeedNodes.Select(n => n.ToNode(this)).ToArray();
		}

		public Node[] GetDNSNodes(CancellationToken cancellationToken = default(CancellationToken))
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
									RPCTrace.ErrorWhileRetrievingDNSSeedIp(s.Name, ex);
									return new IPAddress[0];
								}
							})
							.Select(s => new Node(new NetworkAddress()
							{
								Endpoint = new IPEndPoint(s, Network.DefaultPort),
								Time = Utils.UnixTimeToDateTime(0)
							}, this)).ToArray();

			_DNSNodes = nodes;
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
		TraceCorrelation listenerTrace;

		
		public void Listen()
		{
			if(_LocalEndpoint == null)
			{

				listenerTrace = new TraceCorrelation();
				RPCTrace.Trace.TraceTransfer(0, "transfer", listenerTrace.Activity);
				using(listenerTrace.Open())
				{
					RPCTrace.Trace.TraceEvent(TraceEventType.Start, 0, "RPC server listening");
					try
					{
						socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
						socket.Bind(new IPEndPoint(IPAddress.Parse("0.0.0.0"), Port));
						socket.Listen(8);
						RPCTrace.Information("Listening on " + socket.LocalEndPoint);
						BeginAccept();
					}
					catch(Exception ex)
					{
						RPCTrace.Error("Error while opening the RPC server", ex);
						throw;
					}
				}
			}
		}

		private void BeginAccept()
		{
			RPCTrace.Information("Accepting connection...");
			socket.BeginAccept(EndAccept, null);
		}
		private void EndAccept(IAsyncResult ar)
		{
			using(listenerTrace.Open())
			{
				try
				{
					var client = socket.EndAccept(ar);
					RPCTrace.Information("Client connection accepted : " + client.RemoteEndPoint);
					//Trace.CorrelationManager.ActivityId
					//RPCTrace.Information("New client connected");
				}
				catch(Exception ex)
				{
					RPCTrace.Error("Error while accepting connection ", ex);
					Thread.Sleep(3000);
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
							 RPCTrace.Warning("can't resolve ip of " + site.DNS + " using hardcoded one " + site.IP, ex);
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
				RPCTrace.ExternalIpRecieved(result);
				return IPAddress.Parse(result);
			}
			catch(InvalidOperationException)
			{
				RPCTrace.ExternalIpFailed(tasks.Select(t => t.Exception).FirstOrDefault());
				throw new WebException("Impossible to detect extenal ip");
			}
		}
	}
}
