#if !NOJSONNET
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
			reader.AssertJsonType(JsonToken.String);
			var network = (string)reader.Value;
			if (network == null)
				return null;
			if (network.Equals("MainNet", StringComparison.OrdinalIgnoreCase) || network.Equals("main", StringComparison.OrdinalIgnoreCase))
				return Network.Main;
			if (network.Equals("TestNet", StringComparison.OrdinalIgnoreCase) || network.Equals("test", StringComparison.OrdinalIgnoreCase))
				return Network.TestNet;
			if (network.Equals("RegTest", StringComparison.OrdinalIgnoreCase) || network.Equals("reg", StringComparison.OrdinalIgnoreCase))
				return Network.RegTest;
			var net = Network.GetNetwork(network);
			if (net != null)
				return net;
			throw new JsonObjectException("Unknown network (valid values : main, test, reg)", reader);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var net = (Network)value;
			String str = null;
			if (net == Network.Main)
				str = "MainNet";
			else if (net == Network.TestNet)
				str = "TestNet";
			else if (net == Network.RegTest)
				str = "RegTest";
			else if (net != null)
				str = net.ToString();
			if (str != null)
				writer.WriteValue(str);
		}
	}
}
#endif
