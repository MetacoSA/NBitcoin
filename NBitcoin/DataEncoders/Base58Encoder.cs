using NBitcoin.Crypto;
using System;
using System.Linq;
using System.Text;

namespace NBitcoin.DataEncoders
{
	public class Base58CheckEncoder : Base58Encoder
	{
		private static readonly Base58Encoder InternalEncoder = new Base58Encoder();

		/// <summary>
		/// Fast check if the string to know if base58 str
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public override bool IsMaybeEncoded(string str)
		{
			return base.IsMaybeEncoded(str) && str.Length > 4;
		}

		public override string EncodeData(byte[] data, int offset, int count)
		{
			var toEncode = new byte[count + 4];
			Buffer.BlockCopy(data, offset, toEncode, 0, count);

			var hash = CalculateHash(data, offset, count);
			Buffer.BlockCopy(hash, 0, toEncode, count, 4);

			return InternalEncoder.EncodeData(toEncode, 0, toEncode.Length);
		}

		public override byte[] DecodeData(string encoded)
		{
			var vchRet = InternalEncoder.DecodeData(encoded);
			if (vchRet.Length < 4)
			{
				Array.Clear(vchRet, 0, vchRet.Length);
				throw new FormatException("Invalid checked base 58 string");
			}
			var calculatedHash = CalculateHash(vchRet, 0, vchRet.Length - 4);

			if (!Utils.ArrayEqual(calculatedHash, 0, vchRet, vchRet.Length - 4, 4))
			{
				Array.Clear(vchRet, 0, vchRet.Length);
				throw new FormatException("Invalid hash of the base 58 string");
			}
			vchRet = vchRet.SafeSubarray(0, vchRet.Length - 4);
			return vchRet;
		}

		protected virtual byte[] CalculateHash(byte[] bytes, int offset, int length)
		{
			return Hashes.DoubleSHA256RawBytes(bytes, offset, length);
		}
	}

	public class Base58Encoder : DataEncoder
	{
		static readonly char[] pszBase58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz".ToCharArray();
		static readonly int[] mapBase58 = new int[]{
	-1,-1,-1,-1,-1,-1,-1,-1, -1,-1,-1,-1,-1,-1,-1,-1,
	-1,-1,-1,-1,-1,-1,-1,-1, -1,-1,-1,-1,-1,-1,-1,-1,
	-1,-1,-1,-1,-1,-1,-1,-1, -1,-1,-1,-1,-1,-1,-1,-1,
	-1, 0, 1, 2, 3, 4, 5, 6,  7, 8,-1,-1,-1,-1,-1,-1,
	-1, 9,10,11,12,13,14,15, 16,-1,17,18,19,20,21,-1,
	22,23,24,25,26,27,28,29, 30,31,32,-1,-1,-1,-1,-1,
	-1,33,34,35,36,37,38,39, 40,41,42,43,-1,44,45,46,
	47,48,49,50,51,52,53,54, 55,56,57,-1,-1,-1,-1,-1,
	-1,-1,-1,-1,-1,-1,-1,-1, -1,-1,-1,-1,-1,-1,-1,-1,
	-1,-1,-1,-1,-1,-1,-1,-1, -1,-1,-1,-1,-1,-1,-1,-1,
	-1,-1,-1,-1,-1,-1,-1,-1, -1,-1,-1,-1,-1,-1,-1,-1,
	-1,-1,-1,-1,-1,-1,-1,-1, -1,-1,-1,-1,-1,-1,-1,-1,
	-1,-1,-1,-1,-1,-1,-1,-1, -1,-1,-1,-1,-1,-1,-1,-1,
	-1,-1,-1,-1,-1,-1,-1,-1, -1,-1,-1,-1,-1,-1,-1,-1,
	-1,-1,-1,-1,-1,-1,-1,-1, -1,-1,-1,-1,-1,-1,-1,-1,
	-1,-1,-1,-1,-1,-1,-1,-1, -1,-1,-1,-1,-1,-1,-1,-1,
};
		/// <summary>
		/// Fast check if the string to know if base58 str
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public virtual bool IsMaybeEncoded(string str)
		{
			bool maybeb58 = true;
			if (maybeb58)
			{
				for (int i = 0; i < str.Length; i++)
				{
					if (!Base58Encoder.pszBase58.Contains(str[i]))
					{
						maybeb58 = false;
						break;
					}
				}
			}
			return maybeb58 && str.Length > 0;
		}

		public override string EncodeData(byte[] data, int offset, int count)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			// Skip & count leading zeroes.
			int zeroes = 0;
			int length = 0;
			while (offset != count && data[offset] == 0)
			{
				offset++;
				zeroes++;
			}
			// Allocate enough space in big-endian base58 representation.
			int size = (count - offset) * 138 / 100 + 1; // log(256) / log(58), rounded up.
#if HAS_SPAN
			Span<byte> b58 = size <= 128 ? stackalloc byte[size] : new byte[size];
#else
			byte[] b58 = new byte[size];
#endif
			// Process the bytes.
			while (offset != count)
			{
				int carry = data[offset];
				int i = 0;
				// Apply "b58 = b58 * 256 + ch".
				for (int it = size - 1; (carry != 0 || i < length) && it >= 0; i++, it--)
				{
					carry += 256 * b58[it];
					b58[it] = (byte)(carry % 58);
					carry /= 58;
				}

				length = i;
				offset++;
			}
			// Skip leading zeroes in base58 result.
			int it2 = (size - length);
			while (it2 != size && b58[it2] == 0)
				it2++;
			// Translate the result into a string.
#if HAS_SPAN
			var s = zeroes + size - it2;
			Span<char> str = s <= 128 ? stackalloc char[s] : new char[s];
			str.Slice(0, zeroes).Fill('1');
#else
			var str = new char[zeroes + size - it2];
#if NO_ARRAY_FILL
			ArrayFill<char>(str, '1', 0, zeroes);
#else
			Array.Fill<char>(str, '1', 0, zeroes);
#endif
#endif
			int i2 = zeroes;
			while (it2 != size)
				str[i2++] = pszBase58[b58[it2++]];
			return new string(str);
		}

#if NO_ARRAY_FILL
		static void ArrayFill<T>(T[] array, T value, int index, int count)
		{
			for (int i = index; i < index + count; i++)
			{
				array[i] = value;
			}
		}
#endif


		public override byte[] DecodeData(string encoded)
		{
			if (encoded == null)
				throw new ArgumentNullException(nameof(encoded));
			int psz = 0;
			// Skip leading spaces.
			while (psz < encoded.Length && IsSpace(encoded[psz]))
				psz++;
			// Skip and count leading '1's.
			int zeroes = 0;
			int length = 0;
			while (psz < encoded.Length && encoded[psz] == '1')
			{
				zeroes++;
				psz++;
			}
			// Allocate enough space in big-endian base256 representation.
			int size = (encoded.Length - psz) * 733 / 1000 + 1; // log(58) / log(256), rounded up.
#if HAS_SPAN
			Span<byte> b256 = size <= 128 ? stackalloc byte[size] : new byte[size];
#else
			byte[] b256 = new byte[size];
#endif
			// Process the characters.
			while (psz < encoded.Length && !IsSpace(encoded[psz]))
			{
				// Decode base58 character
				int carry = mapBase58[(byte)encoded[psz]];
				if (carry == -1)  // Invalid b58 character
					throw new FormatException("Invalid base58 data");
				int i = 0;
				for (int it = size - 1; (carry != 0 || i < length) && it >= 0; i++, it--)
				{
					carry += 58 * b256[it];
					b256[it] = (byte)(carry % 256);
					carry /= 256;
				}
				length = i;
				psz++;
			}
			// Skip trailing spaces.
			while (psz < encoded.Length && IsSpace(encoded[psz]))
				psz++;
			if (psz != encoded.Length)
				throw new FormatException("Invalid base58 data");
			// Skip leading zeroes in b256.
			var it2 = size - length;
			// Copy result into output vector.
			var vch = new byte[zeroes + size - it2];
#if NO_ARRAY_FILL
			ArrayFill<byte>(vch, 0, 0, zeroes);
#else
			Array.Fill<byte>(vch, 0, 0, zeroes);
#endif
			int i2 = zeroes;
			while (it2 != size)
				vch[i2++] = (b256[it2++]);
			return vch;
		}
	}
}
