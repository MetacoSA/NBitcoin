#if !NOJSONNET
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.JsonConverters
{
#if !NOJSONNET
	public
#else
	internal
#endif
	class SequenceJsonConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(Sequence) || objectType == typeof(Sequence?);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			try
			{
				var nullable = objectType == typeof(Sequence?);
				if (reader.TokenType == JsonToken.Null)
				{
					if (nullable)
						return null;
					return LockTime.Zero;
				}
				reader.AssertJsonType(JsonToken.Integer);
				return new Sequence((uint)(long)reader.Value);
			}
			catch
			{
				throw new JsonObjectException("Invalid sequence", reader);
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value != null)
			{
				writer.WriteValue(((Sequence)value).Value);
			}
		}
	}
}
#endif
