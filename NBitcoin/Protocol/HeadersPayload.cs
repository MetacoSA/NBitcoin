using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	[Payload("headers")]
	public class HeadersPayload : Payload
	{
		List<BlockHeader> headers = new List<BlockHeader>();

		public List<BlockHeader> Headers
		{
			get
			{
				return headers;
			}
		}

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref headers);
		}
	}
}
