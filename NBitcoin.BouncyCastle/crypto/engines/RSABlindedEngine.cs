using System;

using NBitcoin.BouncyCastle.Crypto.Parameters;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Security;
using NBitcoin.BouncyCastle.Utilities;

namespace NBitcoin.BouncyCastle.Crypto.Engines
{
    /**
     * this does your basic RSA algorithm with blinding
     */
    public class RsaBlindedEngine
        : IAsymmetricBlockCipher
    {
        private readonly RsaCoreEngine core = new RsaCoreEngine();
        private RsaKeyParameters key;
        private SecureRandom random;

        public virtual string AlgorithmName
        {
            get { return "RSA"; }
        }

        /**
         * initialise the RSA engine.
         *
         * @param forEncryption true if we are encrypting, false otherwise.
         * @param param the necessary RSA key parameters.
         */
        public virtual void Init(
            bool forEncryption,
            ICipherParameters param)
        {
            core.Init(forEncryption, param);

            if (param is ParametersWithRandom)
            {
                ParametersWithRandom rParam = (ParametersWithRandom)param;

                key = (RsaKeyParameters)rParam.Parameters;
                random = rParam.Random;
            }
            else
            {
                key = (RsaKeyParameters)param;
                random = new SecureRandom();
            }
        }

        /**
         * Return the maximum size for an input block to this engine.
         * For RSA this is always one byte less than the key size on
         * encryption, and the same length as the key size on decryption.
         *
         * @return maximum size for an input block.
         */
        public virtual int GetInputBlockSize()
        {
            return core.GetInputBlockSize();
        }

        /**
         * Return the maximum size for an output block to this engine.
         * For RSA this is always one byte less than the key size on
         * decryption, and the same length as the key size on encryption.
         *
         * @return maximum size for an output block.
         */
        public virtual int GetOutputBlockSize()
        {
            return core.GetOutputBlockSize();
        }

        /**
         * Process a single block using the basic RSA algorithm.
         *
         * @param inBuf the input array.
         * @param inOff the offset into the input buffer where the data starts.
         * @param inLen the length of the data to be processed.
         * @return the result of the RSA process.
         * @exception DataLengthException the input block is too large.
         */
        public virtual byte[] ProcessBlock(
            byte[] inBuf,
            int inOff,
            int inLen)
        {
            if (key == null)
                throw new InvalidOperationException("RSA engine not initialised");

            BigInteger input = core.ConvertInput(inBuf, inOff, inLen);

            BigInteger result;
            if (key is RsaPrivateCrtKeyParameters)
            {
                RsaPrivateCrtKeyParameters k = (RsaPrivateCrtKeyParameters)key;
                BigInteger e = k.PublicExponent;
                if (e != null)   // can't do blinding without a public exponent
                {
                    BigInteger m = k.Modulus;
                    BigInteger r = BigIntegers.CreateRandomInRange(
                        BigInteger.One, m.Subtract(BigInteger.One), random);

                    BigInteger blindedInput = r.ModPow(e, m).Multiply(input).Mod(m);
                    BigInteger blindedResult = core.ProcessBlock(blindedInput);

                    BigInteger rInv = r.ModInverse(m);
                    result = blindedResult.Multiply(rInv).Mod(m);

                    // defence against Arjen Lenstra�s CRT attack
                    if (!input.Equals(result.ModPow(e, m)))
                        throw new InvalidOperationException("RSA engine faulty decryption/signing detected");
                }
                else
                {
                    result = core.ProcessBlock(input);
                }
            }
            else
            {
                result = core.ProcessBlock(input);
            }

            return core.ConvertOutput(result);
        }
    }
}
