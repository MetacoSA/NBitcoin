#if !NOJSONNET
using NBitcoin;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;

namespace NBitcoin.JsonConverters
{
#if !NOJSONNET
	public
#else
	internal
#endif
	class PSBTJsonConverter : JsonConverter
	{
		public PSBTJsonConverter(Network network)
		{
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			Network = network;
		}

		public Network Network
		{
			get; set;
		}
		public override bool CanConvert(Type objectType)
		{
			return typeof(PSBT).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;
			reader.AssertJsonType(JsonToken.String);
			try
			{
				return PSBT.Parse((string)reader.Value, Network);
			}
			catch (EndOfStreamException)
			{
			}
			catch (FormatException)
			{
			}
			throw new JsonObjectException("Invalid PSBT", reader);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var str = (value as PSBT)?.ToBase64();
			if (str != null)
				writer.WriteValue(str);
		}
	}
}
#endif
