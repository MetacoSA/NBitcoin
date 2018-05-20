using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class NoSqlBlockRepository : IBlockRepository
	{
		NoSqlRepository _Repository;
		public NoSqlBlockRepository(NoSqlRepository repository)
		{
			if(repository == null)
				throw new ArgumentNullException(nameof(repository));
			_Repository = repository;
		}
		public NoSqlBlockRepository()
			: this(new InMemoryNoSqlRepository())
		{

		}

		#region IBlockRepository Members

		public Task<Block> GetBlockAsync(uint256 blockId)
		{
			return _Repository.GetAsync<Block>(blockId.ToString());
		}

		#endregion

		public Task PutAsync(Block block)
		{
			return PutAsync(block.GetHash(), block);
		}
		public Task PutAsync(uint256 blockId, Block block)
		{
			return _Repository.PutAsync(blockId.ToString(), block);
		}
	}
}
