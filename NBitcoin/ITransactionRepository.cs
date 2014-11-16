using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public interface ITransactionRepository
	{
		Transaction Get(uint256 txId);
		void Put(uint256 txId, Transaction tx);
	}

	public static class TxRepoExtensions
	{
		public static Transaction Get(this ITransactionRepository repo, string txId)
		{
			return repo.Get(new uint256(txId));
		}

		public static void Put(this ITransactionRepository repo, Transaction tx)
		{
			repo.Put(tx.GetHash(), tx);
		}
	}
}
