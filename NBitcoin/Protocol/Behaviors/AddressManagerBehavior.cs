#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Behaviors
{
	[Flags]
	public enum AddressManagerBehaviorMode
	{
		/// <summary>
		/// Do not advertise or discover new peers
		/// </summary>
		None = 0,
		/// <summary>
		/// Only advertise known peers
		/// </summary>
		Advertize = 1,
		/// <summary>
		/// Only discover peers
		/// </summary>
		Discover = 2,
		/// <summary>
		/// Advertise known peer and discover peer
		/// </summary>
		AdvertizeDiscover = 3,
	}

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
			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));
			return GetAddrman(parameters.TemplateBehaviors);
		}

		public static AddressManager GetAddrman(NodeBehaviorsCollection behaviors)
		{
			if (behaviors == null)
				throw new ArgumentNullException(nameof(behaviors));
			var behavior = behaviors.Find<AddressManagerBehavior>();
			if (behavior == null)
				return null;
			return behavior.AddressManager;
		}
		public static void SetAddrman(Node node, AddressManager addrman)
		{
			if (node == null)
				throw new ArgumentNullException(nameof(node));
			SetAddrman(node.Behaviors, addrman);
		}

		public static void SetAddrman(NodeConnectionParameters parameters, AddressManager addrman)
		{
			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));
			SetAddrman(parameters.TemplateBehaviors, addrman);
		}


		int _PeersToDiscover = 1000;
		/// <summary>
		/// The minimum number of peers to discover before trying to connect to a node using the AddressManager (Default: 1000)
		/// </summary>
		public int PeersToDiscover
		{
			get
			{
				return _PeersToDiscover;
			}
			set
			{
				_PeersToDiscover = value;
			}
		}

		public static void SetAddrman(NodeBehaviorsCollection behaviors, AddressManager addrman)
		{
			if (behaviors == null)
				throw new ArgumentNullException(nameof(behaviors));
			var behavior = behaviors.Find<AddressManagerBehavior>();
			if (behavior == null)
			{
				// FIXME: Please take a look at this
				behavior = new AddressManagerBehavior(addrman);
				behaviors.Add(behavior);
			}
			behavior.AddressManager = addrman;
		}

		public AddressManagerBehavior(AddressManager manager)
		{
			if (manager == null)
				throw new ArgumentNullException(nameof(manager));
			_AddressManager = manager;
			Mode = AddressManagerBehaviorMode.AdvertizeDiscover;
		}

		public AddressManagerBehaviorMode Mode
		{
			get;
			set;
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
				if (value == null)
					throw new ArgumentNullException(nameof(value));
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
			var payload = message.Message.Payload; 
			if (payload is SendAddrV2Payload)
			{
				node.PreferAddressV2 = true;
				return;
			}
			
			if ((Mode & AddressManagerBehaviorMode.Advertize) != 0)
			{
				if (payload is GetAddrPayload)
				{
					var addressesToSend = AddressManager.GetAddr()
						.Where(a => a.Endpoint.IsValid())
						.Where(a => node.PreferAddressV2 || a.IsAddrV1Compatible)
						.Take(1000)
						.ToArray();

					node.SendMessageAsync(node.PreferAddressV2
						? new AddrV2Payload(addressesToSend)
						: new AddrPayload(addressesToSend));
				}
			}

			if ((Mode & AddressManagerBehaviorMode.Discover) != 0)
			{
				if (payload is AddrPayload addr)
				{
					AddressManager.Add(addr.Addresses, node.RemoteSocketAddress);
				}
			}
		}

		void AttachedNode_StateChanged(Node node, NodeState oldState)
		{
			if ((Mode & AddressManagerBehaviorMode.Discover) != 0)
			{
				if (node.State <= NodeState.Disconnecting && oldState == NodeState.HandShaked)
					AddressManager.Connected(node.Peer);
				if (node.State == NodeState.HandShaked)
					AddressManager.Good(node.Peer);
			}
		}

		protected override void DetachCore()
		{
			AttachedNode.StateChanged -= AttachedNode_StateChanged;
		}

		#region ICloneable Members

		public override object Clone()
		{
			return new AddressManagerBehavior(AddressManager)
			{
				PeersToDiscover = PeersToDiscover,
				Mode = Mode
			};
		}

		internal void DiscoverPeers(Network network, NodeConnectionParameters parameters)
		{
			if (Mode.HasFlag(AddressManagerBehaviorMode.Discover))
				AddressManager.DiscoverPeers(network, parameters, PeersToDiscover);
		}

		#endregion
	}
}
#endif