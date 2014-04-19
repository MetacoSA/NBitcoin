using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
	[Payload("verack")]
	public class VerAckPayload : IBitcoinSerializable
	{
		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
		}

		#endregion
	}
}
