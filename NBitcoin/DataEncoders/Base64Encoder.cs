using System;

namespace NBitcoin.DataEncoders
{
	public class Base64Encoder : DataEncoder
	{
		public override byte[] DecodeData(string encoded)
		{
			return Convert.FromBase64String(encoded);
		}

		public override string EncodeData(byte[] data, int offset, int count)
		{
			return Convert.ToBase64String(data, offset, count);
		}
	}
}
