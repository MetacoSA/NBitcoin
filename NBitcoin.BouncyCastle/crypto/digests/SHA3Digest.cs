﻿using System;
using System.Diagnostics;

using NBitcoin.BouncyCastle.Utilities;

namespace NBitcoin.BouncyCastle.Crypto.Digests
{
    /// <summary>
    /// Implementation of SHA-3 based on following KeccakNISTInterface.c from http://keccak.noekeon.org/
    /// </summary>
    /// <remarks>
    /// Following the naming conventions used in the C source code to enable easy review of the implementation.
    /// </remarks>
    public class Sha3Digest
        : KeccakDigest
    {
        private static int CheckBitLength(int bitLength)
        {
            switch (bitLength)
            {
            case 224:
            case 256:
            case 384:
            case 512:
                return bitLength;
            default:
                throw new ArgumentException(bitLength + " not supported for SHA-3", "bitLength");
            }
        }

        public Sha3Digest()
            : this(256)
        {
        }

        public Sha3Digest(int bitLength)
            : base(CheckBitLength(bitLength))
        {
        }

        public Sha3Digest(Sha3Digest source)
            : base(source)
        {
        }

        public override string AlgorithmName
        {
            get { return "SHA3-" + fixedOutputLength; }
        }

        public override int DoFinal(byte[] output, int outOff)
        {
            Absorb(new byte[]{ 0x02 }, 0, 2);

            return base.DoFinal(output,  outOff);
        }

        /*
         * TODO Possible API change to support partial-byte suffixes.
         */
        protected override int DoFinal(byte[] output, int outOff, byte partialByte, int partialBits)
        {
            if (partialBits < 0 || partialBits > 7)
                throw new ArgumentException("must be in the range [0,7]", "partialBits");

            int finalInput = (partialByte & ((1 << partialBits) - 1)) | (0x02 << partialBits);
            Debug.Assert(finalInput >= 0);
            int finalBits = partialBits + 2;

            if (finalBits >= 8)
            {
                oneByte[0] = (byte)finalInput;
                Absorb(oneByte, 0, 8);
                finalBits -= 8;
                finalInput >>= 8;
            }

            return base.DoFinal(output, outOff, (byte)finalInput, finalBits);
        }

        public override IMemoable Copy()
		{
			return new Sha3Digest(this);
		}
    }
}
