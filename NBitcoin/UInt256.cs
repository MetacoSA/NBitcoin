
using System;
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using NBitcoin.DataEncoders;

namespace NBitcoin
{
	public sealed class uint256 : IComparable<uint256>, IEquatable<uint256>, IComparable
	{
		public class MutableUint256 : IBitcoinSerializable
		{
			uint256 _Value;
			public uint256 Value
			{
				get
				{
					return _Value;
				}
				set
				{
					_Value = value;
				}
			}
			public MutableUint256()
			{
				_Value = uint256.Zero;
			}
			public MutableUint256(uint256 value)
			{
				_Value = value;
			}

			public void ReadWrite(BitcoinStream stream)
			{
				if (stream.Serializing)
				{
#if !HAS_SPAN
					var b = Value.ToBytes();
					stream.ReadWrite(ref b);
#else
					Span<byte> b = stackalloc byte[WIDTH_BYTE];
					Value.ToBytes(b);
					stream.ReadWrite(ref b);
#endif
				}
				else
				{
#if !HAS_SPAN
					byte[] b = new byte[WIDTH_BYTE];
					stream.ReadWrite(ref b);
					_Value = new uint256(b);
#else
					Span<byte> b = stackalloc byte[WIDTH_BYTE];
					stream.ReadWrite(ref b);
					_Value = new uint256(b);
#endif
				}
			}
		}
		static readonly uint256 _Zero = new uint256();
		public static uint256 Zero
		{
			get
			{
				return _Zero;
			}
		}

		static readonly uint256 _One = new uint256(1);
		public static uint256 One
		{
			get
			{
				return _One;
			}
		}

		public uint256()
		{
		}

		public uint256(uint256 b)
		{
			pn0 = b.pn0;
			pn1 = b.pn1;
			pn2 = b.pn2;
			pn3 = b.pn3;
		}

		public static uint256 Parse(string hex)
		{
			return new uint256(hex);
		}
		public static bool TryParse(string hex, out uint256 result)
		{
			if (hex == null)
				throw new ArgumentNullException(nameof(hex));
			if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				hex = hex.Substring(2);
			result = null;
			if (hex.Length != WIDTH_BYTE * 2)
				return false;
			if (!((HexEncoder)Encoders.Hex).IsValid(hex))
				return false;
			result = new uint256(hex);
			return true;
		}

		private static readonly HexEncoder Encoder = new HexEncoder();
		private const int WIDTH_BYTE = 256 / 8;
		internal readonly ulong pn0;
		internal readonly ulong pn1;
		internal readonly ulong pn2;
		internal readonly ulong pn3;

		public byte GetByte(int index)
		{
#if HAS_SPAN
			if (index < 0 || index > 31)
				throw new ArgumentOutOfRangeException("index");
			if (BitConverter.IsLittleEndian)
			{
				Span<ulong> temp = stackalloc ulong[4];
				temp[0] = pn0;
				temp[1] = pn1;
				temp[2] = pn2;
				temp[3] = pn3;
				Span<byte> temp2 = MemoryMarshal.Cast<ulong, byte>(temp);
				return temp2[index];
			}
#endif

			var ulongIndex = index / sizeof(ulong);
			var byteIndex = index % sizeof(ulong);
			ulong value;
			switch (ulongIndex)
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
				default:
					throw new ArgumentOutOfRangeException("index");
			}
			return (byte)(value >> (byteIndex * 8));
		}
		public override string ToString()
		{
			var bytes = ToBytes();
			Array.Reverse(bytes);
			return Encoder.EncodeData(bytes);
		}

		public uint256(ulong b)
		{
			pn0 = (uint)b;
			pn1 = (uint)(b >> 32);
		}

		public uint256(byte[] vch, bool lendian = true) : this(vch, 0, vch.Length, lendian)
		{

		}

		public uint256(byte[] vch, int offset, int length, bool lendian = true)
		{
			if (length != WIDTH_BYTE)
			{
				throw new FormatException("the byte array should be 32 bytes long");
			}
#if HAS_SPAN
			if (BitConverter.IsLittleEndian && lendian)
			{
				var uints = MemoryMarshal.Cast<byte, ulong>(vch.AsSpan().Slice(offset, length));
				pn0 = uints[0];
				pn1 = uints[1];
				pn2 = uints[2];
				pn3 = uints[3];
				return;
			}
#endif

			if (!lendian)
			{
				if (length != vch.Length)
					vch = vch.Take(32).ToArray();
				vch = vch.Reverse().ToArray();
			}

			pn0 = Utils.ToUInt64(vch, offset + 8 * 0, true);
			pn1 = Utils.ToUInt64(vch, offset + 8 * 1, true);
			pn2 = Utils.ToUInt64(vch, offset + 8 * 2, true);
			pn3 = Utils.ToUInt64(vch, offset + 8 * 3, true);

		}

#if HAS_SPAN
		public uint256(ReadOnlySpan<byte> bytes)
		{
			if (bytes.Length != WIDTH_BYTE)
			{
				throw new FormatException("the byte array should be 32 bytes long");
			}
			if (BitConverter.IsLittleEndian)
			{
				var uints = MemoryMarshal.Cast<byte, ulong>(bytes);
				pn0 = uints[0];
				pn1 = uints[1];
				pn2 = uints[2];
				pn3 = uints[3];
				return;
			}
			pn0 = Utils.ToUInt64(bytes.Slice(0), true);
			pn1 = Utils.ToUInt64(bytes.Slice(8 * 1), true);
			pn2 = Utils.ToUInt64(bytes.Slice(8 * 2), true);
			pn3 = Utils.ToUInt64(bytes.Slice(8 * 3), true);
		}
#endif
		/// <summary>
		/// Create a uint256 from a string in big endian
		/// </summary>
		/// <param name="str"></param>
		public uint256(string str)
		{
			if (str == null)
				throw new ArgumentNullException(nameof(str));
			if (str.Length != 64)
			{
				if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
					str = str.Substring(2);
				str = str.Trim();
				if (str.Length != 64)
					throw new FormatException("A uint256 must be 64 characters");
			}

#if HAS_SPAN
			if (BitConverter.IsLittleEndian)
			{
				Span<byte> tmp = stackalloc byte[32];
				Encoder.DecodeData(str, tmp);
				tmp.Reverse();
				Span<ulong> uints = MemoryMarshal.Cast<byte, ulong>(tmp);
				pn0 = uints[0];
				pn1 = uints[1];
				pn2 = uints[2];
				pn3 = uints[3];
				return;
			}
#endif
			var bytes = Encoder.DecodeData(str);
			Array.Reverse(bytes);
			pn0 = Utils.ToUInt64(bytes, 8 * 0, true);
			pn1 = Utils.ToUInt64(bytes, 8 * 1, true);
			pn2 = Utils.ToUInt64(bytes, 8 * 2, true);
			pn3 = Utils.ToUInt64(bytes, 8 * 3, true);
		}

		public int GetBisCount()
		{
			if (pn3 != 0)
			{
				for (int nbits = 63; nbits > 0; nbits--)
				{
					if ((pn3 & 1UL << nbits) != 0)
						return 64 * 3 + nbits + 1;
				}
				return 64 * 3 + 1;
			}
			if (pn2 != 0)
			{
				for (int nbits = 63; nbits > 0; nbits--)
				{
					if ((pn2 & 1UL << nbits) != 0)
						return 64 * 2 + nbits + 1;
				}
				return 64 * 2 + 1;
			}
			if (pn1 != 0)
			{
				for (int nbits = 63; nbits > 0; nbits--)
				{
					if ((pn1 & 1UL << nbits) != 0)
						return 64 * 1 + nbits + 1;
				}
				return 64 * 1 + 1;
			}
			if (pn0 != 0)
			{
				for (int nbits = 63; nbits > 0; nbits--)
				{
					if ((pn0 & 1UL << nbits) != 0)
						return 64 * 0 + nbits + 1;
				}
				return 64 * 0 + 1;
			}
			return 0;
		}

		public uint256(byte[] vch)
			: this(vch, true)
		{
		}

		public override bool Equals(object obj)
		{
			var item = obj as uint256;
			return Equals(item);
		}

		public bool Equals(uint256 other)
		{
			if (other is null)
			{
				return false;
			}

			bool equals = true;
			equals &= pn0 == other.pn0;
			equals &= pn1 == other.pn1;
			equals &= pn2 == other.pn2;
			equals &= pn3 == other.pn3;
			return equals;
		}

		public int CompareTo(uint256 other)
		{
			return Comparison(this, other);
		}

		public int CompareTo(object obj)
		{
			return obj is uint256 v ? CompareTo(v) :
				   obj is null ? CompareTo(null as uint256) : throw new ArgumentException($"Object is not an instance of uint256", nameof(obj));
		}

		public static bool operator ==(uint256 a, uint256 b)
		{
			if (System.Object.ReferenceEquals(a, b))
				return true;
			if (((object)a == null) || ((object)b == null))
				return false;

			bool equals = true;
			equals &= a.pn0 == b.pn0;
			equals &= a.pn1 == b.pn1;
			equals &= a.pn2 == b.pn2;
			equals &= a.pn3 == b.pn3;
			return equals;
		}

		public static bool operator <(uint256 a, uint256 b)
		{
			return Comparison(a, b) < 0;
		}

		public static bool operator >(uint256 a, uint256 b)
		{
			return Comparison(a, b) > 0;
		}

		public static bool operator <=(uint256 a, uint256 b)
		{
			return Comparison(a, b) <= 0;
		}

		public static bool operator >=(uint256 a, uint256 b)
		{
			return Comparison(a, b) >= 0;
		}

		private static int Comparison(uint256 a, uint256 b)
		{
			if (a is null && b is null)
				return 0;
			if (a is null && !(b is null))
				return -1;
			if (!(a is null) && b is null)
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

		public static bool operator !=(uint256 a, uint256 b)
		{
			return !(a == b);
		}

		public static bool operator ==(uint256 a, ulong b)
		{
			return (a == new uint256(b));
		}

		public static bool operator !=(uint256 a, ulong b)
		{
			return !(a == new uint256(b));
		}

		public static implicit operator uint256(ulong value)
		{
			return new uint256(value);
		}


		public byte[] ToBytes(bool lendian = true)
		{
			var arr = new byte[WIDTH_BYTE];
			ToBytes(arr);
			if (!lendian)
				Array.Reverse(arr);
			return arr;
		}

		/// <summary>
		/// Write this instance to the output in little endian
		/// </summary>
		/// <param name="output"></param>
		public void ToBytes(byte[] output)
		{
			ToBytes(output, true);
		}
		/// <summary>
		/// Write this instance to the output
		/// </summary>
		/// <param name="output"></param>
		/// <param name="lendian"></param>
		public void ToBytes(byte[] output, bool lendian)
		{
#if HAS_SPAN
			ToBytes(output.AsSpan(), lendian);
#else
			if (lendian)
			{
				Buffer.BlockCopy(Utils.ToBytes(pn0, true), 0, output, 8 * 0, 8);
				Buffer.BlockCopy(Utils.ToBytes(pn1, true), 0, output, 8 * 1, 8);
				Buffer.BlockCopy(Utils.ToBytes(pn2, true), 0, output, 8 * 2, 8);
				Buffer.BlockCopy(Utils.ToBytes(pn3, true), 0, output, 8 * 3, 8);
			}
			else
			{
				Buffer.BlockCopy(Utils.ToBytes(pn3, false), 0, output, 8 * 0, 8);
				Buffer.BlockCopy(Utils.ToBytes(pn2, false), 0, output, 8 * 1, 8);
				Buffer.BlockCopy(Utils.ToBytes(pn1, false), 0, output, 8 * 2, 8);
				Buffer.BlockCopy(Utils.ToBytes(pn0, false), 0, output, 8 * 3, 8);
			}
#endif
		}

#if HAS_SPAN
		public void ToBytes(Span<byte> output, bool lendian = true)
		{
			if (output.Length < WIDTH_BYTE)
				throw new ArgumentException(message: $"The array should be at least of size {WIDTH_BYTE}", paramName: nameof(output));

			if (BitConverter.IsLittleEndian)
			{
				if (lendian)
				{
					var ulongOutput = MemoryMarshal.Cast<byte, ulong>(output);
					ulongOutput[0] = pn0;
					ulongOutput[1] = pn1;
					ulongOutput[2] = pn2;
					ulongOutput[3] = pn3;
				}
				else
				{
					Span<ulong> temp = stackalloc ulong[4];
					temp[0] = pn0;
					temp[1] = pn1;
					temp[2] = pn2;
					temp[3] = pn3;
					var tempBytes = MemoryMarshal.Cast<ulong, byte>(temp);
					tempBytes.Reverse();
					tempBytes.CopyTo(output);
				}
				return;
			}
			var initial = output;
			Utils.ToBytes(pn0, true, output);
			output = output.Slice(8);
			Utils.ToBytes(pn1, true, output);
			output = output.Slice(8);
			Utils.ToBytes(pn2, true, output);
			output = output.Slice(8);
			Utils.ToBytes(pn3, true, output);

			if (!lendian)
				initial.Reverse();
		}
#endif
		public MutableUint256 AsBitcoinSerializable()
		{
			return new MutableUint256(this);
		}

		public int GetSerializeSize(int nType = 0, uint? protocolVersion = null)
		{
			return WIDTH_BYTE;
		}

		public int Size
		{
			get
			{
				return WIDTH_BYTE;
			}
		}

		public ulong GetLow64()
		{
			return pn0;
		}

		public uint GetLow32()
		{
			return unchecked((uint)(pn0 & 0xFFFFFFFF));
		}

		public override int GetHashCode()
		{
			long hash = 17;
			unchecked
			{
				hash = hash * 61 + (long)pn0;
				hash = hash * 61 + (long)pn1;
				hash = hash * 61 + (long)pn2;
				hash = hash * 61 + (long)pn3;
				return (int)hash;
			}
		}
	}
	public sealed class uint160 : IComparable<uint160>, IEquatable<uint160>, IComparable
	{
		public class MutableUint160 : IBitcoinSerializable
		{
			uint160 _Value;
			public uint160 Value
			{
				get
				{
					return _Value;
				}
				set
				{
					_Value = value;
				}
			}
			public MutableUint160()
			{
				_Value = uint160.Zero;
			}
			public MutableUint160(uint160 value)
			{
				_Value = value;
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
					_Value = new uint160(b);
				}
			}
		}
		static readonly uint160 _Zero = new uint160();
		public static uint160 Zero
		{
			get
			{
				return _Zero;
			}
		}

		static readonly uint160 _One = new uint160(1);
		public static uint160 One
		{
			get
			{
				return _One;
			}
		}

		public uint160()
		{
		}

		public uint160(uint160 b)
		{
			pn0 = b.pn0;
			pn1 = b.pn1;
			pn2 = b.pn2;
			pn3 = b.pn3;
			pn4 = b.pn4;
		}

		public static uint160 Parse(string hex)
		{
			return new uint160(hex);
		}
		public static bool TryParse(string hex, out uint160 result)
		{
			if (hex == null)
				throw new ArgumentNullException(nameof(hex));
			if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				hex = hex.Substring(2);
			result = null;
			if (hex.Length != WIDTH_BYTE * 2)
				return false;
			if (!((HexEncoder)Encoders.Hex).IsValid(hex))
				return false;
			result = new uint160(hex);
			return true;
		}

		private static readonly HexEncoder Encoder = new HexEncoder();
		private const int WIDTH_BYTE = 160 / 8;
		internal readonly UInt32 pn0;
		internal readonly UInt32 pn1;
		internal readonly UInt32 pn2;
		internal readonly UInt32 pn3;
		internal readonly UInt32 pn4;

		public byte GetByte(int index)
		{
			var uintIndex = index / sizeof(uint);
			var byteIndex = index % sizeof(uint);
			UInt32 value;
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
				default:
					throw new ArgumentOutOfRangeException("index");
			}
			return (byte)(value >> (byteIndex * 8));
		}

		public override string ToString()
		{
			return Encoder.EncodeData(ToBytes().Reverse().ToArray());
		}

		public uint160(ulong b)
		{
			pn0 = (uint)b;
			pn1 = (uint)(b >> 32);
			pn2 = 0;
			pn3 = 0;
			pn4 = 0;
		}

		public uint160(byte[] vch, bool lendian = true)
		{
			if (vch.Length != WIDTH_BYTE)
			{
				throw new FormatException("the byte array should be 20 bytes long");
			}

			if (!lendian)
				vch = vch.Reverse().ToArray();

			pn0 = Utils.ToUInt32(vch, 4 * 0, true);
			pn1 = Utils.ToUInt32(vch, 4 * 1, true);
			pn2 = Utils.ToUInt32(vch, 4 * 2, true);
			pn3 = Utils.ToUInt32(vch, 4 * 3, true);
			pn4 = Utils.ToUInt32(vch, 4 * 4, true);

		}

		public uint160(string str)
		{
			pn0 = 0;
			pn1 = 0;
			pn2 = 0;
			pn3 = 0;
			pn4 = 0;
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

		}

		public uint160(byte[] vch)
			: this(vch, true)
		{
		}

		public override bool Equals(object obj)
		{
			var item = obj as uint160;
			return Equals(item);
		}

		public bool Equals(uint160 other)
		{
			if (other is null)
				return false;
			bool equals = true;
			equals &= pn0 == other.pn0;
			equals &= pn1 == other.pn1;
			equals &= pn2 == other.pn2;
			equals &= pn3 == other.pn3;
			equals &= pn4 == other.pn4;
			return equals;
		}

		public int CompareTo(uint160 other)
		{
			return Comparison(this, other);
		}

		public int CompareTo(object obj)
		{
			return obj is uint160 v ? CompareTo(v) :
				   obj is null ? CompareTo(null as uint160) : throw new ArgumentException($"Object is not an instance of uint160", nameof(obj));
		}

		public static bool operator ==(uint160 a, uint160 b)
		{
			if (System.Object.ReferenceEquals(a, b))
				return true;
			if (((object)a == null) || ((object)b == null))
				return false;

			bool equals = true;
			equals &= a.pn0 == b.pn0;
			equals &= a.pn1 == b.pn1;
			equals &= a.pn2 == b.pn2;
			equals &= a.pn3 == b.pn3;
			equals &= a.pn4 == b.pn4;
			return equals;
		}

		public static bool operator <(uint160 a, uint160 b)
		{
			return Comparison(a, b) < 0;
		}

		public static bool operator >(uint160 a, uint160 b)
		{
			return Comparison(a, b) > 0;
		}

		public static bool operator <=(uint160 a, uint160 b)
		{
			return Comparison(a, b) <= 0;
		}

		public static bool operator >=(uint160 a, uint160 b)
		{
			return Comparison(a, b) >= 0;
		}

		private static int Comparison(uint160 a, uint160 b)
		{
			if (a is null && b is null)
				return 0;
			if (a is null && !(b is null))
				return -1;
			if (!(a is null) && b is null)
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

		public static bool operator !=(uint160 a, uint160 b)
		{
			return !(a == b);
		}

		public static bool operator ==(uint160 a, ulong b)
		{
			return (a == new uint160(b));
		}

		public static bool operator !=(uint160 a, ulong b)
		{
			return !(a == new uint160(b));
		}

		public static implicit operator uint160(ulong value)
		{
			return new uint160(value);
		}


		public byte[] ToBytes(bool lendian = true)
		{
			var arr = new byte[WIDTH_BYTE];
			Buffer.BlockCopy(Utils.ToBytes(pn0, true), 0, arr, 4 * 0, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn1, true), 0, arr, 4 * 1, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn2, true), 0, arr, 4 * 2, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn3, true), 0, arr, 4 * 3, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn4, true), 0, arr, 4 * 4, 4);
			if (!lendian)
				Array.Reverse(arr);
			return arr;
		}

		public MutableUint160 AsBitcoinSerializable()
		{
			return new MutableUint160(this);
		}

		public int GetSerializeSize(int nType = 0, uint? protocolVersion = null)
		{
			return WIDTH_BYTE;
		}

		public int Size
		{
			get
			{
				return WIDTH_BYTE;
			}
		}

		public ulong GetLow64()
		{
			return pn0 | (ulong)pn1 << 32;
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
				hash = hash * 31 + (int)pn0;
				hash = hash * 31 + (int)pn1;
				hash = hash * 31 + (int)pn2;
				hash = hash * 31 + (int)pn3;
				hash = hash * 31 + (int)pn4;
			}
			return hash;
		}
	}
}
