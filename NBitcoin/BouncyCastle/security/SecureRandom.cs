using System;

namespace NBitcoin.BouncyCastle.security
{
	internal class SecureRandom : Random
	{
		public SecureRandom()
		{

		}

		internal static byte[] GetNextBytes(SecureRandom random, int p)
		{
			throw new NotImplementedException();
		}

		internal byte NextInt()
		{
			throw new NotImplementedException();
		}

		internal void NextBytes(byte[] cekBlock, int p1, int p2)
		{
			throw new NotImplementedException();
		}
	}
}
