using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
	abstract class RawFormatter
	{
		public RawFormatter()
		{
			Network = Network.Main;
		}
		public Network Network
		{
			get;
			set;
		}
		public Transaction Parse(string str)
		{
			JObject obj = JObject.Parse(str);
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
			jsonWriter.WriteStartObject();
			WriteTransaction(jsonWriter, transaction);
			jsonWriter.WriteEndObject();
			jsonWriter.Flush();
			return strWriter.ToString();
		}

		protected abstract void WriteTransaction(JsonTextWriter writer, Transaction tx);
	}
}
