using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using NBitcoin.DataEncoders;

namespace NBitcoin.JsonConverters
{
	class BitcoinSerializableJsonConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return typeof(IBitcoinSerializable).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if(reader.TokenType == JsonToken.Null)
				return null;

			try
			{

				var obj = (IBitcoinSerializable)Activator.CreateInstance(objectType);
				var bytes = Encoders.Hex.DecodeData((string)reader.Value);
				obj.ReadWrite(bytes);
				return obj;
			}
			catch(EndOfStreamException)
			{
			}
			catch(FormatException)
			{
			}
			throw new FormatException("Invalid bitcoin object of type " + objectType.Name + " : " + reader.Path);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var bytes = ((IBitcoinSerializable)value).ToBytes();
			writer.WriteValue(Encoders.Hex.EncodeData(bytes));
		}
	}
}
