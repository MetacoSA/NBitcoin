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
	[Payload("getaddr")]
	public class GetAddrPayload : Payload
	{
	}

	/// <summary>
	/// Ask for known peer addresses in the network (BIP 155)
	/// </summary>
	[Payload("getaddrv2")]
	public class GetAddrV2Payload : Payload
	{
	}
}
