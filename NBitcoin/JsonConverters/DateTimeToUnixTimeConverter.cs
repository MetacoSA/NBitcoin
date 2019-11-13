#if !NOJSONNET
using NBitcoin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;

namespace NBitcoin.JsonConverters
{
#if !NOJSONNET
	public
#else
	internal
#endif
	class DateTimeToUnixTimeConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return typeof(DateTime).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo()) ||
				typeof(DateTimeOffset).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo()) ||
				typeof(DateTimeOffset?).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.Value == null)
				return null;
			if (reader.TokenType == JsonToken.Null)
				return null;
			reader.AssertJsonType(JsonToken.Integer);
			var result = Utils.UnixTimeToDateTime((ulong)(long)reader.Value);
			if (objectType == typeof(DateTime))
				return result.UtcDateTime;
			return result;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			DateTime time;
			if (value is DateTime)
				time = (DateTime)value;
			else
				time = ((DateTimeOffset)value).UtcDateTime;

			if (time < Utils.UnixTimeToDateTime(0))
				time = Utils.UnixTimeToDateTime(0).UtcDateTime;
			writer.WriteValue(Utils.DateTimeToUnixTime(time));
		}
	}
}
#endif
