using System;

using NBitcoin.BouncyCastle.Crypto;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Security;

namespace NBitcoin.BouncyCastle.Crypto.Parameters
{
	/**
	 * Parameters for NaccacheStern public private key generation. For details on
	 * this cipher, please see
	 *
	 * http://www.gemplus.com/smart/rd/publications/pdf/NS98pkcs.pdf
	 */
	public class NaccacheSternKeyGenerationParameters : KeyGenerationParameters
	{
		// private BigInteger publicExponent;
		private readonly int certainty;
		private readonly int countSmallPrimes;

        /**
		 * Parameters for generating a NaccacheStern KeyPair.
		 *
		 * @param random
		 *            The source of randomness
		 * @param strength
		 *            The desired strength of the Key in Bits
		 * @param certainty
		 *            the probability that the generated primes are not really prime
		 *            as integer: 2^(-certainty) is then the probability
		 * @param countSmallPrimes
		 *            How many small key factors are desired
		 */
		public NaccacheSternKeyGenerationParameters(
			SecureRandom	random,
			int				strength,
			int				certainty,
			int				countSmallPrimes)
            : base(random, strength)
        {
            if (countSmallPrimes % 2 == 1)
                throw new ArgumentException("countSmallPrimes must be a multiple of 2");
            if (countSmallPrimes < 30)
                throw new ArgumentException("countSmallPrimes must be >= 30 for security reasons");

            this.certainty = certainty;
            this.countSmallPrimes = countSmallPrimes;
        }

		/**
		 * Parameters for a NaccacheStern KeyPair.
		 *
		 * @param random
		 *            The source of randomness
		 * @param strength
		 *            The desired strength of the Key in Bits
		 * @param certainty
		 *            the probability that the generated primes are not really prime
		 *            as integer: 2^(-certainty) is then the probability
		 * @param cntSmallPrimes
		 *            How many small key factors are desired
		 * @param debug
         *            Ignored
		 */
        [Obsolete("Use version without 'debug' parameter")]
		public NaccacheSternKeyGenerationParameters(
            SecureRandom    random,
			int             strength,
			int             certainty,
			int             countSmallPrimes,
			bool            debug)
			: this(random, strength, certainty, countSmallPrimes)
		{
		}

		/**
		 * @return Returns the certainty.
		 */
		public int Certainty
		{
			get { return certainty; }
		}

		/**
		 * @return Returns the countSmallPrimes.
		 */
		public int CountSmallPrimes
		{
			get { return countSmallPrimes; }
		}

        [Obsolete("Remove: always false")]
        public bool IsDebug
		{
			get { return false; }
		}
	}
}
