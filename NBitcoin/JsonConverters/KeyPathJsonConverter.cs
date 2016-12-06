#if !NOJSONNET
using NBitcoin;
using System;
using System.Reflection;
using Newtonsoft.Json;

namespace NBitcoin.JsonConverters
{
#if !NOJSONNET
	public
#else
	internal
#endif
	class KeyPathJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(KeyPath).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                return reader.TokenType == JsonToken.Null ? null : KeyPath.Parse(reader.Value.ToString());
            }
            catch (FormatException)
            {
                throw new JsonObjectException("Invalid key path", reader);
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var keyPath = value as KeyPath;
            if (keyPath != null)
                writer.WriteValue(keyPath.ToString());
        }
    }
}
#endif