#if !NOJSONNET
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.JsonConverters
{
	public class LockTimeJsonConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(LockTime) || objectType == typeof(LockTime?);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			try
			{
				var nullable = objectType == typeof(LockTime?);
				if (reader.TokenType == JsonToken.Null)
				{
					if (nullable)
						return null;
					return LockTime.Zero;
				}
				reader.AssertJsonType(JsonToken.Integer);
				return new LockTime((uint)(long)reader.Value);
			}
			catch
			{
				throw new JsonObjectException("Invalid locktime", reader);
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value != null)
			{
				writer.WriteValue(((LockTime)value).Value);
			}
		}
	}
}
#endif
