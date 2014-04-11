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
			return ArrayEqual(a, 0, b, 0, a.Length);
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
	}
}
