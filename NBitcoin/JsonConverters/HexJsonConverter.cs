#if !NOJSONNET
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
	class HexJsonConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(byte[]);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			try
			{
				if (reader.TokenType == JsonToken.Null)
					return null;
				reader.AssertJsonType(JsonToken.String);
				return Encoders.Hex.DecodeData((string)reader.Value);
			}
			catch
			{
				throw new JsonObjectException("Invalid hex", reader);
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value != null)
			{
				writer.WriteValue(Encoders.Hex.EncodeData((byte[])value));
			}
		}
	}
}
#endif
