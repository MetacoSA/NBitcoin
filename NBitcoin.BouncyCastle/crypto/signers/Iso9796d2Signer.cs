using System;
using System.Collections;

using NBitcoin.BouncyCastle.Crypto;
using NBitcoin.BouncyCastle.Crypto.Digests;
using NBitcoin.BouncyCastle.Crypto.Parameters;
using NBitcoin.BouncyCastle.Utilities;

namespace NBitcoin.BouncyCastle.Crypto.Signers
{
    /// <summary> ISO9796-2 - mechanism using a hash function with recovery (scheme 1)</summary>
    public class Iso9796d2Signer : ISignerWithRecovery
    {
        /// <summary>
        /// Return a reference to the recoveredMessage message.
        /// </summary>
        /// <returns>The full/partial recoveredMessage message.</returns>
        /// <seealso cref="ISignerWithRecovery.GetRecoveredMessage"/>
        public byte[] GetRecoveredMessage()
        {
            return recoveredMessage;
        }

        [Obsolete("Use 'IsoTrailers' instead")]
        public const int TrailerImplicit = 0xBC;
        [Obsolete("Use 'IsoTrailers' instead")]
        public const int TrailerRipeMD160 = 0x31CC;
        [Obsolete("Use 'IsoTrailers' instead")]
        public const int TrailerRipeMD128 = 0x32CC;
        [Obsolete("Use 'IsoTrailers' instead")]
        public const int TrailerSha1 = 0x33CC;
        [Obsolete("Use 'IsoTrailers' instead")]
        public const int TrailerSha256 = 0x34CC;
        [Obsolete("Use 'IsoTrailers' instead")]
        public const int TrailerSha512 = 0x35CC;
        [Obsolete("Use 'IsoTrailers' instead")]
        public const int TrailerSha384 = 0x36CC;
        [Obsolete("Use 'IsoTrailers' instead")]
        public const int TrailerWhirlpool = 0x37CC;

        private IDigest digest;
        private IAsymmetricBlockCipher cipher;

        private int trailer;
        private int keyBits;
        private byte[] block;
        private byte[] mBuf;
        private int messageLength;
        private bool fullMessage;
        private byte[] recoveredMessage;

        private byte[] preSig;
        private byte[] preBlock;

        /// <summary>
        /// Generate a signer with either implicit or explicit trailers for ISO9796-2.
        /// </summary>
        /// <param name="cipher">base cipher to use for signature creation/verification</param>
        /// <param name="digest">digest to use.</param>
        /// <param name="isImplicit">whether or not the trailer is implicit or gives the hash.</param>
        public Iso9796d2Signer(
            IAsymmetricBlockCipher	cipher,
            IDigest					digest,
            bool					isImplicit)
        {
            this.cipher = cipher;
            this.digest = digest;

            if (isImplicit)
            {
                trailer = IsoTrailers.TRAILER_IMPLICIT;
            }
            else if (IsoTrailers.NoTrailerAvailable(digest))
            {
                throw new ArgumentException("no valid trailer", "digest");
            }
            else
            {
                trailer = IsoTrailers.GetTrailer(digest);
            }
        }

        /// <summary> Constructor for a signer with an explicit digest trailer.
        ///
        /// </summary>
        /// <param name="cipher">cipher to use.
        /// </param>
        /// <param name="digest">digest to sign with.
        /// </param>
        public Iso9796d2Signer(IAsymmetricBlockCipher cipher, IDigest digest)
            : this(cipher, digest, false)
        {
        }

        public virtual string AlgorithmName
        {
            get { return digest.AlgorithmName + "with" + "ISO9796-2S1"; }
        }

        public virtual void Init(bool forSigning, ICipherParameters parameters)
        {
            RsaKeyParameters kParam = (RsaKeyParameters) parameters;

            cipher.Init(forSigning, kParam);

            keyBits = kParam.Modulus.BitLength;

            block = new byte[(keyBits + 7) / 8];
            if (trailer == IsoTrailers.TRAILER_IMPLICIT)
            {
                mBuf = new byte[block.Length - digest.GetDigestSize() - 2];
            }
            else
            {
                mBuf = new byte[block.Length - digest.GetDigestSize() - 3];
            }

            Reset();
        }

        /// <summary> compare two byte arrays - constant time.</summary>
        private bool IsSameAs(byte[] a, byte[] b)
        {
            int checkLen;
            if (messageLength > mBuf.Length)
            {
                if (mBuf.Length > b.Length)
                {
                    return false;
                }

                checkLen = mBuf.Length;
            }
            else
            {
                if (messageLength != b.Length)
                {
                    return false;
                }

                checkLen = b.Length;
            }

            bool isOkay = true;

            for (int i = 0; i != checkLen; i++)
            {
                if (a[i] != b[i])
                {
                    isOkay = false;
                }
            }

            return isOkay;
        }

        /// <summary> clear possible sensitive data</summary>
        private void  ClearBlock(
            byte[] block)
        {
            Array.Clear(block, 0, block.Length);
        }

        public virtual void UpdateWithRecoveredMessage(
            byte[] signature)
        {
            byte[] block = cipher.ProcessBlock(signature, 0, signature.Length);

            if (((block[0] & 0xC0) ^ 0x40) != 0)
                throw new InvalidCipherTextException("malformed signature");

            if (((block[block.Length - 1] & 0xF) ^ 0xC) != 0)
                throw new InvalidCipherTextException("malformed signature");

            int delta = 0;

            if (((block[block.Length - 1] & 0xFF) ^ 0xBC) == 0)
            {
                delta = 1;
            }
            else
            {
                int sigTrail = ((block[block.Length - 2] & 0xFF) << 8) | (block[block.Length - 1] & 0xFF);

                if (IsoTrailers.NoTrailerAvailable(digest))
                    throw new ArgumentException("unrecognised hash in signature");

                if (sigTrail != IsoTrailers.GetTrailer(digest))
                    throw new InvalidOperationException("signer initialised with wrong digest for trailer " + sigTrail);

                delta = 2;
            }

            //
            // find out how much padding we've got
            //
            int mStart = 0;

            for (mStart = 0; mStart != block.Length; mStart++)
            {
                if (((block[mStart] & 0x0f) ^ 0x0a) == 0)
                    break;
            }

            mStart++;

            int off = block.Length - delta - digest.GetDigestSize();

            //
            // there must be at least one byte of message string
            //
            if ((off - mStart) <= 0)
                throw new InvalidCipherTextException("malformed block");

            //
            // if we contain the whole message as well, check the hash of that.
            //
            if ((block[0] & 0x20) == 0)
            {
                fullMessage = true;

                recoveredMessage = new byte[off - mStart];
                Array.Copy(block, mStart, recoveredMessage, 0, recoveredMessage.Length);
            }
            else
            {
                fullMessage = false;

                recoveredMessage = new byte[off - mStart];
                Array.Copy(block, mStart, recoveredMessage, 0, recoveredMessage.Length);
            }

            preSig = signature;
            preBlock = block;

            digest.BlockUpdate(recoveredMessage, 0, recoveredMessage.Length);
            messageLength = recoveredMessage.Length;
            recoveredMessage.CopyTo(mBuf, 0);
        }

        /// <summary> update the internal digest with the byte b</summary>
        public virtual void Update(
            byte input)
        {
            digest.Update(input);

            if (messageLength < mBuf.Length)
            {
                mBuf[messageLength] = input;
            }

            messageLength++;
        }

        /// <summary> update the internal digest with the byte array in</summary>
        public virtual void BlockUpdate(
            byte[]	input,
            int		inOff,
            int		length)
        {
            while (length > 0 && messageLength < mBuf.Length)
            {
                //for (int i = 0; i < length && (i + messageLength) < mBuf.Length; i++)
                //{
                //    mBuf[messageLength + i] = input[inOff + i];
                //}
                this.Update(input[inOff]);
                inOff++;
                length--;
            }

            digest.BlockUpdate(input, inOff, length);
            messageLength += length;
        }

        /// <summary> reset the internal state</summary>
        public virtual void Reset()
        {
            digest.Reset();
            messageLength = 0;
            ClearBlock(mBuf);

            if (recoveredMessage != null)
            {
                ClearBlock(recoveredMessage);
            }

            recoveredMessage = null;
            fullMessage = false;

            if (preSig != null)
            {
                preSig = null;
                ClearBlock(preBlock);
                preBlock = null;
            }
        }

        /// <summary> Generate a signature for the loaded message using the key we were
        /// initialised with.
        /// </summary>
        public virtual byte[] GenerateSignature()
        {
            int digSize = digest.GetDigestSize();

            int t = 0;
            int delta = 0;

            if (trailer == IsoTrailers.TRAILER_IMPLICIT)
            {
                t = 8;
                delta = block.Length - digSize - 1;
                digest.DoFinal(block, delta);
                block[block.Length - 1] = (byte)IsoTrailers.TRAILER_IMPLICIT;
            }
            else
            {
                t = 16;
                delta = block.Length - digSize - 2;
                digest.DoFinal(block, delta);
                block[block.Length - 2] = (byte) ((uint)trailer >> 8);
                block[block.Length - 1] = (byte) trailer;
            }

            byte header = 0;
            int x = (digSize + messageLength) * 8 + t + 4 - keyBits;

            if (x > 0)
            {
                int mR = messageLength - ((x + 7) / 8);
                header = (byte) (0x60);

                delta -= mR;

                Array.Copy(mBuf, 0, block, delta, mR);
            }
            else
            {
                header = (byte) (0x40);
                delta -= messageLength;

                Array.Copy(mBuf, 0, block, delta, messageLength);
            }

            if ((delta - 1) > 0)
            {
                for (int i = delta - 1; i != 0; i--)
                {
                    block[i] = (byte) 0xbb;
                }
                block[delta - 1] ^= (byte) 0x01;
                block[0] = (byte) 0x0b;
                block[0] |= header;
            }
            else
            {
                block[0] = (byte) 0x0a;
                block[0] |= header;
            }

            byte[] b = cipher.ProcessBlock(block, 0, block.Length);

            ClearBlock(mBuf);
            ClearBlock(block);

            return b;
        }

        /// <summary> return true if the signature represents a ISO9796-2 signature
        /// for the passed in message.
        /// </summary>
        public virtual bool VerifySignature(byte[] signature)
        {
            byte[] block;

            if (preSig == null)
            {
                try
                {
                    block = cipher.ProcessBlock(signature, 0, signature.Length);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                if (!Arrays.AreEqual(preSig, signature))
                    throw new InvalidOperationException("updateWithRecoveredMessage called on different signature");

                block = preBlock;

                preSig = null;
                preBlock = null;
            }

            if (((block[0] & 0xC0) ^ 0x40) != 0)
                return ReturnFalse(block);

            if (((block[block.Length - 1] & 0xF) ^ 0xC) != 0)
                return ReturnFalse(block);

            int delta = 0;

            if (((block[block.Length - 1] & 0xFF) ^ 0xBC) == 0)
            {
                delta = 1;
            }
            else
            {
                int sigTrail = ((block[block.Length - 2] & 0xFF) << 8) | (block[block.Length - 1] & 0xFF);

                if (IsoTrailers.NoTrailerAvailable(digest))
                    throw new ArgumentException("unrecognised hash in signature");

                if (sigTrail != IsoTrailers.GetTrailer(digest))
                    throw new InvalidOperationException("signer initialised with wrong digest for trailer " + sigTrail);

                delta = 2;
            }

            //
            // find out how much padding we've got
            //
            int mStart = 0;
            for (; mStart != block.Length; mStart++)
            {
                if (((block[mStart] & 0x0f) ^ 0x0a) == 0)
                {
                    break;
                }
            }

            mStart++;

            //
            // check the hashes
            //
            byte[] hash = new byte[digest.GetDigestSize()];

            int off = block.Length - delta - hash.Length;

            //
            // there must be at least one byte of message string
            //
            if ((off - mStart) <= 0)
            {
                return ReturnFalse(block);
            }

            //
            // if we contain the whole message as well, check the hash of that.
            //
            if ((block[0] & 0x20) == 0)
            {
                fullMessage = true;

                // check right number of bytes passed in.
                if (messageLength > off - mStart)
                {
                    return ReturnFalse(block);
                }

                digest.Reset();
                digest.BlockUpdate(block, mStart, off - mStart);
                digest.DoFinal(hash, 0);

                bool isOkay = true;
                
                for (int i = 0; i != hash.Length; i++)
                {
                    block[off + i] ^= hash[i];
                    if (block[off + i] != 0)
                    {
                        isOkay = false;
                    }
                }

                if (!isOkay)
                {
                    return ReturnFalse(block);
                }

                recoveredMessage = new byte[off - mStart];
                Array.Copy(block, mStart, recoveredMessage, 0, recoveredMessage.Length);
            }
            else
            {
                fullMessage = false;

                digest.DoFinal(hash, 0);

                bool isOkay = true;

                for (int i = 0; i != hash.Length; i++)
                {
                    block[off + i] ^= hash[i];
                    if (block[off + i] != 0)
                    {
                        isOkay = false;
                    }
                }

                if (!isOkay)
                {
                    return ReturnFalse(block);
                }

                recoveredMessage = new byte[off - mStart];
                Array.Copy(block, mStart, recoveredMessage, 0, recoveredMessage.Length);
            }

            //
            // if they've input a message check what we've recovered against
            // what was input.
            //
            if (messageLength != 0)
            {
                if (!IsSameAs(mBuf, recoveredMessage))
                {
                    return ReturnFalse(block);
                }
            }

            ClearBlock(mBuf);
            ClearBlock(block);

            return true;
        }

        private bool ReturnFalse(byte[] block)
        {
            ClearBlock(mBuf);
            ClearBlock(block);

            return false;
        }

        /// <summary>
        /// Return true if the full message was recoveredMessage.
        /// </summary>
        /// <returns> true on full message recovery, false otherwise.</returns>
        /// <seealso cref="ISignerWithRecovery.HasFullMessage"/>
        public virtual bool HasFullMessage()
        {
            return fullMessage;
        }
    }
}
