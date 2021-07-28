#if !NOJSONNET
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
	public class RPCRequest
	{
		public RPCRequest(RPCOperations method, object[] parameters)
			: this(method.ToString(), parameters)
		{

		}
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
		public string JsonRpc
		{
			get;
			set;
		}
		public int Id
		{
			get;
			set;
		}
		public string Method
		{
			get;
			set;
		}
		public object[] Params
		{
			get;
			set;
		}
		public bool ThrowIfRPCError { get; set; } = true;
		public Dictionary<string, object> NamedParams
		{
			get; set;
		}

		public void WriteJSON(TextWriter writer)
		{
			var jsonWriter = new JsonTextWriter(writer);
			WriteJSON(jsonWriter);
			jsonWriter.Flush();
		}

		internal void WriteJSON(JsonTextWriter writer)
		{
			writer.WriteStartObject();
			WriteProperty(writer, "jsonrpc", JsonRpc);
			WriteProperty(writer, "id", Id);
			WriteProperty(writer, "method", Method);

			writer.WritePropertyName("params");


			if (Params != null)
			{
				writer.WriteStartArray();
				for (int i = 0; i < Params.Length; i++)
				{
					WriteValue(writer, Params[i]);
				}
				writer.WriteEndArray();
			}
			else if (NamedParams != null)
			{
				writer.WriteStartObject();
				foreach (var namedParam in NamedParams)
				{
					writer.WritePropertyName(namedParam.Key);
					WriteValue(writer, namedParam.Value);
				}
				writer.WriteEndObject();
			}
			else
			{
				writer.WriteStartArray();
				writer.WriteEndArray();
			}

			writer.WriteEndObject();
		}

		private void WriteValue(JsonTextWriter writer, object obj)
		{
			if (obj is JToken)
			{
				((JToken)obj).WriteTo(writer);
			}
			else if (obj is Array)
			{
				writer.WriteStartArray();
				foreach (var x in (Array)obj)
				{
					writer.WriteValue(x);
				}
				writer.WriteEndArray();
			}
			else if (obj is uint256)
			{
				writer.WriteValue(obj.ToString());
			}
			else
			{
				writer.WriteValue(obj);
			}
		}

		private void WriteProperty<TValue>(JsonTextWriter writer, string property, TValue value)
		{
			writer.WritePropertyName(property);
			writer.WriteValue(value);
		}
	}
}
#endif
