using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class BlockRepository
	{
		public void WriteBlock(Block block)
		{
		}

		internal void WriteBlockHeader(BlockHeader header)
		{
			throw new NotImplementedException();
		}
	}
}
