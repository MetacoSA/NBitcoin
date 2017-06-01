#if !NOJSONNET
#if !NOHTTPCLIENT
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class BlockrException : Exception
	{
		internal BlockrException(JObject response)
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
				using(var response = await Client.GetAsync(BlockrAddress + "tx/raw/" + txId).ConfigureAwait(false))
				{
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
					var tx = Transaction.Parse(json["data"]["tx"]["hex"].ToString());
					return tx;
				}
			}
		}

		readonly static HttpClient Client = new HttpClient();
		public async Task<List<Coin>> GetUnspentAsync(string Address)
		{
			while(true)
			{
				using(var response = await Client.GetAsync(BlockrAddress + "address/unspent/" + Address).ConfigureAwait(false))
				{
					if(response.StatusCode == HttpStatusCode.NotFound)
						return null;
					var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
					var json = JObject.Parse(result);
					var status = json["status"];
					var code = json["code"];
					if((status != null && status.ToString() == "error") || (json["data"]["address"].ToString() != Address))
					{
						throw new BlockrException(json);
					}
					List<Coin> list = new List<Coin>();
					foreach(var element in json["data"]["unspent"])
					{
						list.Add(new Coin(uint256.Parse(element["tx"].ToString()), (uint)element["n"], new Money((decimal)element["amount"], MoneyUnit.BTC), new Script(DataEncoders.Encoders.Hex.DecodeData(element["script"].ToString()))));
					}
					return list;
				}
			}
		}

		public Task PutAsync(uint256 txId, Transaction tx)
		{
			return Task.FromResult(true);
		}

		internal static string BroadcastPath = "tx/push";

		public async Task BroadcastAsync(Transaction tx)
		{
			if(tx == null)
				throw new ArgumentNullException("tx");
			var jsonTx = new JObject();
			jsonTx["hex"] = tx.ToHex();
			var content = new StringContent(jsonTx.ToString(), Encoding.UTF8, "application/json");
			using(var response = await Client.PostAsync(BlockrAddress + BroadcastPath, content).ConfigureAwait(false))
			{
				var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
				var json = JObject.Parse(result);
				var status = json["status"];
				var code = json["code"];
				if(status != null && (status.ToString() == "error" || status.ToString() == "fail"))
				{
					throw new BlockrException(json);
				}
			}
		}

#endregion

		string BlockrAddress
		{
			get
			{
				// https cert get rejected by .net
				return "http://" + (Network == Network.Main ? "" : "t") + "btc.blockr.io/api/v1/";
			}
		}
	}
}
#endif
#endif
