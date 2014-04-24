using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class BlockChain
	{
		public virtual bool Contains(uint256 blockHash)
		{
			return hashes.Contains(blockHash);
		}

		public void Add(Block block)
		{
			hashes.Add(block.GetHash());
		}

		List<uint256> hashes = new List<uint256>();
	}
	public class ValidateAllBlockChain : BlockChain
	{
		public override bool Contains(uint256 blockHash)
		{
			return true;
		}
	}
}
