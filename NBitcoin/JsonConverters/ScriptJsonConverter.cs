using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace NBitcoin.JsonConverters
{
	class ScriptJsonConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return typeof(Script).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			try
			{
				return reader.TokenType == JsonToken.Null ? null : Script.FromBytesUnsafe(Encoders.Hex.DecodeData((string)reader.Value));
			}
			catch(FormatException)
			{
				throw new FormatException("A script should be a byte string : " + reader.Path);
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			writer.WriteValue(Encoders.Hex.EncodeData(((Script)value).ToBytes(false)));
		}
	}
}
