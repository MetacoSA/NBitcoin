using System;

namespace NBitcoin.DataEncoders
{
	public class Base64UrlSafeEncoder : DataEncoder
	{
		static readonly char[] padding = { '=' };
		public override byte[] DecodeData(string encoded)
		{
			return Convert.FromBase64String(encoded.TrimEnd(padding).Replace('-', '+').Replace('_', '/'));
		}

		public override string EncodeData(byte[] data, int offset, int count)
		{
			return Convert.ToBase64String(data, offset, count).Replace("+", "-").Replace("/", "_");
		}
	}
}
