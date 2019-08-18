#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Behaviors
{
	public interface INodeBehavior
	{
		Node AttachedNode
		{
			get;
		}
		void Attach(Node node);
		void Detach();
		INodeBehavior Clone();
	}
	public abstract class NodeBehavior : INodeBehavior
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

		public void Attach(Node node)
		{
			if (node == null)
				throw new ArgumentNullException(nameof(node));
			if (AttachedNode != null)
				throw new InvalidOperationException("Behavior already attached to a node");
			lock (cs)
			{
				AttachedNode = node;
				if (Disconnected(node))
					return;
				AttachCore();
			}
		}

		protected void AssertNotAttached()
		{
			if (AttachedNode != null)
				throw new InvalidOperationException("Can't modify the behavior while it is attached");
		}

		private static bool Disconnected(Node node)
		{
			return node.State == NodeState.Disconnecting || node.State == NodeState.Failed || node.State == NodeState.Offline;
		}

		protected abstract void AttachCore();

		public void Detach()
		{
			lock (cs)
			{
				if (AttachedNode == null)
					return;

				DetachCore();
				foreach (var dispo in _Disposables)
					dispo.Dispose();

				_Disposables.Clear();
				AttachedNode = null;
			}
		}

		protected abstract void DetachCore();

		public abstract object Clone();

		INodeBehavior INodeBehavior.Clone()
		{
			return (INodeBehavior)Clone();
		}
	}
}
#endif