using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class RandomNumberGeneratorRandom : IRandom
	{
		readonly RandomNumberGenerator _Instance;
		public RandomNumberGeneratorRandom()
		{
			_Instance = RandomNumberGenerator.Create();
		}
		#region IRandom Members

		public void GetBytes(byte[] output)
		{
			_Instance.GetBytes(output);
		}

		#endregion
	}

	public partial class RandomUtils
	{
		static RandomUtils()
		{
			//Thread safe http://msdn.microsoft.com/en-us/library/system.security.cryptography.rngcryptoserviceprovider(v=vs.110).aspx
			Random = new RandomNumberGeneratorRandom();
			AddEntropy(Guid.NewGuid().ToByteArray());
		}
	}
}
