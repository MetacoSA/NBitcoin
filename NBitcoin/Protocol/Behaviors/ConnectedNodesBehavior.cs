#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Behaviors
{
	/// <summary>
	/// Maintain connection to a given set of nodes
	/// </summary>
	public class ConnectedNodesBehavior : NodeBehavior, ICloneable
	{
		NodeConnectionParameters _ConnectionParameters;
		NodeRequirement _Requirements;
		CancellationTokenSource _Disconnect;
		Network _Network;
		object cs;

		public ConnectedNodesBehavior(
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
			_Requirements = requirements ?? new NodeRequirement();
			_Disconnect = new CancellationTokenSource();
		}
		ConnectedNodesBehavior()
		{
		}

		#region ICloneable Members

		public object Clone()
		{
			var clone = new ConnectedNodesBehavior();
			clone._ConnectedNodes = _ConnectedNodes;
			clone._ConnectionParameters = _ConnectionParameters;
			clone.MaximumNodeConnection = MaximumNodeConnection;
			clone._Requirements = _Requirements;
			clone._Network = _Network;
			clone.cs = cs;
			clone._Disconnect = _Disconnect;
			clone.AllowSameGroup = AllowSameGroup;
			return clone;
		}

		#endregion

		public int MaximumNodeConnection
		{
			get;
			set;
		}

		private NodesCollection _ConnectedNodes;
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


		protected override void AttachCore()
		{
			AttachedNode.StateChanged += AttachedNode_StateChanged;
		}

		void AttachedNode_StateChanged(Node node, NodeState oldState)
		{
			if(node.State == NodeState.Failed || node.State == NodeState.Disconnecting || node.State == NodeState.Offline)
			{
				_ConnectedNodes.Remove(AttachedNode);
				StartConnecting();
			}
		}

		protected override void DetachCore()
		{
			AttachedNode.StateChanged -= AttachedNode_StateChanged;
		}

		AddressManager _TempAddressManager;
		private void StartConnecting()
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
							var me = parameters.TemplateBehaviors.Find<ConnectedNodesBehavior>();
							if(me == null)
							{
								parameters.TemplateBehaviors.Add(this);
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

		void node_StateChanged(Node node, NodeState oldState)
		{
			if(node.State == NodeState.Failed || node.State == NodeState.Disconnecting || node.State == NodeState.Offline)
			{
				_ConnectedNodes.Remove(node);
				StartConnecting();
			}
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
	}
}
#endif