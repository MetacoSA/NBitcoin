using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
			AttachedNode.MessageReceived += AttachedNode_MessageReceived;
		}

		void AttachedNode_MessageReceived(Node node, IncomingMessage message)
		{
			var getaddr = message.Message.Payload as GetAddrPayload;
			if(getaddr != null)
			{
				node.SendMessage(new AddrPayload(AddressManager.GetAddr().Take(1000).ToArray()));
			}
			var addr = message.Message.Payload as AddrPayload;
			if(addr != null)
			{
				AddressManager.Add(addr.Addresses, (IPAddress)((System.Net.IPEndPoint)node.Socket.RemoteEndPoint).Address);
			}
		}

		void AttachedNode_StateChanged(Node node, NodeState oldState)
		{
			if(node.State <= NodeState.Disconnecting && oldState == NodeState.HandShaked)
				AddressManager.Connected(node.Peer);
			if(node.State == NodeState.HandShaked)
				AddressManager.Good(node.Peer);
		}

		protected override void DetachCore()
		{
			AttachedNode.StateChanged -= AttachedNode_StateChanged;
		}
	}
}
