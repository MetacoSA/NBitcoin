
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

#if !NOJSONNET
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NBitcoin.JsonConverters;
#else
using System.Text.Json;
using System.Text.Json.Serialization;
using NBitcoin.SystemJsonConverters;
#endif
using NBitcoin.Scripting;

namespace NBitcoin.RPC
{

#if !NOJSONNET
	[JsonElement(MemberSerialization.OptIn)]
#else
[JsonConverter(typeof(MemberOptInJsonConverter))]
#endif
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

#if !NOJSONNET
			[JsonProperty("address", NullValueHandling = NullValueHandling.Ignore)]
#else
			[JsonPropertyName("address")]
			[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
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
#if !NOJSONNET
		[JsonProperty("scriptPubKey", NullValueHandling = NullValueHandling.Ignore)]
#else
		[JsonPropertyName("scriptPubKey")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
		[JsonConverter(typeof(ImportMultiScriptPubKeyConverter))]
		public ScriptPubKeyObject ScriptPubKey { get; set; }

		/// <summary>
		/// Creation time of the key, keep null if this address has just been generated
		/// </summary>
#if !NOJSONNET
		[JsonProperty("timestamp")]
#else
		[JsonPropertyName("timestamp")]
#endif
		public DateTimeOffset? Timestamp { get; set; }
#if !NOJSONNET
		[JsonProperty("redeemscript", NullValueHandling = NullValueHandling.Ignore)]
#else
		[JsonPropertyName("redeemscript")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
		public Script RedeemScript { get; set; }
#if !NOJSONNET
		[JsonProperty("pubkeys", NullValueHandling = NullValueHandling.Ignore)]
#else
		[JsonPropertyName("pubkeys")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
		public PubKey[] PubKeys { get; set; }
#if !NOJSONNET
		[JsonProperty("keys", NullValueHandling = NullValueHandling.Ignore)]
#else
		[JsonPropertyName("keys")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
		public BitcoinSecret[] Keys { get; set; }
#if !NOJSONNET
		[JsonProperty("internal", NullValueHandling = NullValueHandling.Ignore)]
#else
		[JsonPropertyName("internal")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
		public bool? Internal { get; set; }
#if !NOJSONNET
		[JsonProperty("watchonly", NullValueHandling = NullValueHandling.Ignore)]
#else
		[JsonPropertyName("watchonly")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
		public bool? WatchOnly { get; set; }
#if !NOJSONNET
		[JsonProperty("label", NullValueHandling = NullValueHandling.Ignore)]
#else
		[JsonPropertyName("label")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
		public string Label { get; set; }
#if !NOJSONNET
		[JsonProperty("desc", NullValueHandling = NullValueHandling.Ignore)]
#else
		[JsonPropertyName("desc")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
		public OutputDescriptor Desc { get; set; }
#if !NOJSONNET
		[JsonProperty("range", NullValueHandling = NullValueHandling.Ignore)]
#else
		[JsonPropertyName("range")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#endif
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
