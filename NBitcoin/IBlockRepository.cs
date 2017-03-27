using System.Threading.Tasks;

namespace NBitcoin
{
	public interface IBlockRepository
	{
		Task<Block> GetBlockAsync(uint256 blockId);
	}

	public interface IBlockTransactionMapStore
	{
		uint256 GetBlockHash(uint256 trxHash);
	}

	public interface IBlockStakeRepository
	{
		BlockStake GetBlockStake(uint256 blockId);
	}
}
