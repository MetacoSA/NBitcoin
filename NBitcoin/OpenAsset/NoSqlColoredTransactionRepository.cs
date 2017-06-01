using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.OpenAsset
{
	public class NoSqlColoredTransactionRepository : IColoredTransactionRepository
	{
		public NoSqlColoredTransactionRepository()
			: this(null, null)
		{

		}
		public NoSqlColoredTransactionRepository(ITransactionRepository transactionRepository)
			: this(transactionRepository, null)
		{

		}
		public NoSqlColoredTransactionRepository(ITransactionRepository transactionRepository, NoSqlRepository repository)
		{
			if(transactionRepository == null)
				transactionRepository = new NoSqlTransactionRepository();
			if(repository == null)
				repository = new InMemoryNoSqlRepository();
			_Transactions = transactionRepository;
			_Repository = repository;
		}

		private readonly NoSqlRepository _Repository;
		public NoSqlRepository Repository
		{
			get
			{
				return _Repository;
			}
		}

		ITransactionRepository _Transactions;
		#region IColoredTransactionRepository Members

		public ITransactionRepository Transactions
		{
			get
			{
				return _Transactions;
			}
		}

		public Task<ColoredTransaction> GetAsync(uint256 txId)
		{
			return _Repository.GetAsync<ColoredTransaction>(GetId(txId));
		}

		private static string GetId(uint256 txId)
		{
			return "ctx-" + txId;
		}

		public Task PutAsync(uint256 txId, ColoredTransaction tx)
		{
			return _Repository.PutAsync(GetId(txId), tx);
		}

		#endregion
	}
}
