using NBitcoin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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
            return typeof(DateTime).IsAssignableFrom(objectType) || typeof(DateTimeOffset).IsAssignableFrom(objectType) || typeof(DateTimeOffset?).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if(reader.Value == null)
                return null;
            var result =  Utils.UnixTimeToDateTime((ulong)(long)reader.Value);
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