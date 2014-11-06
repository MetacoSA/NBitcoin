using System;

using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Security;
using NBitcoin.BouncyCastle.Crypto.Parameters;

namespace NBitcoin.BouncyCastle.Crypto.Generators
{
    public class DHParametersGenerator
    {
        private int				size;
        private int				certainty;
        private SecureRandom	random;

        public virtual void Init(
            int				size,
            int				certainty,
            SecureRandom	random)
        {
            this.size = size;
            this.certainty = certainty;
            this.random = random;
        }

        /**
         * which Generates the p and g values from the given parameters,
         * returning the DHParameters object.
         * <p>
         * Note: can take a while...</p>
         */
        public virtual DHParameters GenerateParameters()
        {
			//
			// find a safe prime p where p = 2*q + 1, where p and q are prime.
			//
			BigInteger[] safePrimes = DHParametersHelper.GenerateSafePrimes(size, certainty, random);

			BigInteger p = safePrimes[0];
			BigInteger q = safePrimes[1];
			BigInteger g = DHParametersHelper.SelectGenerator(p, q, random);

			return new DHParameters(p, g, q, BigInteger.Two, null);
		}
	}
}
