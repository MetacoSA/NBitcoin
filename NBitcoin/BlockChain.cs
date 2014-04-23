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
			throw new NotImplementedException();
		}
	}
	public class ValidateAllBlockChain : BlockChain
	{
		public override bool Contains(uint256 blockHash)
		{
			return true;
		}
	}
}
