#if !NOJSONNET
using NBitcoin;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NBitcoin.JsonConverters
{
#if !NOJSONNET
	public
#else
	internal
#endif
	class SignatureJsonConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(ECDSASignature) || objectType == typeof(TransactionSignature);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;
			reader.AssertJsonType(JsonToken.String);
			try
			{
				if (objectType == typeof(ECDSASignature))
					return new ECDSASignature(Encoders.Hex.DecodeData((string)reader.Value));

				if (objectType == typeof(TransactionSignature))
					return new TransactionSignature(Encoders.Hex.DecodeData((string)reader.Value));
			}
			catch
			{
			}
			throw new JsonObjectException("Invalid signature", reader);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value != null)
			{
				if (value is ECDSASignature)
					writer.WriteValue(Encoders.Hex.EncodeData(((ECDSASignature)value).ToDER()));
				if (value is TransactionSignature)
					writer.WriteValue(Encoders.Hex.EncodeData(((TransactionSignature)value).ToBytes()));
			}
		}
	}
}
#endif
