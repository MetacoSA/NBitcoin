using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NBitcoin
{ 
	public class ThreadSafeList<T> : IEnumerable<T>
	{
		private List<T> _Behaviors;
		private object _lock = new object();

		public ThreadSafeList()
		{
			lock (_lock)
				_Behaviors = new List<T>();
		}

		/// <summary>
		/// Add an item to the collection
		/// </summary>
		/// <param name="item"></param>
		/// <returns>When disposed, the item is removed</returns>
		public IDisposable Add(T item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));
			OnAdding(item);
			lock (_lock)
			{
				_Behaviors.Add(item);
			}
			return new ActionDisposable(() =>
			{
			}, () => Remove(item));
		}

		protected virtual void OnAdding(T obj)
		{
		}
		protected virtual void OnRemoved(T obj)
		{
		}

		public bool Remove(T item)
		{
			bool removed = false;
			lock (_lock)
			{
				removed = _Behaviors.Remove(item);
			}

			if (removed)
				OnRemoved(item);
			return removed;
		}

		public void Clear()
		{
			foreach (var behavior in this)
				Remove(behavior);
		}

		public T FindOrCreate<U>() where U : T, new()
		{
			return FindOrCreate<U>(() => new U());
		}
		public U FindOrCreate<U>(Func<U> create) where U : T
		{
			var result = this.OfType<U>().FirstOrDefault();
			if (result == null)
			{
				result = create();
				Add(result);
			}
			return result;
		}
		public U Find<U>() where U : T
		{
			return this.OfType<U>().FirstOrDefault();
		}

		public void Remove<U>() where U : T
		{
			foreach (var b in this.OfType<U>())
			{
				Remove(b);
			}
		}

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator()
		{
			List<T> list = null;
			lock (_lock)
			{
				list = _Behaviors.ToList();
			}
			return list?.GetEnumerator();
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
