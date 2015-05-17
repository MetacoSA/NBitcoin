using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Behaviors
{
	public abstract class NodeBehavior
	{
		List<IDisposable> _Disposables = new List<IDisposable>();
		protected void RegisterDisposable(IDisposable disposable)
		{
			_Disposables.Add(disposable);
		}

		public Node AttachedNode
		{
			get;
			private set;
		}
		object cs = new object();
		internal void Attach(Node node)
		{
			if(node == null)
				throw new ArgumentNullException("node");
			if(AttachedNode != null)
				throw new InvalidOperationException("Behavior already attached to a node");
			lock(cs)
			{
				AttachedNode = node;
				if(Disconnected(node))
					return;
				AttachCore();
			}
		}

		protected void AssertNotAttached()
		{
			if(AttachedNode != null)
				throw new InvalidOperationException("Can't modify the behavior while it is attached");
		}

		private static bool Disconnected(Node node)
		{
			return node.State == NodeState.Disconnecting || node.State == NodeState.Failed || node.State == NodeState.Offline;
		}

		protected abstract void AttachCore();

		internal void Detach()
		{
			lock(cs)
			{
				if(AttachedNode == null)
					return;
				try
				{
					DetachCore();
					foreach(var dispo in _Disposables)
						dispo.Dispose();
				}
				catch(Exception ex)
				{
					NodeServerTrace.Error("Error while detaching behavior", ex);
				}
				_Disposables.Clear();
				AttachedNode = null;
			}
		}

		protected abstract void DetachCore();
	}
}
