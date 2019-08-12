using System;

namespace NBitcoin.Tests.Helpers
{
	public static class Extensions
	{
		public static string ToHexString(this Span<byte> span)
		{
			return "0x" + BitConverter.ToString(span.ToArray()).Replace("-", "").ToLowerInvariant();

		}

		public static string ToHexString(this byte[] b)
			=> ToHexString(b.AsSpan());

		public static byte[] HexToBytes(this string hexString)
		{
			int chars = hexString.Length;
			byte[] bytes = new byte[chars / 2];
			for (int i = 0; i < chars; i += 2)
			{
				bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
			}
			return bytes;
		}
	}
}