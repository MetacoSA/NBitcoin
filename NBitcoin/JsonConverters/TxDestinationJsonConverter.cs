#if !NOJSONNET
using NBitcoin;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NBitcoin.JsonConverters
{
#if !NOJSONNET
	public
#else
	internal
#endif
	class TxDestinationJsonConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(KeyId) ||
				objectType == typeof(ScriptId) ||
				objectType == typeof(WitKeyId) ||
				objectType == typeof(WitScriptId);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;
			reader.AssertJsonType(JsonToken.String);
			try
			{
				if (objectType == typeof(KeyId))
					return new KeyId(Encoders.Hex.DecodeData((string)reader.Value));
				if (objectType == typeof(ScriptId))
					return new ScriptId(Encoders.Hex.DecodeData((string)reader.Value));
				if (objectType == typeof(WitKeyId))
					return new WitKeyId(Encoders.Hex.DecodeData((string)reader.Value));
				if (objectType == typeof(WitScriptId))
					return new WitScriptId(Encoders.Hex.DecodeData((string)reader.Value));
			}
			catch
			{
			}
			throw new JsonObjectException("Invalid signature", reader);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value != null)
			{
				if (value is KeyId)
					writer.WriteValue(Encoders.Hex.EncodeData(((KeyId)value).ToBytes()));
				if (value is ScriptId)
					writer.WriteValue(Encoders.Hex.EncodeData(((ScriptId)value).ToBytes()));
				if (value is WitKeyId)
					writer.WriteValue(Encoders.Hex.EncodeData(((WitKeyId)value).ToBytes()));
				if (value is WitScriptId)
					writer.WriteValue(Encoders.Hex.EncodeData(((WitScriptId)value).ToBytes()));
			}
		}
	}
}
#endif
