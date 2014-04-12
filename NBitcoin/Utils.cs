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
		public static bool ArrayEqual(byte[] a, byte[] b)
		{
			return ArrayEqual(a, 0, b, 0, Math.Max(a.Length,b.Length));
		}
		public static bool ArrayEqual(byte[] a, int startA, byte[] b, int startB, int length)
		{
			var alen = a.Length - startA;
			var blen = b.Length - startB;

			if(alen < length || blen < length)
				return false;

			for(int ai = startA, bi = startB ; ai < startA + length ; ai++, bi++)
			{
				if(a[ai] != b[bi])
					return false;
			}
			return true;
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

			VarInt size = new VarInt((ulong)message.Length);
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




		//https://en.bitcoin.it/wiki/Script
		public static byte[] BigIntegerToBytes(BigInteger num)
		{
			if(num == 0)
				//Positive 0 is represented by a null-length vector
				return new byte[0];

			bool isPositive = true;
			if(num < 0)
			{
				isPositive = false;
				num *= -1;
			}
			var array = num.ToByteArray();
			if(!isPositive)
				array[array.Length - 1] |= 0x80;
			return array;
		}

		public static BigInteger BytesToBigInteger(byte[] data)
		{
			if(data == null)
				throw new ArgumentNullException("data");
			if(data.Length == 0)
				return BigInteger.Zero;
			data = data.ToArray();
			var positive = (data[data.Length - 1] & 0x80) == 0;
			if(!positive)
			{
				data[data.Length - 1] &= unchecked((byte)~0x80);
				return -new BigInteger(data);
			}
			return new BigInteger(data);
		}
	}
}
