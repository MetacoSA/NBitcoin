using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class BlockLocator : IBitcoinSerializable
	{
		public BlockLocator()
		{

		}
		public BlockLocator(List<uint256> hashes)
		{
			vHave = hashes;
		}

		List<uint256> vHave = new List<uint256>();
		public List<uint256> Blocks
		{
			get
			{
				return vHave;
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref vHave);
		}

		#endregion
	}
}
