using System;
using FsCheck;
using NBitcoin;
using System.Linq;

namespace NBitcoin.Tests.Generators
{
  public class MoneyGenerator
  {

    public static Gen<Money> Money() =>
      (from bytes in Gen.ListOf(8, PrimitiveGenerator.randomByte())
       select BitConverter.ToUInt64(bytes.ToArray(), 0))
       .Where(u => u < Transaction.MAX_MONEY)
       .Select(u64 => new Money(u64));
  }
}