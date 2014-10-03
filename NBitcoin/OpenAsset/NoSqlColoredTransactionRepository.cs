using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.OpenAsset
{
	public class NoSqlColoredTransactionRepository : IColoredTransactionRepository
	{
		public NoSqlColoredTransactionRepository()
			: this(new NoSqlTransactionRepository(), new InMemoryNoSqlRepository())
		{

		}
		public NoSqlColoredTransactionRepository(ITransactionRepository transactionRepository, NoSqlRepository repository)
		{
			if(transactionRepository == null)
				throw new ArgumentNullException("transactionRepository");
			if(repository == null)
				throw new ArgumentNullException("repository");
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

		public ColoredTransaction Get(uint256 txId)
		{
			return _Repository.Get<ColoredTransaction>(GetId(txId));
		}

		private string GetId(uint256 txId)
		{
			return "ctx-" + txId;
		}

		public void Put(uint256 txId, ColoredTransaction tx)
		{
			_Repository.Put(GetId(txId), tx);
		}

		#endregion
	}
}
