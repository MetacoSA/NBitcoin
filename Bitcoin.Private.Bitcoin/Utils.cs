using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bitcoin.Private.Bitcoin
{
	public class Utils
	{
		public const int PROTOCOL_VERSION = 70002;
		public const long COIN = 100000000;
		public const long CENT = 1000000;

		public static string HexStr(byte[] bytes, bool fSpaces = false)
		{
			if(bytes == null)
				throw new ArgumentNullException("bytes");
			return HexStr(bytes, bytes.Length, fSpaces);
		}
		public static string HexStr(byte[] bytes, int count, bool fSpaces = false)
		{
			if(bytes == null)
				throw new ArgumentNullException("bytes");
			if(count < 0)
				count = bytes.Length;

			StringBuilder rv = new StringBuilder();
			for(int i = 0 ; i < count ; i++)
			{
				var val = bytes[i];
				if(fSpaces && i != 0)
					rv.Append(' ');
				rv.Append(hexDigits[val >> 4]);
				rv.Append(hexDigits[val & 15]);
			}

			return rv.ToString();
		}
		public static byte[] ParseHex(string hex)
		{
			if(hex == null)
				throw new ArgumentNullException("hex");

			// convert hex dump to vector
			Queue<byte> vch = new Queue<byte>();

			int i = 0;

			while(true)
			{
				if(i >= hex.Length)
					break;
				char psz = hex[i];
				while(isspace(psz))
				{
					i++;
					if(i >= hex.Length)
						break;
					psz = hex[i];
				}
				if(i >= hex.Length)
					break;
				psz = hex[i];

				int c = HexDigit(psz);
				i++;
				if(i >= hex.Length)
					break;
				psz = hex[i];
				if(c == -1)
					break;
				int n = (c << 4);
				c = HexDigit(psz);
				i++;
				if(c == -1)
					break;
				n |= c;
				vch.Enqueue((byte)n);
			}
			return vch.ToArray();
		}

		static readonly char[] hexDigits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F' };
		static readonly byte[] hexValues = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 10, 11, 12, 13, 14, 15 };
		public static int HexDigit(char c)
		{
			var i = Array.IndexOf(hexDigits, c);
			if(i == -1)
				return -1;
			return hexValues[i];
		}

		private static bool isspace(char c)
		{
			return c == ' ' || c == '\t' || c == '\n' || c == '\v' || c == '\f' || c == '\r';
		}

		public static string FormatMoney(long n, bool fPlus = false)
		{
			// Note: not using straight sprintf here because we do NOT want
			// localized number formatting.
			long n_abs = (n > 0 ? n : -n);
			long quotient = n_abs / COIN;
			long remainder = n_abs % COIN;

			string str = String.Format("{0}.{1:D8}", quotient, remainder);

			// Right-trim excess zeros before the decimal point:
			int nTrim = 0;
			for(int i = str.Length - 1 ; (str[i] == '0' && isdigit(str[i - 2])) ; --i)
				++nTrim;
			if(nTrim != 0)
				str = str.Remove(str.Length - nTrim, nTrim);

			if(n < 0)
				str = "-" + str;
			else if(fPlus && n > 0)
				str = "+" + str;
			return str;
		}

		private static bool isdigit(char c)
		{
			return c == '0' || c == '1' || c == '2' || c == '3' || c == '4' || c == '5' || c == '6' || c == '7' || c == '8' || c == '9';
		}

		public static bool ParseMoney(string money, out long nRet)
		{
			nRet = 0;
			if(money.Length == 0)
				return false;

			string strWhole = "";
			long nUnits = 0;


			int i = 0;
			if(i >= money.Length)
				return false;
			while(isspace(money[i]))
			{
				if(i >= money.Length)
					return false;
				i++;
			}
			for( ; i < money.Length ; i++)
			{
				if(money[i] == '.')
				{
					i++;
					if(i >= money.Length)
						break;
					long nMult = CENT * 10;
					while(isdigit(money[i]) && (nMult > 0))
					{
						nUnits += nMult * (money[i] - '0');
						i++;
						if(i >= money.Length)
							break;
						nMult /= 10;
					}
					break;
				}
				if(isspace(money[i]))
					break;
				if(!isdigit(money[i]))
					return false;
				strWhole += money[i];
			}
			for( ; i < money.Length ; i++)
				if(!isspace(money[i]))
					return false;
			if(strWhole.Length > 10) // guard against 63 bit overflow
				return false;
			if(nUnits < 0 || nUnits > COIN)
				return false;
			long nWhole = long.Parse(strWhole);
			long nValue = nWhole * COIN + nUnits;

			nRet = nValue;
			return true;
		}

		public static bool IsHex(string str)
		{
			foreach(var c in str)
			{
				if(HexDigit(c) < 0)
					return false;
			}
			return (str.Length > 0) && (str.Length % 2 == 0);
		}


		const string pszBase58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
		public static string EncodeBase58(byte[] data)
		{
			BigInteger bn58 = 58;
			BigInteger bn0 = 0;

			// Convert big endian data to little endian
			// Extra zero at the end make sure bignum will interpret as a positive number
			byte[] vchTmp = data.Reverse().Concat(new byte[] { 0x00 }).ToArray();

			// Convert little endian data to bignum
			BigInteger bn = new BigInteger(vchTmp);

			// Convert bignum to std::string
			String str = "";
			// Expected size increase from base58 conversion is approximately 137%
			// use 138% to be safe

			BigInteger dv = BigInteger.Zero;
			BigInteger rem = BigInteger.Zero;
			while(bn > bn0)
			{
				dv = BigInteger.DivRem(bn, bn58, out rem);
				bn = dv;
				var c = (int)rem;
				str += pszBase58[c];
			}

			// Leading zeroes encoded as base58 zeros
			for(int i = 0 ; i < data.Length && data[i] == 0 ; i++)
				str += pszBase58[0];

			// Convert little endian std::string to big endian
			str = new String(str.Reverse().ToArray());
			return str;
		}

		public static bool DecodeBase58(string base58string, out byte[] result)
		{
			result = new byte[0];
			if(base58string.Length == 0)
				return true;
			BigInteger bn58 = 58;
			BigInteger bn = 0;
			BigInteger bnChar;
			int i = 0;
			while(isspace(base58string[i]))
			{
				i++;
				if(i >= base58string.Length)
					return true;
			}

			for(int y = i ; y < base58string.Length ; y++)
			{
				var p1 = pszBase58.IndexOf(base58string[y]);
				if(p1 == -1)
				{
					while(isspace(base58string[y]))
					{
						y++;
						if(y >= base58string.Length)
							break;
					}
					if(y != base58string.Length)
						return false;
					break;
				}
				bnChar = new BigInteger(p1);
				bn = BigInteger.Multiply(bn, bn58);
				bn += bnChar;
			}

			// Get bignum as little endian data
			var vchTmp = bn.ToByteArray();
			if(vchTmp.All(b => b == 0))
				vchTmp = new byte[0];

			// Trim off sign byte if present
			if(vchTmp.Length >= 2 && vchTmp[vchTmp.Length - 1] == 0 && vchTmp[vchTmp.Length - 2] >= 0x80)
				vchTmp = vchTmp.Take(vchTmp.Length - 1).ToArray();

			// Restore leading zeros
			int nLeadingZeros = 0;
			for(int y = i ; y < base58string.Length && base58string[y] == pszBase58[0] ; y++)
				nLeadingZeros++;


			result = new byte[nLeadingZeros + vchTmp.Length];
			Array.Copy(vchTmp.Reverse().ToArray(), 0, result, nLeadingZeros, vchTmp.Length);
			return true;

		}





		public static bool DecodeBase58Check(string psz, out byte[] vchRet)
		{
			vchRet = new byte[0];
			if(!DecodeBase58(psz, out vchRet))
				return false;
			if(vchRet.Length < 4)
			{
				Array.Clear(vchRet, 0, vchRet.Length);
				return false;
			}
			var vchRet2 = vchRet;
			var calculatedHash = Utils.Hash(vchRet, vchRet.Length - 4).ToBytes().Take(4).ToArray();
			var expectedHash = vchRet.Skip(vchRet.Length - 4).Take(4).ToArray();

			if(!Utils.ArrayEqual(calculatedHash,expectedHash))
			{
				Array.Clear(vchRet, 0, vchRet.Length);
				return false;
			}
			vchRet = vchRet.Take(vchRet.Length - 4).ToArray();
			return true;
		}

		private static bool ArrayEqual(byte[] a, byte[] b)
		{
			if(a.Length != b.Length)
				return false;
			for(int i = 0 ; i < a.Length ; i++)
			{
				if(a[i] != b[i])
					return false;
			}
			return true;
		}

		public static uint256 Hash(byte[] data, int count)
		{
			byte[] pblank = new byte[1];
			uint256 hash1;
			SHA256((count == 0 ? pblank : data), count, out hash1);
			uint256 hash2;
			SHA256(hash1.ToBytes(), hash1.Size, out hash2);
			return hash2;
		}

		private static void SHA256(byte[] data, int count, out uint256 result)
		{
			using(var sha1 = System.Security.Cryptography.SHA256.Create())
			{
				result = new uint256(sha1.ComputeHash(data, 0, count));
			}
		}

		
	}
}
