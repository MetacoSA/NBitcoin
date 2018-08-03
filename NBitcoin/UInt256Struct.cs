using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin
{
	/// <summary>
	/// Same as uint256, but as a structure
	/// </summary>
	public readonly struct UInt256Struct : IEquatable<UInt256Struct>
	{
		public readonly static UInt256Struct Zero = new UInt256Struct();
		public const int Length = 256 / 8;
		internal readonly UInt32 pn0;
		internal readonly UInt32 pn1;
		internal readonly UInt32 pn2;
		internal readonly UInt32 pn3;
		internal readonly UInt32 pn4;
		internal readonly UInt32 pn5;
		internal readonly UInt32 pn6;
		internal readonly UInt32 pn7;

		/// <summary>
		/// Parse hexadecimal string into a Uint256Struct instance
		/// </summary>
		/// <param name="hex">Big endian hex string</param>
		public UInt256Struct(string hex)
		{
			if(hex == null)
				throw new ArgumentNullException(nameof(hex));
			hex = hex.Trim();
			var bytes = ((NBitcoin.DataEncoders.HexEncoder)Encoders.Hex).DecodeData(hex);
			Array.Reverse(bytes);
			if(bytes.Length != Length)
				throw new FormatException("Invalid hex length");
			pn0 = Utils.ToUInt32(bytes, 4 * 0, true);
			pn1 = Utils.ToUInt32(bytes, 4 * 1, true);
			pn2 = Utils.ToUInt32(bytes, 4 * 2, true);
			pn3 = Utils.ToUInt32(bytes, 4 * 3, true);
			pn4 = Utils.ToUInt32(bytes, 4 * 4, true);
			pn5 = Utils.ToUInt32(bytes, 4 * 5, true);
			pn6 = Utils.ToUInt32(bytes, 4 * 6, true);
			pn7 = Utils.ToUInt32(bytes, 4 * 7, true);
		}

#if HAS_SPAN
		/// <summary>
		/// Deserialize bytes into a Uint256Struct instance
		/// </summary>
		/// <param name="bytes">Little endian bytes</param>
		public UInt256Struct(ReadOnlySpan<byte> bytes)
		{
			if(bytes.Length != Length)
			{
				throw new FormatException("the byte array should be 32 bytes long");
			}

			pn0 = Utils.ToUInt32(bytes, 4 * 0, true);
			pn1 = Utils.ToUInt32(bytes, 4 * 1, true);
			pn2 = Utils.ToUInt32(bytes, 4 * 2, true);
			pn3 = Utils.ToUInt32(bytes, 4 * 3, true);
			pn4 = Utils.ToUInt32(bytes, 4 * 4, true);
			pn5 = Utils.ToUInt32(bytes, 4 * 5, true);
			pn6 = Utils.ToUInt32(bytes, 4 * 6, true);
			pn7 = Utils.ToUInt32(bytes, 4 * 7, true);
		}
#endif

		public UInt256Struct(uint256 value)
		{
			if(value == null)
				value = uint256.Zero;
			pn0 = value.pn0;
			pn1 = value.pn1;
			pn2 = value.pn2;
			pn3 = value.pn3;
			pn4 = value.pn4;
			pn5 = value.pn5;
			pn6 = value.pn6;
			pn7 = value.pn7;
		}

		/// <summary>
		/// Deserialize bytes into a Uint256Struct instance
		/// </summary>
		/// <param name="hex">Little endian bytes</param>
		public UInt256Struct(byte[] bytes) : this(bytes, 0)
		{

		}
		/// <summary>
		/// Deserialize bytes into a Uint256Struct instance
		/// </summary>
		/// <param name="bytes">Little endian bytes</param>
		/// <param name="offset">Offset of the array</param>
		public UInt256Struct(byte[] bytes, int offset)
		{
			if(bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			if(bytes.Length - offset < Length)
				throw new ArgumentOutOfRangeException(paramName: nameof(bytes), message: $"bytes should be at least {Length} bytes");

			pn0 = Utils.ToUInt32(bytes, offset + 4 * 0, true);
			pn1 = Utils.ToUInt32(bytes, offset + 4 * 1, true);
			pn2 = Utils.ToUInt32(bytes, offset + 4 * 2, true);
			pn3 = Utils.ToUInt32(bytes, offset + 4 * 3, true);
			pn4 = Utils.ToUInt32(bytes, offset + 4 * 4, true);
			pn5 = Utils.ToUInt32(bytes, offset + 4 * 5, true);
			pn6 = Utils.ToUInt32(bytes, offset + 4 * 6, true);
			pn7 = Utils.ToUInt32(bytes, offset + 4 * 7, true);
		}

		public static UInt256Struct Parse(string hex)
		{
			return new UInt256Struct(hex);
		}
		public static bool TryParse(string hex, out UInt256Struct result)
		{
			if(hex == null)
				throw new ArgumentNullException(nameof(hex));
			result = default(UInt256Struct);
			if(hex.Length != Length * 2)
				return false;
			if(!((NBitcoin.DataEncoders.HexEncoder)Encoders.Hex).IsValid(hex))
				return false;
			result = new UInt256Struct(hex);
			return true;
		}

		public static implicit operator UInt256Struct(uint256 value)
		{
			return new UInt256Struct(value);
		}

		public override string ToString()
		{
			var bytes = ToBytes();
			Array.Reverse(bytes);
			return Encoders.Hex.EncodeData(bytes);
		}

		public byte[] ToBytes()
		{
			var arr = new byte[Length];
			ToBytes(arr);
			return arr;
		}
		public void ToBytes(byte[] output)
		{
			if(output == null)
				throw new ArgumentNullException(nameof(output));
			Buffer.BlockCopy(Utils.ToBytes(pn0, true), 0, output, 4 * 0, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn1, true), 0, output, 4 * 1, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn2, true), 0, output, 4 * 2, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn3, true), 0, output, 4 * 3, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn4, true), 0, output, 4 * 4, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn5, true), 0, output, 4 * 5, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn6, true), 0, output, 4 * 6, 4);
			Buffer.BlockCopy(Utils.ToBytes(pn7, true), 0, output, 4 * 7, 4);
		}

		public uint256 ToUInt256()
		{
			return new uint256(this);
		}


#if HAS_SPAN
		public void ToBytes(Span<byte> output)
		{
			if(output.Length < Length)
				throw new ArgumentException(message: $"The array should be at least of size {Length}", paramName: nameof(output));

			var initial = output;
			Utils.ToBytes(pn0, true, output);
			output = output.Slice(4);
			Utils.ToBytes(pn1, true, output);
			output = output.Slice(4);
			Utils.ToBytes(pn2, true, output);
			output = output.Slice(4);
			Utils.ToBytes(pn3, true, output);
			output = output.Slice(4);
			Utils.ToBytes(pn4, true, output);
			output = output.Slice(4);
			Utils.ToBytes(pn5, true, output);
			output = output.Slice(4);
			Utils.ToBytes(pn6, true, output);
			output = output.Slice(4);
			Utils.ToBytes(pn7, true, output);
		}
#endif

		public bool Equals(UInt256Struct other)
		{
			bool equals = true;
			equals &= pn0 == other.pn0;
			equals &= pn1 == other.pn1;
			equals &= pn2 == other.pn2;
			equals &= pn3 == other.pn3;
			equals &= pn4 == other.pn4;
			equals &= pn5 == other.pn5;
			equals &= pn6 == other.pn6;
			equals &= pn7 == other.pn7;
			return equals;
		}

		public override bool Equals(object obj)
		{
			if(obj == null)
				return false;
			if(obj is UInt256Struct other)
				return this.Equals(other);
			if(obj is uint256 other2)
				return this.Equals(new UInt256Struct(other2));
			return false;
		}

		public static bool operator ==(UInt256Struct a, UInt256Struct b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(UInt256Struct a, UInt256Struct b)
		{
			return !a.Equals(b);
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
				hash = hash * 31 + (int)pn5;
				hash = hash * 31 + (int)pn6;
				hash = hash * 31 + (int)pn7;
			}
			return hash;
		}
	}
}
