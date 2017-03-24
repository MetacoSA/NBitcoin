using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.BouncyCastle.Security
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
