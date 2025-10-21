using System;
using NBitcoin.RPC;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NBitcoin.JsonConverters;

class RPCErrorJsonConverter : JsonConverter<RPCError>
{
	public override void WriteJson(JsonWriter writer, RPCError value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}

	public override RPCError ReadJson(JsonReader reader, Type objectType, RPCError existingValue, bool hasExistingValue,
		JsonSerializer serializer)
	{
		if (reader.TokenType != JsonToken.StartObject)
			return null;
		return new((JObject)JObject.ReadFrom(reader));
	}
}
