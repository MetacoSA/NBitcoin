using System;
using System.Collections.Generic;
using FsCheck;
using FsCheck.Xunit;
using NBitcoin.Crypto;
using NBitcoin.Tests.Generators;
using Xunit;

namespace NBitcoin.Tests.PropertyTest
{
	public class SegwitTransactionTest
	{
		public SegwitTransactionTest()
		{
			Arb.Register<SegwitTransactionGenerators>();
		}

		[Property(MaxTest = 100)]
		[Trait("UnitTest", "UnitTest")]
		public void WitnessTxIdProp(Tuple<Transaction, Network> testcase)
		{
			var tx = testcase.Item1;
			Assert.Equal(tx.GetWitHash(), Hashes.DoubleSHA256(tx.ToBytes()));
			Assert.NotEqual(tx.GetHash(), tx.GetWitHash());
			var tx2 = Transaction.Parse(tx.ToHex(), testcase.Item2);
			Assert.Equal(tx, tx2, new TransactionComparer());
		}

		private class TransactionComparer : IEqualityComparer<Transaction>
		{
			public bool Equals(Transaction a, Transaction b) =>
				a.GetHash() == b.GetHash() && a.GetWitHash() == b.GetWitHash();

			public int GetHashCode(Transaction a) => a.GetHashCode();
		}
	}
}