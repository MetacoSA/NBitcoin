#if !NOJSONNET
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace NBitcoin.RPC
{
	abstract class RawFormatter
	{
		protected RawFormatter()
		{
			Network = Network.Main;
		}
		public Network Network
		{
			get;
			set;
		}

		[Obsolete("Use RawFormatter.ParseJson method instead")]
		public Transaction Parse(string str)
		{
			JObject obj = JObject.Parse(str);
			return Parse(obj);
		}

		[Obsolete("Do not parse JSON")]
		public Transaction Parse(JObject obj)
		{
			Transaction tx = new Transaction();
			BuildTransaction(obj, tx);
			return tx;
		}
		protected void WritePropertyValue<TValue>(JsonWriter writer, string name, TValue value)
		{
			writer.WritePropertyName(name);
			writer.WriteValue(value);
		}


		protected abstract void BuildTransaction(JObject json, Transaction tx);
		public string ToString(Transaction transaction)
		{
			var strWriter = new StringWriter();
			var jsonWriter = new JsonTextWriter(strWriter);
			jsonWriter.Formatting = Formatting.Indented;
			jsonWriter.WriteStartObject();
			WriteTransaction(jsonWriter, transaction);
			jsonWriter.WriteEndObject();
			jsonWriter.Flush();
			return strWriter.ToString();
		}

		protected abstract void WriteTransaction(JsonTextWriter writer, Transaction tx);
	}
}
#endif
