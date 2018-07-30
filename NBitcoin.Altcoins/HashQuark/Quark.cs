using System;
using System.Linq;
using NBitcoin.Altcoins.HashX11;

namespace NBitcoin.Altcoins.HashQuark
{
	public class Quark
	{
		private readonly IHash blake512;
		private readonly IHash bmw512;
		private readonly IHash groestl512;
		private readonly IHash skein512;
		private readonly IHash jh512;
		private readonly IHash keccak512;

		public Quark()
		{
			blake512 = HashFactory.Crypto.SHA3.CreateBlake512();
			bmw512 = HashFactory.Crypto.SHA3.CreateBlueMidnightWish512();
			groestl512 = HashFactory.Crypto.SHA3.CreateGroestl512();
			skein512 = HashFactory.Crypto.SHA3.CreateSkein512_Custom();
			jh512 = HashFactory.Crypto.SHA3.CreateJH512();
			keccak512 = HashFactory.Crypto.SHA3.CreateKeccak512();
		}

		public static byte[] BitwiseAnd(byte[] ba, byte[] bt)
		{
			int longlen = Math.Max(ba.Length, bt.Length);
			int shortlen = Math.Min(ba.Length, bt.Length);
			byte[] result = new byte[longlen];
			for (int i = 0; i < shortlen; i++)
			{
				result[i] = (byte) (ba[i] & bt[i]);
			}

			return result;
		}

		public byte[] ComputeBytes(byte[] input)
		{
			var mask = new Uint512(8).ToBytes();
			var zero = new Uint512(0).ToBytes();

			var hash = new byte[9][];

			// ZBLAKE;
			hash[0] = blake512.ComputeBytes(input).GetBytes();

			// ZBMW;
			hash[1] = bmw512.ComputeBytes(hash[0]).GetBytes();

			if (!BitwiseAnd(hash[1], mask).Equals(zero))
			{
				// ZGROESTL;
				hash[2] = groestl512.ComputeBytes(hash[1]).GetBytes();
			}
			else
			{
				// ZSKEIN;
				hash[2] = skein512.ComputeBytes(hash[1]).GetBytes();
			}

			// ZGROESTL;
			hash[3] = groestl512.ComputeBytes(hash[2]).GetBytes();

			// ZJH;
			hash[4] = jh512.ComputeBytes(hash[2]).GetBytes();

			if (!BitwiseAnd(hash[4], mask).Equals(zero))
			{
				// ZBLAKE;
				hash[5] = blake512.ComputeBytes(hash[4]).GetBytes();
			}
			else
			{
				// ZBMW;
				hash[5] = bmw512.ComputeBytes(hash[4]).GetBytes();
			}

			// ZKECCAK;
			hash[6] = keccak512.ComputeBytes(hash[5]).GetBytes();

			// SKEIN;
			hash[7] = skein512.ComputeBytes(hash[6]).GetBytes();

			if (!BitwiseAnd(hash[7], mask).Equals(zero))
			{
				// ZKECCAK;
				hash[8] = keccak512.ComputeBytes(hash[4]).GetBytes();
			}
			else
			{
				// ZJH;
				hash[8] = jh512.ComputeBytes(hash[4]).GetBytes();
			}

			return hash[8].Take(32).ToArray();
		}
	}
}