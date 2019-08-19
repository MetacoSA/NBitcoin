#if !NOSOCKET
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class BoundedDictionary<TKey, TValue>
	{
		int _MaxItems;
		ConcurrentDictionary<TKey, TValue> _Dictionnary = new ConcurrentDictionary<TKey, TValue>();
		ConcurrentQueue<TKey> _Queue = new ConcurrentQueue<TKey>();
		public BoundedDictionary(int maxItems)
		{
			_MaxItems = maxItems;
		}

		public int Count
		{
			get
			{
				return _Dictionnary.Count;
			}
		}

		public TValue AddOrUpdate(TKey key, TValue value, Func<TKey, TValue, TValue> update)
		{
			bool wasPresent = false;
			TValue val = _Dictionnary.AddOrUpdate(key, value, (k, v) =>
			{
				wasPresent = true;
				return update(k, v);
			});
			if (!wasPresent)
			{
				_Queue.Enqueue(key);
				Clean();
			}
			return val;
		}

		public bool TryAdd(TKey key, TValue value)
		{
			var added = _Dictionnary.TryAdd(key, value);
			if (added)
			{
				_Queue.Enqueue(key);
				Clean();
			}
			return added;
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			return _Dictionnary.TryGetValue(key, out value);
		}

		public bool TryRemove(TKey key, out TValue value)
		{
			return _Dictionnary.TryRemove(key, out value);
		}

		private void Clean()
		{
			while (_Queue.Count > _MaxItems)
			{
				TKey result;
				if (_Queue.TryDequeue(out result))
				{
					TValue result2;
					_Dictionnary.TryRemove(result, out result2);
				}
			}
		}
	}
}
#endif