using NBitcoin.Protocol.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class NodesGroup
	{
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
			_ConnectionParameters.TemplateBehaviors.Add(CreateBehavior());
		}

		public void Connect()
		{
			StartConnecting();
		}
		public void Disconnect()
		{
			_Disconnect.Cancel();
			_ConnectedNodes.DisconnectAll();
		}

		AddressManager _TempAddressManager;
		internal void StartConnecting()
		{
			if(_Disconnect.IsCancellationRequested)
				return;
			if(_ConnectedNodes.Count >= MaximumNodeConnection)
				return;
			Task.Factory.StartNew(() =>
			{
				if(Monitor.TryEnter(cs))
				{
					try
					{
						while(!_Disconnect.IsCancellationRequested)
						{
							if(_ConnectedNodes.Count >= MaximumNodeConnection)
							{
								break;
							}

							var parameters = _ConnectionParameters.Clone();
							var addrman = parameters.TemplateBehaviors.Find<AddressManagerBehavior>();
							if(addrman == null)
							{
								_TempAddressManager = _TempAddressManager ?? new AddressManager();
								parameters.TemplateBehaviors.Add(new AddressManagerBehavior(_TempAddressManager));
							}
							Node node = null;
							try
							{
								node = Node.Connect(_Network, parameters, AllowSameGroup ? null : _ConnectedNodes.Select(n => n.RemoteSocketAddress).ToArray());
								var timeout = CancellationTokenSource.CreateLinkedTokenSource(_Disconnect.Token);
								timeout.CancelAfter(5000);
								node.VersionHandshake(_Requirements, timeout.Token);
								if(node.State == NodeState.HandShaked)
								{
									node.StateChanged += node_StateChanged;
									_ConnectedNodes.Add(node);
								}
							}
							catch(OperationCanceledException)
							{
								if(_Disconnect.Token.IsCancellationRequested)
									throw;
							}
							catch
							{
							}
						}
					}
					finally
					{
						Monitor.Exit(cs);
					}
				}
			});
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
		internal void Purge(string reason)
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
		/// If false, the search process will do its best to connect to Node in different network group to prevent sybil attacks (Default : false)
		/// </summary>
		public bool AllowSameGroup
		{
			get;
			set;
		}



	
		void node_StateChanged(Node node, NodeState oldState)
		{
			if(node.State == NodeState.Failed || node.State == NodeState.Disconnecting || node.State == NodeState.Offline)
			{
				_ConnectedNodes.Remove(node);
				StartConnecting();
			}
		}
		

		NodesGroupBehavior CreateBehavior()
		{
			return new NodesGroupBehavior(this);
		}

	}
}
