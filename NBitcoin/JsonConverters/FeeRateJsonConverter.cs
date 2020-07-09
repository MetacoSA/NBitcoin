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
	class FeeRateJsonConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return typeof(FeeRate).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;
			reader.AssertJsonType(new[] { JsonToken.Integer, JsonToken.Float });
			try
			{
				if (reader.TokenType == JsonToken.Integer)
					return new FeeRate(Money.Satoshis((long)reader.Value), 1);
				if (reader.TokenType == JsonToken.Float)
				{
					if (reader.Value is decimal v)
						return new FeeRate(v);
					if (reader.Value is double d)
						return new FeeRate((decimal)d);
					return new FeeRate((decimal)(double)reader.Value);
				}
				throw new JsonObjectException("Fee rate amount should be in satoshi", reader);
			}
			catch (InvalidCastException)
			{
				throw new JsonObjectException("Fee rate should be in satoshi", reader);
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			writer.WriteValue(((FeeRate)value).SatoshiPerByte);
		}
	}
}

#endif
