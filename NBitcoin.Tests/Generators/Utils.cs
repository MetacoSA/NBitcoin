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
			foreach (var k in keys)
				foreach (var v in values)
					dict.Add(k, v);

			return dict;
		}

		public static Gen<Dictionary<byte[], byte[]>> UnknownKVMap() =>
				from itemNum in Gen.Choose(0, 15)
				from keys in Gen.ListOf(itemNum, PrimitiveGenerator.RandomBytes())
				from values in Gen.ListOf(itemNum, PrimitiveGenerator.RandomBytes())
				select Utils.DictionaryFromList<byte[], byte[]>(keys.ToList(), values.ToList());
	}
}