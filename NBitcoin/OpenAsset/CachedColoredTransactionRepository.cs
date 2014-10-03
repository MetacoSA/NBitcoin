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
		ITransactionRepository _InnerTransactionRepository;
		Dictionary<uint256, ColoredTransaction> _ColoredTransactions = new Dictionary<uint256, ColoredTransaction>();
		public CachedColoredTransactionRepository(IColoredTransactionRepository inner)
		{
			if(inner == null)
				throw new ArgumentNullException("inner");
			_Inner = inner;
			_InnerTransactionRepository = new CachedTransactionRepository(inner.Transactions);
		}
		#region IColoredTransactionRepository Members

		public ITransactionRepository Transactions
		{
			get
			{
				return _InnerTransactionRepository;
			}
		}

		public ColoredTransaction Get(uint256 txId)
		{
			ColoredTransaction result = null;
			if(!_ColoredTransactions.TryGetValue(txId, out result))
			{
				result = _Inner.Get(txId);
				_ColoredTransactions.Add(txId, result);
			}
			return result;
		}

		public void Put(uint256 txId, ColoredTransaction tx)
		{
			if(!_ColoredTransactions.ContainsKey(txId))
				_ColoredTransactions.Add(txId, tx);
			else
				_ColoredTransactions[txId] = tx;
			_Inner.Put(txId, tx);
		}

		#endregion
	}
}
