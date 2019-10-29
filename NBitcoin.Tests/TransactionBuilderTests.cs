
using Xunit;
using NBitcoin;
using System.Linq;

namespace NBitcoin.Tests
{
	public class TransactionBuilderTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void TxBuilderDegradationTest()
		{
			var n = Network.RegTest;
			var prevTx = n.CreateTransaction();
			var amount = Money.Satoshis(50000);
			var inputAmount = amount + Money.Satoshis(600000);
			var bob = new Key();
			prevTx.Outputs.Add(new TxOut(inputAmount, bob.PubKey.WitHash));
			var coin = prevTx.Outputs.AsCoins().First();

			var dummyChange = new Key();
			var txb = n.CreateTransactionBuilder();
			txb
				.AddKeys(dummyChange)
				.AddCoins(coin);

			var dest = new Key().PubKey;
			txb.Send(dest, Money.Satoshis(20000));

			txb.SetChange(dummyChange);

			var feeRate = new FeeRate(Money.Satoshis(1000));
			var fees = txb.EstimateFees(feeRate);
			txb.SendFees(fees);

			txb.BuildTransaction(true);
		}
	}
}