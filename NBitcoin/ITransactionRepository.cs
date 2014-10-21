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

        Task<Transaction> GetAsync(uint256 txId);
        Task PutAsync(uint256 txId, Transaction tx);
	}
}
