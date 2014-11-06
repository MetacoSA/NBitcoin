using System;

using NBitcoin.BouncyCastle.Crypto;
using NBitcoin.BouncyCastle.Crypto.Parameters;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Math.EC.Multiplier;

namespace NBitcoin.BouncyCastle.Crypto.Generators
{
    /**
     * an RSA key pair generator.
     */
    public class RsaKeyPairGenerator
        : IAsymmetricCipherKeyPairGenerator
    {
        private static readonly BigInteger DefaultPublicExponent = BigInteger.ValueOf(0x10001);
        private const int DefaultTests = 12;

        private RsaKeyGenerationParameters param;

        public void Init(
            KeyGenerationParameters parameters)
        {
            if (parameters is RsaKeyGenerationParameters)
            {
                this.param = (RsaKeyGenerationParameters)parameters;
            }
            else
            {
                this.param = new RsaKeyGenerationParameters(
                    DefaultPublicExponent, parameters.Random, parameters.Strength, DefaultTests);
            }
        }

        public AsymmetricCipherKeyPair GenerateKeyPair()
        {
            BigInteger p, q, n, d, e, pSub1, qSub1, phi;

            //
            // p and q values should have a length of half the strength in bits
            //
            int strength = param.Strength;
            int qBitlength = strength >> 1;
            int pBitlength = strength - qBitlength;
            int mindiffbits = strength / 3;
            int minWeight = strength >> 2;

            e = param.PublicExponent;

            // TODO Consider generating safe primes for p, q (see DHParametersHelper.GenerateSafePrimes)
            // (then p-1 and q-1 will not consist of only small factors - see "Pollard's algorithm")

            p = ChooseRandomPrime(pBitlength, e);

            //
            // Generate a modulus of the required length
            //
            for (;;)
            {
                q = ChooseRandomPrime(qBitlength, e);

                // p and q should not be too close together (or equal!)
                BigInteger diff = q.Subtract(p).Abs();
                if (diff.BitLength < mindiffbits)
                    continue;

                //
                // calculate the modulus
                //
                n = p.Multiply(q);

                if (n.BitLength != strength)
                {
                    //
                    // if we get here our primes aren't big enough, make the largest
                    // of the two p and try again
                    //
                    p = p.Max(q);
                    continue;
                }

                /*
                 * Require a minimum weight of the NAF representation, since low-weight composites may
                 * be weak against a version of the number-field-sieve for factoring.
                 * 
                 * See "The number field sieve for integers of low weight", Oliver Schirokauer.
                 */
                if (WNafUtilities.GetNafWeight(n) < minWeight)
                {
                    p = ChooseRandomPrime(pBitlength, e);
                    continue;
                }

                break;
            }

            if (p.CompareTo(q) < 0)
            {
                phi = p;
                p = q;
                q = phi;
            }

            pSub1 = p.Subtract(BigInteger.One);
            qSub1 = q.Subtract(BigInteger.One);
            phi = pSub1.Multiply(qSub1);

            //
            // calculate the private exponent
            //
            d = e.ModInverse(phi);

            //
            // calculate the CRT factors
            //
            BigInteger dP, dQ, qInv;

            dP = d.Remainder(pSub1);
            dQ = d.Remainder(qSub1);
            qInv = q.ModInverse(p);

            return new AsymmetricCipherKeyPair(
                new RsaKeyParameters(false, n, e),
                new RsaPrivateCrtKeyParameters(n, e, d, p, q, dP, dQ, qInv));
        }

        /// <summary>Choose a random prime value for use with RSA</summary>
        /// <param name="bitlength">the bit-length of the returned prime</param>
        /// <param name="e">the RSA public exponent</param>
        /// <returns>a prime p, with (p-1) relatively prime to e</returns>
        protected virtual BigInteger ChooseRandomPrime(int bitlength, BigInteger e)
        {
            for (;;)
            {
                BigInteger p = new BigInteger(bitlength, 1, param.Random);

                if (p.Mod(e).Equals(BigInteger.One))
                    continue;

                if (!p.IsProbablePrime(param.Certainty))
                    continue;

                if (!e.Gcd(p.Subtract(BigInteger.One)).Equals(BigInteger.One))
                    continue;

                return p;
            }
        }
    }
}
