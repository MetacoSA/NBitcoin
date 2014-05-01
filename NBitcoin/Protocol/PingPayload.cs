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
		readonly Random _Rand = new Random();
		public PingPayload()
		{
			lock(_Rand)
			{
				byte[] nonce = new byte[sizeof(uint)];
				_Rand.NextBytes(nonce);
				_Nonce = BitConverter.ToUInt32(nonce, 0);
			}
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
