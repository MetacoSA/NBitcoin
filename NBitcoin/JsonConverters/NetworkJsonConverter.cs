using NBitcoin;
using Newtonsoft.Json;
using System;
using System.Reflection;

namespace NBitcoin.JsonConverters
{
#if !NOJSONNET
	public
#else
	internal
#endif
	class NetworkJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Network).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var network = (string)reader.Value;
            if (network == null)
                return null;
            if (network.Equals("MainNet", StringComparison.OrdinalIgnoreCase) || network.Equals("main", StringComparison.OrdinalIgnoreCase))
                return Network.Main;
            if (network.Equals("TestNet", StringComparison.OrdinalIgnoreCase) || network.Equals("test", StringComparison.OrdinalIgnoreCase))
                return Network.TestNet;
            throw new JsonObjectException("Unknown network (valid values : main, test)", reader);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var net = (Network)value;
            String str = null;
            if (net == Network.Main)
                str = "MainNet";
            if (net == Network.TestNet)
                str = "TestNet";
            if (str != null)
                writer.WriteValue(str);
        }
    }
}
