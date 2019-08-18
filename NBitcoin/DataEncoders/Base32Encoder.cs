using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.DataEncoders
{
	public class Base32Encoder : DataEncoder
	{
		static readonly int[] decode32_table =
   {
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 26, 27, 28, 29, 30, 31, -1, -1, -1, -1,
		-1, -1, -1, -1, -1,  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14,
		15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, -1, -1, -1, -1, -1, -1,  0,  1,  2,
		 3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22,
		23, 24, 25, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
	};
		const string pbase32 = "abcdefghijklmnopqrstuvwxyz234567";
		public override string EncodeData(byte[] data, int offset, int count)
		{
			var str = new char[((count + 4) / 5) * 8];
			var stri = 0;
			ConvertBits(c => str[stri++] = pbase32[c], data, offset, count, 8, 5, true);
			while (stri % 8 != 0)
				str[stri++] = '=';
			return new string(str);
		}
#if HAS_SPAN
		static bool ConvertBits(Action<byte> outfn, Span<byte> val, int valOffset, int valCount, int frombits, int tobits, bool pad)
#else
		static bool ConvertBits(Action<byte> outfn, byte[] val, int valOffset, int valCount, int frombits, int tobits, bool pad)
#endif
		{
			int acc = 0;
			int bits = 0;
			int maxv = (1 << tobits) - 1;
			int max_acc = (1 << (frombits + tobits - 1)) - 1;
			for (int i = valOffset; i < valOffset + valCount; i++)
			{
				acc = ((acc << frombits) | val[i]) & max_acc;
				bits += frombits;
				while (bits >= tobits)
				{
					bits -= tobits;
					outfn((byte)((acc >> bits) & maxv));
				}
			}
			if (pad)
			{
				if (bits != 0)
					outfn((byte)((acc << (tobits - bits)) & maxv));
			}
			else if (bits >= frombits || ((acc << (tobits - bits)) & maxv) != 0)
			{
				return false;
			}
			return true;
		}

		public override byte[] DecodeData(string encoded)
		{
			if (encoded.Length == 0)
				return new byte[0];
			int p = 0;
#if HAS_SPAN
			Span<byte> val = encoded.Length < 100 ? stackalloc byte[encoded.Length] : new byte[encoded.Length];
#else
			var val = new byte[encoded.Length];
#endif
			var vali = 0;
			foreach (var c in encoded)
			{
				int x = decode32_table[(byte)c];
				if (x == -1)
					break;
				val[vali++] = (byte)x;
				++p;
			}

			var ret = new byte[(encoded.Length * 5) / 8];

			int reti = 0;
			bool valid = ConvertBits((c) => ret[reti++] = c, val, 0, val.Length, 5, 8, false);
			int q = p;
			while (valid && p < encoded.Length)
			{
				if (encoded[p] != '=')
				{
					valid = false;
					break;
				}
				++p;
			}
			valid = valid && p % 8 == 0 && p - q < 8;
			if (!valid)
				throw new FormatException("Invalid base32 string");

			return ret;
		}
	}
}
