using System;

using NBitcoin.BouncyCastle.Crypto.Digests;
using NBitcoin.BouncyCastle.Crypto.Parameters;
using NBitcoin.BouncyCastle.Security;

namespace NBitcoin.BouncyCastle.Crypto.Encodings
{
    /**
    * Optimal Asymmetric Encryption Padding (OAEP) - see PKCS 1 V 2.
    */
    public class OaepEncoding
        : IAsymmetricBlockCipher
    {
        private byte[] defHash;
        private IDigest hash;
        private IDigest mgf1Hash;

        private IAsymmetricBlockCipher engine;
        private SecureRandom random;
        private bool forEncryption;

        public OaepEncoding(
            IAsymmetricBlockCipher cipher)
            : this(cipher, new Sha1Digest(), null)
        {
        }

        public OaepEncoding(
            IAsymmetricBlockCipher	cipher,
            IDigest					hash)
            : this(cipher, hash, null)
        {
        }

        public OaepEncoding(
            IAsymmetricBlockCipher	cipher,
            IDigest					hash,
            byte[]					encodingParams)
            : this(cipher, hash, hash, encodingParams)
        {
        }

        public OaepEncoding(
            IAsymmetricBlockCipher	cipher,
            IDigest					hash,
            IDigest					mgf1Hash,
            byte[]					encodingParams)
        {
            this.engine = cipher;
            this.hash = hash;
            this.mgf1Hash = mgf1Hash;
            this.defHash = new byte[hash.GetDigestSize()];

            if (encodingParams != null)
            {
                hash.BlockUpdate(encodingParams, 0, encodingParams.Length);
            }

            hash.DoFinal(defHash, 0);
        }

        public IAsymmetricBlockCipher GetUnderlyingCipher()
        {
            return engine;
        }

        public string AlgorithmName
        {
            get { return engine.AlgorithmName + "/OAEPPadding"; }
        }

        public void Init(
            bool				forEncryption,
            ICipherParameters	param)
        {
            if (param is ParametersWithRandom)
            {
                ParametersWithRandom rParam = (ParametersWithRandom)param;
                this.random = rParam.Random;
            }
            else
            {
                this.random = new SecureRandom();
            }

            engine.Init(forEncryption, param);

            this.forEncryption = forEncryption;
        }

        public int GetInputBlockSize()
        {
            int baseBlockSize = engine.GetInputBlockSize();

            if (forEncryption)
            {
                return baseBlockSize - 1 - 2 * defHash.Length;
            }
            else
            {
                return baseBlockSize;
            }
        }

        public int GetOutputBlockSize()
        {
            int baseBlockSize = engine.GetOutputBlockSize();

            if (forEncryption)
            {
                return baseBlockSize;
            }
            else
            {
                return baseBlockSize - 1 - 2 * defHash.Length;
            }
        }

        public byte[] ProcessBlock(
            byte[]	inBytes,
            int		inOff,
            int		inLen)
        {
            if (forEncryption)
            {
                return EncodeBlock(inBytes, inOff, inLen);
            }
            else
            {
                return DecodeBlock(inBytes, inOff, inLen);
            }
        }

        private byte[] EncodeBlock(
            byte[]	inBytes,
            int		inOff,
            int		inLen)
        {
            byte[] block = new byte[GetInputBlockSize() + 1 + 2 * defHash.Length];

            //
            // copy in the message
            //
            Array.Copy(inBytes, inOff, block, block.Length - inLen, inLen);

            //
            // add sentinel
            //
            block[block.Length - inLen - 1] = 0x01;

            //
            // as the block is already zeroed - there's no need to add PS (the >= 0 pad of 0)
            //

            //
            // add the hash of the encoding params.
            //
            Array.Copy(defHash, 0, block, defHash.Length, defHash.Length);

            //
            // generate the seed.
            //
            byte[] seed = SecureRandom.GetNextBytes(random, defHash.Length);

            //
            // mask the message block.
            //
            byte[] mask = maskGeneratorFunction1(seed, 0, seed.Length, block.Length - defHash.Length);

            for (int i = defHash.Length; i != block.Length; i++)
            {
                block[i] ^= mask[i - defHash.Length];
            }

            //
            // add in the seed
            //
            Array.Copy(seed, 0, block, 0, defHash.Length);

            //
            // mask the seed.
            //
            mask = maskGeneratorFunction1(
                block, defHash.Length, block.Length - defHash.Length, defHash.Length);

            for (int i = 0; i != defHash.Length; i++)
            {
                block[i] ^= mask[i];
            }

            return engine.ProcessBlock(block, 0, block.Length);
        }

        /**
        * @exception InvalidCipherTextException if the decrypted block turns out to
        * be badly formatted.
        */
        private byte[] DecodeBlock(
            byte[]	inBytes,
            int		inOff,
            int		inLen)
        {
            byte[] data = engine.ProcessBlock(inBytes, inOff, inLen);
            byte[] block;

            //
            // as we may have zeros in our leading bytes for the block we produced
            // on encryption, we need to make sure our decrypted block comes back
            // the same size.
            //
            if (data.Length < engine.GetOutputBlockSize())
            {
                block = new byte[engine.GetOutputBlockSize()];

                Array.Copy(data, 0, block, block.Length - data.Length, data.Length);
            }
            else
            {
                block = data;
            }

            if (block.Length < (2 * defHash.Length) + 1)
            {
                throw new InvalidCipherTextException("data too short");
            }

            //
            // unmask the seed.
            //
            byte[] mask = maskGeneratorFunction1(
                block, defHash.Length, block.Length - defHash.Length, defHash.Length);

            for (int i = 0; i != defHash.Length; i++)
            {
                block[i] ^= mask[i];
            }

            //
            // unmask the message block.
            //
            mask = maskGeneratorFunction1(block, 0, defHash.Length, block.Length - defHash.Length);

            for (int i = defHash.Length; i != block.Length; i++)
            {
                block[i] ^= mask[i - defHash.Length];
            }

            //
            // check the hash of the encoding params.
            // long check to try to avoid this been a source of a timing attack.
            //
            {
                int diff = 0;
                for (int i = 0; i < defHash.Length; ++i)
                {
                    diff |= (byte)(defHash[i] ^ block[defHash.Length + i]);
                }
 
                if (diff != 0)
                    throw new InvalidCipherTextException("data hash wrong");
            }

            //
            // find the data block
            //
            int start;
            for (start = 2 * defHash.Length; start != block.Length; start++)
            {
                if (block[start] != 0)
                {
                    break;
                }
            }

            if (start > (block.Length - 1) || block[start] != 1)
            {
                throw new InvalidCipherTextException("data start wrong " + start);
            }

            start++;

            //
            // extract the data block
            //
            byte[] output = new byte[block.Length - start];

            Array.Copy(block, start, output, 0, output.Length);

            return output;
        }

        /**
        * int to octet string.
        */
        private void ItoOSP(
            int		i,
            byte[]	sp)
        {
            sp[0] = (byte)((uint)i >> 24);
            sp[1] = (byte)((uint)i >> 16);
            sp[2] = (byte)((uint)i >> 8);
            sp[3] = (byte)((uint)i >> 0);
        }

        /**
        * mask generator function, as described in PKCS1v2.
        */
        private byte[] maskGeneratorFunction1(
            byte[]	Z,
            int		zOff,
            int		zLen,
            int		length)
        {
            byte[] mask = new byte[length];
            byte[] hashBuf = new byte[mgf1Hash.GetDigestSize()];
            byte[] C = new byte[4];
            int counter = 0;

            hash.Reset();

            do
            {
                ItoOSP(counter, C);

                mgf1Hash.BlockUpdate(Z, zOff, zLen);
                mgf1Hash.BlockUpdate(C, 0, C.Length);
                mgf1Hash.DoFinal(hashBuf, 0);

                Array.Copy(hashBuf, 0, mask, counter * hashBuf.Length, hashBuf.Length);
            }
            while (++counter < (length / hashBuf.Length));

            if ((counter * hashBuf.Length) < length)
            {
                ItoOSP(counter, C);

                mgf1Hash.BlockUpdate(Z, zOff, zLen);
                mgf1Hash.BlockUpdate(C, 0, C.Length);
                mgf1Hash.DoFinal(hashBuf, 0);

                Array.Copy(hashBuf, 0, mask, counter * hashBuf.Length, mask.Length - (counter * hashBuf.Length));
            }

            return mask;
        }
    }
}

