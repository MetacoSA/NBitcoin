#if !NOFILEIO
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.BitcoinCore
{
	public class BlockUndoStore : Store<StoredItem<BlockUndo>, BlockUndo>
	{
		public BlockUndoStore(string folder, Network network)
			: this(new DirectoryInfo(folder), network)
		{
		}
		public BlockUndoStore(DirectoryInfo folder, Network network)
			: base(folder, network)
		{
			FilePrefix = "rev";
			MaxFileSize = 0x1000000; // 16 MiB
		}
		protected override StoredItem<BlockUndo> CreateStoredItem(BlockUndo item, DiskBlockPos position)
		{
			var stored = new StoredItem<BlockUndo>(Network.Magic, item, position);
			stored.HasChecksum = true;
			if(item.CalculatedChecksum == null)
				throw new InvalidOperationException("A block undo should have an calculated checksum with ComputeChecksum");
			stored.Checksum = item.CalculatedChecksum;
			return stored;
		}

		protected override StoredItem<BlockUndo> ReadStoredItem(System.IO.Stream stream, DiskBlockPos pos)
		{
			StoredItem<BlockUndo> item = new StoredItem<BlockUndo>(Network, pos);
			item.HasChecksum = true;
			item.ReadWrite(stream, false);
			item.Item.CalculatedChecksum = item.Checksum;
			return item;
		}
	}
}
#endif