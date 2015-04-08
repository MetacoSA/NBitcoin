using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.MicroPayment
{
	public class PayerEndpoint : MicroEndpoint
	{
		
		public PayerEndpoint(MicroChannelArguments args)
			: base(args)
		{
		}
		public PayerEndpoint(MicroChannelState state)
			: base(state.Arguments)
		{
			ImportState(state);
		}
		public Transaction Fund
		{
			get;
			set;
		}

		public override MicroChannelState ExportState()
		{
			var state = base.ExportState();
			state.Fund = Fund;
			state.PayerState = true;
			return state;
		}
		internal override void ImportState(MicroChannelState state)
		{
			if(!state.PayerState)
				throw new FormatException("This micro channel state is the payee's copy");
			base.ImportState(state);
			Fund = state.Fund;
		}

		public OpenChannelMessage OpenChannel(ICoin[] fundingCoins,
											  Key[] fundingKeys,
											  Key payerKey)
		{
			if(payerKey.PubKey != Arguments.Payer.PaymentPubKey)
				throw new ArgumentException("Invalid payerKey", "payerKey");
			var p2sh = Arguments.Redeem.Hash.ScriptPubKey;

			var builder = new TransactionBuilder();
			Fund =
				builder
				.AddCoins(fundingCoins)
				.AddKeys(fundingKeys)
				.Send(p2sh, Arguments.GetFundAmount())
				.SetChange(Arguments.Payer.ScriptPubkey)
				.SendFees(Arguments.Fees)
				.Shuffle()
				.BuildTransaction(true);

			if(!builder.Verify(Fund, Arguments.Fees))
				throw new MicroPaymentException("Funding transaction incorreclty signed");

			var fundCoin = Fund.Outputs.AsCoins().First(c => c.ScriptPubKey == p2sh).ToScriptCoin(Arguments.Redeem);

			var unsignedRefund = Arguments.CreatePayment(Arguments.Fees, fundCoin);
			unsignedRefund.LockTime = Arguments.Expiration;
			builder =
			   new TransactionBuilder()
			   .AddKeys(payerKey)
			   .AddCoins(fundCoin);
			Refund = builder.SignTransaction(unsignedRefund);
			return new OpenChannelMessage()
			{
				UnsignedRefund = unsignedRefund
			};
		}

		public void AssertAckOpenChannel(OpenChannelAckMessage ackMsg)
		{
			var builder = new TransactionBuilder();
			var fullySigned =
				builder
				.AddCoins(FundCoin)
				.CombineSignatures(Refund, ackMsg.SignedRefund);
			if(!builder.Verify(fullySigned, Arguments.Fees))
			{
				throw new MicroPaymentException("Transaction incorrectly signed");
			}
			Refund = fullySigned;
		}

		public PayMessage Pay(Money amount, Key payerKey)
		{
			var sequence = Sequence;
			Sequence++;

			var toPay = Paid + amount;
			var pay = Arguments.CreatePayment(toPay, FundCoin);
			Arguments.Assert(pay, false, Paid + amount, FundCoin);
			pay = Arguments.SignPayment(pay, payerKey, FundCoin);
			Payment = pay;
			Paid = toPay;
			return new PayMessage()
			{
				Sequence = sequence,
				Amount = amount,
				Payment = pay
			};
		}
	}
}
