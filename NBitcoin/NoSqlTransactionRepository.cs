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

		public NoSqlTransactionRepository():this(new InMemoryNoSqlRepository())
		{

		}
		public NoSqlTransactionRepository(NoSqlRepository repository)
		{
			if(repository == null)
				throw new ArgumentNullException("repository");
			_Repository = repository;
		}
		#region ITransactionRepository Members

		public Transaction Get(uint256 txId)
		{
			return _Repository.Get<Transaction>(GetId(txId));
		}

        public Task<Transaction> GetAsync(uint256 txId)
        {
            return Task.Run(() => Get(txId));
        }

		private string GetId(uint256 txId)
		{
			return "tx-" + txId.ToString();
		}

		public void Put(uint256 txId, Transaction tx)
		{
			_Repository.Put(GetId(txId), tx);
		}

        public Task PutAsync(uint256 txId, Transaction tx)
        {
            return Task.Run(() => Put(txId, tx));
        }

		#endregion
	}
}
