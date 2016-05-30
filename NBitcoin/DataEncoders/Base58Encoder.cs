using NBitcoin.Crypto;
using System;
using System.Linq;
using System.Numerics;

namespace NBitcoin.DataEncoders
{
	public class Base58CheckEncoder : Base58Encoder
	{
		private static readonly Base58Encoder InternalEncoder = new Base58Encoder();

		public override string EncodeData(byte[] data, int offset, int count)
		{
			var toEncode = new byte[count + 4];
			Buffer.BlockCopy(data, offset, toEncode, 0, count);

			var hash = Hashes.Hash256(data, offset, count).ToBytes();
			Buffer.BlockCopy(hash, 0, toEncode, count, 4);

			return InternalEncoder.EncodeData(toEncode, 0, toEncode.Length);
		}

		public override byte[] DecodeData(string encoded)
		{
			var vchRet = InternalEncoder.DecodeData(encoded);
			if(vchRet.Length < 4)
			{
				Array.Clear(vchRet, 0, vchRet.Length);
				throw new FormatException("Invalid checked base 58 string");
			}
			var calculatedHash = Hashes.Hash256(vchRet, 0, vchRet.Length - 4).ToBytes().SafeSubarray(0, 4);
			var expectedHash = vchRet.SafeSubarray(vchRet.Length - 4, 4);

			if(!Utils.ArrayEqual(calculatedHash, expectedHash))
			{
				Array.Clear(vchRet, 0, vchRet.Length);
				throw new FormatException("Invalid hash of the base 58 string");
			}
			vchRet = vchRet.SafeSubarray(0, vchRet.Length - 4);
			return vchRet;
		}
	}

	public class Base58Encoder : DataEncoder
	{
		public override string EncodeData(byte[] data, int offset, int count)
		{
			BigInteger bn58 = 58;
			BigInteger bn0 = 0;

			// Convert big endian data to little endian
			// Extra zero at the end make sure bignum will interpret as a positive number
			var vchTmp = data.SafeSubarray(offset, count).Reverse().Concat(new byte[] { 0x00 }).ToArray();

			// Convert little endian data to bignum
			var bn = new BigInteger(vchTmp);

			// Convert bignum to std::string
			var str = "";
			// Expected size increase from base58 conversion is approximately 137%
			// use 138% to be safe

			while(bn > bn0)
			{
				BigInteger rem;
				var dv = BigInteger.DivRem(bn, bn58, out rem);
				bn = dv;
				var c = (int)rem;
				str += pszBase58[c];
			}

			// Leading zeroes encoded as base58 zeros
			for(int i = offset; i < offset + count && data[i] == 0; i++)
				str += pszBase58[0];

			// Convert little endian std::string to big endian
			str = new String(str.ToCharArray().Reverse().ToArray()); //keep that way to be portable
			return str;
		}


		const string pszBase58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";


		public override byte[] DecodeData(string encoded)
		{
			if(encoded == null)
				throw new ArgumentNullException("encoded");

			var result = new byte[0];
			if(encoded.Length == 0)
				return result;
			BigInteger bn58 = 58;
			BigInteger bn = 0;
			int i = 0;
			while(IsSpace(encoded[i]))
			{
				i++;
				if(i >= encoded.Length)
					return result;
			}

			for(int y = i; y < encoded.Length; y++)
			{
				var p1 = pszBase58.IndexOf(encoded[y]);
				if(p1 == -1)
				{
					while(IsSpace(encoded[y]))
					{
						y++;
						if(y >= encoded.Length)
							break;
					}
					if(y != encoded.Length)
						throw new FormatException("Invalid base 58 string");
					break;
				}
				var bnChar = new BigInteger(p1);
				bn = BigInteger.Multiply(bn, bn58);
				bn += bnChar;
			}

			// Get bignum as little endian data
			var vchTmp = bn.ToByteArray();
			if(vchTmp.All(b => b == 0))
				vchTmp = new byte[0];

			// Trim off sign byte if present
			if(vchTmp.Length >= 2 && vchTmp[vchTmp.Length - 1] == 0 && vchTmp[vchTmp.Length - 2] >= 0x80)
				vchTmp = vchTmp.SafeSubarray(0, vchTmp.Length - 1);

			// Restore leading zeros
			int nLeadingZeros = 0;
			for(int y = i; y < encoded.Length && encoded[y] == pszBase58[0]; y++)
				nLeadingZeros++;


			result = new byte[nLeadingZeros + vchTmp.Length];
			Array.Copy(vchTmp.Reverse().ToArray(), 0, result, nLeadingZeros, vchTmp.Length);
			return result;
		}
	}
}
