#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class IncomingMessage
	{
		public IncomingMessage()
		{

		}
		public IncomingMessage(Payload payload, Network network)
		{
			Message = new Message();
			Message.Magic = network.Magic;
			Message.Payload = payload;
		}
		public Message Message
		{
			get;
			set;
		}
		internal Socket Socket
		{
			get;
			set;
		}
		public Node Node
		{
			get;
			set;
		}
		public long Length
		{
			get;
			set;
		}

		internal T AssertPayload<T>() where T : Payload
		{
			if(Message.Payload is T)
				return (T)(Message.Payload);
			else
			{
				var ex = new ProtocolException("Expected message " + typeof(T).Name + " but got " + Message.Payload.GetType().Name);
				throw ex;
			}
		}
	}
}
#endif