using System.Collections.Generic;

namespace NBitcoin.BitcoinCore
{
	public interface IBlockProvider
	{
		Block GetBlock(uint256 id, List<byte[]> searchedData);
	}
}
