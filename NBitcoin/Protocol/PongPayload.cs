using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	[Payload("pong")]
	public class PongPayload : Payload
	{
		private uint _Nonce;
		public uint Nonce
		{
			get
			{
				return _Nonce;
			}
			set
			{
				_Nonce = value;
			}
		}

		public override void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _Nonce);
		}
	}
}
