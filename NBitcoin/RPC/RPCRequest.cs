using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
    [DataContract]
	public class RPCRequest
	{
		public RPCRequest(string method, object[] parameters)
			: this()
		{
			Method = method;
			Params = parameters;
		}
		public RPCRequest()
		{
			JsonRpc = "1.0";
			Id = 1;
		}

        [DataMember(Name="jsonrpc")]
        public string JsonRpc { get; set; }

        [DataMember(Name="id")]
        public int Id { get; set; }

        [DataMember(Name="method")]
        public string Method { get; set; }

        [DataMember(Name="params")]
        public object[] Params { get; set; }

		public void WriteJSON(TextWriter writer)
		{
			var jsonWriter = new JsonTextWriter(writer);
			WriteJSON(jsonWriter);
			jsonWriter.Flush();
		}

		public void WriteJSON(JsonTextWriter writer)
		{
			writer.WriteStartObject();
			WriteProperty(writer, "jsonrpc", JsonRpc);
			WriteProperty(writer, "id", Id);
			WriteProperty(writer, "method", Method);

			writer.WritePropertyName("params");
			writer.WriteStartArray();

			if(Params != null)
			{
				for(int i = 0 ; i < Params.Length ; i++)
				{
					if(Params[i] is JToken)
					{
						((JToken)Params[i]).WriteTo(writer);
					}
					else
					{
						writer.WriteValue(Params[i]);
					}
				}
			}

			writer.WriteEndArray();
			writer.WriteEndObject();
		}

		private void WriteProperty<TValue>(JsonTextWriter writer, string property, TValue value)
		{
			writer.WritePropertyName(property);
			writer.WriteValue(value);
		}
	}
}
