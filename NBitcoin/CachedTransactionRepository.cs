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
		Queue<uint256> _EvictionQueue = new Queue<uint256>();
		ReaderWriterLock @lock = new ReaderWriterLock();
		public CachedTransactionRepository(ITransactionRepository inner)
		{
			if (inner == null)
				throw new ArgumentNullException(nameof(inner));
			ReadThrough = true;
			WriteThrough = true;
			_Inner = inner;
			MaxCachedTransactions = 100;
		}

		public int MaxCachedTransactions
		{
			get;
			set;
		}

		public Transaction GetFromCache(uint256 txId)
		{
			using (@lock.LockRead())
			{
				return _Transactions.TryGet(txId);
			}
		}

		#region ITransactionRepository Members

		public async Task<Transaction> GetAsync(uint256 txId)
		{
			bool found = false;
			Transaction result = null;
			using (@lock.LockRead())
			{
				found = _Transactions.TryGetValue(txId, out result);
			}
			if (!found)
			{
				result = await _Inner.GetAsync(txId).ConfigureAwait(false);
				if (ReadThrough)
				{
					using (@lock.LockWrite())
					{

						_Transactions.AddOrReplace(txId, result);
						EvictIfNecessary(txId);
					}
				}
			}
			return result;

		}

		private void EvictIfNecessary(uint256 txId)
		{
			_EvictionQueue.Enqueue(txId);
			while (_Transactions.Count > MaxCachedTransactions && _EvictionQueue.Count > 0)
				_Transactions.Remove(_EvictionQueue.Dequeue());
		}

		public Task PutAsync(uint256 txId, Transaction tx)
		{
			if (WriteThrough)
			{
				using (@lock.LockWrite())
				{

					if (!_Transactions.ContainsKey(txId))
					{

						_Transactions.AddOrReplace(txId, tx);
						EvictIfNecessary(txId);
					}
					else
						_Transactions[txId] = tx;
				}
			}
			return _Inner.PutAsync(txId, tx);
		}

		#endregion

		public bool WriteThrough
		{
			get;
			set;
		}

		public bool ReadThrough
		{
			get;
			set;
		}
	}
}
