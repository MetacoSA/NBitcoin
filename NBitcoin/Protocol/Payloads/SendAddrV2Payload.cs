using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	/// <summary>
	/// Ask for known peer addresses in the network
	/// </summary>
	[Payload("sendaddrv2")]
	public class SendAddrV2Payload : Payload
	{
	}
}
