using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using Xunit;

namespace NBitcoin.Tests.Generators
{
	public static class Utils
	{
		internal static Dictionary<T, U> DictionaryFromList<T, U>(List<T> keys, List<U> values)
		{
			Assert.Equal(keys.Count(), values.Count());
			var dict = new Dictionary<T, U>();
			foreach (var kv in keys.Zip(values, (k, v) => Tuple.Create(k, v)))
				dict.Add(kv.Item1, kv.Item2);

			return dict;
		}

		public static Gen<Dictionary<byte[], byte[]>> UnknownKVMap() =>
				from itemNum in Gen.Choose(0, 15)
				from keys in Gen.ListOf(itemNum, PrimitiveGenerator.RandomBytes())
				from values in Gen.ListOf(itemNum, PrimitiveGenerator.RandomBytes())
				select Utils.DictionaryFromList<byte[], byte[]>(keys.ToList(), values.ToList());
	}
}