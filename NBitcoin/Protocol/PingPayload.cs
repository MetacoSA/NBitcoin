using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	[Payload("ping")]
	public class PingPayload : Payload
	{
		
		public PingPayload()
		{
			_Nonce = RandomUtils.GetUInt32();
		}
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

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _Nonce);
		}

		public PongPayload CreatePong()
		{
			return new PongPayload()
			{
				Nonce = Nonce
			};
		}
	}
}
