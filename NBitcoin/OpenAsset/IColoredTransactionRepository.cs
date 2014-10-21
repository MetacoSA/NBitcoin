using System;
using System.Collections.Generic;
using System.Linq;
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
		ColoredTransaction Get(uint256 txId);
		void Put(uint256 txId, ColoredTransaction tx);

        Task<ColoredTransaction> GetAsync(uint256 txId);
        Task PutAsync(uint256 txId, ColoredTransaction tx);
    }
}
