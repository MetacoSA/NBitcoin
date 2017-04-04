#if !NOFILEIO
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.BitcoinCore
{
	public abstract class IndexedStore<TStoredItem, TItem>
		where TStoredItem : StoredItem<TItem>
		where TItem : IBitcoinSerializable, new()
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

		protected IndexedStore(NoSqlRepository index, Store<TStoredItem, TItem> store)
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
			try
			{
				return ReIndexAsync().Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
				return 0; //Can't happen
			}
		}

		public async Task<int> ReIndexAsync()
		{
			var last = await _Index.GetAsync<DiskBlockPos>(IndexedLimit).ConfigureAwait(false);
			int count = 0;
			List<TStoredItem> lastBlocks = null;
			foreach(var blocks in EnumerateForIndex(new DiskBlockPosRange(last)).Partition(400))
			{
				count += blocks.Count;
				await _Index.PutBatch(blocks.Select(b => new Tuple<String, IBitcoinSerializable>(GetKey(b.Item), b.BlockPosition))).ConfigureAwait(false);
				lastBlocks = blocks;
			}
			if(lastBlocks != null && lastBlocks.Count > 0)
			{
				var block = lastBlocks.Last();
				await _Index.PutAsync(IndexedLimit, new DiskBlockPos(block.BlockPosition.File, block.BlockPosition.Position + (uint)block.GetStorageSize())).ConfigureAwait(false);
			}
			return count;
		}

		public TItem Get(string key)
		{
			try
			{
				return GetAsync(key).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
				return default(TItem); //Can't happen
			}
		}

		public async Task<TItem> GetAsync(string key)
		{
			if(key == null)
				return default(TItem);
			var pos = await Index.GetAsync<DiskBlockPos>(key).ConfigureAwait(false);
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
			Index.PutAsync(hash, position);
			Index.PutAsync(IndexedLimit, position);
		}

		protected abstract string GetKey(TItem item);

		protected abstract IEnumerable<TStoredItem> EnumerateForIndex(DiskBlockPosRange range);
		protected abstract IEnumerable<TStoredItem> EnumerateForGet(DiskBlockPosRange range);
	}
}
#endif