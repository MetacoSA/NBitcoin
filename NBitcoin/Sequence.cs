using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public struct Sequence
	{
		/* If this flag set, CTxIn::nSequence is NOT interpreted as a
 * relative lock-time. */
		public const uint SEQUENCE_LOCKTIME_DISABLE_FLAG = (1U << 31);

		/* If CTxIn::nSequence encodes a relative lock-time and this flag
		 * is set, the relative lock-time has units of 512 seconds,
		 * otherwise it specifies blocks with a granularity of 1. */
		public const uint SEQUENCE_LOCKTIME_TYPE_FLAG = (1U << 22);

		/* If CTxIn::nSequence encodes a relative lock-time, this mask is
		 * applied to extract that lock-time from the sequence field. */
		public const uint SEQUENCE_LOCKTIME_MASK = 0x0000ffff;
	}
}
