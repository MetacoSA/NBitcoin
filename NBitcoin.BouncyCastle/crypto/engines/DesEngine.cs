using System;

using NBitcoin.BouncyCastle.Crypto.Parameters;
using NBitcoin.BouncyCastle.Crypto.Utilities;
using NBitcoin.BouncyCastle.Utilities;

namespace NBitcoin.BouncyCastle.Crypto.Engines
{
	/// <remarks>A class that provides a basic DES engine.</remarks>
    public class DesEngine
		: IBlockCipher
    {
        internal const int BLOCK_SIZE = 8;

		private int[] workingKey;

        public virtual int[] GetWorkingKey()
		{
			return workingKey;
		}

		/**
        * initialise a DES cipher.
        *
        * @param forEncryption whether or not we are for encryption.
        * @param parameters the parameters required to set up the cipher.
        * @exception ArgumentException if the parameters argument is
        * inappropriate.
        */
        public virtual void Init(
            bool				forEncryption,
            ICipherParameters	parameters)
        {
            if (!(parameters is KeyParameter))
				throw new ArgumentException("invalid parameter passed to DES init - " + Platform.GetTypeName(parameters));

			workingKey = GenerateWorkingKey(forEncryption, ((KeyParameter)parameters).GetKey());
        }

		public virtual string AlgorithmName
        {
            get { return "DES"; }
        }

        public virtual bool IsPartialBlockOkay
		{
			get { return false; }
		}

		public virtual int GetBlockSize()
        {
            return BLOCK_SIZE;
        }

        public virtual int ProcessBlock(
            byte[]	input,
            int		inOff,
            byte[]	output,
            int		outOff)
        {
            if (workingKey == null)
                throw new InvalidOperationException("DES engine not initialised");

            Check.DataLength(input, inOff, BLOCK_SIZE, "input buffer too short");
            Check.OutputLength(output, outOff, BLOCK_SIZE, "output buffer too short");

            DesFunc(workingKey, input, inOff, output, outOff);

			return BLOCK_SIZE;
        }

        public virtual void Reset()
        {
        }

        /**
        * what follows is mainly taken from "Applied Cryptography", by
        * Bruce Schneier, however it also bears great resemblance to Richard
        * Outerbridge's D3DES...
        */

//        private static readonly short[] Df_Key =
//        {
//            0x01,0x23,0x45,0x67,0x89,0xab,0xcd,0xef,
//            0xfe,0xdc,0xba,0x98,0x76,0x54,0x32,0x10,
//            0x89,0xab,0xcd,0xef,0x01,0x23,0x45,0x67
//        };

		private static readonly short[] bytebit =
        {
            128, 64, 32, 16, 8, 4, 2, 1
        };

		private static readonly int[] bigbyte =
        {
            0x800000,	0x400000,	0x200000,	0x100000,
            0x80000,	0x40000,	0x20000,	0x10000,
            0x8000,		0x4000,		0x2000,		0x1000,
            0x800,		0x400,		0x200,		0x100,
            0x80,		0x40,		0x20,		0x10,
            0x8,		0x4,		0x2,		0x1
        };

		/*
        * Use the key schedule specified in the Standard (ANSI X3.92-1981).
        */
        private static readonly byte[] pc1 =
        {
            56, 48, 40, 32, 24, 16,  8,   0, 57, 49, 41, 33, 25, 17,
            9,  1, 58, 50, 42, 34, 26,  18, 10,  2, 59, 51, 43, 35,
            62, 54, 46, 38, 30, 22, 14,   6, 61, 53, 45, 37, 29, 21,
            13,  5, 60, 52, 44, 36, 28,  20, 12,  4, 27, 19, 11,  3
        };

        private static readonly byte[] totrot =
        {
            1, 2, 4, 6, 8, 10, 12, 14,
            15, 17, 19, 21, 23, 25, 27, 28
        };

		private static readonly byte[] pc2 =
        {
            13, 16, 10, 23,  0,  4,  2, 27, 14,  5, 20,  9,
            22, 18, 11,  3, 25,  7, 15,  6, 26, 19, 12,  1,
            40, 51, 30, 36, 46, 54, 29, 39, 50, 44, 32, 47,
            43, 48, 38, 55, 33, 52, 45, 41, 49, 35, 28, 31
        };

		private static readonly uint[] SP1 =
		{
            0x01010400, 0x00000000, 0x00010000, 0x01010404,
            0x01010004, 0x00010404, 0x00000004, 0x00010000,
            0x00000400, 0x01010400, 0x01010404, 0x00000400,
            0x01000404, 0x01010004, 0x01000000, 0x00000004,
            0x00000404, 0x01000400, 0x01000400, 0x00010400,
            0x00010400, 0x01010000, 0x01010000, 0x01000404,
            0x00010004, 0x01000004, 0x01000004, 0x00010004,
            0x00000000, 0x00000404, 0x00010404, 0x01000000,
            0x00010000, 0x01010404, 0x00000004, 0x01010000,
            0x01010400, 0x01000000, 0x01000000, 0x00000400,
            0x01010004, 0x00010000, 0x00010400, 0x01000004,
            0x00000400, 0x00000004, 0x01000404, 0x00010404,
            0x01010404, 0x00010004, 0x01010000, 0x01000404,
            0x01000004, 0x00000404, 0x00010404, 0x01010400,
            0x00000404, 0x01000400, 0x01000400, 0x00000000,
            0x00010004, 0x00010400, 0x00000000, 0x01010004
        };

		private static readonly uint[] SP2 =
		{
            0x80108020, 0x80008000, 0x00008000, 0x00108020,
            0x00100000, 0x00000020, 0x80100020, 0x80008020,
            0x80000020, 0x80108020, 0x80108000, 0x80000000,
            0x80008000, 0x00100000, 0x00000020, 0x80100020,
            0x00108000, 0x00100020, 0x80008020, 0x00000000,
            0x80000000, 0x00008000, 0x00108020, 0x80100000,
            0x00100020, 0x80000020, 0x00000000, 0x00108000,
            0x00008020, 0x80108000, 0x80100000, 0x00008020,
            0x00000000, 0x00108020, 0x80100020, 0x00100000,
            0x80008020, 0x80100000, 0x80108000, 0x00008000,
            0x80100000, 0x80008000, 0x00000020, 0x80108020,
            0x00108020, 0x00000020, 0x00008000, 0x80000000,
            0x00008020, 0x80108000, 0x00100000, 0x80000020,
            0x00100020, 0x80008020, 0x80000020, 0x00100020,
            0x00108000, 0x00000000, 0x80008000, 0x00008020,
            0x80000000, 0x80100020, 0x80108020, 0x00108000
        };

		private static readonly uint[] SP3 =
		{
            0x00000208, 0x08020200, 0x00000000, 0x08020008,
            0x08000200, 0x00000000, 0x00020208, 0x08000200,
            0x00020008, 0x08000008, 0x08000008, 0x00020000,
            0x08020208, 0x00020008, 0x08020000, 0x00000208,
            0x08000000, 0x00000008, 0x08020200, 0x00000200,
            0x00020200, 0x08020000, 0x08020008, 0x00020208,
            0x08000208, 0x00020200, 0x00020000, 0x08000208,
            0x00000008, 0x08020208, 0x00000200, 0x08000000,
            0x08020200, 0x08000000, 0x00020008, 0x00000208,
            0x00020000, 0x08020200, 0x08000200, 0x00000000,
            0x00000200, 0x00020008, 0x08020208, 0x08000200,
            0x08000008, 0x00000200, 0x00000000, 0x08020008,
            0x08000208, 0x00020000, 0x08000000, 0x08020208,
            0x00000008, 0x00020208, 0x00020200, 0x08000008,
            0x08020000, 0x08000208, 0x00000208, 0x08020000,
            0x00020208, 0x00000008, 0x08020008, 0x00020200
        };

		private static readonly uint[] SP4 =
		{
            0x00802001, 0x00002081, 0x00002081, 0x00000080,
            0x00802080, 0x00800081, 0x00800001, 0x00002001,
            0x00000000, 0x00802000, 0x00802000, 0x00802081,
            0x00000081, 0x00000000, 0x00800080, 0x00800001,
            0x00000001, 0x00002000, 0x00800000, 0x00802001,
            0x00000080, 0x00800000, 0x00002001, 0x00002080,
            0x00800081, 0x00000001, 0x00002080, 0x00800080,
            0x00002000, 0x00802080, 0x00802081, 0x00000081,
            0x00800080, 0x00800001, 0x00802000, 0x00802081,
            0x00000081, 0x00000000, 0x00000000, 0x00802000,
            0x00002080, 0x00800080, 0x00800081, 0x00000001,
            0x00802001, 0x00002081, 0x00002081, 0x00000080,
            0x00802081, 0x00000081, 0x00000001, 0x00002000,
            0x00800001, 0x00002001, 0x00802080, 0x00800081,
            0x00002001, 0x00002080, 0x00800000, 0x00802001,
            0x00000080, 0x00800000, 0x00002000, 0x00802080
        };

		private static readonly uint[] SP5 =
		{
            0x00000100, 0x02080100, 0x02080000, 0x42000100,
            0x00080000, 0x00000100, 0x40000000, 0x02080000,
            0x40080100, 0x00080000, 0x02000100, 0x40080100,
            0x42000100, 0x42080000, 0x00080100, 0x40000000,
            0x02000000, 0x40080000, 0x40080000, 0x00000000,
            0x40000100, 0x42080100, 0x42080100, 0x02000100,
            0x42080000, 0x40000100, 0x00000000, 0x42000000,
            0x02080100, 0x02000000, 0x42000000, 0x00080100,
            0x00080000, 0x42000100, 0x00000100, 0x02000000,
            0x40000000, 0x02080000, 0x42000100, 0x40080100,
            0x02000100, 0x40000000, 0x42080000, 0x02080100,
            0x40080100, 0x00000100, 0x02000000, 0x42080000,
            0x42080100, 0x00080100, 0x42000000, 0x42080100,
            0x02080000, 0x00000000, 0x40080000, 0x42000000,
            0x00080100, 0x02000100, 0x40000100, 0x00080000,
            0x00000000, 0x40080000, 0x02080100, 0x40000100
        };

		private static readonly uint[] SP6 =
		{
            0x20000010, 0x20400000, 0x00004000, 0x20404010,
            0x20400000, 0x00000010, 0x20404010, 0x00400000,
            0x20004000, 0x00404010, 0x00400000, 0x20000010,
            0x00400010, 0x20004000, 0x20000000, 0x00004010,
            0x00000000, 0x00400010, 0x20004010, 0x00004000,
            0x00404000, 0x20004010, 0x00000010, 0x20400010,
            0x20400010, 0x00000000, 0x00404010, 0x20404000,
            0x00004010, 0x00404000, 0x20404000, 0x20000000,
            0x20004000, 0x00000010, 0x20400010, 0x00404000,
            0x20404010, 0x00400000, 0x00004010, 0x20000010,
            0x00400000, 0x20004000, 0x20000000, 0x00004010,
            0x20000010, 0x20404010, 0x00404000, 0x20400000,
            0x00404010, 0x20404000, 0x00000000, 0x20400010,
            0x00000010, 0x00004000, 0x20400000, 0x00404010,
            0x00004000, 0x00400010, 0x20004010, 0x00000000,
            0x20404000, 0x20000000, 0x00400010, 0x20004010
        };

		private static readonly uint[] SP7 =
		{
            0x00200000, 0x04200002, 0x04000802, 0x00000000,
            0x00000800, 0x04000802, 0x00200802, 0x04200800,
            0x04200802, 0x00200000, 0x00000000, 0x04000002,
            0x00000002, 0x04000000, 0x04200002, 0x00000802,
            0x04000800, 0x00200802, 0x00200002, 0x04000800,
            0x04000002, 0x04200000, 0x04200800, 0x00200002,
            0x04200000, 0x00000800, 0x00000802, 0x04200802,
            0x00200800, 0x00000002, 0x04000000, 0x00200800,
            0x04000000, 0x00200800, 0x00200000, 0x04000802,
            0x04000802, 0x04200002, 0x04200002, 0x00000002,
            0x00200002, 0x04000000, 0x04000800, 0x00200000,
            0x04200800, 0x00000802, 0x00200802, 0x04200800,
            0x00000802, 0x04000002, 0x04200802, 0x04200000,
            0x00200800, 0x00000000, 0x00000002, 0x04200802,
            0x00000000, 0x00200802, 0x04200000, 0x00000800,
            0x04000002, 0x04000800, 0x00000800, 0x00200002
        };

		private static readonly uint[] SP8 =
		{
            0x10001040, 0x00001000, 0x00040000, 0x10041040,
            0x10000000, 0x10001040, 0x00000040, 0x10000000,
            0x00040040, 0x10040000, 0x10041040, 0x00041000,
            0x10041000, 0x00041040, 0x00001000, 0x00000040,
            0x10040000, 0x10000040, 0x10001000, 0x00001040,
            0x00041000, 0x00040040, 0x10040040, 0x10041000,
            0x00001040, 0x00000000, 0x00000000, 0x10040040,
            0x10000040, 0x10001000, 0x00041040, 0x00040000,
            0x00041040, 0x00040000, 0x10041000, 0x00001000,
            0x00000040, 0x10040040, 0x00001000, 0x00041040,
            0x10001000, 0x00000040, 0x10000040, 0x10040000,
            0x10040040, 0x10000000, 0x00040000, 0x10001040,
            0x00000000, 0x10041040, 0x00040040, 0x10000040,
            0x10040000, 0x10001000, 0x10001040, 0x00000000,
            0x10041040, 0x00041000, 0x00041000, 0x00001040,
            0x00001040, 0x00040040, 0x10000000, 0x10041000
        };

		/**
        * Generate an integer based working key based on our secret key
        * and what we processing we are planning to do.
        *
        * Acknowledgements for this routine go to James Gillogly and Phil Karn.
        *         (whoever, and wherever they are!).
        */
        protected static int[] GenerateWorkingKey(
            bool	encrypting,
            byte[]	key)
        {
            int[] newKey = new int[32];
            bool[] pc1m = new bool[56];
			bool[] pcr = new bool[56];

			for (int j = 0; j < 56; j++ )
            {
                int l = pc1[j];

				pc1m[j] = ((key[(uint) l >> 3] & bytebit[l & 07]) != 0);
            }

            for (int i = 0; i < 16; i++)
            {
                int l, m, n;

                if (encrypting)
                {
                    m = i << 1;
                }
                else
                {
                    m = (15 - i) << 1;
                }

                n = m + 1;
                newKey[m] = newKey[n] = 0;

                for (int j = 0; j < 28; j++)
                {
                    l = j + totrot[i];
                    if ( l < 28 )
                    {
                        pcr[j] = pc1m[l];
                    }
                    else
                    {
                        pcr[j] = pc1m[l - 28];
                    }
                }

                for (int j = 28; j < 56; j++)
                {
                    l = j + totrot[i];
                    if (l < 56 )
                    {
                        pcr[j] = pc1m[l];
                    }
                    else
                    {
                        pcr[j] = pc1m[l - 28];
                    }
                }

                for (int j = 0; j < 24; j++)
                {
                    if (pcr[pc2[j]])
                    {
                        newKey[m] |= bigbyte[j];
                    }

                    if (pcr[pc2[j + 24]])
                    {
                        newKey[n] |= bigbyte[j];
                    }
                }
            }

            //
            // store the processed key
            //
            for (int i = 0; i != 32; i += 2)
            {
                int i1, i2;

                i1 = newKey[i];
                i2 = newKey[i + 1];

                newKey[i] = (int) ( (uint) ((i1 & 0x00fc0000) << 6)  |
                                    (uint) ((i1 & 0x00000fc0) << 10) |
                                    ((uint) (i2 & 0x00fc0000) >> 10) |
                                    ((uint) (i2 & 0x00000fc0) >> 6));

                newKey[i + 1] = (int) ( (uint) ((i1 & 0x0003f000) << 12) |
                                        (uint) ((i1 & 0x0000003f) << 16) |
                                        ((uint) (i2 & 0x0003f000) >> 4) |
                                        (uint) (i2 & 0x0000003f));
            }

            return newKey;
        }

        /**
        * the DES engine.
        */
        internal static void DesFunc(
            int[]	wKey,
            byte[]	input,
            int		inOff,
            byte[]	outBytes,
            int		outOff)
        {
			uint left = Pack.BE_To_UInt32(input, inOff);
			uint right = Pack.BE_To_UInt32(input, inOff + 4);
			uint work;

            work = ((left >> 4) ^ right) & 0x0f0f0f0f;
            right ^= work;
            left ^= (work << 4);
            work = ((left >> 16) ^ right) & 0x0000ffff;
            right ^= work;
            left ^= (work << 16);
            work = ((right >> 2) ^ left) & 0x33333333;
            left ^= work;
            right ^= (work << 2);
            work = ((right >> 8) ^ left) & 0x00ff00ff;
            left ^= work;
            right ^= (work << 8);
            right = (right << 1) | (right >> 31);
            work = (left ^ right) & 0xaaaaaaaa;
            left ^= work;
            right ^= work;
            left = (left << 1) | (left >> 31);

            for (int round = 0; round < 8; round++)
            {
                uint fval;

                work  = (right << 28) | (right >> 4);
                work ^= (uint)wKey[round * 4 + 0];
                fval  = SP7[work         & 0x3f];
                fval |= SP5[(work >>  8) & 0x3f];
                fval |= SP3[(work >> 16) & 0x3f];
                fval |= SP1[(work >> 24) & 0x3f];
                work  = right ^ (uint)wKey[round * 4 + 1];
                fval |= SP8[ work        & 0x3f];
                fval |= SP6[(work >>  8) & 0x3f];
                fval |= SP4[(work >> 16) & 0x3f];
                fval |= SP2[(work >> 24) & 0x3f];
                left ^= fval;
                work  = (left << 28) | (left >> 4);
                work ^= (uint)wKey[round * 4 + 2];
                fval  = SP7[ work        & 0x3f];
                fval |= SP5[(work >>  8) & 0x3f];
                fval |= SP3[(work >> 16) & 0x3f];
                fval |= SP1[(work >> 24) & 0x3f];
                work  = left ^ (uint)wKey[round * 4 + 3];
                fval |= SP8[ work        & 0x3f];
                fval |= SP6[(work >>  8) & 0x3f];
                fval |= SP4[(work >> 16) & 0x3f];
                fval |= SP2[(work >> 24) & 0x3f];
                right ^= fval;
            }

            right = (right << 31) | (right >> 1);
            work = (left ^ right) & 0xaaaaaaaa;
            left ^= work;
            right ^= work;
            left = (left << 31) | (left >> 1);
            work = ((left >> 8) ^ right) & 0x00ff00ff;
            right ^= work;
            left ^= (work << 8);
            work = ((left >> 2) ^ right) & 0x33333333;
            right ^= work;
            left ^= (work << 2);
            work = ((right >> 16) ^ left) & 0x0000ffff;
            left ^= work;
            right ^= (work << 16);
            work = ((right >> 4) ^ left) & 0x0f0f0f0f;
            left ^= work;
            right ^= (work << 4);

			Pack.UInt32_To_BE(right, outBytes, outOff);
			Pack.UInt32_To_BE(left, outBytes, outOff + 4);
        }
    }
}
