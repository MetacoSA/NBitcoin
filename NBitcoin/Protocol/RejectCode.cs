using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public enum RejectCode : byte
	{
		MALFORMED = 0x01,
		INVALID = 0x10,
		OBSOLETE = 0x11,
		DUPLICATE = 0x12,
		NONSTANDARD = 0x40,
		DUST = 0x41,
		INSUFFICIENTFEE = 0x42,
		CHECKPOINT = 0x43
	}
}
