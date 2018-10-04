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
			return objectType == typeof(LockTime);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			try
			{
				return reader.TokenType == JsonToken.Null ? LockTime.Zero : new LockTime((uint)(long)reader.Value);
			}
			catch
			{
				throw new JsonObjectException("Invalid locktime", reader);
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if(value != null)
			{
				writer.WriteValue(((LockTime)value).Value);
			}
		}
	}
}
#endif