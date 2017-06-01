#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.SPV
{
	public class WalletSummary
	{
		public WalletSummary()
		{
			Confirmed = new WalletSummaryDetails();
		}
		public WalletSummaryDetails UnConfirmed
		{
			get;
			set;
		}
		public WalletSummaryDetails Confirmed
		{
			get;
			set;
		}
		public WalletSummaryDetails Spendable
		{
			get;
			set;
		}
		public WalletSummaryDetails Immature
		{
			get;
			set;
		}
	}
	public class WalletSummaryDetails
	{


		public WalletSummaryDetails()
		{
			Amount = Money.Zero;
		}

		public WalletSummaryDetails(WalletTransaction[] transactions)
		{
			TransactionCount = transactions.Length;
			Amount = transactions.SelectMany(s => s.ReceivedCoins).Select(s => s.Amount).Sum() - transactions.SelectMany(s => s.SpentCoins).Select(s => s.Amount).Sum();
		}
		public int TransactionCount
		{
			get;
			set;
		}
		public Money Amount
		{
			get;
			set;
		}

		public static WalletSummaryDetails operator +(WalletSummaryDetails c1, WalletSummaryDetails c2)
		{
			if(c1 == null)
				return c2;
			if(c2 == null)
				return c1;
			return new WalletSummaryDetails
			{
				Amount = c1.Amount + c2.Amount,
				TransactionCount = c1.TransactionCount + c2.TransactionCount,
			};
		}
		public static WalletSummaryDetails operator -(WalletSummaryDetails c1, WalletSummaryDetails c2)
		{
			return c1 + (-c2);
		}

		public static WalletSummaryDetails operator -(WalletSummaryDetails c1)
		{
			if(c1 == null)
				return null;
			WalletSummaryDetails result = new WalletSummaryDetails
			{
				Amount = -c1.Amount,
				TransactionCount = -c1.TransactionCount
			};
			return result;
		}

	}
}
#endif