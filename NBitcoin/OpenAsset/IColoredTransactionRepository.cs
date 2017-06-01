using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.OpenAsset
{
	public interface IColoredTransactionRepository
	{
		ITransactionRepository Transactions
		{
			get;
		}
		Task<ColoredTransaction> GetAsync(uint256 txId);
		Task PutAsync(uint256 txId, ColoredTransaction tx);
	}

	public static class ColoredTxRepoExtensions
	{
		public static Task<ColoredTransaction> GetAsync(this IColoredTransactionRepository repo, string txId)
		{
			return repo.GetAsync(uint256.Parse(txId));
		}

		public static ColoredTransaction Get(this IColoredTransactionRepository repo, string txId)
		{
			return repo.Get(uint256.Parse(txId));
		}

		public static ColoredTransaction Get(this IColoredTransactionRepository repo, uint256 txId)
		{
			try
			{
				return repo.GetAsync(txId).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
				return null;
			}
		}

		public static void Put(this IColoredTransactionRepository repo, uint256 txId, ColoredTransaction tx)
		{
			try
			{
				repo.PutAsync(txId, tx).Wait();
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
			}
		}
	}
}
