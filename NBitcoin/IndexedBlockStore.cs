using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class IndexedBlockStore
	{
		private readonly NoSqlRepository _Index;
		private readonly BlockStore _Store;

		public BlockStore Store
		{
			get
			{
				return _Store;
			}
		}
		public IndexedBlockStore(NoSqlRepository index, BlockStore store)
		{
			if(index == null)
				throw new ArgumentNullException("index");
			if(store == null)
				throw new ArgumentNullException("store");
			_Index = index;
			_Store = store;
		}
		const string IndexedLimit = "Last Index Position";
		public int ReIndex()
		{
			var last = _Index.Get<DiskBlockPos>(IndexedLimit);
			int count = 0;
			List<StoredBlock> lastBlocks = null;
			foreach(var blocks in _Store.Enumerate(true, new DiskBlockPosRange(last)).Partition(400))
			{
				count += blocks.Count;
				_Index.PutBatch(blocks.Select(b => new Tuple<String, IBitcoinSerializable>(b.Item.GetHash().ToString(), b.BlockPosition)));
				lastBlocks = blocks;
			}
			if(lastBlocks != null && lastBlocks.Count > 0)
			{
				var block = lastBlocks.Last();
				_Index.Put(IndexedLimit, new DiskBlockPos(block.BlockPosition.File, block.BlockPosition.Position + (uint)block.GetStorageSize()));
			}
			return count;
		}

		public Block Get(uint256 hash)
		{
			var pos = _Index.Get<DiskBlockPos>(hash.ToString());
			if(pos == null)
				return null;
			var stored = _Store.Enumerate(false, new DiskBlockPosRange(pos)).FirstOrDefault();
			if(stored == null)
				return null;
			return stored.Item;
		}
		public BlockHeader GetHeader(uint256 hash)
		{
			var pos = _Index.Get<DiskBlockPos>(hash.ToString());
			if(pos == null)
				return null;
			var stored = _Store.Enumerate(false, new DiskBlockPosRange(pos)).FirstOrDefault();
			if(stored == null)
				return null;
			return stored.Item.Header;
		}

		public void Put(Block block)
		{
			var hash = block.Header.GetHash();
			var position = _Store.Append(block);
			_Index.Put(hash.ToString(), position);
			_Index.Put(IndexedLimit, position);
		}

	}
}
