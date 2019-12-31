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
			if (stream.Serializing)
			{
				ulong n = _Value;
#if HAS_SPAN
				Span<byte> tmp = stackalloc byte[(_Size * 8 + 6) / 7];
#else
				byte[] tmp = new byte[(_Size * 8 + 6) / 7];
#endif
				int len = 0;
				while (true)
				{
					byte a = (byte)(n & 0x7F);
					byte b = (byte)(len != 0 ? 0x80 : 0x00);
					tmp[len] = (byte)(a | b);
					if (n <= 0x7F)
						break;
					n = (n >> 7) - 1;
					len++;
				}
				do
				{
					byte b = tmp[len];
					stream.ReadWrite(ref b);
				} while (len-- != 0);
			}
			else
			{
				ulong n = 0;
				while (true)
				{
					byte chData = 0;
					stream.ReadWrite(ref chData);
					ulong a = (n << 7);
					byte b = (byte)(chData & 0x7F);
					n = (a | b);
					if ((chData & 0x80) != 0)
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
		private ulong _Value = 0;

		public VarInt()
			: this(0)
		{

		}
		public VarInt(ulong value)
		{
			SetValue(value);
		}

		internal void SetValue(ulong value)
		{
			this._Value = value;
		}

		public static void StaticWrite(BitcoinStream bs, ulong length)
		{
			if (!bs.Serializing)
				throw new InvalidOperationException("Stream should be serializing");
			var stream = bs.Inner;
			bs.Counter.AddWritten(1);
			if (length < 0xFD)
			{
				stream.WriteByte((byte)length);
			}
			else if (length <= 0xffff)
			{
				var value = (ushort)length;
				stream.WriteByte((byte)0xFD);
				bs.ReadWrite(ref value);
			}
			else if (length <= 0xffffffff)
			{
				var value = (uint)length;
				stream.WriteByte((byte)0xFE);
				bs.ReadWrite(ref value);
			}
			else
			{
				var value = length;
				stream.WriteByte((byte)0xFF);
				bs.ReadWrite(ref value);
			}
		}

		public static ulong StaticRead(BitcoinStream bs)
		{
			if (bs.Serializing)
				throw new InvalidOperationException("Stream should not be serializing");
			var prefix = bs.Inner.ReadByte();
			bs.Counter.AddReaden(1);
			if (prefix == -1)
				throw new EndOfStreamException("No more byte to read");
			if (prefix < 0xFD)
				return (byte)prefix;
			else if (prefix == 0xFD)
			{
				var value = (ushort)0;
				bs.ReadWrite(ref value);
				return value;
			}
			else if (prefix == 0xFE)
			{
				var value = (uint)0;
				bs.ReadWrite(ref value);
				return value;
			}
			else
			{
				var value = (ulong)0;
				bs.ReadWrite(ref value);
				return value;
			}
		}

		public ulong ToLong()
		{
			return _Value;
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if (stream.Serializing)
				StaticWrite(stream, _Value);
			else
				_Value = StaticRead(stream);
		}

		#endregion


	}
}
