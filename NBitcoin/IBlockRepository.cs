using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin
{
	public interface IBlockRepository
	{
		Task<Block> GetBlockAsync(uint256 blockId, CancellationToken cancellationToken);
	}
}
