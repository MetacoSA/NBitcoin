using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
	[JsonObject(MemberSerialization.OptIn)]
	public class ImportMultiAddress
	{
		public class ScriptPubKeyProperty
		{
			[JsonIgnore]
			public string ScriptPubKey { get; set; }

			[JsonProperty("address", NullValueHandling = NullValueHandling.Ignore)]
			public string Address { get; set; }

			/// <summary>
			/// Returns true if Address property is populated AND ScriptPubKey is not. If Address and ScriptPubKey are populated, ScriptPubKey takes precedence.
			/// </summary>
			///
			[JsonIgnore]
			public bool IsAddress
			{
				get
				{
					return (!string.IsNullOrWhiteSpace(this.Address) && string.IsNullOrWhiteSpace(this.ScriptPubKey));
				}
			}
		}

		[JsonProperty("scriptPubKey", NullValueHandling = NullValueHandling.Ignore)]
		[JsonConverter(typeof(ImportMultiScriptPubKeyConverter))]
		public ScriptPubKeyProperty ScriptPubKey { get; set; }

		[JsonProperty("timestamp")]
		public uint Timestamp { get; set; }

		[JsonProperty("redeemscript", NullValueHandling = NullValueHandling.Ignore)]
		public string RedeemScript { get; set; }

		[JsonProperty("pubkeys", NullValueHandling = NullValueHandling.Ignore)]
		public string[] PubKeys { get; set; }

		[JsonProperty("keys", NullValueHandling = NullValueHandling.Ignore)]
		public string[] Keys { get; set; }

		[JsonProperty("internal", NullValueHandling = NullValueHandling.Ignore)]
		public bool? Internal { get; set; }

		[JsonProperty("watchonly", NullValueHandling = NullValueHandling.Ignore)]
		public bool? WatchOnly { get; set; }

		[JsonProperty("label", NullValueHandling = NullValueHandling.Ignore)]
		public string Label { get; set; }
	}

	/// <summary>
	/// Customer JsonConvert to deal with loose type of scriptPubKey property in the ImportMulti method
	/// </summary>
	public class ImportMultiScriptPubKeyConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(ImportMultiAddress.ScriptPubKeyProperty);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			return new NotImplementedException();
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			ImportMultiAddress.ScriptPubKeyProperty req = (ImportMultiAddress.ScriptPubKeyProperty)value;
			JToken t = JToken.FromObject(value);

			if (req.IsAddress)
			{
				// Serialize as a complex object (i.e., { "address": "XYZasASDFASDasdfasdfasdfsd" } )
				JObject jo = (JObject)t;
				jo.WriteTo(writer);
			}
			else
			{
				// Serialize as a simple string value (i.e., "035cd888286d39e64c7c52a49561b323c732fc5cfa48d73faedcf5c40d99747474"
				JToken pubKey = JToken.FromObject(req.ScriptPubKey);
				pubKey.WriteTo(writer);
			}
		}
	}
}
