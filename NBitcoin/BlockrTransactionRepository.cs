using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin
{
	[Serializable]
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
		{

		}



		#region ITransactionRepository Members

		public Transaction Get(uint256 txId)
		{
			while(true)
			{
				WebClient client = new WebClient();
				var result = client.DownloadString("http://btc.blockr.io/api/v1/tx/raw/" + txId);
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

		public void Put(uint256 txId, Transaction tx)
		{
			
		}

		#endregion
	}
}
