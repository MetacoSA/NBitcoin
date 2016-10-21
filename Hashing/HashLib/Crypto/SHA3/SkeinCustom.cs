
using System;

// a customized Skein implementation to fit the StratisX implementation
// this is actually the implementation from the older version of hashlib (version 2.0.1)

namespace HashLib.Crypto.SHA3.Custom
{
   
    internal class Skein224 : SkeinBase
    {
        public Skein224()
            : base(HashLib.HashSize.HashSize224)
        {
        }
    }

    internal class Skein256 : SkeinBase
    {
        public Skein256()
            : base(HashLib.HashSize.HashSize256)
        {
        }
    }

    internal class Skein384 : SkeinBase
    {
        public Skein384()
            : base(HashLib.HashSize.HashSize384)
        {
        }
    }

    internal class Skein512 : SkeinBase
    {
        public Skein512()
            : base(HashLib.HashSize.HashSize512)
        {
        }
    }

    internal abstract class SkeinBase : BlockHash, ICryptoNotBuildIn
    {
        #region Consts

        private static readonly ulong[] SKEIN_IV_224 =
        {
            0xCCD0616248677224,
            0xCBA65CF3A92339EF,
            0x8CCD69D652FF4B64,
            0x398AED7B3AB890B4,
            0x0F59D1B1457D2BD0,
            0x6776FE6575D4EB3D,
            0x99FBC70E997413E9,
            0x9E2CFCCFE1C41EF7
        };

        private static readonly ulong[] SKEIN_IV_256 =
        {
            0xCCD044A12FDB3E13,
            0xE83590301A79A9EB,
            0x55AEA0614F816E6F,
            0x2A2767A4AE9B94DB,
            0xEC06025E74DD7683,
            0xE7A436CDC4746251,
            0xC36FBAF9393AD185,
            0x3EEDBA1833EDFC13
        };

        private static readonly ulong[] SKEIN_IV_384 =
        {
            0xA3F6C6BF3A75EF5F,
            0xB0FEF9CCFD84FAA4,
            0x9D77DD663D770CFE,
            0xD798CBF3B468FDDA,
            0x1BC4A6668A0E4465,
            0x7ED7D434E5807407,
            0x548FC1ACD4EC44D6,
            0x266E17546AA18FF8
        };

        private static readonly ulong[] SKEIN_IV_512 =
        {
            0x4903ADFF749C51CE,
            0x0D95DE399746DF03,
            0x8FD1934127C79BCE,
            0x9A255629FF352CB1,
            0x5DB62599DF6CA7B0,
            0xEABE394CA9D5C3F4,
            0x991112C71A75B523,
            0xAE18A40B660FCC33
        };

        #endregion

        protected ulong m_temp2;
        protected readonly ulong[] m_state = new ulong[8];

        public SkeinBase(HashLib.HashSize a_hashSize)
            : base((int)a_hashSize, 64)
        {
            Initialize();
        }

        protected override void Finish()
        {
            m_temp2 |= 0x8000000000000000;

            TransformBlock(m_buffer.GetBytesZeroPadded(), 0);

            m_processed_bytes = 8;
            m_temp2 = 0xff00000000000000;

            TransformBlock(m_buffer.GetBytesZeroPadded(), 0);
        }

        protected override byte[] GetResult()
        {
            return Converters.ConvertULongsToBytes(m_state, 0, BlockSize / 8).SubArray(0, HashSize);
        }

        public override void Initialize()
        {
            m_temp2 = 0x7000000000000000;

            switch (HashSize)
            {
                case 28: Array.Copy(SKEIN_IV_224, 0, m_state, 0, 8); break;
                case 32: Array.Copy(SKEIN_IV_256, 0, m_state, 0, 8); break;
                case 48: Array.Copy(SKEIN_IV_384, 0, m_state, 0, 8); break;
                case 64: Array.Copy(SKEIN_IV_512, 0, m_state, 0, 8); break;
            }

            base.Initialize();
        }

        public override void TransformBytes(byte[] a_data, int a_index, int a_length)
        {
            if (a_length == 0)
                return;

            if (m_buffer.IsFull)
                TransformBlock(m_buffer.GetBytes(), 0);

            if (a_length + m_buffer.Pos > BlockSize)
            {
                if (m_buffer.Feed(a_data, ref a_index, ref a_length, ref m_processed_bytes))
                    TransformBlock(m_buffer.GetBytes(), 0);
            }

            while (a_length > BlockSize)
            {
                m_processed_bytes += (uint)BlockSize;
                TransformBlock(a_data, a_index);
                a_length -= BlockSize;
                a_index += BlockSize;
            }

            if (a_length > 0)
                m_buffer.Feed(a_data, ref a_index, ref a_length, ref m_processed_bytes);
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            ulong state0 = m_processed_bytes;
            ulong state1 = m_temp2;
            ulong state2 = state0 ^ state1;
            ulong state3 = m_state[0];
            ulong state4 = m_state[1];
            ulong state5 = m_state[2];
            ulong state6 = m_state[3];
            ulong state7 = m_state[4];
            ulong state8 = m_state[5];
            ulong state9 = m_state[6];
            ulong state10 = m_state[7];
            ulong state11 = state3 ^ state4 ^ state5 ^ state6 ^ state7 ^ state8 ^ state9 ^ state10 ^ 0x1BD11BDAA9FC1A22;

            ulong[] data = Converters.ConvertBytesToULongs(a_data, a_index, BlockSize);

            ulong X0 = data[0] + state3;
            ulong X1 = data[1] + state4;
            ulong X2 = data[2] + state5;
            ulong X3 = data[3] + state6;
            ulong X4 = data[4] + state7;
            ulong X5 = data[5] + state8 + state0;
            ulong X6 = data[6] + state9 + state1;
            ulong X7 = data[7] + state10;

            X0 += X1;
            X1 = (X1 << 46) | (X1 >> (64 - 46));
            X1 ^= X0;
            X2 += X3;
            X3 = (X3 << 36) | (X3 >> (64 - 36));
            X3 ^= X2;
            X4 += X5;
            X5 = (X5 << 19) | (X5 >> (64 - 19));
            X5 ^= X4;
            X6 += X7;
            X7 = (X7 << 37) | (X7 >> (64 - 37));
            X7 ^= X6;
            X2 += X1;
            X1 = (X1 << 33) | (X1 >> (64 - 33));
            X1 ^= X2;
            X4 += X7;
            X7 = (X7 << 27) | (X7 >> (64 - 27));
            X7 ^= X4;
            X6 += X5;
            X5 = (X5 << 14) | (X5 >> (64 - 14));
            X5 ^= X6;
            X0 += X3;
            X3 = (X3 << 42) | (X3 >> (64 - 42));
            X3 ^= X0;
            X4 += X1;
            X1 = (X1 << 17) | (X1 >> (64 - 17));
            X1 ^= X4;
            X6 += X3;
            X3 = (X3 << 49) | (X3 >> (64 - 49));
            X3 ^= X6;
            X0 += X5;
            X5 = (X5 << 36) | (X5 >> (64 - 36));
            X5 ^= X0;
            X2 += X7;
            X7 = (X7 << 39) | (X7 >> (64 - 39));
            X7 ^= X2;
            X6 += X1;
            X1 = (X1 << 44) | (X1 >> (64 - 44));
            X1 ^= X6;
            X0 += X7;
            X7 = (X7 << 9) | (X7 >> (64 - 9));
            X7 ^= X0;
            X2 += X5;
            X5 = (X5 << 54) | (X5 >> (64 - 54));
            X5 ^= X2;
            X4 += X3;
            X3 = (X3 << 56) | (X3 >> (64 - 56));
            X3 ^= X4;
            X0 += state4;
            X1 += state5;
            X2 += state6;
            X3 += state7;
            X4 += state8;
            X5 += state9 + state1;
            X6 += state10 + state2;
            X7 += state11 + 1;
            X0 += X1;
            X1 = (X1 << 39) | (X1 >> (64 - 39));
            X1 ^= X0;
            X2 += X3;
            X3 = (X3 << 30) | (X3 >> (64 - 30));
            X3 ^= X2;
            X4 += X5;
            X5 = (X5 << 34) | (X5 >> (64 - 34));
            X5 ^= X4;
            X6 += X7;
            X7 = (X7 << 24) | (X7 >> (64 - 24));
            X7 ^= X6;
            X2 += X1;
            X1 = (X1 << 13) | (X1 >> (64 - 13));
            X1 ^= X2;
            X4 += X7;
            X7 = (X7 << 50) | (X7 >> (64 - 50));
            X7 ^= X4;
            X6 += X5;
            X5 = (X5 << 10) | (X5 >> (64 - 10));
            X5 ^= X6;
            X0 += X3;
            X3 = (X3 << 17) | (X3 >> (64 - 17));
            X3 ^= X0;
            X4 += X1;
            X1 = (X1 << 25) | (X1 >> (64 - 25));
            X1 ^= X4;
            X6 += X3;
            X3 = (X3 << 29) | (X3 >> (64 - 29));
            X3 ^= X6;
            X0 += X5;
            X5 = (X5 << 39) | (X5 >> (64 - 39));
            X5 ^= X0;
            X2 += X7;
            X7 = (X7 << 43) | (X7 >> (64 - 43));
            X7 ^= X2;
            X6 += X1;
            X1 = (X1 << 8) | (X1 >> (64 - 8));
            X1 ^= X6;
            X0 += X7;
            X7 = (X7 << 35) | (X7 >> (64 - 35));
            X7 ^= X0;
            X2 += X5;
            X5 = (X5 << 56) | (X5 >> (64 - 56));
            X5 ^= X2;
            X4 += X3;
            X3 = (X3 << 22) | (X3 >> (64 - 22));
            X3 ^= X4;
            X0 += state5;
            X1 += state6;
            X2 += state7;
            X3 += state8;
            X4 += state9;
            X5 += state10 + state2;
            X6 += state11 + state0;
            X7 += state3 + 2;

            X0 += X1;
            X1 = (X1 << 46) | (X1 >> (64 - 46));
            X1 ^= X0;
            X2 += X3;
            X3 = (X3 << 36) | (X3 >> (64 - 36));
            X3 ^= X2;
            X4 += X5;
            X5 = (X5 << 19) | (X5 >> (64 - 19));
            X5 ^= X4;
            X6 += X7;
            X7 = (X7 << 37) | (X7 >> (64 - 37));
            X7 ^= X6;
            X2 += X1;
            X1 = (X1 << 33) | (X1 >> (64 - 33));
            X1 ^= X2;
            X4 += X7;
            X7 = (X7 << 27) | (X7 >> (64 - 27));
            X7 ^= X4;
            X6 += X5;
            X5 = (X5 << 14) | (X5 >> (64 - 14));
            X5 ^= X6;
            X0 += X3;
            X3 = (X3 << 42) | (X3 >> (64 - 42));
            X3 ^= X0;
            X4 += X1;
            X1 = (X1 << 17) | (X1 >> (64 - 17));
            X1 ^= X4;
            X6 += X3;
            X3 = (X3 << 49) | (X3 >> (64 - 49));
            X3 ^= X6;
            X0 += X5;
            X5 = (X5 << 36) | (X5 >> (64 - 36));
            X5 ^= X0;
            X2 += X7;
            X7 = (X7 << 39) | (X7 >> (64 - 39));
            X7 ^= X2;
            X6 += X1;
            X1 = (X1 << 44) | (X1 >> (64 - 44));
            X1 ^= X6;
            X0 += X7;
            X7 = (X7 << 9) | (X7 >> (64 - 9));
            X7 ^= X0;
            X2 += X5;
            X5 = (X5 << 54) | (X5 >> (64 - 54));
            X5 ^= X2;
            X4 += X3;
            X3 = (X3 << 56) | (X3 >> (64 - 56));
            X3 ^= X4;
            X0 += state6;
            X1 += state7;
            X2 += state8;
            X3 += state9;
            X4 += state10;
            X5 += state11 + state0;
            X6 += state3 + state1;
            X7 += state4 + 3;
            X0 += X1;
            X1 = (X1 << 39) | (X1 >> (64 - 39));
            X1 ^= X0;
            X2 += X3;
            X3 = (X3 << 30) | (X3 >> (64 - 30));
            X3 ^= X2;
            X4 += X5;
            X5 = (X5 << 34) | (X5 >> (64 - 34));
            X5 ^= X4;
            X6 += X7;
            X7 = (X7 << 24) | (X7 >> (64 - 24));
            X7 ^= X6;
            X2 += X1;
            X1 = (X1 << 13) | (X1 >> (64 - 13));
            X1 ^= X2;
            X4 += X7;
            X7 = (X7 << 50) | (X7 >> (64 - 50));
            X7 ^= X4;
            X6 += X5;
            X5 = (X5 << 10) | (X5 >> (64 - 10));
            X5 ^= X6;
            X0 += X3;
            X3 = (X3 << 17) | (X3 >> (64 - 17));
            X3 ^= X0;
            X4 += X1;
            X1 = (X1 << 25) | (X1 >> (64 - 25));
            X1 ^= X4;
            X6 += X3;
            X3 = (X3 << 29) | (X3 >> (64 - 29));
            X3 ^= X6;
            X0 += X5;
            X5 = (X5 << 39) | (X5 >> (64 - 39));
            X5 ^= X0;
            X2 += X7;
            X7 = (X7 << 43) | (X7 >> (64 - 43));
            X7 ^= X2;
            X6 += X1;
            X1 = (X1 << 8) | (X1 >> (64 - 8));
            X1 ^= X6;
            X0 += X7;
            X7 = (X7 << 35) | (X7 >> (64 - 35));
            X7 ^= X0;
            X2 += X5;
            X5 = (X5 << 56) | (X5 >> (64 - 56));
            X5 ^= X2;
            X4 += X3;
            X3 = (X3 << 22) | (X3 >> (64 - 22));
            X3 ^= X4;
            X0 += state7;
            X1 += state8;
            X2 += state9;
            X3 += state10;
            X4 += state11;
            X5 += state3 + state1;
            X6 += state4 + state2;
            X7 += state5 + 4;

            X0 += X1;
            X1 = (X1 << 46) | (X1 >> (64 - 46));
            X1 ^= X0;
            X2 += X3;
            X3 = (X3 << 36) | (X3 >> (64 - 36));
            X3 ^= X2;
            X4 += X5;
            X5 = (X5 << 19) | (X5 >> (64 - 19));
            X5 ^= X4;
            X6 += X7;
            X7 = (X7 << 37) | (X7 >> (64 - 37));
            X7 ^= X6;
            X2 += X1;
            X1 = (X1 << 33) | (X1 >> (64 - 33));
            X1 ^= X2;
            X4 += X7;
            X7 = (X7 << 27) | (X7 >> (64 - 27));
            X7 ^= X4;
            X6 += X5;
            X5 = (X5 << 14) | (X5 >> (64 - 14));
            X5 ^= X6;
            X0 += X3;
            X3 = (X3 << 42) | (X3 >> (64 - 42));
            X3 ^= X0;
            X4 += X1;
            X1 = (X1 << 17) | (X1 >> (64 - 17));
            X1 ^= X4;
            X6 += X3;
            X3 = (X3 << 49) | (X3 >> (64 - 49));
            X3 ^= X6;
            X0 += X5;
            X5 = (X5 << 36) | (X5 >> (64 - 36));
            X5 ^= X0;
            X2 += X7;
            X7 = (X7 << 39) | (X7 >> (64 - 39));
            X7 ^= X2;
            X6 += X1;
            X1 = (X1 << 44) | (X1 >> (64 - 44));
            X1 ^= X6;
            X0 += X7;
            X7 = (X7 << 9) | (X7 >> (64 - 9));
            X7 ^= X0;
            X2 += X5;
            X5 = (X5 << 54) | (X5 >> (64 - 54));
            X5 ^= X2;
            X4 += X3;
            X3 = (X3 << 56) | (X3 >> (64 - 56));
            X3 ^= X4;
            X0 += state8;
            X1 += state9;
            X2 += state10;
            X3 += state11;
            X4 += state3;
            X5 += state4 + state2;
            X6 += state5 + state0;
            X7 += state6 + 5;
            X0 += X1;
            X1 = (X1 << 39) | (X1 >> (64 - 39));
            X1 ^= X0;
            X2 += X3;
            X3 = (X3 << 30) | (X3 >> (64 - 30));
            X3 ^= X2;
            X4 += X5;
            X5 = (X5 << 34) | (X5 >> (64 - 34));
            X5 ^= X4;
            X6 += X7;
            X7 = (X7 << 24) | (X7 >> (64 - 24));
            X7 ^= X6;
            X2 += X1;
            X1 = (X1 << 13) | (X1 >> (64 - 13));
            X1 ^= X2;
            X4 += X7;
            X7 = (X7 << 50) | (X7 >> (64 - 50));
            X7 ^= X4;
            X6 += X5;
            X5 = (X5 << 10) | (X5 >> (64 - 10));
            X5 ^= X6;
            X0 += X3;
            X3 = (X3 << 17) | (X3 >> (64 - 17));
            X3 ^= X0;
            X4 += X1;
            X1 = (X1 << 25) | (X1 >> (64 - 25));
            X1 ^= X4;
            X6 += X3;
            X3 = (X3 << 29) | (X3 >> (64 - 29));
            X3 ^= X6;
            X0 += X5;
            X5 = (X5 << 39) | (X5 >> (64 - 39));
            X5 ^= X0;
            X2 += X7;
            X7 = (X7 << 43) | (X7 >> (64 - 43));
            X7 ^= X2;
            X6 += X1;
            X1 = (X1 << 8) | (X1 >> (64 - 8));
            X1 ^= X6;
            X0 += X7;
            X7 = (X7 << 35) | (X7 >> (64 - 35));
            X7 ^= X0;
            X2 += X5;
            X5 = (X5 << 56) | (X5 >> (64 - 56));
            X5 ^= X2;
            X4 += X3;
            X3 = (X3 << 22) | (X3 >> (64 - 22));
            X3 ^= X4;
            X0 += state9;
            X1 += state10;
            X2 += state11;
            X3 += state3;
            X4 += state4;
            X5 += state5 + state0;
            X6 += state6 + state1;
            X7 += state7 + 6;

            X0 += X1;
            X1 = (X1 << 46) | (X1 >> (64 - 46));
            X1 ^= X0;
            X2 += X3;
            X3 = (X3 << 36) | (X3 >> (64 - 36));
            X3 ^= X2;
            X4 += X5;
            X5 = (X5 << 19) | (X5 >> (64 - 19));
            X5 ^= X4;
            X6 += X7;
            X7 = (X7 << 37) | (X7 >> (64 - 37));
            X7 ^= X6;
            X2 += X1;
            X1 = (X1 << 33) | (X1 >> (64 - 33));
            X1 ^= X2;
            X4 += X7;
            X7 = (X7 << 27) | (X7 >> (64 - 27));
            X7 ^= X4;
            X6 += X5;
            X5 = (X5 << 14) | (X5 >> (64 - 14));
            X5 ^= X6;
            X0 += X3;
            X3 = (X3 << 42) | (X3 >> (64 - 42));
            X3 ^= X0;
            X4 += X1;
            X1 = (X1 << 17) | (X1 >> (64 - 17));
            X1 ^= X4;
            X6 += X3;
            X3 = (X3 << 49) | (X3 >> (64 - 49));
            X3 ^= X6;
            X0 += X5;
            X5 = (X5 << 36) | (X5 >> (64 - 36));
            X5 ^= X0;
            X2 += X7;
            X7 = (X7 << 39) | (X7 >> (64 - 39));
            X7 ^= X2;
            X6 += X1;
            X1 = (X1 << 44) | (X1 >> (64 - 44));
            X1 ^= X6;
            X0 += X7;
            X7 = (X7 << 9) | (X7 >> (64 - 9));
            X7 ^= X0;
            X2 += X5;
            X5 = (X5 << 54) | (X5 >> (64 - 54));
            X5 ^= X2;
            X4 += X3;
            X3 = (X3 << 56) | (X3 >> (64 - 56));
            X3 ^= X4;
            X0 += state10;
            X1 += state11;
            X2 += state3;
            X3 += state4;
            X4 += state5;
            X5 += state6 + state1;
            X6 += state7 + state2;
            X7 += state8 + 7;
            X0 += X1;
            X1 = (X1 << 39) | (X1 >> (64 - 39));
            X1 ^= X0;
            X2 += X3;
            X3 = (X3 << 30) | (X3 >> (64 - 30));
            X3 ^= X2;
            X4 += X5;
            X5 = (X5 << 34) | (X5 >> (64 - 34));
            X5 ^= X4;
            X6 += X7;
            X7 = (X7 << 24) | (X7 >> (64 - 24));
            X7 ^= X6;
            X2 += X1;
            X1 = (X1 << 13) | (X1 >> (64 - 13));
            X1 ^= X2;
            X4 += X7;
            X7 = (X7 << 50) | (X7 >> (64 - 50));
            X7 ^= X4;
            X6 += X5;
            X5 = (X5 << 10) | (X5 >> (64 - 10));
            X5 ^= X6;
            X0 += X3;
            X3 = (X3 << 17) | (X3 >> (64 - 17));
            X3 ^= X0;
            X4 += X1;
            X1 = (X1 << 25) | (X1 >> (64 - 25));
            X1 ^= X4;
            X6 += X3;
            X3 = (X3 << 29) | (X3 >> (64 - 29));
            X3 ^= X6;
            X0 += X5;
            X5 = (X5 << 39) | (X5 >> (64 - 39));
            X5 ^= X0;
            X2 += X7;
            X7 = (X7 << 43) | (X7 >> (64 - 43));
            X7 ^= X2;
            X6 += X1;
            X1 = (X1 << 8) | (X1 >> (64 - 8));
            X1 ^= X6;
            X0 += X7;
            X7 = (X7 << 35) | (X7 >> (64 - 35));
            X7 ^= X0;
            X2 += X5;
            X5 = (X5 << 56) | (X5 >> (64 - 56));
            X5 ^= X2;
            X4 += X3;
            X3 = (X3 << 22) | (X3 >> (64 - 22));
            X3 ^= X4;
            X0 += state11;
            X1 += state3;
            X2 += state4;
            X3 += state5;
            X4 += state6;
            X5 += state7 + state2;
            X6 += state8 + state0;
            X7 += state9 + 8;

            X0 += X1;
            X1 = (X1 << 46) | (X1 >> (64 - 46));
            X1 ^= X0;
            X2 += X3;
            X3 = (X3 << 36) | (X3 >> (64 - 36));
            X3 ^= X2;
            X4 += X5;
            X5 = (X5 << 19) | (X5 >> (64 - 19));
            X5 ^= X4;
            X6 += X7;
            X7 = (X7 << 37) | (X7 >> (64 - 37));
            X7 ^= X6;
            X2 += X1;
            X1 = (X1 << 33) | (X1 >> (64 - 33));
            X1 ^= X2;
            X4 += X7;
            X7 = (X7 << 27) | (X7 >> (64 - 27));
            X7 ^= X4;
            X6 += X5;
            X5 = (X5 << 14) | (X5 >> (64 - 14));
            X5 ^= X6;
            X0 += X3;
            X3 = (X3 << 42) | (X3 >> (64 - 42));
            X3 ^= X0;
            X4 += X1;
            X1 = (X1 << 17) | (X1 >> (64 - 17));
            X1 ^= X4;
            X6 += X3;
            X3 = (X3 << 49) | (X3 >> (64 - 49));
            X3 ^= X6;
            X0 += X5;
            X5 = (X5 << 36) | (X5 >> (64 - 36));
            X5 ^= X0;
            X2 += X7;
            X7 = (X7 << 39) | (X7 >> (64 - 39));
            X7 ^= X2;
            X6 += X1;
            X1 = (X1 << 44) | (X1 >> (64 - 44));
            X1 ^= X6;
            X0 += X7;
            X7 = (X7 << 9) | (X7 >> (64 - 9));
            X7 ^= X0;
            X2 += X5;
            X5 = (X5 << 54) | (X5 >> (64 - 54));
            X5 ^= X2;
            X4 += X3;
            X3 = (X3 << 56) | (X3 >> (64 - 56));
            X3 ^= X4;
            X0 += state3;
            X1 += state4;
            X2 += state5;
            X3 += state6;
            X4 += state7;
            X5 += state8 + state0;
            X6 += state9 + state1;
            X7 += state10 + 9;
            X0 += X1;
            X1 = (X1 << 39) | (X1 >> (64 - 39));
            X1 ^= X0;
            X2 += X3;
            X3 = (X3 << 30) | (X3 >> (64 - 30));
            X3 ^= X2;
            X4 += X5;
            X5 = (X5 << 34) | (X5 >> (64 - 34));
            X5 ^= X4;
            X6 += X7;
            X7 = (X7 << 24) | (X7 >> (64 - 24));
            X7 ^= X6;
            X2 += X1;
            X1 = (X1 << 13) | (X1 >> (64 - 13));
            X1 ^= X2;
            X4 += X7;
            X7 = (X7 << 50) | (X7 >> (64 - 50));
            X7 ^= X4;
            X6 += X5;
            X5 = (X5 << 10) | (X5 >> (64 - 10));
            X5 ^= X6;
            X0 += X3;
            X3 = (X3 << 17) | (X3 >> (64 - 17));
            X3 ^= X0;
            X4 += X1;
            X1 = (X1 << 25) | (X1 >> (64 - 25));
            X1 ^= X4;
            X6 += X3;
            X3 = (X3 << 29) | (X3 >> (64 - 29));
            X3 ^= X6;
            X0 += X5;
            X5 = (X5 << 39) | (X5 >> (64 - 39));
            X5 ^= X0;
            X2 += X7;
            X7 = (X7 << 43) | (X7 >> (64 - 43));
            X7 ^= X2;
            X6 += X1;
            X1 = (X1 << 8) | (X1 >> (64 - 8));
            X1 ^= X6;
            X0 += X7;
            X7 = (X7 << 35) | (X7 >> (64 - 35));
            X7 ^= X0;
            X2 += X5;
            X5 = (X5 << 56) | (X5 >> (64 - 56));
            X5 ^= X2;
            X4 += X3;
            X3 = (X3 << 22) | (X3 >> (64 - 22));
            X3 ^= X4;
            X0 += state4;
            X1 += state5;
            X2 += state6;
            X3 += state7;
            X4 += state8;
            X5 += state9 + state1;
            X6 += state10 + state2;
            X7 += state11 + 10;

            X0 += X1;
            X1 = (X1 << 46) | (X1 >> (64 - 46));
            X1 ^= X0;
            X2 += X3;
            X3 = (X3 << 36) | (X3 >> (64 - 36));
            X3 ^= X2;
            X4 += X5;
            X5 = (X5 << 19) | (X5 >> (64 - 19));
            X5 ^= X4;
            X6 += X7;
            X7 = (X7 << 37) | (X7 >> (64 - 37));
            X7 ^= X6;
            X2 += X1;
            X1 = (X1 << 33) | (X1 >> (64 - 33));
            X1 ^= X2;
            X4 += X7;
            X7 = (X7 << 27) | (X7 >> (64 - 27));
            X7 ^= X4;
            X6 += X5;
            X5 = (X5 << 14) | (X5 >> (64 - 14));
            X5 ^= X6;
            X0 += X3;
            X3 = (X3 << 42) | (X3 >> (64 - 42));
            X3 ^= X0;
            X4 += X1;
            X1 = (X1 << 17) | (X1 >> (64 - 17));
            X1 ^= X4;
            X6 += X3;
            X3 = (X3 << 49) | (X3 >> (64 - 49));
            X3 ^= X6;
            X0 += X5;
            X5 = (X5 << 36) | (X5 >> (64 - 36));
            X5 ^= X0;
            X2 += X7;
            X7 = (X7 << 39) | (X7 >> (64 - 39));
            X7 ^= X2;
            X6 += X1;
            X1 = (X1 << 44) | (X1 >> (64 - 44));
            X1 ^= X6;
            X0 += X7;
            X7 = (X7 << 9) | (X7 >> (64 - 9));
            X7 ^= X0;
            X2 += X5;
            X5 = (X5 << 54) | (X5 >> (64 - 54));
            X5 ^= X2;
            X4 += X3;
            X3 = (X3 << 56) | (X3 >> (64 - 56));
            X3 ^= X4;
            X0 += state5;
            X1 += state6;
            X2 += state7;
            X3 += state8;
            X4 += state9;
            X5 += state10 + state2;
            X6 += state11 + state0;
            X7 += state3 + 11;
            X0 += X1;
            X1 = (X1 << 39) | (X1 >> (64 - 39));
            X1 ^= X0;
            X2 += X3;
            X3 = (X3 << 30) | (X3 >> (64 - 30));
            X3 ^= X2;
            X4 += X5;
            X5 = (X5 << 34) | (X5 >> (64 - 34));
            X5 ^= X4;
            X6 += X7;
            X7 = (X7 << 24) | (X7 >> (64 - 24));
            X7 ^= X6;
            X2 += X1;
            X1 = (X1 << 13) | (X1 >> (64 - 13));
            X1 ^= X2;
            X4 += X7;
            X7 = (X7 << 50) | (X7 >> (64 - 50));
            X7 ^= X4;
            X6 += X5;
            X5 = (X5 << 10) | (X5 >> (64 - 10));
            X5 ^= X6;
            X0 += X3;
            X3 = (X3 << 17) | (X3 >> (64 - 17));
            X3 ^= X0;
            X4 += X1;
            X1 = (X1 << 25) | (X1 >> (64 - 25));
            X1 ^= X4;
            X6 += X3;
            X3 = (X3 << 29) | (X3 >> (64 - 29));
            X3 ^= X6;
            X0 += X5;
            X5 = (X5 << 39) | (X5 >> (64 - 39));
            X5 ^= X0;
            X2 += X7;
            X7 = (X7 << 43) | (X7 >> (64 - 43));
            X7 ^= X2;
            X6 += X1;
            X1 = (X1 << 8) | (X1 >> (64 - 8));
            X1 ^= X6;
            X0 += X7;
            X7 = (X7 << 35) | (X7 >> (64 - 35));
            X7 ^= X0;
            X2 += X5;
            X5 = (X5 << 56) | (X5 >> (64 - 56));
            X5 ^= X2;
            X4 += X3;
            X3 = (X3 << 22) | (X3 >> (64 - 22));
            X3 ^= X4;
            X0 += state6;
            X1 += state7;
            X2 += state8;
            X3 += state9;
            X4 += state10;
            X5 += state11 + state0;
            X6 += state3 + state1;
            X7 += state4 + 12;

            X0 += X1;
            X1 = (X1 << 46) | (X1 >> (64 - 46));
            X1 ^= X0;
            X2 += X3;
            X3 = (X3 << 36) | (X3 >> (64 - 36));
            X3 ^= X2;
            X4 += X5;
            X5 = (X5 << 19) | (X5 >> (64 - 19));
            X5 ^= X4;
            X6 += X7;
            X7 = (X7 << 37) | (X7 >> (64 - 37));
            X7 ^= X6;
            X2 += X1;
            X1 = (X1 << 33) | (X1 >> (64 - 33));
            X1 ^= X2;
            X4 += X7;
            X7 = (X7 << 27) | (X7 >> (64 - 27));
            X7 ^= X4;
            X6 += X5;
            X5 = (X5 << 14) | (X5 >> (64 - 14));
            X5 ^= X6;
            X0 += X3;
            X3 = (X3 << 42) | (X3 >> (64 - 42));
            X3 ^= X0;
            X4 += X1;
            X1 = (X1 << 17) | (X1 >> (64 - 17));
            X1 ^= X4;
            X6 += X3;
            X3 = (X3 << 49) | (X3 >> (64 - 49));
            X3 ^= X6;
            X0 += X5;
            X5 = (X5 << 36) | (X5 >> (64 - 36));
            X5 ^= X0;
            X2 += X7;
            X7 = (X7 << 39) | (X7 >> (64 - 39));
            X7 ^= X2;
            X6 += X1;
            X1 = (X1 << 44) | (X1 >> (64 - 44));
            X1 ^= X6;
            X0 += X7;
            X7 = (X7 << 9) | (X7 >> (64 - 9));
            X7 ^= X0;
            X2 += X5;
            X5 = (X5 << 54) | (X5 >> (64 - 54));
            X5 ^= X2;
            X4 += X3;
            X3 = (X3 << 56) | (X3 >> (64 - 56));
            X3 ^= X4;
            X0 += state7;
            X1 += state8;
            X2 += state9;
            X3 += state10;
            X4 += state11;
            X5 += state3 + state1;
            X6 += state4 + state2;
            X7 += state5 + 13;
            X0 += X1;
            X1 = (X1 << 39) | (X1 >> (64 - 39));
            X1 ^= X0;
            X2 += X3;
            X3 = (X3 << 30) | (X3 >> (64 - 30));
            X3 ^= X2;
            X4 += X5;
            X5 = (X5 << 34) | (X5 >> (64 - 34));
            X5 ^= X4;
            X6 += X7;
            X7 = (X7 << 24) | (X7 >> (64 - 24));
            X7 ^= X6;
            X2 += X1;
            X1 = (X1 << 13) | (X1 >> (64 - 13));
            X1 ^= X2;
            X4 += X7;
            X7 = (X7 << 50) | (X7 >> (64 - 50));
            X7 ^= X4;
            X6 += X5;
            X5 = (X5 << 10) | (X5 >> (64 - 10));
            X5 ^= X6;
            X0 += X3;
            X3 = (X3 << 17) | (X3 >> (64 - 17));
            X3 ^= X0;
            X4 += X1;
            X1 = (X1 << 25) | (X1 >> (64 - 25));
            X1 ^= X4;
            X6 += X3;
            X3 = (X3 << 29) | (X3 >> (64 - 29));
            X3 ^= X6;
            X0 += X5;
            X5 = (X5 << 39) | (X5 >> (64 - 39));
            X5 ^= X0;
            X2 += X7;
            X7 = (X7 << 43) | (X7 >> (64 - 43));
            X7 ^= X2;
            X6 += X1;
            X1 = (X1 << 8) | (X1 >> (64 - 8));
            X1 ^= X6;
            X0 += X7;
            X7 = (X7 << 35) | (X7 >> (64 - 35));
            X7 ^= X0;
            X2 += X5;
            X5 = (X5 << 56) | (X5 >> (64 - 56));
            X5 ^= X2;
            X4 += X3;
            X3 = (X3 << 22) | (X3 >> (64 - 22));
            X3 ^= X4;
            X0 += state8;
            X1 += state9;
            X2 += state10;
            X3 += state11;
            X4 += state3;
            X5 += state4 + state2;
            X6 += state5 + state0;
            X7 += state6 + 14;

            X0 += X1;
            X1 = (X1 << 46) | (X1 >> (64 - 46));
            X1 ^= X0;
            X2 += X3;
            X3 = (X3 << 36) | (X3 >> (64 - 36));
            X3 ^= X2;
            X4 += X5;
            X5 = (X5 << 19) | (X5 >> (64 - 19));
            X5 ^= X4;
            X6 += X7;
            X7 = (X7 << 37) | (X7 >> (64 - 37));
            X7 ^= X6;
            X2 += X1;
            X1 = (X1 << 33) | (X1 >> (64 - 33));
            X1 ^= X2;
            X4 += X7;
            X7 = (X7 << 27) | (X7 >> (64 - 27));
            X7 ^= X4;
            X6 += X5;
            X5 = (X5 << 14) | (X5 >> (64 - 14));
            X5 ^= X6;
            X0 += X3;
            X3 = (X3 << 42) | (X3 >> (64 - 42));
            X3 ^= X0;
            X4 += X1;
            X1 = (X1 << 17) | (X1 >> (64 - 17));
            X1 ^= X4;
            X6 += X3;
            X3 = (X3 << 49) | (X3 >> (64 - 49));
            X3 ^= X6;
            X0 += X5;
            X5 = (X5 << 36) | (X5 >> (64 - 36));
            X5 ^= X0;
            X2 += X7;
            X7 = (X7 << 39) | (X7 >> (64 - 39));
            X7 ^= X2;
            X6 += X1;
            X1 = (X1 << 44) | (X1 >> (64 - 44));
            X1 ^= X6;
            X0 += X7;
            X7 = (X7 << 9) | (X7 >> (64 - 9));
            X7 ^= X0;
            X2 += X5;
            X5 = (X5 << 54) | (X5 >> (64 - 54));
            X5 ^= X2;
            X4 += X3;
            X3 = (X3 << 56) | (X3 >> (64 - 56));
            X3 ^= X4;
            X0 += state9;
            X1 += state10;
            X2 += state11;
            X3 += state3;
            X4 += state4;
            X5 += state5 + state0;
            X6 += state6 + state1;
            X7 += state7 + 15;
            X0 += X1;
            X1 = (X1 << 39) | (X1 >> (64 - 39));
            X1 ^= X0;
            X2 += X3;
            X3 = (X3 << 30) | (X3 >> (64 - 30));
            X3 ^= X2;
            X4 += X5;
            X5 = (X5 << 34) | (X5 >> (64 - 34));
            X5 ^= X4;
            X6 += X7;
            X7 = (X7 << 24) | (X7 >> (64 - 24));
            X7 ^= X6;
            X2 += X1;
            X1 = (X1 << 13) | (X1 >> (64 - 13));
            X1 ^= X2;
            X4 += X7;
            X7 = (X7 << 50) | (X7 >> (64 - 50));
            X7 ^= X4;
            X6 += X5;
            X5 = (X5 << 10) | (X5 >> (64 - 10));
            X5 ^= X6;
            X0 += X3;
            X3 = (X3 << 17) | (X3 >> (64 - 17));
            X3 ^= X0;
            X4 += X1;
            X1 = (X1 << 25) | (X1 >> (64 - 25));
            X1 ^= X4;
            X6 += X3;
            X3 = (X3 << 29) | (X3 >> (64 - 29));
            X3 ^= X6;
            X0 += X5;
            X5 = (X5 << 39) | (X5 >> (64 - 39));
            X5 ^= X0;
            X2 += X7;
            X7 = (X7 << 43) | (X7 >> (64 - 43));
            X7 ^= X2;
            X6 += X1;
            X1 = (X1 << 8) | (X1 >> (64 - 8));
            X1 ^= X6;
            X0 += X7;
            X7 = (X7 << 35) | (X7 >> (64 - 35));
            X7 ^= X0;
            X2 += X5;
            X5 = (X5 << 56) | (X5 >> (64 - 56));
            X5 ^= X2;
            X4 += X3;
            X3 = (X3 << 22) | (X3 >> (64 - 22));
            X3 ^= X4;
            X0 += state10;
            X1 += state11;
            X2 += state3;
            X3 += state4;
            X4 += state5;
            X5 += state6 + state1;
            X6 += state7 + state2;
            X7 += state8 + 16;

            X0 += X1;
            X1 = (X1 << 46) | (X1 >> (64 - 46));
            X1 ^= X0;
            X2 += X3;
            X3 = (X3 << 36) | (X3 >> (64 - 36));
            X3 ^= X2;
            X4 += X5;
            X5 = (X5 << 19) | (X5 >> (64 - 19));
            X5 ^= X4;
            X6 += X7;
            X7 = (X7 << 37) | (X7 >> (64 - 37));
            X7 ^= X6;
            X2 += X1;
            X1 = (X1 << 33) | (X1 >> (64 - 33));
            X1 ^= X2;
            X4 += X7;
            X7 = (X7 << 27) | (X7 >> (64 - 27));
            X7 ^= X4;
            X6 += X5;
            X5 = (X5 << 14) | (X5 >> (64 - 14));
            X5 ^= X6;
            X0 += X3;
            X3 = (X3 << 42) | (X3 >> (64 - 42));
            X3 ^= X0;
            X4 += X1;
            X1 = (X1 << 17) | (X1 >> (64 - 17));
            X1 ^= X4;
            X6 += X3;
            X3 = (X3 << 49) | (X3 >> (64 - 49));
            X3 ^= X6;
            X0 += X5;
            X5 = (X5 << 36) | (X5 >> (64 - 36));
            X5 ^= X0;
            X2 += X7;
            X7 = (X7 << 39) | (X7 >> (64 - 39));
            X7 ^= X2;
            X6 += X1;
            X1 = (X1 << 44) | (X1 >> (64 - 44));
            X1 ^= X6;
            X0 += X7;
            X7 = (X7 << 9) | (X7 >> (64 - 9));
            X7 ^= X0;
            X2 += X5;
            X5 = (X5 << 54) | (X5 >> (64 - 54));
            X5 ^= X2;
            X4 += X3;
            X3 = (X3 << 56) | (X3 >> (64 - 56));
            X3 ^= X4;
            X0 += state11;
            X1 += state3;
            X2 += state4;
            X3 += state5;
            X4 += state6;
            X5 += state7 + state2;
            X6 += state8 + state0;
            X7 += state9 + 17;
            X0 += X1;
            X1 = (X1 << 39) | (X1 >> (64 - 39));
            X1 ^= X0;
            X2 += X3;
            X3 = (X3 << 30) | (X3 >> (64 - 30));
            X3 ^= X2;
            X4 += X5;
            X5 = (X5 << 34) | (X5 >> (64 - 34));
            X5 ^= X4;
            X6 += X7;
            X7 = (X7 << 24) | (X7 >> (64 - 24));
            X7 ^= X6;
            X2 += X1;
            X1 = (X1 << 13) | (X1 >> (64 - 13));
            X1 ^= X2;
            X4 += X7;
            X7 = (X7 << 50) | (X7 >> (64 - 50));
            X7 ^= X4;
            X6 += X5;
            X5 = (X5 << 10) | (X5 >> (64 - 10));
            X5 ^= X6;
            X0 += X3;
            X3 = (X3 << 17) | (X3 >> (64 - 17));
            X3 ^= X0;
            X4 += X1;
            X1 = (X1 << 25) | (X1 >> (64 - 25));
            X1 ^= X4;
            X6 += X3;
            X3 = (X3 << 29) | (X3 >> (64 - 29));
            X3 ^= X6;
            X0 += X5;
            X5 = (X5 << 39) | (X5 >> (64 - 39));
            X5 ^= X0;
            X2 += X7;
            X7 = (X7 << 43) | (X7 >> (64 - 43));
            X7 ^= X2;
            X6 += X1;
            X1 = (X1 << 8) | (X1 >> (64 - 8));
            X1 ^= X6;
            X0 += X7;
            X7 = (X7 << 35) | (X7 >> (64 - 35));
            X7 ^= X0;
            X2 += X5;
            X5 = (X5 << 56) | (X5 >> (64 - 56));
            X5 ^= X2;
            X4 += X3;
            X3 = (X3 << 22) | (X3 >> (64 - 22));
            X3 ^= X4;
            X0 += state3;
            X1 += state4;
            X2 += state5;
            X3 += state6;
            X4 += state7;
            X5 += state8 + state0;
            X6 += state9 + state1;
            X7 += state10 + 18;

            m_state[0] = X0 ^ data[0];
            m_state[1] = X1 ^ data[1];
            m_state[2] = X2 ^ data[2];
            m_state[3] = X3 ^ data[3];
            m_state[4] = X4 ^ data[4];
            m_state[5] = X5 ^ data[5];
            m_state[6] = X6 ^ data[6];
            m_state[7] = X7 ^ data[7];

            m_temp2 = state1 & ~0x4000000000000000U;
        }

    };
}