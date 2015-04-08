using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.MicroPayment
{
	public class MicroEndpoint
	{
		public MicroEndpoint(MicroChannelArguments args)
		{
			Paid = Money.Zero;
			if(args == null)
				throw new ArgumentNullException("args");
			if(!args.Verify())
				throw new ArgumentException("Incoherent args", "args");
			Arguments = args;
		}

		public MicroChannelArguments Arguments
		{
			get;
			internal set;
		}
		public Transaction Refund
		{
			get;
			internal set;
		}

		public ScriptCoin FundCoin
		{
			get
			{
				return new Coin(Refund.Inputs[0].PrevOut, new TxOut(Arguments.Fees + Arguments.Amount, Arguments.Redeem.Hash)).ToScriptCoin(Arguments.Redeem);
			}
		}
		public Money Paid
		{
			get;
			internal set;
		}
		public int Sequence
		{
			get;
			internal set;
		}
		public Transaction Payment
		{
			get;
			internal set;
		}

		public double Progress
		{
			get
			{
				return (double)Paid.Satoshi / (double)Arguments.GetFundAmount().Satoshi;
			}
		}

		public Money EffectivelyPaid
		{
			get
			{
				return Arguments.GetPaid(Payment);
			}
		}

		protected Transaction CreatePayment(Money amount)
		{
			return Arguments.CreatePayment(amount, FundCoin);
		}

		public virtual MicroChannelState ExportState()
		{
			return new MicroChannelState()
			{
				Paid = Paid,
				Payment = Payment,
				Sequence = Sequence,
				Refund = Refund,
				Arguments = Arguments
			};
		}
		internal virtual void ImportState(MicroChannelState state)
		{
			Paid = state.Paid;
			Payment = state.Payment;
			Refund = state.Refund;
			Sequence = state.Sequence;
			Arguments = state.Arguments;
		}
	}
}
