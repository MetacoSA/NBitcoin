using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	/// <summary>
	/// Compact representation of one's chain position which can be used to find forks with another chain
	/// </summary>
	public class BlockLocator : IBitcoinSerializable
	{
		public BlockLocator()
		{

		}
		public BlockLocator(IEnumerable<uint256> hashes)
		{
			vHave = hashes.ToList();
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
