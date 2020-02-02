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
	class BitcoinStringJsonConverter : JsonConverter
	{
		public BitcoinStringJsonConverter(Network network)
		{
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			Network = network;
		}
		public override bool CanConvert(Type objectType)
		{
			return
				typeof(IBitcoinString).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo()) ||
				(typeof(IDestination).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo()) && objectType.GetTypeInfo().AssemblyQualifiedName.Contains("NBitcoin"));
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;
			reader.AssertJsonType(JsonToken.String);
			try
			{
				IBitcoinString result = Network.Parse(reader.Value.ToString(), objectType);
				if (result == null)
				{
					throw new JsonObjectException("Invalid BitcoinString network", reader);
				}

				if (!objectType.GetTypeInfo().IsAssignableFrom(result.GetType().GetTypeInfo()))
				{
					throw new JsonObjectException("Invalid BitcoinString type expected " + objectType.Name + ", actual " + result.GetType().Name, reader);
				}
				return result;
			}
			catch (FormatException ex)
			{
				throw new JsonObjectException(ex.Message, reader);
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var base58 = value as IBitcoinString;
			if (base58 != null)
			{
				writer.WriteValue(value.ToString());
			}
		}

		public Network Network
		{
			get;
			set;
		}
	}
}
#endif
