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
		public HDFingerprint(uint value)
		{
			_Value = value;
		}

		public byte[] ToBytes()
		{
			return Utils.ToBytes(_Value, true);
		}

		public uint ToUInt32()
		{
			return _Value;
		}

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
