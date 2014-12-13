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
			Message.Command = payload.Command;
			Message.Magic = network.Magic;
			Message.UpdatePayload(payload, ProtocolVersion.PROTOCOL_VERSION);
		}
		public Message Message
		{
			get;
			set;
		}
		public Socket Socket
		{
			get;
			set;
		}
		public Node Node
		{
			get;
			set;
		}

		public T AssertPayload<T>()
		{
			if(Message.Payload is T)
				return (T)(Message.Payload);
			else
			{
				var ex = new FormatException("Expected message " + typeof(T).Name + " but got " + Message.Payload.GetType().Name);
				throw ex;
			}
		}
	}
}
#endif