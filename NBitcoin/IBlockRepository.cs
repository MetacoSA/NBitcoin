using System.Threading.Tasks;

namespace NBitcoin
{
	public interface IBlockRepository
	{
		Task<Block> GetBlockAsync(uint256 blockId);

		Task<Block> GetBlockAsync(uint256 blockId, int verbosity);
	}
}
