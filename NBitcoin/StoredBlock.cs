using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class DiskBlockPos : IBitcoinSerializable
	{
		private static DiskBlockPos _Beginning = new DiskBlockPos(0, 0);

		public static DiskBlockPos Beginning
		{
			get
			{
				return DiskBlockPos._Beginning;
			}
		}
		public DiskBlockPos()
		{

		}
		public DiskBlockPos(uint file, uint position)
		{
			_File = new VarInt(file);
			_Position = new VarInt(position);
			UpdateHash();
		}
		private VarInt _File;
		public uint File
		{
			get
			{
				return (uint)_File.ToLong();
			}
		}
		private VarInt _Position;
		public uint Position
		{
			get
			{
				return (uint)_Position.ToLong();
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _File);
			stream.ReadWrite(ref _Position);
			if(!stream.Serializing)
				UpdateHash();
		}

		private void UpdateHash()
		{
			_Hash = ("Position : " + Position + " file " + File).GetHashCode();
		}

		int _Hash;

		#endregion

		public override bool Equals(object obj)
		{
			DiskBlockPos item = obj as DiskBlockPos;
			if(item == null)
				return false;
			return _Hash.Equals(item._Hash);
		}
		public static bool operator ==(DiskBlockPos a, DiskBlockPos b)
		{
			if(System.Object.ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return a._Hash == b._Hash;
		}

		public static bool operator !=(DiskBlockPos a, DiskBlockPos b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return _Hash.GetHashCode();
		}

		public static DiskBlockPos operator ++(DiskBlockPos a)
		{
			return new DiskBlockPos(a.File, a.Position + 1);
		}
	}
	public class StoredBlock : IBitcoinSerializable
	{
		public StoredBlock(DiskBlockPos blockPosition)
		{
			_BlockPosition = blockPosition;
		}

		private readonly DiskBlockPos _BlockPosition;
		public DiskBlockPos BlockPosition
		{
			get
			{
				return _BlockPosition;
			}
		}

		uint magic;
		public uint Magic
		{
			get
			{
				return magic;
			}
			set
			{
				magic = value;
			}
		}

		uint blockSize;
		public uint BlockSize
		{
			get
			{
				return blockSize;
			}
			set
			{
				blockSize = value;
			}
		}

		Block block;
		internal bool SkipContent;

		public Block Block
		{
			get
			{
				return block;
			}
			set
			{
				block = value;
			}
		}


		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref magic);
			stream.ReadWrite(ref blockSize);
			if(!SkipContent)
				stream.ReadWrite(ref block);
			else
				stream.Inner.Position += blockSize;
		}

		#endregion

		public static IEnumerable<StoredBlock> EnumerateFile(FileInfo file, DiskBlockPos from = null)
		{
			if(from == null)
				from = DiskBlockPos.Beginning;
			using(var fs = file.Open(FileMode.Open, FileAccess.Read))
			{
				foreach(var block in Enumerate(fs, from))
				{
					yield return block;
				}
			}
		}
		public static IEnumerable<StoredBlock> EnumerateFile(string fileName, DiskBlockPos from = null)
		{
			if(from == null)
				from = DiskBlockPos.Beginning;
			return EnumerateFile(new FileInfo(fileName), from);
		}

		static IEnumerable<StoredBlock> Enumerate(Stream stream, DiskBlockPos from = null)
		{
			if(from == null)
				from = DiskBlockPos.Beginning;
			var position = new DiskBlockPos(from.File, 0);
			while(position.Position < from.Position)
			{
				StoredBlock block = new StoredBlock(position);
				block.SkipContent = true;
				block.ReadWrite(stream, false);
				position++;
			}
			while(stream.Position < stream.Length)
			{
				StoredBlock block = new StoredBlock(position);
				block.ReadWrite(stream, false);
				yield return block;
				position++;
			}
		}

		public static IEnumerable<StoredBlock> EnumerateFolder(DirectoryInfo folder, DiskBlockPos from = null)
		{
			if(from == null)
				from = DiskBlockPos.Beginning;
			foreach(var file in folder.GetFiles().OrderBy(f => f.Name))
			{
				var match = Regex.Match(file.Name, "blk([0-5]{5,5}).dat");
				if(!match.Success)
					continue;
				var fileIndex = uint.Parse(match.Groups[1].Value);
				DiskBlockPos fromLocal = null;
				if(fileIndex < from.File)
					continue;
				if(fileIndex == from.File)
					fromLocal = new DiskBlockPos(fileIndex, from.Position);
				else
					fromLocal = new DiskBlockPos(fileIndex, 0);

				foreach(var block in EnumerateFile(file, fromLocal))
				{
					yield return block;
				}
			}
		}
		public static IEnumerable<StoredBlock> EnumerateFolder(string folder, DiskBlockPos from = null)
		{
			if(from == null)
				from = DiskBlockPos.Beginning;
			return EnumerateFolder(new DirectoryInfo(folder), from);
		}
	}
}
