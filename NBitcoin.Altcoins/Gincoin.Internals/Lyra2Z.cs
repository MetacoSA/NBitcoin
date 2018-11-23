using NBitcoin.Altcoins.HashX11.Crypto.SHA3;
using System;

namespace NBitcoin.Altcoins.GincoinInternals
{
	public class Lyra2Z
	{
		public byte[] ComputeHash(byte[] input)
		{
			// IT HOLDS INT32 - so 4 bytes * 8 = 32 bytes
			UInt64 hashSizeInBytes = 32;
			byte[] hashA;
			byte[] hashB = new byte[hashSizeInBytes];
			var blake = new Blake256();
			hashA = blake.ComputeBytes(input).GetBytes();
			Lyra2.Lyra2 lyra2 = new Lyra2.Lyra2();
			lyra2.Calculate(hashB, hashA, hashA, 8, 8, 8);
		
			return hashB;
		}
	}
}