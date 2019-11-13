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
	class OutpointJsonConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return typeof(OutPoint).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;

			reader.AssertJsonType(JsonToken.String);
			try
			{
				if (!OutPoint.TryParse((string)reader.Value, out var outpoint))
					throw new JsonObjectException("Invalid bitcoin object of type OutPoint", reader);
				return outpoint;
			}
			catch (EndOfStreamException)
			{
			}
			throw new JsonObjectException("Invalid bitcoin object of type OutPoint", reader);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var bytes = ((IBitcoinSerializable)value).ToBytes();
			writer.WriteValue(Encoders.Hex.EncodeData(bytes));
		}
	}
}
#endif
