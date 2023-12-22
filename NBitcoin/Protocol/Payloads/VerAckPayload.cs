using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{

	public class VerAckPayload : Payload, IBitcoinSerializable
	{
		#region IBitcoinSerializable Members

		public override void ReadWriteCore(BitcoinStream stream)
		{
		}

		#endregion
	}
}
