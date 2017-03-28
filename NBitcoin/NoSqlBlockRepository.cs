using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class NoSqlBlockRepository : IBlockRepository
	{
		readonly NoSqlRepository repository;

		public NoSqlBlockRepository(NoSqlRepository repository)
		{
			if(repository == null)
				throw new ArgumentNullException(nameof(repository));
			this.repository = repository;
		}

		public NoSqlBlockRepository()
			: this(new InMemoryNoSqlRepository())
		{

		}

		#region IBlockRepository Members

		public Task<Block> GetBlockAsync(uint256 blockId)
		{
			return repository.GetAsync<Block>(blockId.ToString());
		}

		#endregion

		public Task PutAsync(Block block)
		{
			return PutAsync(block.GetHash(), block);
		}
		public Task PutAsync(uint256 blockId, Block block)
		{
			return repository.PutAsync(blockId.ToString(), block);
		}
	}

	/// <summary>
	/// An in memory container of block hashes mapped to trasnaction hashes
	/// </summary>
	public class BlockTransactionMapStore : IBlockTransactionMapStore
	{
		readonly NoSqlRepository repository;

		public BlockTransactionMapStore(NoSqlRepository repository)
		{
			if (repository == null)
				throw new ArgumentNullException(nameof(repository));
			this.repository = repository;
		}

		public BlockTransactionMapStore()
			: this(new InMemoryNoSqlRepository())
		{

		}

		#region IBlockTransactionMapStore Members

		public uint256 GetBlockHash(uint256 trxHash)
		{
			return repository.GetAsync<uint256.MutableUint256>(trxHash.ToString()).Result?.Value;
		}

		#endregion
	
		public void PutAsync(uint256 trxId, uint256 blockId)
		{
			repository.PutAsync(trxId.ToString(), blockId.AsBitcoinSerializable());
		}
	}

	public class MemoryStakeChain : StakeChain
	{
		private readonly Network network;
		private Dictionary<uint256, BlockStake> items = new Dictionary<uint256, BlockStake>();

		public MemoryStakeChain(Network network)
		{
			this.network = network;
		}

		public override BlockStake Get(uint256 blockid)
		{
			return this.items.TryGet(blockid);
		}

		public sealed override void Set(uint256 blockid, BlockStake blockStake)
		{
			// throw if item already exists
			this.items.Add(blockid, blockStake);
		}
	}
}
