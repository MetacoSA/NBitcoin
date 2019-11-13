#nullable enable
#if !NOJSONNET
using NBitcoin;
using NBitcoin.OpenAsset;
using Newtonsoft.Json;
using System;
using System.Reflection;

namespace NBitcoin.JsonConverters
{
#if !NOJSONNET
	public
#else
	internal
#endif
	class CoinJsonConverter : JsonConverter
	{
		public class CoinJson
		{
			public CoinJson()
			{

			}
			public CoinJson(ICoin coin, Network network)
			{
				if (network == null)
					network = Network.Main;
				TransactionId = coin.Outpoint.Hash;
				Index = coin.Outpoint.N;
				ScriptPubKey = coin.TxOut.ScriptPubKey;
				if (coin is ScriptCoin)
				{
					RedeemScript = ((ScriptCoin)coin).Redeem;
				}
				if (coin is Coin)
				{
					Value = ((Coin)coin).Amount;
				}
				if (coin is ColoredCoin)
				{
					var cc = (ColoredCoin)coin;
					AssetId = cc.AssetId.GetWif(network);
					Quantity = cc.Amount.Quantity;
					Value = cc.Bearer.Amount;
					var scc = cc.Bearer as ScriptCoin;
					if (scc != null)
					{
						RedeemScript = scc.Redeem;
					}
				}
			}
			public ICoin ToCoin(string path)
			{
				if (TransactionId == null)
					throw new JsonObjectException("'transactionId' is missing", path);
				if (!(Index is uint index))
					throw new JsonObjectException("'index' is missing", path);
				if (Value is null)
					throw new JsonObjectException("'value' is missing", path);
				if (ScriptPubKey is null)
					throw new JsonObjectException("'scriptPubKey' is missing", path);

				
				var coin = RedeemScript == null ? new Coin(new OutPoint(TransactionId, index), new TxOut(Value, ScriptPubKey)) : new ScriptCoin(new OutPoint(TransactionId, index), new TxOut(Value, ScriptPubKey), RedeemScript);
				if (AssetId != null)
					return coin.ToColoredCoin(new AssetMoney(AssetId, Quantity));
				return coin;
			}

			public uint256? TransactionId
			{
				get;
				set;
			}
			public uint? Index
			{
				get;
				set;
			}
			public Money? Value
			{
				get;
				set;
			}

			public Script? ScriptPubKey
			{
				get;
				set;
			}

			public Script? RedeemScript
			{
				get;
				set;
			}
			[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public BitcoinAssetId? AssetId
			{
				get;
				set;
			}
			[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
			public long Quantity
			{
				get;
				set;
			}
		}

		public CoinJsonConverter(Network network)
		{
			Network = network;
		}

		public Network Network
		{
			get;
			set;
		}
		public override bool CanConvert(Type objectType)
		{
			return typeof(ICoin).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
		}

		public override object? ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return default;
			reader.AssertJsonType(JsonToken.StartObject);
			var path = reader.Path;
			return serializer.Deserialize<CoinJson>(reader).ToCoin(path);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			serializer.Serialize(writer, new CoinJson((ICoin)value, Network));
		}
	}
}
#endif
#nullable disable
