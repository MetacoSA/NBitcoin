using NBitcoin.MicroPayment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class micropayment_tests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanDoMicroPayment()
		{
			var aliceKey = new Key();
			var bobKey = new Key();
			var alicePaymentKey = new Key();
			var bobPaymentKey = new Key();

			var bobRefundKey = new Key();

			var init = new Transaction()
			{
				Outputs =
				{
					new TxOut(Money.Coins(1.0m), aliceKey),
					new TxOut(Money.Coins(1.0m), bobKey),
				}
			};
			NoSqlTransactionRepository repo = new NoSqlTransactionRepository();
			repo.Put(init);

			var args = new MicroChannelArguments()
			{
				Amount = Money.Coins(0.5m),
				Fees = Money.Coins(0.0001m),
				Expiration = new LockTime(DateTimeOffset.UtcNow + TimeSpan.FromHours(1.0)),
				Payee = new MicroChannelParticipant()
				{
					PaymentPubKey = alicePaymentKey.PubKey,
					ScriptPubkey = aliceKey.ScriptPubKey
				},
				Payer = new MicroChannelParticipant()
				{
					PaymentPubKey = bobPaymentKey.PubKey,
					ScriptPubkey = bobKey.ScriptPubKey
				}
			}.MakeRedeem();
			Assert.True(args.Verify());

			var bob = new PayerEndpoint(args);
			var alice = new PayeeEndpoint(args);

			var bobCoins = init.Outputs.AsCoins().Skip(1).ToArray();
			var openMsg = bob.OpenChannel(bobCoins, new Key[] { bobKey }, bobPaymentKey);
			var ackMsg = alice.AckOpenChannel(openMsg, alicePaymentKey);
			bob.AssertAckOpenChannel(ackMsg);

			Assert.True(0.0d == bob.Progress);
			var payMessage = bob.Pay(Money.Coins(0.01m), bobPaymentKey);
			Assert.True(Money.Coins(0.01m) == bob.Paid);
			alice.ReceivePay(payMessage);

			payMessage = bob.Pay(Money.Coins(0.015m), bobPaymentKey);
			bob = (PayerEndpoint)Clone(bob); //Test if serialization/deseria is messed up
			Assert.True(Money.Coins(0.025m) == bob.Paid);
			alice.ReceivePay(payMessage);
			alice = (PayeeEndpoint)Clone(alice); //Test if serialization/deseria is messed up

			payMessage = bob.Pay(Money.Coins(0.0001m), bobPaymentKey);
			Assert.True(Money.Coins(0.0251m) == bob.Paid);
			alice.ReceivePay(payMessage);
			Assert.True(Money.Coins(0.0251m) == alice.Paid);

			var pay = args.Amount - Money.Coins(0.0251m) - Money.Satoshis(3) + args.Fees;
			payMessage = bob.Pay(pay, bobPaymentKey);
			alice.ReceivePay(payMessage);

			payMessage = bob.Pay(Money.Satoshis(3), bobPaymentKey);
			alice.ReceivePay(payMessage);
			Assert.True(args.GetFundAmount() == alice.Paid);
			Assert.True(args.GetFundAmount() == bob.Paid);
			Assert.True(1.0d == bob.Progress);

			Assert.Throws<MicroPaymentException>(() => bob.Pay(Money.Satoshis(1), bobPaymentKey));

			alice.Finalize(alicePaymentKey);
		}

		public MicroEndpoint Clone(MicroEndpoint endpoint)
		{
			var state = endpoint.ExportState();
			return state.ToEndpoint();
		}
	}
}
