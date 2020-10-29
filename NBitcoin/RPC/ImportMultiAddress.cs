using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using NBitcoin.JsonConverters;
using NBitcoin.Scripting;

namespace NBitcoin.RPC
{

	[JsonObject(MemberSerialization.OptIn)]
	public class ImportMultiAddress
	{
		public class ScriptPubKeyObject
		{
			public ScriptPubKeyObject()
			{

			}
			public ScriptPubKeyObject(Script scriptPubKey)
			{
				ScriptPubKey = scriptPubKey;
			}
			public ScriptPubKeyObject(BitcoinAddress address)
			{
				Address = address;
			}
			[JsonIgnore]
			public Script ScriptPubKey { get; set; }

			[JsonProperty("address", NullValueHandling = NullValueHandling.Ignore)]
			public BitcoinAddress Address { get; set; }

			/// <summary>
			/// Returns true if Address property is populated AND ScriptPubKey is not. If Address and ScriptPubKey are populated, ScriptPubKey takes precedence.
			/// </summary>
			///
			[JsonIgnore]
			public bool IsAddress
			{
				get
				{
					return this.Address != null && this.ScriptPubKey == null;
				}
			}
		}

		[JsonProperty("scriptPubKey", NullValueHandling = NullValueHandling.Ignore)]
		[JsonConverter(typeof(ImportMultiScriptPubKeyConverter))]
		public ScriptPubKeyObject ScriptPubKey { get; set; }

		/// <summary>
		/// Creation time of the key, keep null if this address has just been generated
		/// </summary>
		[JsonProperty("timestamp")]
		public DateTimeOffset? Timestamp { get; set; }

		[JsonProperty("redeemscript", NullValueHandling = NullValueHandling.Ignore)]
		public Script RedeemScript { get; set; }

		[JsonProperty("pubkeys", NullValueHandling = NullValueHandling.Ignore)]
		public PubKey[] PubKeys { get; set; }

		[JsonProperty("keys", NullValueHandling = NullValueHandling.Ignore)]
		public BitcoinSecret[] Keys { get; set; }

		[JsonProperty("internal", NullValueHandling = NullValueHandling.Ignore)]
		public bool? Internal { get; set; }

		[JsonProperty("watchonly", NullValueHandling = NullValueHandling.Ignore)]
		public bool? WatchOnly { get; set; }

		[JsonProperty("label", NullValueHandling = NullValueHandling.Ignore)]
		public string Label { get; set; }

		[JsonProperty("desc", NullValueHandling = NullValueHandling.Ignore)]
		public OutputDescriptor Desc { get; set; }

		[JsonProperty("range", NullValueHandling = NullValueHandling.Ignore)]
		public int[] Ranges { get; set; }

		[JsonIgnore]
		public int Range
		{
			set
			{
				Ranges ??= new[] {0, 0};
				Ranges[1] = value;
			}
		}
	}

	/// <summary>
	/// Custom JsonConverter to deal with loose type of scriptPubKey property in the ImportMulti method
	/// </summary>
	internal class ImportMultiScriptPubKeyConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(ImportMultiAddress.ScriptPubKeyObject);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			return new NotImplementedException();
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			ImportMultiAddress.ScriptPubKeyObject req = (ImportMultiAddress.ScriptPubKeyObject)value;
			if (req.IsAddress)
			{
				// Serialize as a complex object (i.e., { "address": "XYZasASDFASDasdfasdfasdfsd" } )
				var obj = new JObject();
				obj.Add("address", req.Address.ToString());
				obj.WriteTo(writer);
			}
			else
			{
				// Serialize as a simple string value (i.e., "035cd888286d39e64c7c52a49561b323c732fc5cfa48d73faedcf5c40d99747474"
				writer.WriteValue(req.ScriptPubKey.ToHex());
			}
		}
	}
}
