using System.Collections.Generic;

namespace nStratis.BitcoinCore
{
	public interface IBlockProvider
	{
		Block GetBlock(uint256 id, List<byte[]> searchedData);
	}
}
