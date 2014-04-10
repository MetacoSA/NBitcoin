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
		public static uint256 Hash(byte[] data, int count)
		{
			byte[] pblank = new byte[1];
			if(count == 0)
				data = pblank;
			var h1 = SHA256(data, count);
			var h2 = SHA256(h1);
			return new uint256(h2);
		}
		public static uint256 Hash256(byte[] data)
		{
			return Hash(data, data.Length);
		}

		public static uint160 Hash160(byte[] data, int count)
		{
			data = count == 0 ? new byte[1] : data;
			using(var h160 = System.Security.Cryptography.RIPEMD160.Create())
			{
				var h1 = SHA256(data, count);
				var h2 = h160.ComputeHash(h1);
				return new uint160(h2);
			}
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
	}
}
