#region License
/*
CryptSharp
Copyright (c) 2013 James F. Bellinger <http://www.zer7.com/software/cryptsharp>

Permission to use, copy, modify, and/or distribute this software for any
purpose with or without fee is hereby granted, provided that the above
copyright notice and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/
#endregion

namespace NBitcoin.Crypto.Internal
{
	static class BitPacking
	{
		public static uint UInt24FromLEBytes(byte[] bytes, int offset)
		{
			return
				(uint)bytes[offset + 2] << 16 |
				(uint)bytes[offset + 1] << 8 |
				(uint)bytes[offset + 0];
		}

		public static uint UInt32FromLEBytes(byte[] bytes, int offset)
		{
			return
				(uint)bytes[offset + 3] << 24 |
				UInt24FromLEBytes(bytes, offset);
		}

		public static void BEBytesFromUInt32(uint value, byte[] bytes, int offset)
		{
			bytes[offset + 0] = (byte)(value >> 24);
			bytes[offset + 1] = (byte)(value >> 16);
			bytes[offset + 2] = (byte)(value >> 8);
			bytes[offset + 3] = (byte)(value);
		}

		public static void LEBytesFromUInt24(uint value, byte[] bytes, int offset)
		{
			bytes[offset + 2] = (byte)(value >> 16);
			bytes[offset + 1] = (byte)(value >> 8);
			bytes[offset + 0] = (byte)(value);
		}

		public static void LEBytesFromUInt32(uint value, byte[] bytes, int offset)
		{
			bytes[offset + 3] = (byte)(value >> 24);
			LEBytesFromUInt24(value, bytes, offset);
		}
	}
}
