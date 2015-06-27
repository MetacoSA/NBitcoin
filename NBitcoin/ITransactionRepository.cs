using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace NBitcoin
{
	public interface ITransactionRepository
	{
		Task<Transaction> GetAsync(uint256 txId);
		Task PutAsync(uint256 txId, Transaction tx);
	}

	public static class TxRepoExtensions
	{
		public static Task<Transaction> GetAsync(this ITransactionRepository repo, string txId)
		{
			return repo.GetAsync(uint256.Parse(txId));
		}

		public static Task PutAsync(this ITransactionRepository repo, Transaction tx)
		{
			return repo.PutAsync(tx.GetHash(), tx);
		}

		public static Transaction Get(this ITransactionRepository repo, string txId)
		{
			return repo.Get(uint256.Parse(txId));
		}

		public static void Put(this ITransactionRepository repo, Transaction tx)
		{
			repo.Put(tx.GetHash(), tx);
		}

		public static Transaction Get(this ITransactionRepository repo, uint256 txId)
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

		public static void Put(this ITransactionRepository repo, uint256 txId, Transaction tx)
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
