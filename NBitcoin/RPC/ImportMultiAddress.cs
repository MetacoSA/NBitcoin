using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace NBitcoin.RPC
{
	[JsonObject(MemberSerialization.OptIn)]
	public class ImportMultiAddress
	{
		public class ScriptPubKeyObject
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

		public class TimestampObject
		{
			public DateTimeOffset? DateTimestamp{ get; set; }

			/// <summary>
			/// Set to "now" or "0". If DateTimestamp is populated, this property will be ignored.
			/// </summary>
			public string NowZero { get; set; }
		}

		[JsonProperty("scriptPubKey", NullValueHandling = NullValueHandling.Ignore)]
		[JsonConverter(typeof(ImportMultiScriptPubKeyConverter))]
		public ScriptPubKeyObject ScriptPubKey { get; set; }

		//[JsonProperty("timestamp")]
		//public uint Timestamp { get; set; }

		[JsonProperty("timestamp")]
		[JsonConverter(typeof(ImportMultiTimestampConverter))]
		public TimestampObject Timestamp { get; set; }

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

	public class ImportMultiTimestampConverter : JsonConverter
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
			ImportMultiAddress.TimestampObject req = (ImportMultiAddress.TimestampObject)value;
			JToken t = JToken.FromObject(value);

			if (req.DateTimestamp.HasValue)
			{
				JToken timestamp = JToken.FromObject(Utils.DateTimeToUnixTime(req.DateTimestamp.Value));
				timestamp.WriteTo(writer);
			}
			else
			{
				// If it's "now" then we serialize a string. Else, serialize as a uint (with "0" expected).
				JToken timestamp =  req.NowZero == "now" ? JToken.FromObject(req.NowZero) : JToken.FromObject(UInt32.Parse(req.NowZero));
				timestamp.WriteTo(writer);
			}
		}
	}

	/// <summary>
	/// Custom JsonConverter to deal with loose type of scriptPubKey property in the ImportMulti method
	/// </summary>
	public class ImportMultiScriptPubKeyConverter : JsonConverter
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
