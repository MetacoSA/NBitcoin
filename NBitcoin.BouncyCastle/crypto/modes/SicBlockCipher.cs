using System;

using NBitcoin.BouncyCastle.Crypto.Parameters;
using NBitcoin.BouncyCastle.Math;

using NBitcoin.BouncyCastle.Utilities;

namespace NBitcoin.BouncyCastle.Crypto.Modes
{
    /**
    * Implements the Segmented Integer Counter (SIC) mode on top of a simple
    * block cipher.
    */
    public class SicBlockCipher
        : IBlockCipher
    {
        private readonly IBlockCipher cipher;
        private readonly int blockSize;
        private readonly byte[] counter;
        private readonly byte[] counterOut;
        private byte[] IV;

        /**
        * Basic constructor.
        *
        * @param c the block cipher to be used.
        */
        public SicBlockCipher(IBlockCipher cipher)
        {
            this.cipher = cipher;
            this.blockSize = cipher.GetBlockSize();
            this.counter = new byte[blockSize];
            this.counterOut = new byte[blockSize];
            this.IV = new byte[blockSize];
        }

        /**
        * return the underlying block cipher that we are wrapping.
        *
        * @return the underlying block cipher that we are wrapping.
        */
        public virtual IBlockCipher GetUnderlyingCipher()
        {
            return cipher;
        }

        public virtual void Init(
            bool				forEncryption, //ignored by this CTR mode
            ICipherParameters	parameters)
        {
            ParametersWithIV ivParam = parameters as ParametersWithIV;
            if (ivParam == null)
                throw new ArgumentException("CTR/SIC mode requires ParametersWithIV", "parameters");

            this.IV = Arrays.Clone(ivParam.GetIV());

            if (blockSize < IV.Length)
                throw new ArgumentException("CTR/SIC mode requires IV no greater than: " + blockSize + " bytes.");

            int maxCounterSize = System.Math.Min(8, blockSize / 2);
            if (blockSize - IV.Length > maxCounterSize)
                throw new ArgumentException("CTR/SIC mode requires IV of at least: " + (blockSize - maxCounterSize) + " bytes.");

            // if null it's an IV changed only.
            if (ivParam.Parameters != null)
            {
                cipher.Init(true, ivParam.Parameters);
            }

            Reset();
        }

        public virtual string AlgorithmName
        {
            get { return cipher.AlgorithmName + "/SIC"; }
        }

        public virtual bool IsPartialBlockOkay
        {
            get { return true; }
        }

        public virtual int GetBlockSize()
        {
            return cipher.GetBlockSize();
        }

        public virtual int ProcessBlock(
            byte[]	input,
            int		inOff,
            byte[]	output,
            int		outOff)
        {
            cipher.ProcessBlock(counter, 0, counterOut, 0);

            //
            // XOR the counterOut with the plaintext producing the cipher text
            //
            for (int i = 0; i < counterOut.Length; i++)
            {
                output[outOff + i] = (byte)(counterOut[i] ^ input[inOff + i]);
            }

            // Increment the counter
            int j = counter.Length;
            while (--j >= 0 && ++counter[j] == 0)
            {
            }

            return counter.Length;
        }

        public virtual void Reset()
        {
            Arrays.Fill(counter, (byte)0);
            Array.Copy(IV, 0, counter, 0, IV.Length);
            cipher.Reset();
        }
    }
}
