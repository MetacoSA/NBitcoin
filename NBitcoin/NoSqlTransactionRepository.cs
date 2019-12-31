using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class NoSqlTransactionRepository : ITransactionRepository
	{
		private readonly NoSqlRepository _Repository;
		public NoSqlRepository Repository
		{
			get
			{
				return _Repository;
			}
		}

		public NoSqlTransactionRepository() : this(new InMemoryNoSqlRepository())
		{

		}
		public NoSqlTransactionRepository(NoSqlRepository repository)
		{
			if (repository == null)
				throw new ArgumentNullException(nameof(repository));
			_Repository = repository;
		}
		#region ITransactionRepository Members

		public Task<Transaction> GetAsync(uint256 txId)
		{
			return _Repository.GetAsync<Transaction>(GetId(txId));
		}

		private string GetId(uint256 txId)
		{
			return "tx-" + txId.ToString();
		}

		public Task PutAsync(uint256 txId, Transaction tx)
		{
			return _Repository.PutAsync(GetId(txId), tx);
		}

		#endregion
	}
}
