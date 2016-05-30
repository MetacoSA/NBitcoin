#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Behaviors
{
	/// <summary>
	/// The AddressManagerBehavior class will respond to getaddr and register advertised nodes from addr messages to the AddressManager.
	/// The AddressManagerBehavior will also receive feedback about connection attempt and success of discovered peers to the AddressManager, so it can be used later to find valid peer faster.
	/// </summary>
	public class AddressManagerBehavior : NodeBehavior
	{
		public static AddressManager GetAddrman(Node node)
		{
			return GetAddrman(node.Behaviors);
		}
		public static AddressManager GetAddrman(NodeConnectionParameters parameters)
		{
			if(parameters == null)
				throw new ArgumentNullException("parameters");
			return GetAddrman(parameters.TemplateBehaviors);
		}

		public static AddressManager GetAddrman(NodeBehaviorsCollection behaviors)
		{
			if(behaviors == null)
				throw new ArgumentNullException("behaviors");
			var behavior = behaviors.Find<AddressManagerBehavior>();
			if(behavior == null)
				return null;
			return behavior.AddressManager;
		}
		public static void SetAddrman(Node node, AddressManager addrman)
		{
			if(node == null)
				throw new ArgumentNullException("node");
			SetAddrman(node.Behaviors, addrman);
		}

		public static void SetAddrman(NodeConnectionParameters parameters, AddressManager addrman)
		{
			if(parameters == null)
				throw new ArgumentNullException("parameters");
			SetAddrman(parameters.TemplateBehaviors, addrman);
		}

		public static void SetAddrman(NodeBehaviorsCollection behaviors, AddressManager addrman)
		{
			if(behaviors == null)
				throw new ArgumentNullException("behaviors");
			var behavior = behaviors.Find<AddressManagerBehavior>();
			if(behavior == null)
			{
				// FIXME: Please take a look at this
				behavior = new AddressManagerBehavior(addrman);
				behaviors.Add(behavior);
			}
			behavior.AddressManager = addrman;
		}

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
				node.SendMessageAsync(new AddrPayload(AddressManager.GetAddr().Take(1000).ToArray()));
			}
			var addr = message.Message.Payload as AddrPayload;
			if(addr != null)
			{
				AddressManager.Add(addr.Addresses, node.RemoteSocketAddress);
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

		#region ICloneable Members

		public override object Clone()
		{
			return new AddressManagerBehavior(AddressManager);
		}

		#endregion
	}
}
#endif