using Org.BouncyCastle.Crypto.Digests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Crypto
{
	public class Hashes
	{
		public static uint256 Hash256(byte[] data, int count)
		{
			data = count == 0 ? new byte[1] : data;
			return new uint256(SHA256(SHA256(data, count)));
		}


		public static uint256 Hash256(byte[] data)
		{
			return Hash256(data, data.Length);
		}

		public static uint160 Hash160(byte[] data, int count)
		{
			data = count == 0 ? new byte[1] : data;
			return new uint160(RIPEMD160(SHA256(data, count)));
		}

		private static byte[] RIPEMD160(byte[] data)
		{
			return RIPEMD160(data, data.Length);
		}
		public static byte[] SHA1(byte[] data, int count)
		{
			var sha1 = new Sha1Digest();
			sha1.BlockUpdate(data, 0, count);
			byte[] rv = new byte[32];
			sha1.DoFinal(rv, 0);
			return rv;
		}

		public static byte[] SHA256(byte[] data)
		{
			return SHA256(data, data.Length);
		}
		public static byte[] SHA256(byte[] data, int count)
		{
			Sha256Digest sha256 = new Sha256Digest();
			sha256.BlockUpdate(data, 0, count);
			byte[] rv = new byte[32];
			sha256.DoFinal(rv, 0);
			return rv;
		}



		public static byte[] RIPEMD160(byte[] data, int count)
		{
			RipeMD160Digest ripemd = new RipeMD160Digest();
			ripemd.BlockUpdate(data, 0, count);
			byte[] rv = new byte[32];
			ripemd.DoFinal(rv, 0);
			return rv;
		}
	}
}
