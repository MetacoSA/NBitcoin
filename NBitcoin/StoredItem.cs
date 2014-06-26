using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class StoredHeader : IBitcoinSerializable
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

		uint size;
		public uint ItemSize
		{
			get
			{
				return size;
			}
			set
			{
				size = value;
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref magic);
			if(magic == 0)
				return;
			stream.ReadWrite(ref size);
		}


		#endregion
	}
	public class StoredItem<T> : IBitcoinSerializable where T : IBitcoinSerializable
	{
		public StoredItem(DiskBlockPos position)
		{
			_BlockPosition = position;
		}
		public StoredItem(uint magic, T item, DiskBlockPos position)
		{
			_BlockPosition = position;
			_Item = item;
			_Header.Magic = magic;
			_Header.ItemSize = (uint)item.GetSerializedSize();
		}
		public bool ParseSkipItem
		{
			get;
			set;
		}

		private readonly DiskBlockPos _BlockPosition;
		public DiskBlockPos BlockPosition
		{
			get
			{
				return _BlockPosition;
			}
		}



		private StoredHeader _Header = new StoredHeader();
		public StoredHeader Header
		{
			get
			{
				return _Header;
			}
		}

		private T _Item;
		public T Item
		{
			get
			{
				return _Item;
			}
		}

		public bool HasChecksum
		{
			get;
			set;
		}

		private uint256 _Checksum = new uint256(0);
		public uint256 Checksum
		{
			get
			{
				return _Checksum;
			}
			set
			{
				_Checksum = value;
			}
		}

		public uint GetStorageSize()
		{
			var ms = new MemoryStream();
			BitcoinStream stream = new BitcoinStream(ms, true);
			stream.ReadWrite(ref _Header);
			return _Header.ItemSize + (uint)stream.Inner.Length + (HasChecksum ? (uint)(256 / 8) : 0);
		}

		public void ReadWrite(BitcoinStream stream)
		{
			while(true)
			{
				stream.ReadWrite(ref _Header);
				if(_Header.Magic != 0)
					break;
				if(stream.Inner.Length - stream.Inner.Position < 4)
					return;
			}
			if(ParseSkipItem)
				stream.Inner.Position += _Header.ItemSize;
			else
				ReadWriteItem(stream, ref _Item);
			if(HasChecksum)
				stream.ReadWrite(ref _Checksum);
		}

		protected virtual void ReadWriteItem(BitcoinStream stream, ref T item)
		{
			stream.ReadWrite(ref item);
		}

	}
}
