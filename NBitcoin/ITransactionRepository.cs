using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace NBitcoin
{
	/// <summary>
	/// Represent a transaction map
	/// </summary>
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
			return repo.GetAsync(txId).GetAwaiter().GetResult();
		}

		public static void Put(this ITransactionRepository repo, uint256 txId, Transaction tx)
		{
			repo.PutAsync(txId, tx).GetAwaiter().GetResult();
		}
	}
}
