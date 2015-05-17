using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	[Payload("merkleblock")]
	public class MerkleBlockPayload : BitcoinSerializablePayload<MerkleBlock>
	{
		public MerkleBlockPayload()
		{

		}
		public MerkleBlockPayload(MerkleBlock block)
			: base(block)
		{

		}
	}
}
