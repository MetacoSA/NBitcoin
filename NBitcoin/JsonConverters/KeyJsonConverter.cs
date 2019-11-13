#if !NOJSONNET
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.JsonConverters
{
#if !NOJSONNET
	public
#else
	internal
#endif
	class KeyJsonConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return typeof(Key) == objectType || typeof(PubKey) == objectType;
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;
			reader.AssertJsonType(JsonToken.String);
			try
			{

				var bytes = Encoders.Hex.DecodeData((string)reader.Value);
				if (objectType == typeof(Key))
					return new Key(bytes);
				else
					return new PubKey(bytes);
			}
			catch (EndOfStreamException)
			{
			}
			catch (FormatException)
			{
			}
			throw new JsonObjectException("Invalid bitcoin object of type " + objectType.Name, reader);
		}

		private static void InverseIfNeeded(Type type, byte[] bytes)
		{
			var inverse = type == typeof(uint256) || type == typeof(uint160);
			if (inverse)
				Array.Reverse(bytes);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value != null)
			{
				var bytes = ((IBitcoinSerializable)value).ToBytes();
				writer.WriteValue(Encoders.Hex.EncodeData(bytes));
			}
		}
	}
}
#endif
