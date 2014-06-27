using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public abstract class IndexedStore<TStoredItem, TItem>
		where TStoredItem : StoredItem<TItem>
		where TItem : IBitcoinSerializable,new()
	{
		private readonly NoSqlRepository _Index;
		private readonly Store<TStoredItem, TItem> _Store;

		public Store<TStoredItem, TItem> Store
		{
			get
			{
				return _Store;
			}
		}
		public NoSqlRepository Index
		{
			get
			{
				return _Index;
			}
		}
		public IndexedStore(NoSqlRepository index, Store<TStoredItem, TItem> store)
		{
			if(index == null)
				throw new ArgumentNullException("index");
			if(store == null)
				throw new ArgumentNullException("store");
			_Index = index;
			_Store = store;
		}

		protected string IndexedLimit = "Position";
		public int ReIndex()
		{
			var last = _Index.Get<DiskBlockPos>(IndexedLimit);
			int count = 0;
			List<TStoredItem> lastBlocks = null;
			foreach(var blocks in EnumerateForIndex(new DiskBlockPosRange(last)).Partition(400))
			{
				count += blocks.Count;
				_Index.PutBatch(blocks.Select(b => new Tuple<String, IBitcoinSerializable>(GetKey(b.Item), b.BlockPosition)));
				lastBlocks = blocks;
			}
			if(lastBlocks != null && lastBlocks.Count > 0)
			{
				var block = lastBlocks.Last();
				_Index.Put(IndexedLimit, new DiskBlockPos(block.BlockPosition.File, block.BlockPosition.Position + (uint)block.GetStorageSize()));
			}
			return count;
		}

		public TItem Get(string key)
		{
			if(key == null)
				return default(TItem);
			var pos = Index.Get<DiskBlockPos>(key);
			if(pos == null)
				return default(TItem);
			var stored = EnumerateForGet(new DiskBlockPosRange(pos)).FirstOrDefault();
			if(stored == null)
				return default(TItem);
			return stored.Item;
		}

		public void Put(TItem block)
		{
			var hash = GetKey(block);
			var position = Store.Append(block);
			Index.Put(hash, position);
			Index.Put(IndexedLimit, position);
		}

		protected abstract string GetKey(TItem item);

		protected abstract IEnumerable<TStoredItem> EnumerateForIndex(DiskBlockPosRange range);
		protected abstract IEnumerable<TStoredItem> EnumerateForGet(DiskBlockPosRange range);
	}
}
