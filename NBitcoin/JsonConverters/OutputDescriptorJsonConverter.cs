#nullable enable
using System;
using System.Reflection;
using Newtonsoft.Json;
using NBitcoin.Scripting;

namespace NBitcoin.JsonConverters
{
	public class OutputDescriptorJsonConverter : JsonConverter
	{
		private readonly bool _requireChecksum;

		public OutputDescriptorJsonConverter(Network network, bool requireChecksum = false) : base()
		{
			_requireChecksum = requireChecksum;
			this.Network = network;
		}

		public Network Network { get; }

		public override bool CanConvert(Type objectType)
		{
			return typeof(OutputDescriptor).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
		}
		public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null || reader.Value is not string)
				return null;
			reader.AssertJsonType(JsonToken.String);
			try
			{
				if (!OutputDescriptor.TryParse((string)reader.Value, Network, out var od, _requireChecksum))
					throw new JsonObjectException("Invalid OutputDescriptor", reader);
				return od;
			}
			catch (FormatException ex)
			{
				throw new JsonObjectException($"Invalid OutputDescriptor {ex}", reader);
			}
		}

		public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
		{
			string? str = null;
			if (value is OutputDescriptor od)
			{
				str = od.ToString();
			}
			if (str != null)
				writer.WriteValue(str);
		}
	}
}

#nullable disable
