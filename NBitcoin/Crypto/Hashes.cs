using NBitcoin.BouncyCastle.Crypto.Digests;
using NBitcoin.BouncyCastle.Crypto.Parameters;
using NBitcoin.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if !WINDOWS_UWP && !USEBC
using System.Security.Cryptography;
#endif

namespace NBitcoin.Crypto
{
	public static class Hashes
	{
		#region Hash256
		public static uint256 Hash256(byte[] data)
		{
			return Hash256(data, 0, data.Length);
		}

		public static uint256 Hash256(byte[] data, int count)
		{
			return Hash256(data, 0, count);
		}

		public static uint256 Hash256(byte[] data, int offset, int count)
		{
#if USEBC || WINDOWS_UWP || NETCORE
			Sha256Digest sha256 = new Sha256Digest();
			sha256.BlockUpdate(data, offset, count);
			byte[] rv = new byte[32];
			sha256.DoFinal(rv, 0);
			sha256.BlockUpdate(rv, 0, rv.Length);
			sha256.DoFinal(rv, 0);
			return new uint256(rv);
#else
			using(var sha = new SHA256Managed())
			{
				var h = sha.ComputeHash(data, offset, count);
				return new uint256(sha.ComputeHash(h, 0, h.Length));
			}
#endif
		}
		#endregion

		#region Hash160
		public static uint160 Hash160(byte[] data)
		{
			return Hash160(data, 0, data.Length);
		}

		public static uint160 Hash160(byte[] data, int count)
		{
			return Hash160(data, 0, count);
		}

		public static uint160 Hash160(byte[] data, int offset, int count)
		{
			return new uint160(RIPEMD160(SHA256(data, offset, count)));
		}
		#endregion

		#region RIPEMD160
		private static byte[] RIPEMD160(byte[] data)
		{
			return RIPEMD160(data, 0, data.Length);
		}

		public static byte[] RIPEMD160(byte[] data, int count)
		{
			return RIPEMD160(data, 0, count);
		}

		public static byte[] RIPEMD160(byte[] data, int offset, int count)
		{
#if USEBC || WINDOWS_UWP || NETCORE
			RipeMD160Digest ripemd = new RipeMD160Digest();
			ripemd.BlockUpdate(data, offset, count);
			byte[] rv = new byte[20];
			ripemd.DoFinal(rv, 0);
			return rv;
#else
			using(var ripm = new RIPEMD160Managed())
			{
				return ripm.ComputeHash(data, offset, count);
			}
#endif
		}

		#endregion

		internal class SipHasher
		{
			ulong v_0;
			ulong v_1;
			ulong v_2;
			ulong v_3;
			ulong count;
			ulong tmp;
			public SipHasher(ulong k0, ulong k1)
			{
				v_0 = 0x736f6d6570736575UL ^ k0;
				v_1 = 0x646f72616e646f6dUL ^ k1;
				v_2 = 0x6c7967656e657261UL ^ k0;
				v_3 = 0x7465646279746573UL ^ k1;
				count = 0;
				tmp = 0;
			}

			public SipHasher Write(ulong data)
			{
				ulong v0 = v_0, v1 = v_1, v2 = v_2, v3 = v_3;
				v3 ^= data;
				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				v0 ^= data;

				v_0 = v0;
				v_1 = v1;
				v_2 = v2;
				v_3 = v3;

				count += 8;
				return this;
			}

			public SipHasher Write(byte[] data)
			{
				ulong v0 = v_0, v1 = v_1, v2 = v_2, v3 = v_3;
				var size = data.Length;
				var t = tmp;
				var c = count;
				int offset = 0;

				while(size-- != 0)
				{
					t |= ((ulong)((data[offset++]))) << (int)(8 * (c % 8));
					c++;
					if((c & 7) == 0)
					{
						v3 ^= t;
						SIPROUND(ref v0, ref v1, ref v2, ref v3);
						SIPROUND(ref v0, ref v1, ref v2, ref v3);
						v0 ^= t;
						t = 0;
					}
				}

				v_0 = v0;
				v_1 = v1;
				v_2 = v2;
				v_3 = v3;
				count = c;
				tmp = t;

				return this;
			}

			public ulong Finalize()
			{
				ulong v0 = v_0, v1 = v_1, v2 = v_2, v3 = v_3;

				ulong t = tmp | (((ulong)count) << 56);

				v3 ^= t;
				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				v0 ^= t;
				v2 ^= 0xFF;
				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				return v0 ^ v1 ^ v2 ^ v3;
			}

			public static ulong SipHashUint256(ulong k0, ulong k1, uint256 val)
			{
				/* Specialized implementation for efficiency */
				ulong d = GetULong(val, 0);

				ulong v0 = 0x736f6d6570736575UL ^ k0;
				ulong v1 = 0x646f72616e646f6dUL ^ k1;
				ulong v2 = 0x6c7967656e657261UL ^ k0;
				ulong v3 = 0x7465646279746573UL ^ k1 ^ d;

				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				v0 ^= d;
				d = GetULong(val, 1);
				v3 ^= d;
				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				v0 ^= d;
				d = GetULong(val, 2);
				v3 ^= d;
				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				v0 ^= d;
				d = GetULong(val, 3);
				v3 ^= d;
				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				v0 ^= d;
				v3 ^= ((ulong)4) << 59;
				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				v0 ^= ((ulong)4) << 59;
				v2 ^= 0xFF;
				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				SIPROUND(ref v0, ref v1, ref v2, ref v3);
				return v0 ^ v1 ^ v2 ^ v3;
			}

			internal static ulong GetULong(uint256 val, int position)
			{
				switch(position)
				{
					case 0:
						return (ulong)val.pn0 + (ulong)((ulong)val.pn1 << 32);
					case 1:
						return (ulong)val.pn2 + (ulong)((ulong)val.pn3 << 32);
					case 2:
						return (ulong)val.pn4 + (ulong)((ulong)val.pn5 << 32);
					case 3:
						return (ulong)val.pn6 + (ulong)((ulong)val.pn7 << 32);
					default:
						throw new ArgumentOutOfRangeException("position should be less than 4", "position");
				}
			}

			static void SIPROUND(ref ulong v_0, ref ulong v_1, ref ulong v_2, ref ulong v_3)
			{
				v_0 += v_1;
				v_1 = rotl64(v_1, 13);
				v_1 ^= v_0;
				v_0 = rotl64(v_0, 32);
				v_2 += v_3;
				v_3 = rotl64(v_3, 16);
				v_3 ^= v_2;
				v_0 += v_3;
				v_3 = rotl64(v_3, 21);
				v_3 ^= v_0;
				v_2 += v_1;
				v_1 = rotl64(v_1, 17);
				v_1 ^= v_2;
				v_2 = rotl64(v_2, 32);
			}
		}

		public static ulong SipHash(ulong k0, ulong k1, uint256 val)
		{
			return SipHasher.SipHashUint256(k0, k1, val);
		}

		public static byte[] SHA1(byte[] data, int offset, int count)
		{
			var sha1 = new Sha1Digest();
			sha1.BlockUpdate(data, offset, count);
			byte[] rv = new byte[20];
			sha1.DoFinal(rv, 0);
			return rv;
		}

		public static byte[] SHA256(byte[] data)
		{
			return SHA256(data, 0, data.Length);
		}

		public static byte[] SHA256(byte[] data, int offset, int count)
		{
#if USEBC || WINDOWS_UWP || NETCORE
			Sha256Digest sha256 = new Sha256Digest();
			sha256.BlockUpdate(data, offset, count);
			byte[] rv = new byte[32];
			sha256.DoFinal(rv, 0);
			return rv;
#else
			using(var sha = new SHA256Managed())
			{
				return sha.ComputeHash(data, offset, count);
			}
#endif
		}


		private static uint rotl32(uint x, byte r)
		{
			return (x << r) | (x >> (32 - r));
		}
		private static ulong rotl64(ulong x, byte b)
		{
			return (((x) << (b)) | ((x) >> (64 - (b))));
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

#if USEBC || WINDOWS_UWP
		public static byte[] HMACSHA512(byte[] key, byte[] data)
		{
			var mac = new NBitcoin.BouncyCastle.Crypto.Macs.HMac(new Sha512Digest());
			mac.Init(new KeyParameter(key));
			mac.Update(data);
			byte[] result = new byte[mac.GetMacSize()];
			mac.DoFinal(result, 0);
			return result;
		}
#else
		public static byte[] HMACSHA512(byte[] key, byte[] data)
		{
			return new HMACSHA512(key).ComputeHash(data);
		}
#endif
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
