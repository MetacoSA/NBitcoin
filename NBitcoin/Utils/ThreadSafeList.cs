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

		private Lazy<List<T>> _EnumeratorList;

		public ThreadSafeList()
		{
			_Behaviors = new List<T>();
			Modified();
		}

		private void Modified()
		{
			if (_EnumeratorList == null || _EnumeratorList.IsValueCreated is true)
				_EnumeratorList = new Lazy<List<T>>(() => { lock (_lock) { return _Behaviors.ToList(); } });
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
				Modified();
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
				Modified();
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
			return _EnumeratorList.Value.GetEnumerator();
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
