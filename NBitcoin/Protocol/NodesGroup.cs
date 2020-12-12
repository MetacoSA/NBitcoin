#if !NOSOCKET
using NBitcoin.Protocol.Behaviors;
using System;
using System.Text;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin.Logging;

namespace NBitcoin.Protocol
{
	public class WellKnownGroupSelectors
	{
		static Random _Rand = new Random();
		static Func<EndPoint, byte[]> _GroupByRandom;
		public static Func<EndPoint, byte[]> ByRandom
		{
			get
			{
				return _GroupByRandom = _GroupByRandom ?? new Func<EndPoint, byte[]>((ip) =>
				{
					var group = new byte[20];
					_Rand.NextBytes(group);
					return group;
				});
			}
		}


		static Func<EndPoint, byte[]> _GroupByIp;
		public static Func<EndPoint, byte[]> ByIp
		{
			get
			{
				return _GroupByIp = _GroupByIp ?? new Func<EndPoint, byte[]>((endpoint) =>
				{
					if (endpoint is IPEndPoint ipEndPoint)
						return ipEndPoint.Address.GetAddressBytes();

					if (endpoint is DnsEndPoint dnsEndPoint)
					{
						if (dnsEndPoint.IsTor()) // All Tor endpoints are grouped together
							return Encoding.UTF8.GetBytes("onion address");

						if (dnsEndPoint.IsI2P()) // All I2P endpoints are grouped together
							return Encoding.UTF8.GetBytes("i2p address");
					}

					return Encoding.UTF8.GetBytes("unknown address");
				});
			}
		}

		static Func<EndPoint, byte[]> _GroupByEndpoint;
		public static Func<EndPoint, byte[]> ByEndpoint
		{
			get
			{
				return _GroupByEndpoint = _GroupByEndpoint ?? new Func<EndPoint, byte[]>((endpoint) =>
				{
					var netaddr = new NetworkAddress(endpoint);
					return netaddr.GetKey();
				});
			}
		}

		static Func<EndPoint, byte[]> _GroupByNetwork;
		public static Func<EndPoint, byte[]> ByNetwork
		{
			get
			{
				return _GroupByNetwork = _GroupByNetwork ?? new Func<EndPoint, byte[]>((address) =>
				{
					return address.GetGroup();
				});
			}
		}
	}
	public class NodesGroup : IDisposable
	{
		IDisposable _logScope = Logs.NodeServer.BeginScope("Group connection");

		NodeConnectionParameters _ConnectionParameters;
		public NodeConnectionParameters NodeConnectionParameters
		{
			get
			{
				return _ConnectionParameters;
			}
			set
			{
				_ConnectionParameters = value;
			}
		}

		NodeRequirement _Requirements;
		CancellationTokenSource _Disconnect;
		Network _Network;
		object cs;

		public NodesGroup(
			Network network,
			NodeConnectionParameters connectionParameters = null,
			NodeRequirement requirements = null)
		{
			AllowSameGroup = false;
			MaximumNodeConnection = 8;
			_Network = network;
			cs = new object();
			_ConnectedNodes = new NodesCollection();
			_ConnectionParameters = connectionParameters ?? new NodeConnectionParameters();
			_ConnectionParameters = _ConnectionParameters.Clone();
			_Requirements = requirements ?? new NodeRequirement();
			_Disconnect = new CancellationTokenSource();
		}

		/// <summary>
		/// Start connecting asynchronously to remote peers
		/// </summary>
		public void Connect()
		{
			_Disconnect = new CancellationTokenSource();
			StartConnecting();
		}
		/// <summary>
		/// Drop connection to all connected nodes
		/// </summary>
		public void Disconnect()
		{
			_Disconnect.Cancel();
			_ConnectedNodes.DisconnectAll();
		}

		AddressManager _DefaultAddressManager = new AddressManager();
		volatile bool _Connecting;

		internal void StartConnecting()
		{
			if (_Disconnect.IsCancellationRequested)
				return;
			if (_ConnectedNodes.Count >= MaximumNodeConnection)
				return;
			if (_Connecting)
				return;
			Task.Factory.StartNew(() =>
			{
				if (Monitor.TryEnter(cs))
				{
					_Connecting = true;
					try
					{
						while (!_Disconnect.IsCancellationRequested && _ConnectedNodes.Count < MaximumNodeConnection)
						{
							Logs.NodeServer.LogInformation("Connected nodes {connectedNodeCount} / {maximumNodeCount} ", _ConnectedNodes.Count, MaximumNodeConnection);
							var parameters = _ConnectionParameters.Clone();
							parameters.TemplateBehaviors.Add(new NodesGroupBehavior(this));
							parameters.ConnectCancellation = _Disconnect.Token;
							var addrman = AddressManagerBehavior.GetAddrman(parameters);

							if (addrman == null)
							{
								addrman = _DefaultAddressManager;
								AddressManagerBehavior.SetAddrman(parameters, addrman);
							}

							Node node = null;
							try
							{
								var groupSelector = CustomGroupSelector != null ? CustomGroupSelector :
									AllowSameGroup ? WellKnownGroupSelectors.ByRandom : null;

								var connectedPeers = _ConnectedNodes.Where(n => n.Peer is { }).Select(n => n.Peer.Endpoint).ToArray();
								node = Node.Connect(_Network, parameters, connectedPeers, groupSelector);
								using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(_Disconnect.Token))
								{
									timeout.CancelAfter(5000);
									node.VersionHandshake(_Requirements, timeout.Token);
									Logs.NodeServer.LogInformation("Node successfully connected to and handshaked");
								}
							}
							catch (OperationCanceledException ex)
							{
								if (_Disconnect.Token.IsCancellationRequested)
									break;
								Logs.NodeServer.LogError(default, ex, "Timeout for picked node");
								if (node != null)
									node.DisconnectAsync("Handshake timeout", ex);
							}
							catch (Exception ex)
							{
								Logs.NodeServer.LogError(default, ex, "Error while connecting to node");
								if (node != null)
									node.DisconnectAsync("Error while connecting", ex);
							}

						}
					}
					finally
					{
						Monitor.Exit(cs);
						_Connecting = false;
					}
				}
			}, TaskCreationOptions.LongRunning);
		}


		public static NodesGroup GetNodeGroup(Node node)
		{
			return GetNodeGroup(node.Behaviors);
		}
		public static NodesGroup GetNodeGroup(NodeConnectionParameters parameters)
		{
			return GetNodeGroup(parameters.TemplateBehaviors);
		}
		public static NodesGroup GetNodeGroup(NodeBehaviorsCollection behaviors)
		{
			return behaviors.OfType<NodesGroupBehavior>().Select(c => c._Parent).FirstOrDefault();
		}

		/// <summary>
		/// Asynchronously create a new set of nodes
		/// </summary>
		public void Purge(string reason)
		{
			Task.Factory.StartNew(() =>
			{
				var initialNodes = _ConnectedNodes.ToDictionary(n => n);
				while (!_Disconnect.IsCancellationRequested && initialNodes.Count != 0)
				{
					var node = initialNodes.First();
					node.Value.Disconnect(reason);
					initialNodes.Remove(node.Value);
					_Disconnect.Token.WaitHandle.WaitOne(5000);
				}
			});
		}

		/// <summary>
		/// The number of node that this behavior will try to maintain online (Default : 8)
		/// </summary>
		public int MaximumNodeConnection
		{
			get;
			set;
		}

		public NodeRequirement Requirements
		{
			get
			{
				return _Requirements;
			}
			set
			{
				_Requirements = value;
			}
		}

		internal NodesCollection _ConnectedNodes;
		public NodesCollection ConnectedNodes
		{
			get
			{
				return _ConnectedNodes;
			}
		}

		/// <summary>
		/// If false, the search process will do its best to connect to Node in different network group to prevent sybil attacks. (Default : false)
		/// If CustomGroupSelector is set, AllowSameGroup is ignored.
		/// </summary>
		public bool AllowSameGroup
		{
			get;
			set;
		}

		/// <summary>
		/// How to calculate a group of an ip, by default using NBitcoin.IpExtensions.GetGroup.
		/// Overrides AllowSameGroup.
		/// </summary>
		public Func<EndPoint, byte[]> CustomGroupSelector
		{
			get; set;
		}

		#region IDisposable Members


		/// <summary>
		/// Same as Disconnect
		/// </summary>
		public void Dispose()
		{
			Disconnect();
			_logScope.Dispose();

		}

		#endregion
	}
}
#endif
