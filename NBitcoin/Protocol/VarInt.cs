using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class CompactVarInt : IBitcoinSerializable
	{
		private ulong _Value = 0;
		private int _Size;
		public CompactVarInt(int size)
		{
			_Size = size;
		}
		public CompactVarInt(ulong value, int size)
		{
			_Value = value;
			_Size = size;
		}
		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if(stream.Serializing)
			{
				ulong n = _Value;
				byte[] tmp = new byte[(_Size * 8 + 6) / 7];
				int len = 0;
				while(true)
				{
					byte a = (byte)(n & 0x7F);
					byte b = (byte)(len != 0 ? 0x80 : 0x00);
					tmp[len] = (byte)(a | b);
					if(n <= 0x7F)
						break;
					n = (n >> 7) - 1;
					len++;
				}
				do
				{
					byte b = tmp[len];
					stream.ReadWrite(ref b);
				} while(len-- != 0);
			}
			else
			{
				ulong n = 0;
				while(true)
				{
					byte chData = 0;
					stream.ReadWrite(ref chData);
					ulong a = (n << 7);
					byte b = (byte)(chData & 0x7F);
					n = (a | b);
					if((chData & 0x80) != 0)
						n++;
					else
						break;
				}
				_Value = n;
			}
		}

		#endregion

		public ulong ToLong()
		{
			return _Value;
		}
	}


	//https://en.bitcoin.it/wiki/Protocol_specification#Variable_length_integer
	public class VarInt : IBitcoinSerializable
	{
		private byte _PrefixByte = 0;
		private ulong _Value = 0;

		public VarInt()
			: this(0)
		{

		}
		public VarInt(ulong value)
		{
			this._Value = value;
			if(_Value < 0xFD)
				_PrefixByte = (byte)(int)_Value;
			else if(_Value <= 0xffff)
				_PrefixByte = 0xFD;
			else if(_Value <= 0xffffffff)
				_PrefixByte = 0xFE;
			else
				_PrefixByte = 0xFF;
		}

		public ulong ToLong()
		{
			return _Value;
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _PrefixByte);
			if(_PrefixByte < 0xFD)
			{
				_Value = _PrefixByte;
			}
			else if(_PrefixByte == 0xFD)
			{
				var value = (ushort)_Value;
				stream.ReadWrite(ref value);
				_Value = value;
			}
			else if(_PrefixByte == 0xFE)
			{
				var value = (uint)_Value;
				stream.ReadWrite(ref value);
				_Value = value;
			}
			else
			{
				var value = (ulong)_Value;
				stream.ReadWrite(ref value);
				_Value = value;
			}
		}

		#endregion


	}
}
