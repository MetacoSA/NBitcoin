using System;
using System.Linq;
using NBitcoin.DataEncoders;

namespace NBitcoin.Altcoins.HashQuark
{
	public class Uint512
	{
		public class MutableUint512 : IBitcoinSerializable
		{
			public Uint512 Value { get; private set; }

			public MutableUint512()
			{
				Value = Zero;
			}

			public MutableUint512(Uint512 value)
			{
				Value = value;
			}

			public void ReadWrite(BitcoinStream stream)
			{
				if (stream.Serializing)
				{
					var b = Value.ToBytes();
					stream.ReadWrite(ref b);
				}
				else
				{
					byte[] b = new byte[WIDTH_BYTE];
					stream.ReadWrite(ref b);
					Value = new Uint512(b);
				}
			}
		}

		public static Uint512 Zero { get; } = new Uint512();

		public static Uint512 One { get; } = new Uint512(1);

		public Uint512()
		{
		}

		public Uint512(Uint512 b)
		{
			pn0 = b.pn0;
			pn1 = b.pn1;
			pn2 = b.pn2;
			pn3 = b.pn3;
			pn4 = b.pn4;
			pn5 = b.pn5;
			pn6 = b.pn6;
			pn7 = b.pn7;
			pn8 = b.pn8;
			pn9 = b.pn9;
			pn10 = b.pn10;
			pn11 = b.pn11;
			pn12 = b.pn12;
			pn13 = b.pn13;
			pn14 = b.pn14;
			pn15 = b.pn15;
		}

		public static Uint512 Parse(string hex)
		{
			return new Uint512(hex);
		}

		public static bool TryParse(string hex, out Uint512 result)
		{
			if (hex == null)
				throw new ArgumentNullException("hex");
			if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				hex = hex.Substring(2);
			result = null;
			if (hex.Length != WIDTH_BYTE * 2)
				return false;
			if (!((HexEncoder) Encoders.Hex).IsValid(hex))
				return false;
			result = new Uint512(hex);
			return true;
		}

		private static readonly HexEncoder Encoder = new HexEncoder();
		private const int WIDTH_BYTE = 512 / 8;
		internal readonly uint pn0;
		internal readonly uint pn1;
		internal readonly uint pn2;
		internal readonly uint pn3;
		internal readonly uint pn4;
		internal readonly uint pn5;
		internal readonly uint pn6;
		internal readonly uint pn7;
		internal readonly uint pn8;
		internal readonly uint pn9;
		internal readonly uint pn10;
		internal readonly uint pn11;
		internal readonly uint pn12;
		internal readonly uint pn13;
		internal readonly uint pn14;
		internal readonly uint pn15;

		public byte GetByte(int index)
		{
			var uintIndex = index / sizeof(uint);
			var byteIndex = index % sizeof(uint);
			uint value;
			switch (uintIndex)
			{
				case 0:
					value = pn0;
					break;
				case 1:
					value = pn1;
					break;
				case 2:
					value = pn2;
					break;
				case 3:
					value = pn3;
					break;
				case 4:
					value = pn4;
					break;
				case 5:
					value = pn5;
					break;
				case 6:
					value = pn6;
					break;
				case 7:
					value = pn7;
					break;
				case 8:
					value = pn8;
					break;
				case 9:
					value = pn9;
					break;
				case 10:
					value = pn10;
					break;
				case 11:
					value = pn11;
					break;
				case 12:
					value = pn12;
					break;
				case 13:
					value = pn13;
					break;
				case 14:
					value = pn14;
					break;
				case 15:
					value = pn15;
					break;
				default:
					throw new ArgumentOutOfRangeException("index");
			}

			return (byte) (value >> (byteIndex * 8));
		}

		public override string ToString()
		{
			return Encoder.EncodeData(ToBytes().Reverse().ToArray());
		}

		public Uint512(ulong b)
		{
			pn0 = (uint) b;
			pn1 = (uint) (b >> 32);
			pn2 = 0;
			pn3 = 0;
			pn4 = 0;
			pn5 = 0;
			pn6 = 0;
			pn7 = 0;
			pn8 = 0;
			pn9 = 0;
			pn10 = 0;
			pn11 = 0;
			pn12 = 0;
			pn13 = 0;
			pn14 = 0;
			pn15 = 0;
		}

		public Uint512(byte[] vch, bool lendian = true)
		{
			if (vch.Length != WIDTH_BYTE)
			{
				throw new FormatException("the byte array should be 512 byte long");
			}

			if (!lendian)
				vch = vch.Reverse().ToArray();

			pn0 = Utils.ToUInt32(vch, 4 * 0, true);
			pn1 = Utils.ToUInt32(vch, 4 * 1, true);
			pn2 = Utils.ToUInt32(vch, 4 * 2, true);
			pn3 = Utils.ToUInt32(vch, 4 * 3, true);
			pn4 = Utils.ToUInt32(vch, 4 * 4, true);
			pn5 = Utils.ToUInt32(vch, 4 * 5, true);
			pn6 = Utils.ToUInt32(vch, 4 * 6, true);
			pn7 = Utils.ToUInt32(vch, 4 * 7, true);
			pn8 = Utils.ToUInt32(vch, 4 * 8, true);
			pn9 = Utils.ToUInt32(vch, 4 * 9, true);
			pn10 = Utils.ToUInt32(vch, 4 * 10, true);
			pn11 = Utils.ToUInt32(vch, 4 * 11, true);
			pn12 = Utils.ToUInt32(vch, 4 * 12, true);
			pn13 = Utils.ToUInt32(vch, 4 * 13, true);
			pn14 = Utils.ToUInt32(vch, 4 * 14, true);
			pn15 = Utils.ToUInt32(vch, 4 * 15, true);
		}

		public Uint512(string str)
		{
			pn0 = 0;
			pn1 = 0;
			pn2 = 0;
			pn3 = 0;
			pn4 = 0;
			pn5 = 0;
			pn6 = 0;
			pn7 = 0;
			pn8 = 0;
			pn9 = 0;
			pn10 = 0;
			pn11 = 0;
			pn12 = 0;
			pn13 = 0;
			pn14 = 0;
			pn15 = 0;
			str = str.Trim();

			if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				str = str.Substring(2);

			var bytes = Encoder.DecodeData(str).Reverse().ToArray();
			if (bytes.Length != WIDTH_BYTE)
				throw new FormatException("Invalid hex length");
			pn0 = Utils.ToUInt32(bytes, 4 * 0, true);
			pn1 = Utils.ToUInt32(bytes, 4 * 1, true);
			pn2 = Utils.ToUInt32(bytes, 4 * 2, true);
			pn3 = Utils.ToUInt32(bytes, 4 * 3, true);
			pn4 = Utils.ToUInt32(bytes, 4 * 4, true);
			pn5 = Utils.ToUInt32(bytes, 4 * 5, true);
			pn6 = Utils.ToUInt32(bytes, 4 * 6, true);
			pn7 = Utils.ToUInt32(bytes, 4 * 7, true);
			pn8 = Utils.ToUInt32(bytes, 4 * 8, true);
			pn9 = Utils.ToUInt32(bytes, 4 * 9, true);
			pn10 = Utils.ToUInt32(bytes, 4 * 10, true);
			pn11 = Utils.ToUInt32(bytes, 4 * 11, true);
			pn12 = Utils.ToUInt32(bytes, 4 * 12, true);
			pn13 = Utils.ToUInt32(bytes, 4 * 13, true);
			pn14 = Utils.ToUInt32(bytes, 4 * 14, true);
			pn15 = Utils.ToUInt32(bytes, 4 * 15, true);
		}

		public Uint512(byte[] vch)
			: this(vch, true)
		{
		}

		public override bool Equals(object obj)
		{
			var item = obj as Uint512;
			if (item == null)
				return false;
			bool equals = true;
			equals &= pn0 == item.pn0;
			equals &= pn1 == item.pn1;
			equals &= pn2 == item.pn2;
			equals &= pn3 == item.pn3;
			equals &= pn4 == item.pn4;
			equals &= pn5 == item.pn5;
			equals &= pn6 == item.pn6;
			equals &= pn7 == item.pn7;
			equals &= pn8 == item.pn8;
			equals &= pn9 == item.pn9;
			equals &= pn10 == item.pn10;
			equals &= pn11 == item.pn11;
			equals &= pn12 == item.pn12;
			equals &= pn13 == item.pn13;
			equals &= pn14 == item.pn14;
			equals &= pn15 == item.pn15;
			return equals;
		}

		public static bool operator ==(Uint512 a, Uint512 b)
		{
			if (ReferenceEquals(a, b))
				return true;
			if ((object) a == null || (object) b == null)
				return false;

			bool equals = true;
			equals &= a.pn0 == b.pn0;
			equals &= a.pn1 == b.pn1;
			equals &= a.pn2 == b.pn2;
			equals &= a.pn3 == b.pn3;
			equals &= a.pn4 == b.pn4;
			equals &= a.pn5 == b.pn5;
			equals &= a.pn6 == b.pn6;
			equals &= a.pn7 == b.pn7;
			equals &= a.pn8 == b.pn8;
			equals &= a.pn9 == b.pn9;
			equals &= a.pn10 == b.pn10;
			equals &= a.pn11 == b.pn11;
			equals &= a.pn12 == b.pn12;
			equals &= a.pn13 == b.pn13;
			equals &= a.pn14 == b.pn14;
			equals &= a.pn15 == b.pn15;
			return equals;
		}

		public static bool operator <(Uint512 a, Uint512 b)
		{
			return Comparison(a, b) < 0;
		}

		public static bool operator >(Uint512 a, Uint512 b)
		{
			return Comparison(a, b) > 0;
		}

		public static bool operator <=(Uint512 a, Uint512 b)
		{
			return Comparison(a, b) <= 0;
		}

		public static bool operator >=(Uint512 a, Uint512 b)
		{
			return Comparison(a, b) >= 0;
		}

		private static int Comparison(Uint512 a, Uint512 b)
		{
			if (a.pn15 < b.pn15)
				return -1;
			if (a.pn15 > b.pn15)
				return 1;
			if (a.pn14 < b.pn14)
				return -1;
			if (a.pn14 > b.pn14)
				return 1;
			if (a.pn13 < b.pn13)
				return -1;
			if (a.pn13 > b.pn13)
				return 1;
			if (a.pn12 < b.pn12)
				return -1;
			if (a.pn12 > b.pn12)
				return 1;
			if (a.pn11 < b.pn11)
				return -1;
			if (a.pn11 > b.pn11)
				return 1;
			if (a.pn10 < b.pn10)
				return -1;
			if (a.pn10 > b.pn10)
				return 1;
			if (a.pn9 < b.pn9)
				return -1;
			if (a.pn9 > b.pn9)
				return 1;
			if (a.pn8 < b.pn8)
				return -1;
			if (a.pn8 > b.pn8)
				return 1;
			if (a.pn7 < b.pn7)
				return -1;
			if (a.pn7 > b.pn7)
				return 1;
			if (a.pn6 < b.pn6)
				return -1;
			if (a.pn6 > b.pn6)
				return 1;
			if (a.pn5 < b.pn5)
				return -1;
			if (a.pn5 > b.pn5)
				return 1;
			if (a.pn4 < b.pn4)
				return -1;
			if (a.pn4 > b.pn4)
				return 1;
			if (a.pn3 < b.pn3)
				return -1;
			if (a.pn3 > b.pn3)
				return 1;
			if (a.pn2 < b.pn2)
				return -1;
			if (a.pn2 > b.pn2)
				return 1;
			if (a.pn1 < b.pn1)
				return -1;
			if (a.pn1 > b.pn1)
				return 1;
			if (a.pn0 < b.pn0)
				return -1;
			if (a.pn0 > b.pn0)
				return 1;
			return 0;
		}

		public static bool operator !=(Uint512 a, Uint512 b)
		{
			return !(a == b);
		}

		public static bool operator ==(Uint512 a, ulong b)
		{
			return a == new Uint512(b);
		}

		public static bool operator !=(Uint512 a, ulong b)
		{
			return !(a == new Uint512(b));
		}

		public static implicit operator Uint512(ulong value)
		{
			return new Uint512(value);
		}

		public byte[] ToBytes(bool lendian = true)
		{
			var arr = new byte[WIDTH_BYTE];
			Buffer.BlockCopy(Utils.ToBytes(pn0, true), 0, arr, 4 * 0, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn1, true), 0, arr, 4 * 1, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn2, true), 0, arr, 4 * 2, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn3, true), 0, arr, 4 * 3, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn4, true), 0, arr, 4 * 4, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn5, true), 0, arr, 4 * 5, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn6, true), 0, arr, 4 * 6, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn7, true), 0, arr, 4 * 7, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn8, true), 0, arr, 4 * 8, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn9, true), 0, arr, 4 * 9, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn10, true), 0, arr, 4 * 10, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn11, true), 0, arr, 4 * 11, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn12, true), 0, arr, 4 * 12, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn13, true), 0, arr, 4 * 13, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn14, true), 0, arr, 4 * 14, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn15, true), 0, arr, 4 * 15, 4);
			if (!lendian)
				Array.Reverse(arr);
			return arr;
		}

		public MutableUint512 AsBitcoinSerializable()
		{
			return new MutableUint512(this);
		}

		//public int GetSerializeSize(int nType = 0, ProtocolVersion protocolVersion = ProtocolVersion.PROTOCOL_VERSION)
		//{
		//	return WIDTH_BYTE;
		//}

		public int Size => WIDTH_BYTE;

		public ulong GetLow64()
		{
			return pn0 | ((ulong) pn1 << 32);
		}

		public uint GetLow32()
		{
			return pn0;
		}

		public override int GetHashCode()
		{
			int hash = 17;
			unchecked
			{
				hash = hash * 31 + (int) pn0;
				hash = hash * 31 + (int) pn1;
				hash = hash * 31 + (int) pn2;
				hash = hash * 31 + (int) pn3;
				hash = hash * 31 + (int) pn4;
				hash = hash * 31 + (int) pn5;
				hash = hash * 31 + (int) pn6;
				hash = hash * 31 + (int) pn7;
				hash = hash * 31 + (int) pn8;
				hash = hash * 31 + (int) pn9;
				hash = hash * 31 + (int) pn10;
				hash = hash * 31 + (int) pn11;
				hash = hash * 31 + (int) pn12;
				hash = hash * 31 + (int) pn13;
				hash = hash * 31 + (int) pn14;
				hash = hash * 31 + (int) pn15;
			}

			return hash;
		}
	}
}