#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.SPV
{
	public class WalletTransactionsCollection : IEnumerable<WalletTransaction>
	{
		public WalletTransactionsCollection(WalletTransaction[] walletTransactions)
		{
			HashSet<uint256> confirmed = new HashSet<uint256>();
			foreach(var conf in walletTransactions
											.Where(t => t.BlockInformation != null)
											.Select(t => t.Transaction.GetHash()))
			{
				confirmed.Add(conf);
			}

			var all = walletTransactions
						.Where(t => t.BlockInformation != null)
						.OrderByDescending(t => t.BlockInformation.Height)
						.ThenByDescending(t => t.UnconfirmedSeen)
						.ToList();

			var unconfs = walletTransactions
							.Where(t => t.BlockInformation == null && !confirmed.Contains(t.Transaction.GetHash()))
							.ToList();

			foreach(var unconf in unconfs)
			{
				int i = 0;
				foreach(var conf in all)
				{
					if(unconf.UnconfirmedSeen > conf.UnconfirmedSeen)
						break;
					i++;
				}
				all.Insert(i, unconf);
			}
			_All = all;
		}


		WalletSummary _Summary;
		public WalletSummary Summary
		{
			get
			{
				if(_Summary == null)
				{
					WalletSummary summary = new WalletSummary();

					var immatures = _All.Where(o => o.Transaction.IsCoinBase && o.BlockInformation.Confirmations < 101).ToArray();
					summary.Immature = new WalletSummaryDetails(immatures);
					var unconf = _All.Where(o => o.BlockInformation == null).ToList();
					var unconfConflict = new List<WalletTransaction>();
					Dictionary<OutPoint, OutPoint> spent = new Dictionary<OutPoint, OutPoint>();
					foreach(var tx in unconf)
					{
						foreach(var o in tx.Transaction.Inputs.Select(i => i.PrevOut))
						{
							if(!spent.TryAdd(o, o))
							{
								unconfConflict.Add(tx);
							}
						}
					}
					summary.UnConfirmed = new WalletSummaryDetails(unconf.Where(u => !unconfConflict.Contains(u)).ToArray());
					summary.Confirmed = new WalletSummaryDetails(_All.Where(o => o.BlockInformation != null).ToArray());
					summary.Spendable = summary.UnConfirmed + summary.Confirmed - summary.Immature;
					_Summary = summary;
				}
				return _Summary;
			}
		}

		public IEnumerable<Coin> GetSpendableCoins()
		{
			var spent = _All.SelectMany(o => o.SpentCoins.Select(c => c.Outpoint)).Distinct().ToDictionary(o => o);
			return _All
					.Where(o => !o.Transaction.IsCoinBase || o.BlockInformation.Confirmations >= 101)
					.SelectMany(o => o.ReceivedCoins)
					.Where(c => !spent.ContainsKey(c.Outpoint))
					.ToList();
		}

		public int Count
		{
			get
			{
				return _All.Count;
			}
		}

		public WalletTransaction this[int index]
		{
			get
			{
				return _All[index];
			}
			set
			{
				_All[index] = value;
			}
		}

		List<WalletTransaction> _All;

		#region IEnumerable<WalletTransaction> Members

		public IEnumerator<WalletTransaction> GetEnumerator()
		{
			return _All.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
#endif