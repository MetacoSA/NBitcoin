#if !NOJSONNET
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.JsonConverters
{
	public class HDFingerprintJsonConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(HDFingerprint) || objectType == typeof(HDFingerprint?);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			try
			{
				if (reader.TokenType == JsonToken.Null)
					return default;
				reader.AssertJsonType(new[] { JsonToken.Integer, JsonToken.String });
				if (reader.TokenType == JsonToken.String)
					return new HDFingerprint(NBitcoin.DataEncoders.Encoders.Hex.DecodeData((string)reader.Value));
				return new HDFingerprint((uint)(long)reader.Value);
			}
			catch
			{
				throw new JsonObjectException("Invalid HDFingerprint", reader);
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value != null)
			{
				writer.WriteValue(NBitcoin.DataEncoders.Encoders.Hex.EncodeData(((HDFingerprint)value).ToBytes()));
			}
		}
	}
}
#endif
