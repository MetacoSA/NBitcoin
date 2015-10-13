using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.MicroPayment
{
	public class PayeeEndpoint : MicroEndpoint
	{
		public PayeeEndpoint(MicroChannelArguments args):base(args)
		{
			
		}
		public PayeeEndpoint(MicroChannelState state)
			: base(state.Arguments)
		{
			ImportState(state);
		}

		public override MicroChannelState ExportState()
		{
			var state = base.ExportState();
			state.PayerState = false;
			return state;
		}
		internal override void ImportState(MicroChannelState state)
		{
			if(state.PayerState)
				throw new FormatException("This micro channel state is the payer's copy");
			base.ImportState(state);
		}

		public OpenChannelAckMessage AckOpenChannel(OpenChannelMessage openMsg,
													Key payeeKey)
		{
			if(payeeKey.PubKey != Arguments.Payee.PaymentPubKey)
				throw new ArgumentException("Invalid payeeKey", "payeeKey");

			Arguments.Assert(openMsg.UnsignedRefund, true, Arguments.Fees);
			var fundCoin = new Coin(openMsg.UnsignedRefund.Inputs[0].PrevOut, new TxOut(Arguments.Amount + Arguments.Fees, Arguments.Redeem.Hash)).ToScriptCoin(Arguments.Redeem);

			var signed =
				new TransactionBuilder()
				.AddCoins(fundCoin)
				.AddKeys(payeeKey)
				.SignTransaction(openMsg.UnsignedRefund);
			Refund = signed;
			return new OpenChannelAckMessage()
			{
				SignedRefund = signed
			};
		}

		public void ReceivePay(PayMessage pay)
		{
			if(pay.Sequence != Sequence)
				throw new MicroPaymentException("Out of order payment");
			var expectedTotalAmount = Paid + pay.Amount;
			Arguments.Assert(pay.Payment, false, expectedTotalAmount, FundCoin);
			var sig = Arguments.ExtractSignature(Arguments.Payer, pay.Payment);
			if(!sig.Check(Arguments.Payer.PaymentPubKey, Arguments.Redeem, pay.Payment, 0))
				throw new MicroPaymentException("Invalid signature");
			Sequence++;
			Payment = pay.Payment;
			Paid = Paid + pay.Amount;
		}

		public Transaction Finalize(Key payeeKey)
		{
			var builder = new TransactionBuilder();
			var tx =  
				builder
				.AddCoins(FundCoin)
				.AddKeys(payeeKey)
				.SignTransaction(Payment);
			if(!builder.Verify(tx, Arguments.Fees))
				throw new MicroPaymentException("Payment incorrectly signed");
			return tx;
		}
	}

}
