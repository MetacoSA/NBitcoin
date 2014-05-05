using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class StoredBlock : IBitcoinSerializable
	{
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
			stream.ReadWrite(ref block);
		}

		#endregion

		public static IEnumerable<StoredBlock> EnumerateFile(FileInfo file)
		{
			using(var fs = file.Open(FileMode.Open, FileAccess.Read))
			{
				foreach(var block in Enumerate(fs))
				{
					yield return block;
				}
			}
		}
		public static IEnumerable<StoredBlock> EnumerateFile(string fileName)
		{
			return EnumerateFile(new FileInfo(fileName));	
		}

		public static IEnumerable<StoredBlock> Enumerate(Stream stream)
		{
			while(stream.Position < stream.Length)
			{
				StoredBlock block = new StoredBlock();
				block.ReadWrite(stream, false);
				yield return block;
			}
		}

		public static IEnumerable<StoredBlock> EnumerateFolder(DirectoryInfo folder)
		{
			foreach(var file in folder.GetFiles().OrderBy(f => f.Name))
			{
				foreach(var block in EnumerateFile(file))
				{
					yield return block;
				}
			}
		}
		public static IEnumerable<StoredBlock> EnumerateFolder(string folder)
		{
			return EnumerateFolder(new DirectoryInfo(folder));
		}
	}
}
