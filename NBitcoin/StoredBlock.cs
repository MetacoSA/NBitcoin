using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class DiskBlockPosRange
	{
		private static DiskBlockPosRange _All = new DiskBlockPosRange(DiskBlockPos.Begin, DiskBlockPos.End);

		public static DiskBlockPosRange All
		{
			get
			{
				return DiskBlockPosRange._All;
			}
		}

		/// <summary>
		/// Represent a disk block range
		/// </summary>
		/// <param name="begin">Beginning of the range (included)</param>
		/// <param name="end">End of the range (excluded)</param>
		public DiskBlockPosRange(DiskBlockPos begin = null, DiskBlockPos end = null)
		{
			if(begin == null)
				begin = DiskBlockPos.Begin;
			if(end == null)
				end = DiskBlockPos.End;
			_Begin = begin;
			_End = end;
			if(end <= begin)
				throw new ArgumentException("End should be more than begin");
		}
		private readonly DiskBlockPos _Begin;
		public DiskBlockPos Begin
		{
			get
			{
				return _Begin;
			}
		}
		private readonly DiskBlockPos _End;
		public DiskBlockPos End
		{
			get
			{
				return _End;
			}
		}

		public bool InRange(DiskBlockPos pos)
		{
			return Begin <= pos && pos < End;
		}
		public override string ToString()
		{
			return Begin + " <= x < " + End;
		}
	}
	public class DiskBlockPos : IBitcoinSerializable
	{
		private static DiskBlockPos _Begin = new DiskBlockPos(0, 0);

		public static DiskBlockPos Begin
		{
			get
			{
				return DiskBlockPos._Begin;
			}
		}

		private static DiskBlockPos _End = new DiskBlockPos(uint.MaxValue, uint.MaxValue);
		public static DiskBlockPos End
		{
			get
			{
				return DiskBlockPos._End;
			}
		}

		public DiskBlockPos()
		{

		}
		public DiskBlockPos(uint file, uint position)
		{
			_File = file;
			_Position = position;
			UpdateHash();
		}
		private uint _File;
		public uint File
		{
			get
			{
				return _File;
			}
		}
		private uint _Position;
		public uint Position
		{
			get
			{
				return _Position;
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWriteAsCompactVarInt(ref _File);
			stream.ReadWriteAsCompactVarInt(ref _Position);
			if(!stream.Serializing)
				UpdateHash();
		}

		private void UpdateHash()
		{
			_Hash = ToString().GetHashCode();
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

		public static bool operator <(DiskBlockPos a, DiskBlockPos b)
		{
			if(a.File < b.File)
				return true;
			if(a.File == b.File && a.Position < b.Position)
				return true;
			return false;
		}
		public static bool operator <=(DiskBlockPos a, DiskBlockPos b)
		{
			return a == b || a < b;
		}
		public static bool operator >(DiskBlockPos a, DiskBlockPos b)
		{
			if(a.File > b.File)
				return true;
			if(a.File == b.File && a.Position > b.Position)
				return true;
			return false;
		}
		public static bool operator >=(DiskBlockPos a, DiskBlockPos b)
		{
			return a == b || a > b;
		}
		public override int GetHashCode()
		{
			return _Hash.GetHashCode();
		}

		public static DiskBlockPos operator ++(DiskBlockPos a)
		{
			return new DiskBlockPos(a.File, a.Position + 1);
		}

		public DiskBlockPos OfFile(uint file)
		{
			return new DiskBlockPos(file, Position);
		}

		public override string ToString()
		{
			return "f:" + File + "p:" + Position;
		}

		static readonly Regex _Reg = new Regex("f:([0-9]*)p:([0-9]*)", RegexOptions.Compiled);
		public static DiskBlockPos Parse(string data)
		{
			var match = _Reg.Match(data);
			if(!match.Success)
				throw new FormatException("Invalid position string : " + data);
			return new DiskBlockPos(uint.Parse(match.Groups[1].Value), uint.Parse(match.Groups[2].Value));
		}
	}
	public class StoredBlock : IBitcoinSerializable
	{
		internal enum ParsingPart
		{
			StoredHeaderOnly,
			BlockHeaderOnly,
			Block,
		}
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


		internal ParsingPart Parsing = ParsingPart.Block;

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
			if(Parsing == StoredBlock.ParsingPart.Block)
				stream.ReadWrite(ref block);
			else
			{
				if(Parsing == ParsingPart.StoredHeaderOnly)
					stream.Inner.Position += blockSize;
				else
				{
					var beforeReading = stream.Inner.Position;
					BlockHeader header = block == null ? null : block.Header;
					stream.ReadWrite(ref header);
					if(!stream.Serializing)
						block = new Block(header);
					stream.Inner.Position = beforeReading + blockSize;
				}
			}
		}

		#endregion

		public static IEnumerable<StoredBlock> EnumerateFile(FileInfo file, DiskBlockPosRange range = null, bool headersOnly = false)
		{
			if(range == null)
				range = DiskBlockPosRange.All;
			using(var fs = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				foreach(var block in Enumerate(fs, range, headersOnly))
				{
					yield return block;
				}
			}
		}
		public static IEnumerable<StoredBlock> EnumerateFile(string fileName, DiskBlockPosRange range = null)
		{
			if(range == null)
				range = DiskBlockPosRange.All;
			return EnumerateFile(new FileInfo(fileName), range);
		}

		static IEnumerable<StoredBlock> Enumerate(Stream stream, DiskBlockPosRange range = null, bool headersOnly = false)
		{
			if(range == null)
				range = DiskBlockPosRange.All;

			var position = SkipUntil(stream, range.Begin);
			while(stream.Position < stream.Length)
			{
				StoredBlock block = new StoredBlock(position);
				block.Parsing = headersOnly ? ParsingPart.BlockHeaderOnly : ParsingPart.Block;
				block.ReadWrite(stream, false);
				yield return block;
				position++;
				if(position >= range.End)
					break;
			}
		}

		private static DiskBlockPos SkipUntil(Stream stream, DiskBlockPos until)
		{
			var position = new DiskBlockPos(until.File, 0);
			while(position < until && stream.Position != stream.Length)
			{
				StoredBlock block = new StoredBlock(position);
				block.Parsing = ParsingPart.BlockHeaderOnly;
				block.ReadWrite(stream, false);
				position++;
			}
			return position;
		}

		public static IEnumerable<StoredBlock> EnumerateFolder(DirectoryInfo folder, DiskBlockPosRange range = null, bool headersOnly = false)
		{
			if(range == null)
				range = DiskBlockPosRange.All;
			foreach(var file in folder.GetFiles().OrderBy(f => f.Name))
			{
				var fileIndex = GetFileIndex(file.Name);
				if(fileIndex < 0)
					continue;
				DiskBlockPos startLocal = null;
				DiskBlockPos endLocal = null;
				if(fileIndex < range.Begin.File)
					continue;
				else if(fileIndex == range.Begin.File)
					startLocal = new DiskBlockPos((uint)fileIndex, range.Begin.Position);
				else
					startLocal = new DiskBlockPos((uint)fileIndex, 0);

				if(fileIndex > range.End.File)
					continue;
				else if(fileIndex == range.End.File)
					endLocal = new DiskBlockPos((uint)fileIndex, range.End.Position);
				else
					endLocal = DiskBlockPos.End;

				foreach(var block in EnumerateFile(file, new DiskBlockPosRange(startLocal, endLocal), headersOnly))
				{
					yield return block;
				}
			}
		}

		static readonly Regex _FileReg = new Regex("blk([0-5]{5,5}).dat", RegexOptions.Compiled);
		private static int GetFileIndex(string fileName)
		{
			var match = _FileReg.Match(fileName);
			if(!match.Success)
				return -1;
			return int.Parse(match.Groups[1].Value);
		}
		public static void Write(DirectoryInfo folder, StoredBlock stored)
		{
			var fileName = string.Format("blk{0:00000}.dat", stored.BlockPosition.File);
			using(var fs = new FileStream(Path.Combine(folder.FullName, fileName), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
			{
				SkipUntil(fs, stored.BlockPosition);
				stored.ReadWrite(fs, true);
			}
		}

		public static IEnumerable<StoredBlock> EnumerateFolder(string folder, DiskBlockPosRange range = null)
		{
			if(range == null)
				range = DiskBlockPosRange.All;
			return EnumerateFolder(new DirectoryInfo(folder), range);
		}

		private static FileInfo CreateFile(DirectoryInfo folder, int file)
		{
			var fileName = string.Format("blk{0:00000}.dat", file);
			var filePath = Path.Combine(folder.FullName, fileName);
			File.Create(filePath).Close();
			return new FileInfo(filePath);
		}


		public static DiskBlockPos SeekEnd(DirectoryInfo folder)
		{
			var highestFile = folder.GetFiles().OrderBy(f => f.Name).Where(f => GetFileIndex(f.Name) != -1).LastOrDefault();
			if(highestFile == null)
				return new DiskBlockPos(0, 0);
			using(var fs = new FileStream(highestFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				var index = (uint)GetFileIndex(highestFile.Name);
				return new DiskBlockPos(index, SkipUntil(fs, DiskBlockPos.End.OfFile(index)).Position);
			}
		}


	}
}
