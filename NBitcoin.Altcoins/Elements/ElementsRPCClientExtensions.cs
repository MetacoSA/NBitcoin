using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin.JsonConverters;
using NBitcoin.RPC;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NBitcoin.Altcoins.Elements
{
	public static class ElementsRPCClientExtensions
	{
		public enum ElementsRPCOperations
		{
			importblindingkey,
			getnewaddress,
			getaddressinfo,
			getbalance,
			unblindrawtransaction,
			issueasset,
			sendtoaddress,
		}
		public static Task<RPCResponse> SendCommandAsync(this RPCClient rpcClient, ElementsRPCOperations commandName,
			params object[] parameters)
		{
			return rpcClient.SendCommandAsync(commandName.ToString(), parameters);
		}

		public static async Task<ElementsTransaction> UnblindRawTransaction(
			this RPCClient rpcClient, string transactionHex, Network network = null)
		{
			var result = await rpcClient.SendCommandAsync(ElementsRPCOperations.unblindrawtransaction, transactionHex);
			result.ThrowIfError();

			return ParseTxHex(result.Result["hex"].Value<string>(), network ?? Liquid.Instance.Mainnet);
		}

		private static ElementsTransaction ParseTxHex(string hex,  Network network)
		{
			var tx = network.Consensus.ConsensusFactory.CreateTransaction() as ElementsTransaction;
			tx.ReadWrite(NBitcoin.DataEncoders.Encoders.Hex.DecodeData(hex), network);
			return tx;
		}

		public static Task<RPCResponse> ImportBlindingKey(this RPCClient rpcClient,
			BitcoinBlindedAddress bitcoinBlindedAddress, Key blindingKey)
		{
			return rpcClient.SendCommandAsync(ElementsRPCOperations.importblindingkey,
				bitcoinBlindedAddress.ToString(), blindingKey.ToHex());
		}

		/// <summary>
		/// This is an aggregate of multiple RPC commands to unblind a confidential transaction. It calls importblindingkey and unblindrawtransaction.
		/// </summary>
		/// <param name="rpcClient"></param>
		/// <param name="addressBlindingKeys"></param>
		/// <param name="transaction"></param>
		/// <param name="network"></param>
		/// <returns></returns>
		public static async Task<ElementsTransaction> UnblindTransaction(
			this RPCClient rpcClient, List<UnblindTransactionBlindingAddressKey> addressBlindingKeys,
			ElementsTransaction transaction, Network network)
		{
			await Task.WhenAll(addressBlindingKeys.Select(async key =>
			{
				var blindImportResponse = await rpcClient.ImportBlindingKey(key.Address, key.BlindingKey);
				blindImportResponse.ThrowIfError();
			}));
			return await rpcClient.UnblindRawTransaction(transaction.ToHex(), network);
		}

		public static async Task<BitcoinAddress> GetNewAddressAsync(this RPCClient rpcClient, Network network)
		{
			var result = await rpcClient.SendCommandAsync(ElementsRPCOperations.getnewaddress).ConfigureAwait(false);
			return new BitcoinBlindedAddress(result.ResultString, network);
		}

		public static async Task<IssueAssetResponse> IssueAsset(this RPCClient rpcClient, Money assetAmount, Money tokenAmount, bool blind = true)
		{
			var result = await rpcClient.SendCommandAsync(ElementsRPCOperations.issueasset, assetAmount.ToDecimal(MoneyUnit.BTC), tokenAmount.ToDecimal(MoneyUnit.BTC), blind).ConfigureAwait(false);
			result.ThrowIfError();
			return IssueAssetResponse.FromJsonResponse((JObject) result.Result, rpcClient.Network);
		}

		public static async Task<uint256> SendToAddressAsync(
			this RPCClient rpcClient,
			BitcoinAddress address,
			Money amount,
			string commentTx = null,
			string commentDest = null,
			bool subtractFeeFromAmount = false,
			bool replaceable = false,
			uint256 assetId = null
		)
		{
			if (assetId == null)
			{
				return await rpcClient.SendToAddressAsync(address, amount, commentTx, commentDest, subtractFeeFromAmount, replaceable);
			}
			if (address == null)
				throw new ArgumentNullException(nameof(address));
			if (amount == null)
				throw new ArgumentNullException(nameof(amount));
			List<object> parameters = new List<object>();
			parameters.Add(address.ToString());
			parameters.Add(amount.ToDecimal(MoneyUnit.BTC));
			parameters.Add($"{commentTx}");
			parameters.Add($"{commentDest}");
			parameters.Add(subtractFeeFromAmount);
			parameters.Add(replaceable);
			parameters.Add(1);
			parameters.Add("UNSET");
			parameters.Add(assetId.ToString());
			var resp = await rpcClient.SendCommandAsync(ElementsRPCOperations.sendtoaddress, parameters.ToArray()).ConfigureAwait(false);
			return uint256.Parse(resp.Result.ToString());
		}


		public static async Task<ElementsGetAddressInfoResponse> GetAddressInfoAsync(this RPCClient rpcClient, string address, Network network)
		{
			var response = await rpcClient.SendCommandAsync(ElementsRPCOperations.getaddressinfo, address);

			return ElementsGetAddressInfoResponse.FromJsonResponse((JObject)response.Result, network);
		}

		public static async Task<Dictionary<string,Money>> GetBalancesAsync(this RPCClient rpcClient)
		{
			var response = await rpcClient.SendCommandAsync(ElementsRPCOperations.getbalance);

			return response.Result.Children().ToDictionary(token => (token as JProperty).Name,
				token => Money.Parse(((JProperty) token).Value.ToString()));
		}


	}

	public class IssueAssetResponse
	{
		public uint256 TransactionId { get; set; }

		public int Vin { get; set; }

		[JsonProperty("entropy")]
		public string Entropy { get; set; }

		[JsonProperty("asset")]
		public uint256 Asset { get; set; }

		[JsonProperty("token")]
		public uint256 Token { get; set; }

		public static IssueAssetResponse FromJsonResponse(JObject raw, Network network)
		{
			return new IssueAssetResponse()
			{
				TransactionId = uint256.Parse(raw.Property("txid").Value.ToString()),
				Asset = uint256.Parse(raw.Property("asset").Value.ToString()),
				Token = uint256.Parse(raw.Property("token").Value.ToString()),
				Entropy = raw.Property("entropy").ToString(),
				Vin = raw.Property("vin").Value.Value<int>(),
			};
		}
	}

	public class UnblindTransactionBlindingAddressKey
	{
		public BitcoinBlindedAddress Address { get; set; }
		public Key BlindingKey { get; set; }
	}

	public class ElementsGetAddressInfoResponse : GetAddressInfoResponse
	{
		public BitcoinBlindedAddress Confidential { get; set; }
		public BitcoinAddress Unconfidential { get; set; }
		public PubKey ConfidentialKey { get; set; }
		public override GetAddressInfoResponse LoadFromJson(JObject raw, Network network)
		{
			base.LoadFromJson(raw, network);
			Confidential = new BitcoinBlindedAddress(raw.Property("confidential").Value.Value<string>(), network);
			Unconfidential = BitcoinAddress.Create(raw.Property("unconfidential").Value.Value<string>(), network);
			ConfidentialKey = new PubKey(raw.Property("confidential_key").Value.Value<string>());
			return this;
		}

		public new static ElementsGetAddressInfoResponse FromJsonResponse(JObject raw, Network network)
		{
			return (ElementsGetAddressInfoResponse) new ElementsGetAddressInfoResponse().LoadFromJson(raw, network);
		}
	}
}
