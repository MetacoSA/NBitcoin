using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class TxOutRepository
	{
		Dictionary<OutPoint, TxOut> _TxOutByOutpoint = new Dictionary<OutPoint, TxOut>();
		internal TxOut GetOutputFor(TxIn txIn)
		{
			TxOut txout = null;
			_TxOutByOutpoint.TryGetValue(txIn.PrevOut, out txout);
			return txout;
		}

		public void AddFromTransaction(Transaction transaction)
		{
			var hash = transaction.GetHash();
			for(int i = 0 ; i < transaction.Outputs.Count; i++)
			{
				AddTxOut(new OutPoint(hash, i), transaction.Outputs[i]);
			}
		}

		private void AddTxOut(OutPoint outpoint, TxOut txOut)
		{
			_TxOutByOutpoint.Add(outpoint, txOut);
		}
	}
}
