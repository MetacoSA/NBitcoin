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

		public IEnumerable<StoredBlock> Enumerate(DiskBlockPosRange range = null)
		{
			if(range == null)
				range = DiskBlockPosRange.All;
			using(CreateLock(FileLockType.Read))
			{
				foreach(var b in StoredBlock.EnumerateFolder(_Folder, range))
				{
					if(b.Magic == Network.Magic)
						yield return b;
				}
			}
		}

		private IDisposable CreateLock(FileLockType fileLockType)
		{
			return new FileLock(Path.Combine(_Folder.FullName, "BlockStoreLock"), fileLockType);
		}

		volatile DiskBlockPos _LastSeekEnd = DiskBlockPos.Begin;
		public DiskBlockPos Append(Block block)
		{
			using(CreateLock(FileLockType.ReadWrite))
			{
				var stored = new StoredBlock(StoredBlock.SeekEnd(_Folder, _LastSeekEnd));
				stored.Magic = Network.Magic;
				stored.Block = block;
				stored.BlockSize = (uint)stored.Block.GetSerializedSize();
				StoredBlock.Write(_Folder, stored);
				_LastSeekEnd = stored.BlockPosition;
				return stored.BlockPosition;
			}
		}

	}
}
