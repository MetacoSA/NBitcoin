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
		private readonly ISigningRepository? _signingRepository;

		public OutputDescriptorJsonConverter(Network network, bool requireChecksum = false, ISigningRepository? signingRepository = null) : base()
		{
			_requireChecksum = requireChecksum;
			_signingRepository = signingRepository;
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
				if (!OutputDescriptor.TryParse((string)reader.Value, Network, out var od, _requireChecksum, _signingRepository))
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
				if (_signingRepository is null)
				{
					str = od.ToString();
				}
				else
				{
					if (od.TryGetPrivateString(_signingRepository, out str ) || str != null)
					{
					}
					else
					{
						str = od.ToString();
					}
				}
			}
			if (str != null)
				writer.WriteValue(str);
		}
	}
}

#nullable disable
