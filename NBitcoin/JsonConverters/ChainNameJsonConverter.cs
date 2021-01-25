#if !NOJSONNET
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
	class ChainNameJsonConverter : JsonConverter<ChainName>
	{
		public override void WriteJson(JsonWriter writer, ChainName value, JsonSerializer serializer)
		{
			if (value is ChainName)
			{
				writer.WriteValue(value.ToString());
			}
		}
		public override ChainName ReadJson(JsonReader reader, Type objectType, ChainName existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;
			reader.AssertJsonType(JsonToken.String);
			var v = (string)reader.Value;
			if (v.Length == 0)
				return null;
			return new ChainName(v);
		}
	}
}
#endif
