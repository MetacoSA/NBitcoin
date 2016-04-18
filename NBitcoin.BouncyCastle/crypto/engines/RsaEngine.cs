using System;

namespace NBitcoin.BouncyCastle.Crypto.Engines
{
    /**
    * this does your basic RSA algorithm.
    */
    public class RsaEngine
		: IAsymmetricBlockCipher
    {
		private RsaCoreEngine core;

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
            bool				forEncryption,
            ICipherParameters	parameters)
        {
			if (core == null)
				core = new RsaCoreEngine();

			core.Init(forEncryption, parameters);
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
            byte[]	inBuf,
            int		inOff,
            int		inLen)
        {
			if (core == null)
				throw new InvalidOperationException("RSA engine not initialised");

			return core.ConvertOutput(core.ProcessBlock(core.ConvertInput(inBuf, inOff, inLen)));
        }
    }
}
