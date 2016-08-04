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
			_Nonce = RandomUtils.GetUInt64();
		}
		private ulong _Nonce;
		public ulong Nonce
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

		public override string ToString()
		{
			return base.ToString() + " : " + Nonce;
		}
	}
}
