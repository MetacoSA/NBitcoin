using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class CachedTransactionRepository : ITransactionRepository
	{
		ITransactionRepository _Inner;
		Dictionary<uint256, Transaction> _Transactions = new Dictionary<uint256, Transaction>();
		public CachedTransactionRepository(ITransactionRepository inner)
		{
			if(inner == null)
				throw new ArgumentNullException("inner");
			_Inner = inner;
		}
		#region ITransactionRepository Members

		public Transaction Get(uint256 txId)
		{
			Transaction result = null;
			if(!_Transactions.TryGetValue(txId, out result))
			{
				result = _Inner.Get(txId);
				_Transactions.Add(txId, result);
			}
			return result;
		}

		public void Put(uint256 txId, Transaction tx)
		{
			if(!_Transactions.ContainsKey(txId))
				_Transactions.Add(txId, tx);
			else
				_Transactions[txId] = tx;
			_Inner.Put(txId, tx);
		}

		#endregion
	}
}
