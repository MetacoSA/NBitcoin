using NBitcoin.Tests.Generators;
using Xunit;
using FsCheck;
using FsCheck.Xunit;
using System;
using NBitcoin.Crypto;

namespace NBitcoin.Tests.PropertyTest
{
	// Test for non-segwit tx.
	public class LegacyTransactionTest
	{

		public LegacyTransactionTest()
		{
			Arb.Register<LegacyTransactionGenerators>();
		}

		[Property(MaxTest = 100)]
		[Trait("UnitTest", "UnitTest")]
		public bool CanConvertToRaw(Tuple<Transaction, Network> param)
		{
			var tx = param.Item1;
			string hex = tx.ToHex();
			var tx2 = Transaction.Parse(hex, param.Item2);

			return tx.GetHash() == tx2.GetHash();
		}

		[Property(MaxTest = 100)]
		[Trait("UnitTest", "UnitTest")]
		public bool TxIdMustMatchHexSha256(Transaction tx)
		{
			return tx.GetHash() == Hashes.Hash256(tx.ToBytes());
		}

	}
}