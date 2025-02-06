using System.Collections.Generic;
using System.Linq;

namespace NBitcoin.BIP370;

public static class SortedDictionaryExtensions
{
	public static bool TryRemove<TKey, TValue>(this SortedDictionary<TKey, TValue> map, TKey key, out TValue value)
	{
		if (!map.TryGetValue(key, out value)) return false;
		map.Remove(key);
		return true;
	}

	public static bool Pop<TKey, TValue>(this SortedDictionary<TKey, TValue> map, out TKey key, out TValue value)
	{
		if (map.Count == 0)
		{
			key = default;
			value = default;
			return false;
		}

		key = map.Keys.First();
		value = map[key];
		map.Remove(key);
		return true;
	}
}