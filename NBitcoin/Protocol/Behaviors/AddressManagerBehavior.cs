using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Behaviors
{
	public class AddressManagerBehavior : NodeBehavior
	{
		public AddressManagerBehavior(AddressManager manager)
		{
			if(manager == null)
				throw new ArgumentNullException("manager");
			_AddressManager = manager;
		}
		AddressManager _AddressManager;
		public AddressManager AddressManager
		{
			get
			{
				return _AddressManager;
			}
			set
			{
				AssertNotAttached();
				if(value == null)
					throw new ArgumentNullException("value");
				_AddressManager = value;
			}
		}
		protected override void AttachCore()
		{
			AttachedNode.StateChanged += AttachedNode_StateChanged;
		}

		void AttachedNode_StateChanged(Node node, NodeState oldState)
		{
			if(node.State == NodeState.Connected)
				AddressManager.Attempt(node.Peer.NetworkAddress);
			if(node.State <= NodeState.Disconnecting && oldState == NodeState.HandShaked)
				AddressManager.Connected(node.Peer.NetworkAddress);
			if(node.State == NodeState.HandShaked)
				AddressManager.Good(node.Peer.NetworkAddress);
		}

		protected override void DetachCore()
		{
			
		}
	}
}
