using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class BlockStore : Store<StoredBlock, Block>
	{
		public const int MAX_BLOCKFILE_SIZE = 0x8000000; // 128 MiB



		public BlockStore(string folder, Network network)
			: base(folder, network)
		{
			MaxFileSize = MAX_BLOCKFILE_SIZE;
			FilePrefix = "blk";
		}

		public IEnumerable<StoredBlock> EnumerateFolder(DiskBlockPosRange range, bool headersOnly)
		{
			using(HeaderOnlyScope(headerOnly))
			{
				return EnumerateFolder(range);
			}
		}


		[ThreadStatic]
		bool headerOnly;
		public IEnumerable<StoredBlock> Enumerate(Stream stream, uint fileIndex = 0, DiskBlockPosRange range = null, bool headersOnly = false)
		{
			using(HeaderOnlyScope(headersOnly))
			{
				return Enumerate(stream, fileIndex, range);
			}
		}

		private IDisposable HeaderOnlyScope(bool headersOnly)
		{
			var old = headersOnly;
			return new Scope(() =>
			{
				this.headerOnly = headersOnly;
			}, () =>
			{
				this.headerOnly = old;
			});
		}

		public IEnumerable<StoredBlock> Enumerate(bool headersOnly, DiskBlockPosRange range = null)
		{
			using(HeaderOnlyScope(headersOnly))
			{
				return Enumerate(range);
			}
		}


		protected override StoredBlock ReadStoredItem(Stream stream, DiskBlockPos pos)
		{
			StoredBlock block = new StoredBlock(Network, pos);
			block.ParseSkipBlockContent = headerOnly;
			block.ReadWrite(stream, false);
			return block;
		}

		protected override StoredBlock CreateStoredItem(Block item, DiskBlockPos position)
		{
			return new StoredBlock(Network.Magic, item, position);
		}
	}
}
