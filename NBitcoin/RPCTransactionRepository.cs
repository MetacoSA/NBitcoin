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

		public Transaction Get(uint256 txId)
		{
			return _Client.GetRawTransaction(txId, false);
		}

		public void Put(uint256 txId, Transaction tx)
		{
			
		}

		#endregion
	}
}
