using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Altcoins.HashX11
{
    public class X13
    {
		private IHash[] _hashers;

		public X13()
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
            		var hamsi512 = HashFactory.Crypto.SHA3.CreateHamsi512();
           		var fugue512 = HashFactory.Crypto.SHA3.CreateFugue512();
			_hashers = new IHash[] 
			{
				blake512, bmw512, groestl512, skein512, jh512, keccak512,
				luffa512, cubehash512, shavite512, simd512, echo512, hamsi512,
				fugue512
			};
		}

		public byte[] ComputeBytes(byte[] input)
		{
			byte[] hashResult = input;
			for (int i = 0; i < _hashers.Length; i++)
			{
				hashResult = _hashers[i].ComputeBytes(hashResult).GetBytes();
			}

			return hashResult.Take(32).ToArray();
		}
    }
}
