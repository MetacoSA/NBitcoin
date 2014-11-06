using NBitcoin.BouncyCastle.Crypto.Digests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
			byte[] rv = new byte[20];
			sha1.DoFinal(rv, 0);
			return rv;
		}

		public static byte[] SHA256(byte[] data)
		{
			return SHA256(data, data.Length);
		}
		public static byte[] SHA256(byte[] data, int count)
		{
#if BOUNCY_ONLY
			Sha256Digest sha256 = new Sha256Digest();
			sha256.BlockUpdate(data, 0, count);
			byte[] rv = new byte[32];
			sha256.DoFinal(rv, 0);
			return rv;
#else
			using(var sha = System.Security.Cryptography.SHA256.Create())
			{
				return sha.ComputeHash(data, 0, count);
			}
#endif
		}



		public static byte[] RIPEMD160(byte[] data, int count)
		{
			RipeMD160Digest ripemd = new RipeMD160Digest();
			ripemd.BlockUpdate(data, 0, count);
			byte[] rv = new byte[20];
			ripemd.DoFinal(rv, 0);
			return rv;
		}

		private static uint rotl32(uint x, byte r)
		{
			return (x << r) | (x >> (32 - r));
		}

		private static uint fmix(uint h)
		{
			h ^= h >> 16;
			h *= 0x85ebca6b;
			h ^= h >> 13;
			h *= 0xc2b2ae35;
			h ^= h >> 16;
			return h;
		}
		public static uint MurmurHash3(uint nHashSeed, byte[] vDataToHash)
		{
			// The following is MurmurHash3 (x86_32), see https://gist.github.com/automatonic/3725443
			const uint c1 = 0xcc9e2d51;
			const uint c2 = 0x1b873593;

			uint h1 = nHashSeed;
			uint k1 = 0;
			uint streamLength = 0;

			using(BinaryReader reader = new BinaryReader(new MemoryStream(vDataToHash)))
			{
				byte[] chunk = reader.ReadBytes(4);
				while(chunk.Length > 0)
				{
					streamLength += (uint)chunk.Length;
					switch(chunk.Length)
					{
						case 4:
							/* Get four bytes from the input into an uint */
							k1 = (uint)
							(chunk[0]
							| chunk[1] << 8
							| chunk[2] << 16
							| chunk[3] << 24);

							/* bitmagic hash */
							k1 *= c1;
							k1 = rotl32(k1, 15);
							k1 *= c2;

							h1 ^= k1;
							h1 = rotl32(h1, 13);
							h1 = h1 * 5 + 0xe6546b64;
							break;
						case 3:
							k1 = (uint)
							(chunk[0]
							| chunk[1] << 8
							| chunk[2] << 16);
							k1 *= c1;
							k1 = rotl32(k1, 15);
							k1 *= c2;
							h1 ^= k1;
							break;
						case 2:
							k1 = (uint)
							(chunk[0]
							| chunk[1] << 8);
							k1 *= c1;
							k1 = rotl32(k1, 15);
							k1 *= c2;
							h1 ^= k1;
							break;
						case 1:
							k1 = (uint)(chunk[0]);
							k1 *= c1;
							k1 = rotl32(k1, 15);
							k1 *= c2;
							h1 ^= k1;
							break;
					}
					chunk = reader.ReadBytes(4);
				}
			}
			// finalization, magic chants to wrap it all up
			h1 ^= streamLength;
			h1 = fmix(h1);

			unchecked //ignore overflow
			{
				return h1;
			}
		}

		internal static uint160 Hash160(byte[] bytes)
		{
			return Hash160(bytes, bytes.Length);
		}

		public static byte[] HMACSHA512(byte[] key, byte[] data)
		{
			return new HMACSHA512(key).ComputeHash(data);
		}

		public static byte[] BIP32Hash(byte[] chainCode, uint nChild, byte header, byte[] data)
		{
			byte[] num = new byte[4];
			num[0] = (byte)((nChild >> 24) & 0xFF);
			num[1] = (byte)((nChild >> 16) & 0xFF);
			num[2] = (byte)((nChild >> 8) & 0xFF);
			num[3] = (byte)((nChild >> 0) & 0xFF);

			return HMACSHA512(chainCode,
				new byte[] { header }
				.Concat(data)
				.Concat(num).ToArray());
		}
	}
}
