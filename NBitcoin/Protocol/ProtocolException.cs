using System;

namespace nStratis.Protocol
{
	public class ProtocolException : Exception
	{
		public ProtocolException(string message)
			: base(message)
		{

		}
	}
}
