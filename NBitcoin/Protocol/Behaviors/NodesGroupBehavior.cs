﻿#if !NOSOCKET
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
	internal class NodesGroupBehavior : NodeBehavior
	{

		internal NodesGroup _Parent;
		public NodesGroupBehavior(NodesGroup parent)
		{
			_Parent = parent;
		}
		
		NodesGroupBehavior()
		{
		}
		

		protected override void AttachCore()
		{
			AttachedNode.StateChanged += AttachedNode_StateChanged;
		}

		protected override void DetachCore()
		{
			AttachedNode.StateChanged -= AttachedNode_StateChanged;
		}


		void AttachedNode_StateChanged(Node node, NodeState oldState)
		{
			if(!node.IsConnected)
			{
				_Parent._ConnectedNodes.Remove(AttachedNode);
				_Parent.StartConnecting();
			}
		}


	

		#region ICloneable Members

		public override object Clone()
		{
			return new NodesGroupBehavior(_Parent);
		}

		#endregion
	}
}
#endif