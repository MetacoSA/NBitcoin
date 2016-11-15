using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class TxOutList : UnsignedList<TxOut>
	{
		public TxOutList()
		{

		}
		public TxOutList(Transaction parent)
			: base(parent)
		{

		}
		public IEnumerable<TxOut> To(IDestination destination)
		{
			return this.Where(r => r.IsTo(destination));
		}
		public IEnumerable<TxOut> To(Script scriptPubKey)
		{
			return this.Where(r => r.ScriptPubKey == scriptPubKey);
		}

		public IEnumerable<IndexedTxOut> AsIndexedOutputs()
		{
			// We want i as the index of txOut in Outputs[], not index in enumerable after where filter
			return this.Select((r, i) => new IndexedTxOut()
			{
				TxOut = r,
				N = (uint)i,
				Transaction = Transaction
			});
		}

		public IEnumerable<Coin> AsCoins()
		{
			var txId = Transaction.GetHash();
			for(int i = 0; i < Count; i++)
			{
				yield return new Coin(new OutPoint(txId, i), this[i]);
			}
		}
	}

}
