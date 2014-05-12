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
			stream.ReadWrite(ref size);
		}

		public uint GetStorageSize()
		{
			var ms = new MemoryStream();
			BitcoinStream stream = new BitcoinStream(ms, true);
			var me = this;
			stream.ReadWrite(ref me);
			return me.ItemSize + (uint)stream.Inner.Length;
		}


		#endregion
	}
	public abstract class StoredItem<T> : IBitcoinSerializable where T : IBitcoinSerializable
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

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _Header);
			if(ParseSkipItem)
			{
				stream.Inner.Position += _Header.ItemSize;
				return;
			}
			ReadWriteItem(stream, ref _Item);
		}

		protected abstract void ReadWriteItem(BitcoinStream stream, ref T item);

	}
}
