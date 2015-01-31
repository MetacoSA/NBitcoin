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
		ReaderWriterLock @lock = new ReaderWriterLock();
		public CachedTransactionRepository(ITransactionRepository inner)
		{
			if(inner == null)
				throw new ArgumentNullException("inner");
			_Inner = inner;
		}

		public Transaction GetFromCache(uint256 txId)
		{
			using(@lock.LockRead())
			{
				return _Transactions.TryGet(txId);
			}
		}

		#region ITransactionRepository Members

		public async Task<Transaction> GetAsync(uint256 txId)
		{
			bool found = false;
			Transaction result = null;
			using(@lock.LockRead())
			{
				found = _Transactions.TryGetValue(txId, out result);
			}
			if(!found)
			{
				result = await _Inner.GetAsync(txId).ConfigureAwait(false);
				using(@lock.LockWrite())
				{
					_Transactions.AddOrReplace(txId, result);
				}
			}
			return result;

		}

		public Task PutAsync(uint256 txId, Transaction tx)
		{
			using(@lock.LockWrite())
			{
				if(!_Transactions.ContainsKey(txId))
					_Transactions.AddOrReplace(txId, tx);
				else
					_Transactions[txId] = tx;
			}
			return _Inner.PutAsync(txId, tx);
		}

		#endregion
	}
}
