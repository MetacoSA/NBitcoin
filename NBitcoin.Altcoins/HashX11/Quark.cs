using System.Linq;

namespace NBitcoin.Altcoins.HashX11
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


		public byte[] ComputeBytes(byte[] input)
		{

			var hash = new byte[9][];

			// ZBLAKE;

			hash[0] = blake512.ComputeBytes(input).GetBytes();
		
			// ZBMW;
			hash[1] = bmw512.ComputeBytes(hash[0]).GetBytes();

			if((hash[1][0] & 8) != 0)
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
			hash[4] = jh512.ComputeBytes(hash[3]).GetBytes();

			if((hash[4][0] & 8) != 0)
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

			if((hash[7][0] & 8) != 0)
			{
				// ZKECCAK;
				hash[8] = keccak512.ComputeBytes(hash[7]).GetBytes();
			}
			else
			{
				// ZJH;
				hash[8] = jh512.ComputeBytes(hash[7]).GetBytes();
			}
			
			return hash[8].Take(32).ToArray();
			
		}
	}
}
