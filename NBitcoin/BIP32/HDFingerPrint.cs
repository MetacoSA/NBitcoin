using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin
{
	public readonly struct HDFingerprint
	{
		readonly uint _Value;
		public HDFingerprint(byte[] bytes)
		{
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			if (bytes.Length != 4)
				throw new ArgumentException(paramName: nameof(bytes), message: "Bytes should be of length 4");
			_Value = Utils.ToUInt32(bytes, true);
		}
#if HAS_SPAN
		public HDFingerprint(ReadOnlySpan<byte> bytes)
		{
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			if (bytes.Length != 4)
				throw new ArgumentException(paramName: nameof(bytes), message: "Bytes should be of length 4");
			_Value = Utils.ToUInt32(bytes, true);
		}
#endif

		public HDFingerprint(byte[] bytes, int index)
		{
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			_Value = Utils.ToUInt32(bytes, index, true);
		}

		public HDFingerprint(uint value)
		{
			_Value = value;
		}

		public byte[] ToBytes()
		{
			return Utils.ToBytes(_Value, true);
		}
#if HAS_SPAN
		public void ToBytes(Span<byte> output)
		{
			Utils.ToBytes(_Value, true, output);
		}
#endif

		public override bool Equals(Object obj)
		{
			return obj is HDFingerprint && this == (HDFingerprint)obj;
		}
		public override int GetHashCode()
		{
			return _Value.GetHashCode();
		}
		public static bool operator ==(HDFingerprint x, HDFingerprint y)
		{
			return x._Value == y._Value;
		}
		public static bool operator !=(HDFingerprint x, HDFingerprint y)
		{
			return !(x == y);
		}

		public override string ToString()
		{
			return NBitcoin.DataEncoders.Encoders.Hex.EncodeData(ToBytes());
		}
	}
}
