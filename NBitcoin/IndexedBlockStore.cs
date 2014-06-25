using NBitcoin.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class IndexedBlockStore : IndexedStore<StoredBlock, Block>, IBlockProvider
	{
		private readonly BlockStore _Store;

		public new BlockStore Store
		{
			get
			{
				return _Store;
			}
		}
		public IndexedBlockStore(NoSqlRepository index, BlockStore store)
			: base(index, store)
		{
			_Store = store;
			IndexedLimit = "Last Index Position";
		}

		public BlockHeader GetHeader(uint256 hash)
		{
			var pos = Index.Get<DiskBlockPos>(hash.ToString());
			if(pos == null)
				return null;
			var stored = _Store.Enumerate(false, new DiskBlockPosRange(pos)).FirstOrDefault();
			if(stored == null)
				return null;
			return stored.Item.Header;
		}

		public Block Get(uint256 id)
		{
			return Get(id.ToString());
		}

		#region IBlockProvider Members

		public Block GetBlock(uint256 id, List<byte[]> searchedData)
		{
			var block = Get(id.ToString());
			if(block == null)
				throw new Exception("Block " + id + " not present in the index");
			return block;
		}

		#endregion

		protected override string GetKey(Block item)
		{
			return item.GetHash().ToString();
		}

		protected override IEnumerable<StoredBlock> EnumerateForIndex(DiskBlockPosRange range)
		{
			return Store.Enumerate(true, range);
		}

		protected override IEnumerable<StoredBlock> EnumerateForGet(DiskBlockPosRange range)
		{
			return Store.Enumerate(false, range);
		}
	}
}
