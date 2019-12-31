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
			return typeof(KeyPath).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo()) || typeof(RootedKeyPath).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;
			reader.AssertJsonType(JsonToken.String);
			if (typeof(KeyPath).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo()))
			{
				if (KeyPath.TryParse(reader.Value.ToString(), out var k))
					return k;
				throw new JsonObjectException("Invalid key path", reader);
			}
			else
			{
				if (RootedKeyPath.TryParse(reader.Value.ToString(), out var k))
					return k;
				throw new JsonObjectException("Invalid rooted key path", reader);
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value is KeyPath keyPath)
				writer.WriteValue(keyPath.ToString());
			else if (value is RootedKeyPath rootedKeyPath)
				writer.WriteValue(rootedKeyPath.ToString());
		}
	}
}
#endif
