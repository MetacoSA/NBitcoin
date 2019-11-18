#if !NOJSONNET
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.JsonConverters
{
	public class Serializer
	{
#if !NOJSONNET
		public
#else
	internal
#endif
		static void RegisterFrontConverters(JsonSerializerSettings settings, Network network = null)
		{
			settings.Converters.Add(new MoneyJsonConverter());
			settings.Converters.Add(new KeyJsonConverter());
			if (network != null)
				settings.Converters.Add(new CoinJsonConverter(network));
			settings.Converters.Add(new ScriptJsonConverter());
			settings.Converters.Add(new FeeRateJsonConverter());
			settings.Converters.Add(new UInt160JsonConverter());
			settings.Converters.Add(new UInt256JsonConverter());
			settings.Converters.Add(new LockTimeJsonConverter());
			settings.Converters.Add(new SequenceJsonConverter());
			if (network != null)
				settings.Converters.Add(new PSBTJsonConverter(network));
			settings.Converters.Add(new HDFingerprintJsonConverter());
			settings.Converters.Add(new OutpointJsonConverter());
			if (network != null)
				settings.Converters.Add(new BitcoinSerializableJsonConverter(network));
			settings.Converters.Add(new NetworkJsonConverter());
			settings.Converters.Add(new KeyPathJsonConverter());
			settings.Converters.Add(new SignatureJsonConverter());
			settings.Converters.Add(new HexJsonConverter());
			settings.Converters.Add(new DateTimeToUnixTimeConverter());
			settings.Converters.Add(new TxDestinationJsonConverter());
			if (network != null)
				settings.Converters.Add(new BitcoinStringJsonConverter(network));
			settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
		}

		public static T ToObject<T>(string data)
		{
			return ToObject<T>(data, null);
		}
		public static T ToObject<T>(string data, Network network)
		{
			JsonSerializerSettings settings = new JsonSerializerSettings
			{
				Formatting = Formatting.Indented
			};
			RegisterFrontConverters(settings, network);
			return JsonConvert.DeserializeObject<T>(data, settings);
		}

		public static string ToString<T>(T response, Network network)
		{
			JsonSerializerSettings settings = new JsonSerializerSettings
			{
				Formatting = Formatting.Indented
			};
			RegisterFrontConverters(settings, network);
			return JsonConvert.SerializeObject(response, settings);
		}
		public static string ToString<T>(T response)
		{
			return ToString<T>(response, null);
		}
	}
}
#endif
