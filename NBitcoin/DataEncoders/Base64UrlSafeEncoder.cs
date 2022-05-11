using System;
using System.Linq;

namespace NBitcoin.DataEncoders
{
	public class Base64UrlSafeEncoder : DataEncoder
	{
		static readonly char[] padding = { '=' };
		static readonly int paddingBoundary = 4;
		public override byte[] DecodeData(string encoded)
		{
			var temp = encoded.Replace('-', '+').Replace('_', '/');

			// Re-add padding if necessary
			var remaining = temp.Length % paddingBoundary;
			switch(remaining) {
				case 2:
				case 3:
					temp += string.Concat(Enumerable.Repeat('=', paddingBoundary-remaining));
					break;
			}
			return Convert.FromBase64String(temp);
		}

		public override string EncodeData(byte[] data, int offset, int count)
		{
			return Convert.ToBase64String(data, offset, count).Replace("+", "-").Replace("/", "_").TrimEnd(padding);
		}
	}
}
