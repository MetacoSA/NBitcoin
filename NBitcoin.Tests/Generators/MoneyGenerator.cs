using System;
using FsCheck;
using NBitcoin;
using System.Linq;

namespace NBitcoin.Tests.Generators
{
	public class MoneyGenerator
	{

		public static Gen<Money> Money() =>
			(from bytes in Gen.ListOf(6, PrimitiveGenerator.RandomByte()) // Make sure we are below 21M
			 select Utils.ToUInt64(bytes.Concat(new byte[] { 0, 0 }).ToArray(), true))
			 .Select(u64 => new Money(u64));
	}
}