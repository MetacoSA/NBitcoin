using System.Threading.Tasks;

namespace nStratis
{
	public interface IBlockRepository
	{
		Task<Block> GetBlockAsync(uint256 blockId);
	}

	public interface IBlockTransactionMapStore
	{
		uint256 GetBlockHash(uint256 trxHash);
	}

}
