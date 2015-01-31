#if !NOFILEIO
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.BitcoinCore
{
	public class BlockRepository
	{
		IndexedBlockStore _BlockStore;
		IndexedBlockStore _HeaderStore;
		public BlockRepository(IndexedBlockStore blockStore, 
							   IndexedBlockStore headerStore)
		{
			if(blockStore == null)
				throw new ArgumentNullException("blockStore");
			if(headerStore == null)
				throw new ArgumentNullException("headerStore");
			if(blockStore == headerStore)
				throw new ArgumentException("The two stores should be different");
			_BlockStore = blockStore;
			_HeaderStore = headerStore;
		}


		public void WriteBlock(Block block)
		{
			WriteBlockHeader(block.Header);
			_BlockStore.Put(block);
		}
		public void WriteBlockHeader(BlockHeader header)
		{
			Block block = new Block(header);
			_HeaderStore.Put(block);
		}

		public Block GetBlock(uint256 hash)
		{
			return _BlockStore.Get(hash) ?? _HeaderStore.Get(hash);
		}

		

	}
}
#endif