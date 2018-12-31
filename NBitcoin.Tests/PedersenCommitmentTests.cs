using Xunit;
using NBitcoin;
using NBitcoin.Crypto;
using NBitcoin.BouncyCastle.Math;

namespace NBitcoin.Tests
{
	public class PedersenCommitmentTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ThirdPartyCanVerifyTotalMoneyInSystem()
		{
			// Alice has 163,000 in her balance and Bob has 78,000 in his balance
			// Alice wants to send 25,000 to Bob
			// A third party who cannot see the real balances should be able verify that
			// the total balance in the system didn't change (no money created from thin air)  
			var aliceBalance = new BigInteger("163000");
			var bobBalance   = new BigInteger("78000");

			var aliceBlindinFactor = BigInteger.Arbitrary(250);
			var bobBlindinFactor = BigInteger.Arbitrary(250);

			var commitToAliceBalance = new PedersenCommitment(aliceBlindinFactor, aliceBalance);
			var commitToBobBalance = new PedersenCommitment(bobBlindinFactor, bobBalance);

			var valueTranferedToBob = new BigInteger("25000");
			var valueTranferedBlindinFactor = BigInteger.Arbitrary(250);
			var commitToTranferedValue = new PedersenCommitment(valueTranferedBlindinFactor, valueTranferedToBob);


			var aliceNewBalance = aliceBalance.Subtract(valueTranferedToBob);
			var bobNewBalance = bobBalance.Add(valueTranferedToBob);
			var commitToAliceNewBalance = commitToAliceBalance - commitToTranferedValue;
			var commitToBobNewBalance = commitToBobBalance + commitToTranferedValue;

			Assert.True(commitToAliceNewBalance.Verify(aliceBlindinFactor.Subtract(valueTranferedBlindinFactor), aliceNewBalance));
			Assert.True(commitToBobNewBalance.Verify(bobBlindinFactor.Add(valueTranferedBlindinFactor), bobNewBalance));
		}
	}
}