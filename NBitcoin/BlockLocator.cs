using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class BlockLocator
	{
		public BlockLocator(List<uint256> hashes)
		{
			vHave = hashes;
		}

		List<uint256> vHave;
		public IEnumerable<uint256> Blocks
		{
			get
			{
				return vHave;
			}
		}
	}
}
