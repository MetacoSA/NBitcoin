using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.OpenAsset
{
	public class CachedColoredTransactionRepository : IColoredTransactionRepository
	{
		IColoredTransactionRepository _Inner;
		CachedTransactionRepository _InnerTransactionRepository;
		Dictionary<uint256, ColoredTransaction> _ColoredTransactions = new Dictionary<uint256, ColoredTransaction>();
		ReaderWriterLock @lock = new ReaderWriterLock();

		public ColoredTransaction GetFromCache(uint256 txId)
		{
			using(@lock.LockRead())
			{
				return _ColoredTransactions.TryGet(txId);
			}
		}

		public CachedColoredTransactionRepository(IColoredTransactionRepository inner)
		{
			if(inner == null)
				throw new ArgumentNullException("inner");
			_Inner = inner;
			_InnerTransactionRepository = new CachedTransactionRepository(inner.Transactions);
		}
		#region IColoredTransactionRepository Members

		public CachedTransactionRepository Transactions
		{
			get
			{
				return _InnerTransactionRepository;
			}
		}

		ITransactionRepository IColoredTransactionRepository.Transactions
		{
			get
			{
				return _InnerTransactionRepository;
			}
		}

		public async Task<ColoredTransaction> GetAsync(uint256 txId)
		{
			ColoredTransaction result = null;
			bool found;
			using(@lock.LockRead())
			{
				found = _ColoredTransactions.TryGetValue(txId, out result);
			}
			if(!found)
			{
				result = await _Inner.GetAsync(txId).ConfigureAwait(false);
				using(@lock.LockWrite())
				{
					_ColoredTransactions.AddOrReplace(txId, result);
				}
			}
			return result;
		}

		public Task PutAsync(uint256 txId, ColoredTransaction tx)
		{
			using(@lock.LockWrite())
			{
				if(!_ColoredTransactions.ContainsKey(txId))
					_ColoredTransactions.AddOrReplace(txId, tx);
				else
					_ColoredTransactions[txId] = tx;
				return _Inner.PutAsync(txId, tx);
			}
		}

		#endregion
	}
}
