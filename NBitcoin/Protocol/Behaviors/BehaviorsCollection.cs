using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Behaviors
{
	public class BehaviorsCollection : IEnumerable<NodeBehavior>
	{
		Node _Node;
		public BehaviorsCollection(Node node)
		{
			if(node == null)
				throw new ArgumentNullException("node");
			_Node = node;
		}
		List<NodeBehavior> _Behaviors = new List<NodeBehavior>();
		object cs = new object();
		public void Add(NodeBehavior behavior)
		{
			lock(cs)
			{
				if(CanAttach)
					behavior.Attach(_Node);
				_Behaviors.Add(behavior);
			}
		}

		bool CanAttach
		{
			get
			{
				return _Node != null && _Node.State != NodeState.Offline && _Node.State != NodeState.Failed && _Node.State != NodeState.Disconnecting;
			}
		}

		public bool Remove(NodeBehavior behavior)
		{
			lock(cs)
			{
				var removed = _Behaviors.Remove(behavior);
				if(removed)
					behavior.Detach();
				return removed;
			}
		}

		public void Clear()
		{
			lock(cs)
			{
				foreach(var behavior in _Behaviors)
					behavior.Detach();
				_Behaviors.Clear();
			}
		}

		public T FindOrCreate<T>() where T : NodeBehavior, new()
		{
			lock(cs)
			{
				var result = _Behaviors.OfType<T>().FirstOrDefault();
				if(result == null)
				{
					result = new T();
					_Behaviors.Add(result);
					if(CanAttach)
						result.Attach(_Node);
				}
				return result;
			}
		}
		public T Find<T>() where T : NodeBehavior
		{
			lock(cs)
			{
				return _Behaviors.OfType<T>().FirstOrDefault();
			}
		}

		public void Remove<T>() where T : NodeBehavior
		{
			lock(cs)
			{
				foreach(var b in _Behaviors.OfType<T>().ToList())
				{
					_Behaviors.Remove(b);
				}
			}
		}

		#region IEnumerable<NodeBehavior> Members

		public IEnumerator<NodeBehavior> GetEnumerator()
		{
			lock(cs)
			{
				return _Behaviors.ToList().GetEnumerator();
			}
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
