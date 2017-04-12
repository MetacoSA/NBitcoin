﻿#if !NOJSONNET
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
	class Base58DataJsonConverter : JsonConverter
	{
		public Base58DataJsonConverter()
		{

		}
		public Base58DataJsonConverter(Network network)
		{
			Network = network;
		}
		public override bool CanConvert(Type objectType)
		{
			return
				typeof(Base58Data).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo()) ||
				(typeof(IDestination).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo()) && objectType.GetTypeInfo().AssemblyQualifiedName.Contains("NBitcoin"));
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if(reader.TokenType == JsonToken.Null)
				return null;

			try
			{
				var result = Base58Data.GetFromBase58Data(reader.Value.ToString());
				if(result == null)
				{
					throw new JsonObjectException("Invalid Base58Check data", reader);
				}
				if(Network != null)
				{
					if(result.Network != Network)
					{
						result = Base58Data.GetFromBase58Data(reader.Value.ToString(), Network);
						if(result.Network != Network)
						{
							throw new JsonObjectException("Invalid Base58Check network", reader);
						}
					}
				}
				if(!objectType.GetTypeInfo().IsAssignableFrom(result.GetType().GetTypeInfo()))
				{
					throw new JsonObjectException("Invalid Base58Check type expected " + objectType.Name + ", actual " + result.GetType().Name, reader);
				}
				return result;
			}
			catch(FormatException)
			{
				throw new JsonObjectException("Invalid Base58Check data", reader);
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var base58 = value as Base58Data;
			if(base58 != null)
			{
				writer.WriteValue(value.ToString());
			}
		}

		public Network Network
		{
			get;
			set;
		}
	}

	class BitcoinAddressJsonConverter : JsonConverter
	{
		public BitcoinAddressJsonConverter()
		{

		}
		public BitcoinAddressJsonConverter(Network network)
		{
			Network = network;
		}
		public override bool CanConvert(Type objectType)
		{
			return
				typeof(BitcoinAddress).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo()) ||
				(typeof(IDestination).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo()) && objectType.GetTypeInfo().AssemblyQualifiedName.Contains("NBitcoin"));
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;

			try
			{
				var result = BitcoinAddress.Create(reader.Value.ToString());
				if (result == null)
				{
					throw new JsonObjectException("Invalid Bitcoin Address data", reader);
				}
				if (Network != null)
				{
					if (result.Network != Network)
					{
						result = BitcoinAddress.Create(reader.Value.ToString(), Network);
						if (result.Network != Network)
						{
							throw new JsonObjectException("Invalid Bitcoin Address network", reader);
						}
					}
				}
				if (!objectType.GetTypeInfo().IsAssignableFrom(result.GetType().GetTypeInfo()))
				{
					throw new JsonObjectException("Invalid Bitcoin Address type expected " + objectType.Name + ", actual " + result.GetType().Name, reader);
				}
				return result;
			}
			catch (FormatException)
			{
				throw new JsonObjectException("Invalid Bitcoin Address data", reader);
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var address = value as BitcoinAddress;
			if (address != null)
			{
				writer.WriteValue(value.ToString());
			}
		}

		public Network Network
		{
			get;
			set;
		}
	}
}
#endif