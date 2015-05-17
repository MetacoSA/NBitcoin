using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	[Payload("filterload")]
	public class FilterLoadPayload : BitcoinSerializablePayload<BloomFilter>
	{
		public FilterLoadPayload()
		{

		}
		public FilterLoadPayload(BloomFilter filter)
			: base(filter)
		{

		}
	}
}
