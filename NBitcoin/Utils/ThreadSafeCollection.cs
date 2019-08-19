#if !NOSOCKET
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class ThreadSafeCollection<T> : IEnumerable<T>
	{
		ConcurrentDictionary<T, T> _Behaviors = new ConcurrentDictionary<T, T>();

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
			_Behaviors.TryAdd(item, item);
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
			T old;
			var removed = _Behaviors.TryRemove(item, out old);
			if (removed)
				OnRemoved(old);
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
				T behavior;
				_Behaviors.TryRemove(b, out behavior);
			}
		}

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator()
		{
			return _Behaviors.Select(k => k.Key).GetEnumerator();
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
#endif