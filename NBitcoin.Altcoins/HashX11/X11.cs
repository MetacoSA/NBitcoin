using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Altcoins.HashX11
{
    public class X11
    {
		private IHash[] hashes;

		public X11()
		{
			var blake512 = HashFactory.Crypto.SHA3.CreateBlake512();
			var bmw512 = HashFactory.Crypto.SHA3.CreateBlueMidnightWish512();
			var groestl512 = HashFactory.Crypto.SHA3.CreateGroestl512();
			var skein512 = HashFactory.Crypto.SHA3.CreateSkein512_Custom();
			var jh512 = HashFactory.Crypto.SHA3.CreateJH512();
			var keccak512 = HashFactory.Crypto.SHA3.CreateKeccak512();
			var luffa512 = HashFactory.Crypto.SHA3.CreateLuffa512();
			var cubehash512 = HashFactory.Crypto.SHA3.CreateCubeHash512();
			var shavite512 = HashFactory.Crypto.SHA3.CreateSHAvite3_512_Custom();
			var simd512 = HashFactory.Crypto.SHA3.CreateSIMD512();
			var echo512 = HashFactory.Crypto.SHA3.CreateEcho512();
			hashes = new IHash[] 
			{
				blake512, bmw512, groestl512, skein512, jh512, keccak512,
				luffa512, cubehash512, shavite512, simd512, echo512
			};
		}

		public byte[] ComputeBytes(byte[] input)
		{
			byte[] hashResult = null;
			for (int i = 0; i < hashes.Length; i++)
			{
				if(hashResult == null)
				{
					hashResult = input;
				}
				hashResult = hashes[i].ComputeBytes(hashResult).GetBytes();
			}

			return hashResult.Take(32).ToArray();
		}
    }
}
