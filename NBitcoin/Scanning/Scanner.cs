using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Scanning
{
	public class ScannedCoins
	{
		public uint256 TxId
		{
			get;
			set;
		}
		public Coins Coins
		{
			get;
			set;
		}
	}
	public abstract class Scanner
	{
		public IEnumerable<ScannedCoins> ScanCoins(Block block, int height)
		{
			foreach(var tx in block.Transactions)
			{
				var txId = tx.GetHash();
				var coins = ScanCoins(tx, height);
				if(coins == null || coins.IsEmpty)
					continue;
				else
					yield return new ScannedCoins()
					{
						Coins = coins,
						TxId = txId
					};
			}
		}

		public abstract Coins ScanCoins(Transaction tx, int height);

		public virtual void GetScannedPushData(List<byte[]> searchedPushData)
		{
		}

		public IEnumerable<Spendable> FindSpent(Block block, IEnumerable<Spendable> among)
		{
			var amongDico = among.ToDictionary(o => o.OutPoint);
			foreach(var spent in block
									.Transactions
									.SelectMany(t => t.Inputs)
									.Where(input => amongDico.ContainsKey(input.PrevOut)))
			{
				var spendable = amongDico[spent.PrevOut];
				amongDico.Remove(spent.PrevOut);
				yield return spendable;
			}
		}
	}
}
