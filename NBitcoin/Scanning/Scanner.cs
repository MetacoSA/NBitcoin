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
				var coins = ScanCoins(txId, tx, height);
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

		public Coins ScanCoins(Transaction tx, int height)
		{
			return ScanCoins(tx.GetHash(), tx, height);
		}

		public abstract Coins ScanCoins(uint256 txId, Transaction tx, int height);

		public virtual void GetScannedPushData(List<byte[]> searchedPushData)
		{
		}

		public IEnumerable<TxIn> FindSpent(Block block)
		{
			return FindSpent(block.Transactions.Where(t => !t.IsCoinBase));
		}
		public abstract IEnumerable<TxIn> FindSpent(IEnumerable<Transaction> transactions);
	}
}
