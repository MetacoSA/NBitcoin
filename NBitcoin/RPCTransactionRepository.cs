using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class RPCTransactionRepository : ITransactionRepository
	{
		RPCClient _Client;
		public RPCTransactionRepository(RPCClient client)
		{
			if(client == null)
				throw new ArgumentNullException("client");
			_Client = client;
		}
		#region ITransactionRepository Members

		public Task<Transaction> GetAsync(uint256 txId)
		{
			return _Client.GetRawTransactionAsync(txId, false);
		}

		public Task PutAsync(uint256 txId, Transaction tx)
		{
			return Task.FromResult(false);
		}

		#endregion
	}
}
