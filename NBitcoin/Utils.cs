using NBitcoin.DataEncoders;
using Org.BouncyCastle.Crypto.Digests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class Utils
	{
		public const int PROTOCOL_VERSION = 70002;
		public const long COIN = 100000000;
		public const long CENT = 1000000;

		
		

		

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
			while(DataEncoder.IsSpace(money[i]))
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
				if(DataEncoder.IsSpace(money[i]))
					break;
				if(!isdigit(money[i]))
					return false;
				strWhole += money[i];
			}
			for( ; i < money.Length ; i++)
				if(!DataEncoder.IsSpace(money[i]))
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

		
		public static bool ArrayEqual(byte[] a, byte[] b)
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
			if(count == 0)
				data = pblank;
			var h1 = SHA256(data, count);
			var h2 = SHA256(h1);
			return new uint256(h2);
		}

		private static byte[] SHA256(byte[] data)
		{
			return SHA256(data, data.Length);
		}
		private static byte[] SHA256(byte[] data, int count)
		{
			Sha256Digest sha256 = new Sha256Digest();
			sha256.BlockUpdate(data, 0, count);
			byte[] rv = new byte[32];
			sha256.DoFinal(rv, 0);
			return rv;
		}



		internal static uint160 Hash160(byte[] data, int count)
		{
			data = count == 0 ? new byte[1] : data;
			using(var h160 = System.Security.Cryptography.RIPEMD160.Create())
			{
				var h1 = SHA256(data, count);
				var h2 = h160.ComputeHash(h1);
				return new uint160(h2);
			}
		}

		private static void RIPEMD160(byte[] data, int count, out uint160 result)
		{
			using(var sha1 = System.Security.Cryptography.RIPEMD160.Create())
			{
				result = new uint160(sha1.ComputeHash(data, 0, count));
			}
		}

		public static uint256 Hash(byte[] data)
		{
			return Hash(data, data.Length);
		}


		public static String BITCOIN_SIGNED_MESSAGE_HEADER = "Bitcoin Signed Message:\n";
		public static byte[] BITCOIN_SIGNED_MESSAGE_HEADER_BYTES = Encoding.UTF8.GetBytes(BITCOIN_SIGNED_MESSAGE_HEADER);

		//http://bitcoinj.googlecode.com/git-history/keychain/core/src/main/java/com/google/bitcoin/core/Utils.java
		public static byte[] FormatMessageForSigning(string messageText)
		{
			MemoryStream ms = new MemoryStream();
			var message = Encoding.UTF8.GetBytes(messageText);

			ms.WriteByte((byte)BITCOIN_SIGNED_MESSAGE_HEADER_BYTES.Length);
			Write(ms, BITCOIN_SIGNED_MESSAGE_HEADER_BYTES);

			VarInt size = new VarInt(message.Length);
			Write(ms, size.ToBytes());
			Write(ms, message);
			return ms.ToArray();
		}


		private static void Write(MemoryStream ms, byte[] bytes)
		{
			ms.Write(bytes, 0, bytes.Length);
		}

		internal static Array BigIntegerToBytes(Org.BouncyCastle.Math.BigInteger b, int numBytes)
		{
			if(b == null)
			{
				return null;
			}
			byte[] bytes = new byte[numBytes];
			byte[] biBytes = b.ToByteArray();
			int start = (biBytes.Length == numBytes + 1) ? 1 : 0;
			int length = Math.Min(biBytes.Length, numBytes);
			Array.Copy(biBytes, start, bytes, numBytes - length, length);
			return bytes;

		}
	}
}
