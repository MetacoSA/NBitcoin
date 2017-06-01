using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	/// <summary>
	/// Ask for the mempool, followed by inv messages
	/// </summary>
	[Payload("mempool")]
	public class MempoolPayload : Payload
	{
	}
}
