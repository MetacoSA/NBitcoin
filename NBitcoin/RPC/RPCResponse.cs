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
	//{"code":-32601,"message":"Method not found"}
	public class RPCError
	{
        public RPCError()
        { }

        public RPCError(JObject error)
        {
            Code = (RPCErrorCode)((int)error.GetValue("code"));
            Message = (string)error.GetValue("message");
        }

		public RPCErrorCode Code
		{
			get;
			set;
		}

		public string Message
		{
			get;
			set;
		}
	}
	//{"result":null,"error":{"code":-32601,"message":"Method not found"},"id":1}
	public class RPCResponse
	{
        public RPCResponse()
        { }

		public RPCResponse(JObject json)
		{
			var error = json.GetValue("error") as JObject;
			if(error != null)
			{
				Error = new RPCError(error);
			}
			Result = json.GetValue("result") as JToken;
		}

		public RPCError Error
		{
			get;
			set;
		}

		public JToken Result
		{
			get;
			set;
		}

		public static RPCResponse Load(Stream stream)
		{
			JsonTextReader reader = new JsonTextReader(new StreamReader(stream, Encoding.UTF8));
			return new RPCResponse(JObject.Load(reader));
		}

		public void ThrowIfError()
		{
			if(Error != null && Error.Code != RPCErrorCode.NO_ERROR)
			{
				throw new RPCException(Error.Code, Error.Message, this);
			}
		}
	}
}
