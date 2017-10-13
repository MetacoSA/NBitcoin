#if !NOSOCKET
using NBitcoin.Protocol.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class WellKnownGroupSelectors
	{
		static Random _Rand = new Random();
		static Func<IPEndPoint, byte[]> _GroupByRandom;
		public static Func<IPEndPoint, byte[]> ByRandom
		{
			get
			{
				return _GroupByRandom = _GroupByRandom ?? new Func<IPEndPoint, byte[]>((ip) =>{

					var group = new byte[20];
					_Rand.NextBytes(group);
					return group;
				});
			}
		}


		static Func<IPEndPoint, byte[]> _GroupByIp;
		public static Func<IPEndPoint, byte[]> ByIp
		{
			get
			{
				return _GroupByIp = _GroupByIp ?? new Func<IPEndPoint, byte[]>((ip) => {
					return ip.Address.GetAddressBytes();	
				});
			}
		}

		static Func<IPEndPoint, byte[]> _GroupByEndpoint;
		public static Func<IPEndPoint, byte[]> ByEndpoint
		{
			get
			{
				return _GroupByEndpoint = _GroupByEndpoint ?? new Func<IPEndPoint, byte[]>((endpoint) => {
					var bytes = endpoint.Address.GetAddressBytes();
					var port = Utils.ToBytes((uint)endpoint.Port, true);
					var result = new byte[bytes.Length + port.Length];
					Array.Copy(bytes, result, bytes.Length);
					Array.Copy(port, 0, result, bytes.Length, port.Length);
					return bytes;
				});
			}
		}

		static Func<IPEndPoint, byte[]> _GroupByNetwork;
		public static Func<IPEndPoint, byte[]> ByNetwork
		{
			get
			{
				return _GroupByNetwork = _GroupByNetwork ?? new Func<IPEndPoint, byte[]>((ip) => {
					return IpExtensions.GetGroup(ip.Address);
				});
			}
		}
	}

	public class RelatedNodesGroups:Dictionary<string, NodesGroup>
	{
		public void Register(string name, NodesGroup nodeGroup)
		{
			if (nodeGroup != null)
			{ 
				this.Add(name, nodeGroup);
				nodeGroup.RelatedGroups = this;
			}
		}

		public IPEndPoint[] GlobalConnectedNodes()
		{
			IPEndPoint[] all = new IPEndPoint[0];
			foreach (var kv in this)
			{
				var endPoints = kv.Value._ConnectedNodes.Select(n => n.RemoteSocketEndpoint).ToArray<IPEndPoint>();
				all = all.Union<IPEndPoint>(endPoints).ToArray<IPEndPoint>();
			}

			return all;
		}
	}

	public class NodesGroup : IDisposable
	{
		TraceCorrelation _Trace = new TraceCorrelation(NodeServerTrace.Trace, "Group connection");
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

		public RelatedNodesGroups RelatedGroups { get; set; }
		NodeRequirement _Requirements;
		CancellationTokenSource _Disconnect;
		Network _Network;
		public string Name
		{
			get
			{
				return this.RelatedGroups?.Where(x => x.Value == this).Select(x => x.Key).FirstOrDefault();
			}
		}
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
			if(_Disconnect.IsCancellationRequested)
				return;
			if(_ConnectedNodes.Count >= MaximumNodeConnection)
				return;
			if(_Connecting)
				return;
			Task.Factory.StartNew(() =>
			{
				if(Monitor.TryEnter(cs))
				{
					_Connecting = true;
					TraceCorrelationScope scope = null;
					try
					{
						while(!_Disconnect.IsCancellationRequested && _ConnectedNodes.Count < MaximumNodeConnection)
						{
							scope = scope ?? _Trace.Open();

							NodeServerTrace.Information("Connected nodes : " + _ConnectedNodes.Count + "/" + MaximumNodeConnection);
							var parameters = _ConnectionParameters.Clone();
							parameters.TemplateBehaviors.Add(new NodesGroupBehavior(this));
							parameters.ConnectCancellation = _Disconnect.Token;
							var addrman = AddressManagerBehavior.GetAddrman(parameters);

							if(addrman == null)
							{
								addrman = _DefaultAddressManager;
								AddressManagerBehavior.SetAddrman(parameters, addrman);
							}

							Node node = null;
							try
							{
								var groupSelector = CustomGroupSelector != null ? CustomGroupSelector :
													AllowSameGroup ? WellKnownGroupSelectors.ByRandom : null;

								if (this.RelatedGroups == null)
									node = Node.Connect(_Network, parameters, this._ConnectedNodes.Select(n => n.RemoteSocketEndpoint).ToArray(), groupSelector);
								else
									node = Node.Connect(_Network, parameters, this.RelatedGroups.GlobalConnectedNodes, groupSelector);

								var timeout = CancellationTokenSource.CreateLinkedTokenSource(_Disconnect.Token);
								timeout.CancelAfter(5000);
								node.VersionHandshake(_Requirements, timeout.Token);
								NodeServerTrace.Information("Node successfully connected to and handshaked");
							}
							catch(OperationCanceledException ex)
							{
								if(_Disconnect.Token.IsCancellationRequested)
									break;
								NodeServerTrace.Error("Timeout for picked node", ex);
								if(node != null)
									node.DisconnectAsync("Handshake timeout", ex);
							}
							catch(Exception ex)
							{
								NodeServerTrace.Error("Error while connecting to node", ex);
								if(node != null)
									node.DisconnectAsync("Error while connecting", ex);
							}

						}
					}
					finally
					{
						Monitor.Exit(cs);
						_Connecting = false;
						if(scope != null)
							scope.Dispose();
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
				while(!_Disconnect.IsCancellationRequested && initialNodes.Count != 0)
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
		public Func<IPEndPoint, byte[]> CustomGroupSelector
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
		}

		#endregion
	}
}
#endif