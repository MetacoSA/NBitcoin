using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.JsonConverters
{
	static class JsonConverterUtils
	{
		public static void AssertJsonType(this JsonReader reader, JsonToken expectedType)
		{
			if (reader.TokenType != expectedType)
				throw new JsonObjectException($"Unexpected json token type, expected is {expectedType} and actual is {reader.TokenType}", reader);
		}
		public static void AssertJsonType(this JsonReader reader, JsonToken[] anyExpectedTypes)
		{
			if (!anyExpectedTypes.Contains(reader.TokenType))
				throw new JsonObjectException($"Unexpected json token type, expected are {string.Join(", ", anyExpectedTypes)} and actual is {reader.TokenType}", reader);
		}
	}
}
