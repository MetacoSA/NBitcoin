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
		Queue<uint256> _EvictionQueue = new Queue<uint256>();
		ReaderWriterLock _lock = new ReaderWriterLock();

		public ColoredTransaction GetFromCache(uint256 txId)
		{
			using(_lock.LockRead())
			{
				return _ColoredTransactions.TryGet(txId);
			}
		}

		public int MaxCachedTransactions
		{
			get
			{
				return _InnerTransactionRepository.MaxCachedTransactions;
			}
			set
			{
				_InnerTransactionRepository.MaxCachedTransactions = value;
			}
		}

		public bool WriteThrough
		{
			get
			{
				return _InnerTransactionRepository.WriteThrough;
			}
			set
			{
				_InnerTransactionRepository.WriteThrough = value;
			}
		}

		public bool ReadThrough
		{
			get
			{
				return _InnerTransactionRepository.ReadThrough;
			}
			set
			{
				_InnerTransactionRepository.ReadThrough = value;
			}
		}

		public CachedColoredTransactionRepository(IColoredTransactionRepository inner)
		{
			if(inner == null)
				throw new ArgumentNullException("inner");
			_Inner = inner;
			_InnerTransactionRepository = new CachedTransactionRepository(inner.Transactions);
			MaxCachedTransactions = 1000;
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

		private void EvictIfNecessary(uint256 txId)
		{
			_EvictionQueue.Enqueue(txId);
			while(_ColoredTransactions.Count > MaxCachedTransactions && _EvictionQueue.Count > 0)
				_ColoredTransactions.Remove(_EvictionQueue.Dequeue());
		}

		public async Task<ColoredTransaction> GetAsync(uint256 txId)
		{
			ColoredTransaction result = null;
			bool found;
			using(_lock.LockRead())
			{
				found = _ColoredTransactions.TryGetValue(txId, out result);
			}
			if(!found)
			{
				result = await _Inner.GetAsync(txId).ConfigureAwait(false);
				if(ReadThrough)
				{
					using(_lock.LockWrite())
					{
						_ColoredTransactions.AddOrReplace(txId, result);
						EvictIfNecessary(txId);
					}
				}
			}
			return result;
		}

		public Task PutAsync(uint256 txId, ColoredTransaction tx)
		{

			if(WriteThrough)
			{
				using(_lock.LockWrite())
				{

					if(!_ColoredTransactions.ContainsKey(txId))
					{
						_ColoredTransactions.AddOrReplace(txId, tx);
						EvictIfNecessary(txId);
					}
					else
						_ColoredTransactions[txId] = tx;
				}
			}
			return _Inner.PutAsync(txId, tx);
		}

		#endregion
	}
}
