#if !NOSOCKET
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Behaviors
{
	public class NodeBehaviorsCollection : ThreadSafeCollection<INodeBehavior>
	{
		Node _Node;
		public NodeBehaviorsCollection(Node node)
		{
			_Node = node;
		}


		bool CanAttach
		{
			get
			{
				return _Node != null && !DelayAttach && _Node.State != NodeState.Offline && _Node.State != NodeState.Failed && _Node.State != NodeState.Disconnecting;
			}
		}

		protected override void OnAdding(INodeBehavior obj)
		{
			if(CanAttach)
				obj.Attach(_Node);
		}

		protected override void OnRemoved(INodeBehavior obj)
		{
			if(obj.AttachedNode != null)
				obj.Detach();
		}
		bool _DelayAttach;
		internal bool DelayAttach
		{
			get
			{
				return _DelayAttach;
			}
			set
			{
				_DelayAttach = value;
				if(CanAttach)
					foreach(var b in this)
						b.Attach(_Node);
			}
		}
	}
}
#endif