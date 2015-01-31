#if !NOHTTPCLIENT
using NBitcoin.DataEncoders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.OpenAsset
{
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
	}
	public class CoinprismColoredTransactionRepository : IColoredTransactionRepository
	{
		class CoinprismTransactionRepository : ITransactionRepository
		{
			#region ITransactionRepository Members

			public Task<Transaction> GetAsync(uint256 txId)
			{
				return Task.FromResult<Transaction>(null);
			}

			public Task PutAsync(uint256 txId, Transaction tx)
			{
				return Task.FromResult(true);
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

		public async Task<ColoredTransaction> GetAsync(uint256 txId)
		{
			try
			{
				ColoredTransaction result = new ColoredTransaction();
				using(HttpClient client = new HttpClient())
				{
					var response = await client.GetAsync("https://api.coinprism.com/v1/transactions/" + txId).ConfigureAwait(false);
					if(response.StatusCode != HttpStatusCode.OK)
						return null;
					var str = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
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

		public Task PutAsync(uint256 txId, ColoredTransaction tx)
		{
			return Task.FromResult(false);
		}

		#endregion
	}
}
#endif