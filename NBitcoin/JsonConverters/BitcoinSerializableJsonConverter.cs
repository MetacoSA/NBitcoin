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
	class BitcoinSerializableJsonConverter : JsonConverter
	{
		public BitcoinSerializableJsonConverter(Network network)
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
			return typeof(IBitcoinSerializable).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;
			reader.AssertJsonType(JsonToken.String);
			try
			{
				IBitcoinSerializable obj = null;
				var bytes = Encoders.Hex.DecodeData((string)reader.Value);
				if (!Network.Consensus.ConsensusFactory.TryCreateNew(objectType, out obj))
				{
					if (objectType == typeof(PubKey))
					{
						obj = new PubKey(bytes);
					}
					else
					{
						obj = (IBitcoinSerializable)Activator.CreateInstance(objectType);
					}
				}
				obj.ReadWrite(bytes, Network);
				return obj;
			}
			catch (EndOfStreamException)
			{
			}
			catch (FormatException)
			{
			}
			throw new JsonObjectException("Invalid bitcoin object of type " + objectType.Name, reader);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var bytes = ((IBitcoinSerializable)value).ToBytes();
			writer.WriteValue(Encoders.Hex.EncodeData(bytes));
		}
	}
}
#endif
