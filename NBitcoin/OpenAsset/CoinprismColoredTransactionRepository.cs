using NBitcoin.DataEncoders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.OpenAsset
{
	[Serializable]
	public class CoinprismException : Exception
	{
		public CoinprismException()
		{
		}
		public CoinprismException(string message)
			: base(message)
		{
		}
		public CoinprismException(string message, Exception inner)
			: base(message, inner)
		{
		}
		protected CoinprismException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context)
		{
		}
	}
	public class CoinprismColoredTransactionRepository : IColoredTransactionRepository
	{
		class CoinprismTransactionRepository : ITransactionRepository
		{
			#region ITransactionRepository Members

			public Transaction Get(uint256 txId)
			{
				return null;
			}

			public void Put(uint256 txId, Transaction tx)
			{

			}

			#endregion
		}
		#region IColoredTransactionRepository Members

		public ITransactionRepository Transactions
		{
			get
			{
				return new CoinprismTransactionRepository();
			}
		}

		public ColoredTransaction Get(uint256 txId)
		{
			try
			{
				ColoredTransaction result = new ColoredTransaction();
				WebClient client = new WebClient();
				var str = client.DownloadString("https://api.coinprism.com/v1/transactions/" + txId);
				var json = JObject.Parse(str);
				var inputs = json["inputs"] as JArray;
				if(inputs != null)
				{
					for(int i = 0 ; i < inputs.Count ; i++)
					{
						if(inputs[i]["asset_id"].Value<string>() == null)
							continue;
						var entry = new ColoredEntry();
						entry.Index = (uint)i;
						entry.Asset = new Asset(
							new BitcoinAssetId(inputs[i]["asset_id"].ToString(), null).AssetId,
							inputs[i]["asset_quantity"].Value<ulong>());

						result.Inputs.Add(entry);
					}
				}

				var outputs = json["outputs"] as JArray;
				if(outputs != null)
				{
					bool issuance = true;
					for(int i = 0 ; i < outputs.Count ; i++)
					{
						var marker = ColorMarker.TryParse(new Script(Encoders.Hex.DecodeData(outputs[i]["script"].ToString())));
						if(marker != null)
						{
							issuance = false;
							result.Marker = marker;
							continue;
						}
						if(outputs[i]["asset_id"].Value<string>() == null)
							continue;
						ColoredEntry entry = new ColoredEntry();
						entry.Index = (uint)i;
						entry.Asset = new Asset(
							new BitcoinAssetId(outputs[i]["asset_id"].ToString(), null).AssetId,
							outputs[i]["asset_quantity"].Value<ulong>()
							);

						if(issuance)
							result.Issuances.Add(entry);
						else
							result.Transfers.Add(entry);
					}
				}
				return result;
			}
			catch(WebException ex)
			{
				try
				{
					var error = JObject.Parse(new StreamReader(ex.Response.GetResponseStream()).ReadToEnd());
					if(error["ErrorCode"].ToString() == "InvalidTransactionHash")
						return null;
					throw new CoinprismException(error["ErrorCode"].ToString());
				}
				catch(CoinprismException)
				{
					throw;
				}
				catch
				{
				}
				throw;
			}
		}

		public void Put(uint256 txId, ColoredTransaction tx)
		{
		}

		#endregion
	}
}
