using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Scanning
{
	public class CompositeScanner : Scanner
	{
		private readonly List<Scanner> _Scanners = new List<Scanner>();
		public List<Scanner> Scanners
		{
			get
			{
				return _Scanners;
			}
		}
		public override Coins ScanCoins(Transaction tx, int height)
		{
			Coins coin = null;
			foreach(var scanner in Scanners)
			{
				Coins localCoin = scanner.ScanCoins(tx, height);
				if(coin == null)
					coin = localCoin;
				else
					coin.MergeFrom(localCoin);
			}
			return coin;
		}

		public override IEnumerable<TxIn> FindSpent(IEnumerable<Transaction> transactions)
		{
			Dictionary<OutPoint, TxIn> spent = new Dictionary<OutPoint, TxIn>();
			foreach(var scanner in Scanners)
			{
				foreach(var txin in scanner.FindSpent(transactions))
				{
					spent.AddOrReplace(txin.PrevOut, txin);
				}
			}
			return spent.Values;
		}
	}
}
