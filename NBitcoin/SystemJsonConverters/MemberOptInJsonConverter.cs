using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NBitcoin.SystemJsonConverters
{
	public class MemberOptInJsonConverter<T> : JsonConverter<T> where T : class
	{
		public override bool HandleNull => false;

		public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			return JsonSerializer.Deserialize<T>(ref reader, options);
		}

		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			if (value is null) return;
			writer.WriteStartObject();
			var type = value.GetType();
			var props = type.GetProperties();

			foreach (var property in props)
			{
				var propValue = property.GetValue(value);
				var jsonAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
				if (jsonAttribute is null)
				{
					continue;
				}

				var ignoreAttribute = property.GetCustomAttribute<JsonIgnoreAttribute>();
				if (ignoreAttribute is not null)
				{
					switch (ignoreAttribute.Condition)
					{
						case JsonIgnoreCondition.Never:
							break;
						case JsonIgnoreCondition.Always:
							continue;
						case JsonIgnoreCondition.WhenWritingDefault:
							if (propValue == null)
							{
								continue;
							}

							break;
						case JsonIgnoreCondition.WhenWritingNull:

							if (propValue == default)
							{
								continue;
							}

							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				switch (true)
				{
					case true when propValue is not null:
					case true when propValue is null && options.DefaultIgnoreCondition == JsonIgnoreCondition.Never:
						writer.WritePropertyName(property.Name);
						JsonSerializer.Serialize(writer, propValue, options);
						break;
				}
			}

			writer.WriteEndObject();
		}
	}

	public class MemberOptInJsonConverter : JsonConverterFactory
	{
		public override bool CanConvert(Type typeToConvert) => !typeToConvert.IsPrimitive;

		public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
		{
			JsonConverter converter = (JsonConverter) Activator.CreateInstance(
				typeof(MemberOptInJsonConverter<>)
					.MakeGenericType(new Type[] {typeToConvert}),
				BindingFlags.Instance | BindingFlags.Public,
				binder: null,
				args: null,
				culture: null)!;

			return converter;
		}
	}
}
