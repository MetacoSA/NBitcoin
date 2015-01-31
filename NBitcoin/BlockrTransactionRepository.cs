#if !NOHTTPCLIENT
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class BlockrException : Exception
	{
		public BlockrException(JObject response)
			: base(response["message"] == null ? "Error from Blockr" : response["message"].ToString())
		{
			Code = response["code"] == null ? 0 : response["code"].Value<int>();
			Status = response["status"] == null ? null : response["status"].ToString();
		}

		public int Code
		{
			get;
			set;
		}
		public string Status
		{
			get;
			set;
		}
	}
	public class BlockrTransactionRepository : ITransactionRepository
	{
		public BlockrTransactionRepository()
			: this(null)
		{

		}
		public BlockrTransactionRepository(Network network)
		{
			if(network == null)
				network = Network.Main;
			Network = network;
		}

		public Network Network
		{
			get;
			set;
		}


		#region ITransactionRepository Members

		public async Task<Transaction> GetAsync(uint256 txId)
		{
			while(true)
			{
				using(HttpClient client = new HttpClient())
				{
					var response = await client.GetAsync("http://" + (Network == Network.Main ? "" : "t") + "btc.blockr.io/api/v1/tx/raw/" + txId).ConfigureAwait(false);
					if(response.StatusCode == HttpStatusCode.NotFound)
						return null;
					var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
					var json = JObject.Parse(result);
					var status = json["status"];
					var code = json["code"];
					if(status != null && status.ToString() == "error")
					{
						throw new BlockrException(json);
					}
					var tx = new Transaction(json["data"]["tx"]["hex"].ToString());
					return tx;
				}
			}
		}

		public Task PutAsync(uint256 txId, Transaction tx)
		{
			return Task.FromResult(false);
		}

		#endregion
	}
}
#endif