using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace NBitcoin.JsonConverters
{
	class MoneyJsonConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return typeof(Money).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			try
			{
				return reader.TokenType == JsonToken.Null ? null : new Money((long)reader.Value);
			}
			catch(InvalidCastException)
			{
				throw new FormatException("Money amount should be in satoshi : " +  reader.Path);
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			writer.WriteValue(((Money)value).Satoshi);
		}
	}
}
