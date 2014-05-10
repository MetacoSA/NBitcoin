using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class BlockStore
	{
		private readonly DirectoryInfo _Folder;
		public DirectoryInfo Folder
		{
			get
			{
				return _Folder;
			}
		}
		private readonly Network _Network;
		public Network Network
		{
			get
			{
				return _Network;
			}
		}
		public BlockStore(string folder, Network network)
		{
			_Folder = new DirectoryInfo(folder);
			_Network = network;
			if(!_Folder.Exists)
				throw new DirectoryNotFoundException(folder);
		}

		public IEnumerable<StoredBlock> Enumerate(bool headerOnly, DiskBlockPosRange range = null)
		{
			if(range == null)
				range = DiskBlockPosRange.All;
			using(CreateLock(FileLockType.Read))
			{
				foreach(var b in StoredBlock.EnumerateFolder(_Folder, range, headerOnly))
				{
					if(b.Magic == Network.Magic)
						yield return b;
				}
			}
		}

		private FileLock CreateLock(FileLockType fileLockType)
		{
			return new FileLock(Path.Combine(_Folder.FullName, "BlockStoreLock"), fileLockType);
		}

		public DiskBlockPos Append(Block block)
		{
			using(var @lock = CreateLock(FileLockType.ReadWrite))
			{
				DiskBlockPos position = SeekEnd(@lock);
				var stored = new StoredBlock(position);
				stored.Magic = Network.Magic;
				stored.Block = block;
				stored.BlockSize = (uint)stored.Block.GetSerializedSize();
				StoredBlock.Write(_Folder, stored);
				position++;
				@lock.SetString(position.ToString());
				return stored.BlockPosition;
			}
		}

		private DiskBlockPos SeekEnd(FileLock @lock)
		{
			var end = @lock.GetString();
			if(!string.IsNullOrEmpty(end))
				try
				{
					return DiskBlockPos.Parse(end);
				}
				catch(FormatException)
				{
					return StoredBlock.SeekEnd(_Folder);
				}
			else
				return StoredBlock.SeekEnd(_Folder);
		}


		public void AppendAll(IEnumerable<Block> blocks)
		{
			foreach(var block in blocks)
			{
				Append(block);
			}
		}
	}
}
