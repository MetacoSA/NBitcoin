using System;

namespace HashLib.Crypto.SHA3
{
    internal class SIMD224 : SIMD256Base
    {
        public SIMD224()
            : base(HashLib.HashSize.HashSize224)
        {
        }
    }

    internal class SIMD256 : SIMD256Base
    {
        public SIMD256()
            : base(HashLib.HashSize.HashSize256)
        {
        }
    }

    internal class SIMD384 : SIMD512Base
    {
        public SIMD384()
            : base(HashLib.HashSize.HashSize384)
        {
        }
    }

    internal class SIMD512 : SIMD512Base
    {
        public SIMD512()
            : base(HashLib.HashSize.HashSize512)
        {
        }
    }

    internal abstract class SIMDBase : BlockHash, ICryptoNotBuildIn
    {
        protected readonly uint[] m_state;

        public SIMDBase(HashLib.HashSize a_hash_size, int a_block_size)
            : base((int)a_hash_size, a_block_size)
        {
            m_state = new uint[BlockSize / 4];

            Initialize();
        }

        protected void FFT8(int[] a_y, int a_index, int a_stripe)
        {
            int u;
            int v;

            u = a_y[a_index + a_stripe * 0];
            v = a_y[a_index + a_stripe * 4];
            a_y[a_index + a_stripe * 0] = u + v;
            a_y[a_index + a_stripe * 4] = (u - v) << (2 * 0);

            u = a_y[a_index + a_stripe * 1];
            v = a_y[a_index + a_stripe * 5];
            a_y[a_index + a_stripe * 1] = u + v;
            a_y[a_index + a_stripe * 5] = (u - v) << (2 * 1);

            u = a_y[a_index + a_stripe * 2];
            v = a_y[a_index + a_stripe * 6];
            a_y[a_index + a_stripe * 2] = u + v;
            a_y[a_index + a_stripe * 6] = (u - v) << (2 * 2);

            u = a_y[a_index + a_stripe * 3];
            v = a_y[a_index + a_stripe * 7];
            a_y[a_index + a_stripe * 3] = u + v;
            a_y[a_index + a_stripe * 7] = (u - v) << (2 * 3); ;

            a_y[a_index + a_stripe * 6] = ((a_y[a_index + a_stripe * 6] & 255) - (a_y[a_index + a_stripe * 6] >> 8));
            a_y[a_index + a_stripe * 7] = ((a_y[a_index + a_stripe * 7] & 255) - (a_y[a_index + a_stripe * 7] >> 8));

            u = a_y[a_index + a_stripe * 0];
            v = a_y[a_index + a_stripe * 2];
            a_y[a_index + a_stripe * 0] = u + v;
            a_y[a_index + a_stripe * 2] = (u - v) << (2 * 0);

            u = a_y[a_index + a_stripe * 4];
            v = a_y[a_index + a_stripe * 6];
            a_y[a_index + a_stripe * 4] = u + v;
            a_y[a_index + a_stripe * 6] = (u - v) << (2 * 0);

            u = a_y[a_index + a_stripe * 1];
            v = a_y[a_index + a_stripe * 3];
            a_y[a_index + a_stripe * 1] = u + v;
            a_y[a_index + a_stripe * 3] = (u - v) << (2 * 2);

            u = a_y[a_index + a_stripe * 5];
            v = a_y[a_index + a_stripe * 7];
            a_y[a_index + a_stripe * 5] = u + v;
            a_y[a_index + a_stripe * 7] = (u - v) << (2 * 2);


            a_y[a_index + a_stripe * 7] = ((a_y[a_index + a_stripe * 7] & 255) - (a_y[a_index + a_stripe * 7] >> 8));

            u = a_y[a_index + a_stripe * 0];
            v = a_y[a_index + a_stripe * 1];
            a_y[a_index + a_stripe * 0] = u + v;
            a_y[a_index + a_stripe * 1] = (u - v) << (2 * 0);

            u = a_y[a_index + a_stripe * 2];
            v = a_y[a_index + a_stripe * 3];
            a_y[a_index + a_stripe * 2] = u + v;
            a_y[a_index + a_stripe * 3] = (u - v) << (2 * 0);

            u = a_y[a_index + a_stripe * 4];
            v = a_y[a_index + a_stripe * 5];
            a_y[a_index + a_stripe * 4] = u + v;
            a_y[a_index + a_stripe * 5] = (u - v) << (2 * 0);

            u = a_y[a_index + a_stripe * 6];
            v = a_y[a_index + a_stripe * 7];
            a_y[a_index + a_stripe * 6] = u + v;
            a_y[a_index + a_stripe * 7] = (u - v) << (2 * 0);

            a_y[a_index + a_stripe * 0] = ((a_y[a_index + a_stripe * 0] & 255) - (a_y[a_index + a_stripe * 0] >> 8));
            a_y[a_index + a_stripe * 0] = (a_y[a_index + a_stripe * 0] <= 128 ?
                a_y[a_index + a_stripe * 0] : a_y[a_index + a_stripe * 0] - 257);

            a_y[a_index + a_stripe * 1] = ((a_y[a_index + a_stripe * 1] & 255) - (a_y[a_index + a_stripe * 1] >> 8));
            a_y[a_index + a_stripe * 1] = (a_y[a_index + a_stripe * 1] <= 128 ?
                a_y[a_index + a_stripe * 1] : a_y[a_index + a_stripe * 1] - 257);

            a_y[a_index + a_stripe * 2] = ((a_y[a_index + a_stripe * 2] & 255) - (a_y[a_index + a_stripe * 2] >> 8));
            a_y[a_index + a_stripe * 2] = (a_y[a_index + a_stripe * 2] <= 128 ?
                a_y[a_index + a_stripe * 2] : a_y[a_index + a_stripe * 2] - 257);

            a_y[a_index + a_stripe * 3] = ((a_y[a_index + a_stripe * 3] & 255) - (a_y[a_index + a_stripe * 3] >> 8));
            a_y[a_index + a_stripe * 3] = (a_y[a_index + a_stripe * 3] <= 128 ?
                a_y[a_index + a_stripe * 3] : a_y[a_index + a_stripe * 3] - 257);

            a_y[a_index + a_stripe * 4] = ((a_y[a_index + a_stripe * 4] & 255) - (a_y[a_index + a_stripe * 4] >> 8));
            a_y[a_index + a_stripe * 4] = (a_y[a_index + a_stripe * 4] <= 128 ?
                a_y[a_index + a_stripe * 4] : a_y[a_index + a_stripe * 4] - 257);

            a_y[a_index + a_stripe * 5] = ((a_y[a_index + a_stripe * 5] & 255) - (a_y[a_index + a_stripe * 5] >> 8));
            a_y[a_index + a_stripe * 5] = (a_y[a_index + a_stripe * 5] <= 128 ?
                a_y[a_index + a_stripe * 5] : a_y[a_index + a_stripe * 5] - 257);

            a_y[a_index + a_stripe * 6] = ((a_y[a_index + a_stripe * 6] & 255) - (a_y[a_index + a_stripe * 6] >> 8));
            a_y[a_index + a_stripe * 6] = (a_y[a_index + a_stripe * 6] <= 128 ?
                a_y[a_index + a_stripe * 6] : a_y[a_index + a_stripe * 6] - 257);

            a_y[a_index + a_stripe * 7] = ((a_y[a_index + a_stripe * 7] & 255) - (a_y[a_index + a_stripe * 7] >> 8));
            a_y[a_index + a_stripe * 7] = (a_y[a_index + a_stripe * 7] <= 128 ?
                a_y[a_index + a_stripe * 7] : a_y[a_index + a_stripe * 7] - 257);
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            TransformBlock(a_data, a_index, false);
        }

        protected override byte[] GetResult()
        {
            return Converters.ConvertUIntsToBytes(m_state, 0, HashSize / 4);
        }

        protected override void Finish()
        {
            if (!m_buffer.IsEmpty)
                TransformBlock(m_buffer.GetBytesZeroPadded(), 0);

            ulong bits = m_processed_bytes * 8;

            byte[] pad = new byte[BlockSize];

            int padindex = 0;

            Converters.ConvertULongToBytes(bits, pad, padindex);

            TransformBlock(pad, 0, true);
        }

        protected abstract void TransformBlock(byte[] a_data, int a_index, bool a_final);
    }

    internal abstract class SIMD256Base : SIMDBase
    {
        #region Consts

        private static readonly uint[] IV_224 = 
        {
            0x33586e9f, 0x12fff033, 0xb2d9f64d, 0x6f8fea53, 0xde943106, 0x2742e439, 0x4fbab5ac, 0x62b9ff96,
            0x22e7b0af, 0xc862b3a8, 0x33e00cdc, 0x236b86a6, 0xf64ae77c, 0xfa373b76, 0x7dc1ee5b, 0x7fb29ce8
        };

        private static readonly uint[] IV_256 = 
        {
            0x4d567983, 0x07190ba9, 0x8474577b, 0x39d726e9, 0xaaf3d925, 0x3ee20b03, 0xafd5e751, 0xc96006d3,
            0xc2c2ba14, 0x49b3bcb4, 0xf67caf46, 0x668626c9, 0xe2eaa8d2, 0x1ff47833, 0xd0c661a5, 0x55693de1
        };

        private static readonly uint[] P4 = 
        {
             2, 34, 18, 50, 6, 38, 22, 54, 0, 32, 16, 48, 4, 36, 20, 52, 
             14, 46, 30, 62, 10, 42, 26, 58, 12, 44, 28, 60, 8, 40, 24, 56, 
             15, 47, 31, 63, 13, 45, 29, 61, 3, 35, 19, 51, 1, 33, 17, 49, 
             9, 41, 25, 57, 11, 43, 27, 59, 5, 37, 21, 53, 7, 39, 23, 55, 
             8, 40, 24, 56, 4, 36, 20, 52, 14, 46, 30, 62, 2, 34, 18, 50, 
             6, 38, 22, 54, 10, 42, 26, 58, 0, 32, 16, 48, 12, 44, 28, 60, 
             70, 102, 86, 118, 64, 96, 80, 112, 72, 104, 88, 120, 78, 110, 94, 126, 
             76, 108, 92, 124, 74, 106, 90, 122, 66, 98, 82, 114, 68, 100, 84, 116
        };

        private static readonly uint[] Q4 = 
        {
             66, 98, 82, 114, 70, 102, 86, 118, 64, 96, 80, 112, 68, 100, 84, 116, 
             78, 110, 94, 126, 74, 106, 90, 122, 76, 108, 92, 124, 72, 104, 88, 120, 
             79, 111, 95, 127, 77, 109, 93, 125, 67, 99, 83, 115, 65, 97, 81, 113, 
             73, 105, 89, 121, 75, 107, 91, 123, 69, 101, 85, 117, 71, 103, 87, 119, 
             9, 41, 25, 57, 5, 37, 21, 53, 15, 47, 31, 63, 3, 35, 19, 51, 
             7, 39, 23, 55, 11, 43, 27, 59, 1, 33, 17, 49, 13, 45, 29, 61, 
             71, 103, 87, 119, 65, 97, 81, 113, 73, 105, 89, 121, 79, 111, 95, 127, 
             77, 109, 93, 125, 75, 107, 91, 123, 67, 99, 83, 115, 69, 101, 85, 117
        };

        private static readonly int[] FFT64_8_8_Twiddle = 
        {
            1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 4, 8, 16, 32, 64, 128,
            1, 60, 2, 120, 4, -17, 8, -34, 1, 120, 8, -68, 64, -30, -2, 17,
            1, 46, 60, -67, 2, 92, 120, 123, 1, 92, -17, -22, 32, 117, -30, 67,
            1, -67, 120, -73, 8, -22, -68, -70, 1, 123, -34, -70, 128, 67, 17, 35
        };

        private static readonly int[] FFT128_2_64_Twiddle =  
        {
            1, -118, 46, -31, 60, 116, -67, -61, 2, 21, 92, -62, 120, -25, 123, -122,
            4, 42, -73, -124, -17, -50, -11, 13, 8, 84, 111, 9, -34, -100, -22, 26,
            16, -89, -35, 18, -68, 57, -44, 52, 32, 79, -70, 36, 121, 114, -88, 104,
            64, -99, 117, 72, -15, -29, 81, -49, 128, 59, -23, -113, -30, -58, -95, -98
        };

        #endregion

        public SIMD256Base(HashLib.HashSize a_hash_size)
            : base(a_hash_size, 64)
        {
        }

        private void Round4(int[] a_y, int a_i, int a_r, int a_s, int a_t, int a_u)
        {
            int code = a_i < 2 ? 185 : 233;
            uint[] w = new uint[32];
            int i32 = a_i * 32;

            for (int a = 0; a < 32; a++)
                w[a] = (((uint)(a_y[Q4[i32 + a]] * code)) << 16) | (((uint)(a_y[P4[i32 + a]] * code)) & 0xffff);

            uint[] R = new uint[4];

            for (int j = 0; j < 4; j++)
                R[j] = ((m_state[0 + j] << a_r) | (m_state[0 + j] >> (32 - a_r)));

            for (int j = 0; j < 4; j++)
            {
                m_state[12 + j] = m_state[12 + j] + w[0 + j] + ((((m_state[4 + j]) ^ (m_state[8 + j])) &
                    (m_state[0 + j])) ^ (m_state[8 + j]));
                m_state[12 + j] = ((m_state[12 + j] << a_s) | (m_state[12 + j] >> (32 - a_s))) + R[(j ^
                    (((8 * a_i + 0) % 3) + 1))];
                m_state[0 + j] = R[j];
            }

            for (int j = 0; j < 4; j++)
                R[j] = ((m_state[12 + j] << a_s) | (m_state[12 + j] >> (32 - a_s)));

            for (int j = 0; j < 4; j++)
            {
                m_state[8 + j] = m_state[8 + j] + w[4 + j] + ((((m_state[0 + j]) ^ (m_state[4 + j])) & (m_state[12 + j])) ^ (m_state[4 + j]));
                m_state[8 + j] = ((m_state[8 + j] << a_t) | (m_state[8 + j] >> (32 - a_t))) + R[(j ^ (((8 * a_i + 1) % 3) + 1))];
                m_state[12 + j] = R[j];
            }

            for (int j = 0; j < 4; j++)
                R[j] = ((m_state[8 + j] << a_t) | (m_state[8 + j] >> (32 - a_t)));

            for (int j = 0; j < 4; j++)
            {
                m_state[4 + j] = m_state[4 + j] + w[8 + j] + ((((m_state[12 + j]) ^ (m_state[0 + j])) & (m_state[8 + j])) ^ (m_state[0 + j]));
                m_state[4 + j] = ((m_state[4 + j] << a_u) | (m_state[4 + j] >> (32 - a_u))) + R[(j ^ (((8 * a_i + 2) % 3) + 1))];
                m_state[8 + j] = R[j];
            }

            for (int j = 0; j < 4; j++)
                R[j] = ((m_state[4 + j] << a_u) | (m_state[4 + j] >> (32 - a_u)));

            for (int j = 0; j < 4; j++)
            {
                m_state[0 + j] = m_state[0 + j] + w[12 + j] + ((((m_state[8 + j]) ^ (m_state[12 + j])) & (m_state[4 + j])) ^ (m_state[12 + j]));
                m_state[0 + j] = ((m_state[0 + j] << a_r) | (m_state[0 + j] >> (32 - a_r))) + R[(j ^ (((8 * a_i + 3) % 3) + 1))];
                m_state[4 + j] = R[j];
            }

            for (int j = 0; j < 4; j++)
                R[j] = ((m_state[0 + j] << a_r) | (m_state[0 + j] >> (32 - a_r)));

            for (int j = 0; j < 4; j++)
            {
                m_state[12 + j] = m_state[12 + j] + w[16 + j] + (((m_state[8 + j]) & (m_state[4 + j])) | (((m_state[8 + j]) | (m_state[4 + j])) & (m_state[0 + j])));
                m_state[12 + j] = ((m_state[12 + j] << a_s) | (m_state[12 + j] >> (32 - a_s))) + R[(j ^ (((8 * a_i + 4) % 3) + 1))];
                m_state[0 + j] = R[j];
            }

            for (int j = 0; j < 4; j++)
                R[j] = ((m_state[12 + j] << a_s) | (m_state[12 + j] >> (32 - a_s)));

            for (int j = 0; j < 4; j++)
            {
                m_state[8 + j] = m_state[8 + j] + w[20 + j] + (((m_state[4 + j]) & (m_state[0 + j])) | (((m_state[4 + j]) | (m_state[0 + j])) & (m_state[12 + j])));
                m_state[8 + j] = ((m_state[8 + j] << a_t) | (m_state[8 + j] >> (32 - a_t))) + R[(j ^ (((8 * a_i + 5) % 3) + 1))];
                m_state[12 + j] = R[j];
            }

            for (int j = 0; j < 4; j++)
                R[j] = ((m_state[8 + j] << a_t) | (m_state[8 + j] >> (32 - a_t)));

            for (int j = 0; j < 4; j++)
            {
                m_state[4 + j] = m_state[4 + j] + w[24 + j] + (((m_state[0 + j]) & (m_state[12 + j])) | (((m_state[0 + j]) | (m_state[12 + j])) & (m_state[8 + j])));
                m_state[4 + j] = ((m_state[4 + j] << a_u) | (m_state[4 + j] >> (32 - a_u))) + R[(j ^ (((8 * a_i + 6) % 3) + 1))];
                m_state[8 + j] = R[j];
            }

            for (int j = 0; j < 4; j++)
                R[j] = ((m_state[4 + j] << a_u) | (m_state[4 + j] >> (32 - a_u)));

            for (int j = 0; j < 4; j++)
            {
                m_state[0 + j] = m_state[0 + j] + w[28 + j] + (((m_state[12 + j]) & (m_state[8 + j])) | (((m_state[12 + j]) | (m_state[8 + j])) & (m_state[4 + j])));
                m_state[0 + j] = ((m_state[0 + j] << a_r) | (m_state[0 + j] >> (32 - a_r))) + R[(j ^ (((8 * a_i + 7) % 3) + 1))];
                m_state[4 + j] = R[j];
            }
        }

        private void FFT64(int[] a_y, int a_index)
        {
            for (int i = 0; i < 8; i++)
                FFT8(a_y, a_index + i, 8);

            for (int i = 8; i < 64; i++)
            {
                if ((i & 7) != 0)
                {
                    a_y[a_index + i] = ((a_y[a_index + i] * FFT64_8_8_Twiddle[i] & 255) -
                        (a_y[a_index + i] * FFT64_8_8_Twiddle[i] >> 8));
                }
            }

            for (int i = 0; i < 8; i++)
                FFT8(a_y, a_index + 8 * i, 1);
        }

        protected override void TransformBlock(byte[] a_data, int a_index, bool a_final)
        {
            int[] y = new int[128];

            for (int i = 0; i < 64; i++)
                y[i] = a_data[i + a_index];

            int tmp = y[63];

            for (int i = 0; i < 63; i++)
                y[64 + i] = ((y[i] * FFT128_2_64_Twiddle[i] & 255) - (y[i] * FFT128_2_64_Twiddle[i] >> 8));

            if (a_final)
            {
                int tmp2 = y[61];
                y[61] = (((tmp2 + 1) & 255) - ((tmp2 + 1) >> 8));
                y[125] = (((tmp2 - 1) * FFT128_2_64_Twiddle[61] & 255) - ((tmp2 - 1) * FFT128_2_64_Twiddle[61] >> 8));
            }

            y[63] = (((tmp + 1) & 255) - ((tmp + 1) >> 8));
            y[127] = (((tmp - 1) * FFT128_2_64_Twiddle[63] & 255) - ((tmp - 1) * FFT128_2_64_Twiddle[63] >> 8));

            FFT64(y, 0);
            FFT64(y, 64);

            uint[] state = new uint[16];
            Array.Copy(m_state, 0, state, 0, m_state.Length);

            uint[] message = Converters.ConvertBytesToUInts(a_data, a_index, 64);

            for (int i = 0; i < 16; i++)
                m_state[i] ^= message[i];

            Round4(y, 0, 3, 23, 17, 27);
            Round4(y, 1, 28, 19, 22, 7);
            Round4(y, 2, 29, 9, 15, 5);
            Round4(y, 3, 4, 13, 10, 25);

            uint[] R = new uint[4];

            for (int j = 0; j < 4; j++)
                R[j] = ((m_state[0 + j] << 4) | (m_state[0 + j] >> (32 - 4)));

            for (int j = 0; j < 4; j++)
            {
                m_state[12 + j] = m_state[12 + j] + state[0 + j] + ((((m_state[4 + j]) ^ (m_state[8 + j])) &
                    (m_state[0 + j])) ^ (m_state[8 + j]));
                m_state[12 + j] = ((m_state[12 + j] << 13) | (m_state[12 + j] >> (32 - 13))) + R[(j ^ (((32) % 3) + 1))];
                m_state[0 + j] = R[j];
            }


            for (int j = 0; j < 4; j++)
                R[j] = ((m_state[12 + j] << 13) | (m_state[12 + j] >> (32 - 13)));

            for (int j = 0; j < 4; j++)
            {
                m_state[8 + j] = m_state[8 + j] + state[4 + j] + ((((m_state[0 + j]) ^ (m_state[4 + j])) &
                    (m_state[12 + j])) ^ (m_state[4 + j]));
                m_state[8 + j] = ((m_state[8 + j] << 10) | (m_state[8 + j] >> (32 - 10))) + R[(j ^ (((33) % 3) + 1))];
                m_state[12 + j] = R[j];
            }

            for (int j = 0; j < 4; j++)
                R[j] = ((m_state[8 + j] << 10) | (m_state[8 + j] >> (32 - 10)));

            for (int j = 0; j < 4; j++)
            {
                m_state[4 + j] = m_state[4 + j] + state[8 + j] + ((((m_state[12 + j]) ^ (m_state[0 + j])) &
                    (m_state[8 + j])) ^ (m_state[0 + j]));
                m_state[4 + j] = ((m_state[4 + j] << 25) | (m_state[4 + j] >> (32 - 25))) + R[(j ^ (((34) % 3) + 1))];
                m_state[8 + j] = R[j];
            }

            for (int j = 0; j < 4; j++)
                R[j] = ((m_state[4 + j] << 25) | (m_state[4 + j] >> (32 - 25)));

            for (int j = 0; j < 4; j++)
            {
                m_state[0 + j] = m_state[0 + j] + state[12 + j] + ((((m_state[8 + j]) ^ (m_state[12 + j])) &
                    (m_state[4 + j])) ^ (m_state[12 + j]));
                m_state[0 + j] = ((m_state[0 + j] << 4) | (m_state[0 + j] >> (32 - 4))) + R[(j ^ (((35) % 3) + 1))];
                m_state[4 + j] = R[j];
            }
        }

        public override void Initialize()
        {
            if (HashSize == 28)
                Array.Copy(IV_224, 0, m_state, 0, IV_224.Length);
            else
                Array.Copy(IV_256, 0, m_state, 0, IV_256.Length);

            base.Initialize();
        }
    }

    internal abstract class SIMD512Base : SIMDBase
    {
        #region Consts

        private static readonly uint[] IV_384 = 
        {
            0x8a36eebc, 0x94a3bd90, 0xd1537b83, 0xb25b070b, 0xf463f1b5, 0xb6f81e20, 0x0055c339, 0xb4d144d1,
            0x7360ca61, 0x18361a03, 0x17dcb4b9, 0x3414c45a, 0xa699a9d2, 0xe39e9664, 0x468bfe77, 0x51d062f8,
            0xb9e3bfe8, 0x63bece2a, 0x8fe506b9, 0xf8cc4ac2, 0x7ae11542, 0xb1aadda1, 0x64b06794, 0x28d2f462,
            0xe64071ec, 0x1deb91a8, 0x8ac8db23, 0x3f782ab5, 0x039b5cb8, 0x71ddd962, 0xfade2cea, 0x1416df71
        };

        private static readonly uint[] IV_512 = 
        {
            0x0ba16b95, 0x72f999ad, 0x9fecc2ae, 0xba3264fc, 0x5e894929, 0x8e9f30e5, 0x2f1daa37, 0xf0f2c558,
            0xac506643, 0xa90635a5, 0xe25b878b, 0xaab7878f, 0x88817f7a, 0x0a02892b, 0x559a7550, 0x598f657e,
            0x7eef60a1, 0x6b70e3e8, 0x9c1714d1, 0xb958e2a8, 0xab02675e, 0xed1c014f, 0xcd8d65bb, 0xfdb7a257,
            0x09254899, 0xd699c7bc, 0x9019b6dc, 0x2b9022e4, 0x8fa14956, 0x21bf9bd3, 0xb94d0943, 0x6ffddc22
        };

        private static readonly uint[] P8 = 
        {
             2, 66, 34, 98, 18, 82, 50, 114, 6, 70, 38, 102, 22, 86, 54, 118, 
             0, 64, 32, 96, 16, 80, 48, 112, 4, 68, 36, 100, 20, 84, 52, 116, 
             14, 78, 46, 110, 30, 94, 62, 126, 10, 74, 42, 106, 26, 90, 58, 122, 
             12, 76, 44, 108, 28, 92, 60, 124, 8, 72, 40, 104, 24, 88, 56, 120, 
             15, 79, 47, 111, 31, 95, 63, 127, 13, 77, 45, 109, 29, 93, 61, 125, 
             3, 67, 35, 99, 19, 83, 51, 115, 1, 65, 33, 97, 17, 81, 49, 113, 
             9, 73, 41, 105, 25, 89, 57, 121, 11, 75, 43, 107, 27, 91, 59, 123, 
             5, 69, 37, 101, 21, 85, 53, 117, 7, 71, 39, 103, 23, 87, 55, 119, 
             8, 72, 40, 104, 24, 88, 56, 120, 4, 68, 36, 100, 20, 84, 52, 116, 
             14, 78, 46, 110, 30, 94, 62, 126, 2, 66, 34, 98, 18, 82, 50, 114, 
             6, 70, 38, 102, 22, 86, 54, 118, 10, 74, 42, 106, 26, 90, 58, 122, 
             0, 64, 32, 96, 16, 80, 48, 112, 12, 76, 44, 108, 28, 92, 60, 124, 
             134, 198, 166, 230, 150, 214, 182, 246, 128, 192, 160, 224, 144, 208, 176, 240, 
             136, 200, 168, 232, 152, 216, 184, 248, 142, 206, 174, 238, 158, 222, 190, 254, 
             140, 204, 172, 236, 156, 220, 188, 252, 138, 202, 170, 234, 154, 218, 186, 250, 
             130, 194, 162, 226, 146, 210, 178, 242, 132, 196, 164, 228, 148, 212, 180, 244
        };

        private static readonly uint[] Q8 = 
        {
             130, 194, 162, 226, 146, 210, 178, 242, 134, 198, 166, 230, 150, 214, 182, 246, 
             128, 192, 160, 224, 144, 208, 176, 240, 132, 196, 164, 228, 148, 212, 180, 244, 
             142, 206, 174, 238, 158, 222, 190, 254, 138, 202, 170, 234, 154, 218, 186, 250, 
             140, 204, 172, 236, 156, 220, 188, 252, 136, 200, 168, 232, 152, 216, 184, 248, 
             143, 207, 175, 239, 159, 223, 191, 255, 141, 205, 173, 237, 157, 221, 189, 253, 
             131, 195, 163, 227, 147, 211, 179, 243, 129, 193, 161, 225, 145, 209, 177, 241, 
             137, 201, 169, 233, 153, 217, 185, 249, 139, 203, 171, 235, 155, 219, 187, 251, 
             133, 197, 165, 229, 149, 213, 181, 245, 135, 199, 167, 231, 151, 215, 183, 247, 
             9, 73, 41, 105, 25, 89, 57, 121, 5, 69, 37, 101, 21, 85, 53, 117, 
             15, 79, 47, 111, 31, 95, 63, 127, 3, 67, 35, 99, 19, 83, 51, 115, 
             7, 71, 39, 103, 23, 87, 55, 119, 11, 75, 43, 107, 27, 91, 59, 123, 
             1, 65, 33, 97, 17, 81, 49, 113, 13, 77, 45, 109, 29, 93, 61, 125, 
             135, 199, 167, 231, 151, 215, 183, 247, 129, 193, 161, 225, 145, 209, 177, 241, 
             137, 201, 169, 233, 153, 217, 185, 249, 143, 207, 175, 239, 159, 223, 191, 255, 
             141, 205, 173, 237, 157, 221, 189, 253, 139, 203, 171, 235, 155, 219, 187, 251, 
             131, 195, 163, 227, 147, 211, 179, 243, 133, 197, 165, 229, 149, 213, 181, 245
        };

        private static readonly int[] FFT128_8_16_Twiddle =  
        {
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 
            1, 60, 2, 120, 4, -17, 8, -34, 16, -68, 32, 121, 64, -15, 128, -30, 
            1, 46, 60, -67, 2, 92, 120, 123, 4, -73, -17, -11, 8, 111, -34, -22, 
            1, -67, 120, -73, 8, -22, -68, -70, 64, 81, -30, -46, -2, -123, 17, -111, 
            1, -118, 46, -31, 60, 116, -67, -61, 2, 21, 92, -62, 120, -25, 123, -122, 
            1, 116, 92, -122, -17, 84, -22, 18, 32, 114, 117, -49, -30, 118, 67, 62, 
            1, -31, -67, 21, 120, -122, -73, -50, 8, 9, -22, -89, -68, 52, -70, 114, 
            1, -61, 123, -50, -34, 18, -70, -99, 128, -98, 67, 25, 17, -9, 35, -79
        };

        private static readonly int[] FFT256_2_128_Twiddle =  
        {
            1, 41, -118, 45, 46, 87, -31, 14, 60, -110, 116, -127, -67, 80, -61, 69, 
            2, 82, 21, 90, 92, -83, -62, 28, 120, 37, -25, 3, 123, -97, -122, -119, 
            4, -93, 42, -77, -73, 91, -124, 56, -17, 74, -50, 6, -11, 63, 13, 19, 
            8, 71, 84, 103, 111, -75, 9, 112, -34, -109, -100, 12, -22, 126, 26, 38, 
            16, -115, -89, -51, -35, 107, 18, -33, -68, 39, 57, 24, -44, -5, 52, 76, 
            32, 27, 79, -102, -70, -43, 36, -66, 121, 78, 114, 48, -88, -10, 104, -105, 
            64, 54, -99, 53, 117, -86, 72, 125, -15, -101, -29, 96, 81, -20, -49, 47, 
            128, 108, 59, 106, -23, 85, -113, -7, -30, 55, -58, -65, -95, -40, -98, 94
        };

        private static readonly int[] p8_xor = 
        {
            1, 6, 2, 3, 5, 7, 4
        };

        #endregion

        public SIMD512Base(HashLib.HashSize a_hash_size)
            : base(a_hash_size, 128)
        {
        }

        private void Round8(int[] a_y, int a_i, int a_r, int a_s, int a_t, int a_u)
        {
            int code = a_i < 2 ? 185 : 233;
            uint[] w = new uint[64];

            for (int a = 0; a < 64; a++)
                w[a] = (((uint)(a_y[Q8[64 * a_i + a]] * code)) << 16) | (((uint)(a_y[P8[64 * a_i + a]] * code)) & 0xffff);

            uint[] R = new uint[8];

            for (int j = 0; j < 8; j++)
                R[j] = ((m_state[0 + j] << a_r) | (m_state[0 + j] >> (32 - a_r)));

            for (int j = 0; j < 8; j++)
            {
                m_state[24 + j] = m_state[24 + j] + w[0 + j] + ((((m_state[8 + j]) ^ (m_state[16 + j])) &
                    (m_state[0 + j])) ^ (m_state[16 + j]));
                m_state[24 + j] = ((m_state[24 + j] << a_s) | (m_state[24 + j] >> (32 - a_s))) + R[j ^ p8_xor[(8 * a_i + 0) % 7]];
                m_state[0 + j] = R[j];
            }

            for (int j = 0; j < 8; j++)
                R[j] = ((m_state[24 + j] << a_s) | (m_state[24 + j] >> (32 - a_s)));

            for (int j = 0; j < 8; j++)
            {
                m_state[16 + j] = m_state[16 + j] + w[8 + j] + ((((m_state[0 + j]) ^ (m_state[8 + j])) &
                    (m_state[24 + j])) ^ (m_state[8 + j]));
                m_state[16 + j] = ((m_state[16 + j] << a_t) | (m_state[16 + j] >> (32 - a_t))) + R[j ^ p8_xor[(8 * a_i + 1) % 7]];
                m_state[24 + j] = R[j];
            }

            for (int j = 0; j < 8; j++)
                R[j] = ((m_state[16 + j] << a_t) | (m_state[16 + j] >> (32 - a_t)));

            for (int j = 0; j < 8; j++)
            {
                m_state[8 + j] = m_state[8 + j] + w[16 + j] + ((((m_state[24 + j]) ^ (m_state[0 + j])) &
                    (m_state[16 + j])) ^ (m_state[0 + j]));
                m_state[8 + j] = ((m_state[8 + j] << a_u) | (m_state[8 + j] >> (32 - a_u))) + R[j ^ p8_xor[(8 * a_i + 2) % 7]];
                m_state[16 + j] = R[j];
            }

            for (int j = 0; j < 8; j++)
                R[j] = ((m_state[8 + j] << a_u) | (m_state[8 + j] >> (32 - a_u)));

            for (int j = 0; j < 8; j++)
            {
                m_state[0 + j] = m_state[0 + j] + w[24 + j] + ((((m_state[16 + j]) ^ (m_state[24 + j])) &
                    (m_state[8 + j])) ^ (m_state[24 + j]));
                m_state[0 + j] = ((m_state[0 + j] << a_r) | (m_state[0 + j] >> (32 - a_r))) + R[j ^ p8_xor[(8 * a_i + 3) % 7]];
                m_state[8 + j] = R[j];
            }

            for (int j = 0; j < 8; j++)
                R[j] = ((m_state[0 + j] << a_r) | (m_state[0 + j] >> (32 - a_r)));

            for (int j = 0; j < 8; j++)
            {
                m_state[24 + j] = m_state[24 + j] + w[32 + j] + (((m_state[16 + j]) & (m_state[8 + j])) |
                    (((m_state[16 + j]) | (m_state[8 + j])) & (m_state[0 + j])));
                m_state[24 + j] = ((m_state[24 + j] << a_s) | (m_state[24 + j] >> (32 - a_s))) + R[j ^ p8_xor[(8 * a_i + 4) % 7]];
                m_state[0 + j] = R[j];
            }

            for (int j = 0; j < 8; j++)
                R[j] = ((m_state[24 + j] << a_s) | (m_state[24 + j] >> (32 - a_s)));

            for (int j = 0; j < 8; j++)
            {
                m_state[16 + j] = m_state[16 + j] + w[40 + j] + (((m_state[8 + j]) & (m_state[0 + j])) |
                    (((m_state[8 + j]) | (m_state[0 + j])) & (m_state[24 + j])));
                m_state[16 + j] = ((m_state[16 + j] << a_t) | (m_state[16 + j] >> (32 - a_t))) + R[j ^ p8_xor[(8 * a_i + 5) % 7]];
                m_state[24 + j] = R[j];
            }

            for (int j = 0; j < 8; j++)
                R[j] = ((m_state[16 + j] << a_t) | (m_state[16 + j] >> (32 - a_t)));

            for (int j = 0; j < 8; j++)
            {
                m_state[8 + j] = m_state[8 + j] + w[48 + j] + (((m_state[0 + j]) & (m_state[24 + j])) |
                    (((m_state[0 + j]) | (m_state[24 + j])) & (m_state[16 + j])));
                m_state[8 + j] = ((m_state[8 + j] << a_u) | (m_state[8 + j] >> (32 - a_u))) + R[j ^ p8_xor[(8 * a_i + 6) % 7]];
                m_state[16 + j] = R[j];
            }

            for (int j = 0; j < 8; j++)
                R[j] = ((m_state[8 + j] << a_u) | (m_state[8 + j] >> (32 - a_u)));

            for (int j = 0; j < 8; j++)
            {
                m_state[0 + j] = m_state[0 + j] + w[56 + j] + (((m_state[24 + j]) & (m_state[16 + j])) |
                    (((m_state[24 + j]) | (m_state[16 + j])) & (m_state[8 + j])));
                m_state[0 + j] = ((m_state[0 + j] << a_r) | (m_state[0 + j] >> (32 - a_r))) + R[j ^ p8_xor[(8 * a_i + 7) % 7]];
                m_state[8 + j] = R[j];
            }
        }

        private void FFT16(int[] a_y, int a_index, int stripe)
        {
            int u;
            int v;

            u = a_y[a_index + stripe * 0];
            v = a_y[a_index + stripe * 8];
            a_y[a_index + stripe * 0] = u + v;
            a_y[a_index + stripe * 8] = (u - v) << 0; ;
            u = a_y[a_index + stripe * 1];
            v = a_y[a_index + stripe * 9];
            a_y[a_index + stripe * 1] = u + v;
            a_y[a_index + stripe * 9] = (u - v) << 1; ;
            u = a_y[a_index + stripe * 2];
            v = a_y[a_index + stripe * 10];
            a_y[a_index + stripe * 2] = u + v;
            a_y[a_index + stripe * 10] = (u - v) << 2; ;
            u = a_y[a_index + stripe * 3];
            v = a_y[a_index + stripe * 11];
            a_y[a_index + stripe * 3] = u + v;
            a_y[a_index + stripe * 11] = (u - v) << 3; ;
            u = a_y[a_index + stripe * 4];
            v = a_y[a_index + stripe * 12];
            a_y[a_index + stripe * 4] = u + v;
            a_y[a_index + stripe * 12] = (u - v) << 4; ;
            u = a_y[a_index + stripe * 5];
            v = a_y[a_index + stripe * 13];
            a_y[a_index + stripe * 5] = u + v;
            a_y[a_index + stripe * 13] = (u - v) << 5; ;
            u = a_y[a_index + stripe * 6];
            v = a_y[a_index + stripe * 14];
            a_y[a_index + stripe * 6] = u + v;
            a_y[a_index + stripe * 14] = (u - v) << 6; ;
            u = a_y[a_index + stripe * 7];
            v = a_y[a_index + stripe * 15];
            a_y[a_index + stripe * 7] = u + v;
            a_y[a_index + stripe * 15] = (u - v) << 7; ;

            a_y[a_index + stripe * 11] = ((a_y[a_index + stripe * 11] & 255) - (a_y[a_index + stripe * 11] >> 8));
            a_y[a_index + stripe * 12] = ((a_y[a_index + stripe * 12] & 255) - (a_y[a_index + stripe * 12] >> 8));
            a_y[a_index + stripe * 13] = ((a_y[a_index + stripe * 13] & 255) - (a_y[a_index + stripe * 13] >> 8));
            a_y[a_index + stripe * 14] = ((a_y[a_index + stripe * 14] & 255) - (a_y[a_index + stripe * 14] >> 8));
            a_y[a_index + stripe * 15] = ((a_y[a_index + stripe * 15] & 255) - (a_y[a_index + stripe * 15] >> 8));

            u = a_y[a_index + stripe * 0];
            v = a_y[a_index + stripe * 4];
            a_y[a_index + stripe * 0] = u + v;
            a_y[a_index + stripe * 4] = (u - v) << 0; ;
            u = a_y[a_index + stripe * 8];
            v = a_y[a_index + stripe * 12];
            a_y[a_index + stripe * 8] = u + v;
            a_y[a_index + stripe * 12] = (u - v) << 0; ;
            u = a_y[a_index + stripe * 1];
            v = a_y[a_index + stripe * 5];
            a_y[a_index + stripe * 1] = u + v;
            a_y[a_index + stripe * 5] = (u - v) << 2; ;
            u = a_y[a_index + stripe * 9];
            v = a_y[a_index + stripe * 13];
            a_y[a_index + stripe * 9] = u + v;
            a_y[a_index + stripe * 13] = (u - v) << 2; ;
            u = a_y[a_index + stripe * 2];
            v = a_y[a_index + stripe * 6];
            a_y[a_index + stripe * 2] = u + v;
            a_y[a_index + stripe * 6] = (u - v) << 4; ;
            u = a_y[a_index + stripe * 10];
            v = a_y[a_index + stripe * 14];
            a_y[a_index + stripe * 10] = u + v;
            a_y[a_index + stripe * 14] = (u - v) << 4; ;
            u = a_y[a_index + stripe * 3];
            v = a_y[a_index + stripe * 7];
            a_y[a_index + stripe * 3] = u + v;
            a_y[a_index + stripe * 7] = (u - v) << 6; ;
            u = a_y[a_index + stripe * 11];
            v = a_y[a_index + stripe * 15];
            a_y[a_index + stripe * 11] = u + v;
            a_y[a_index + stripe * 15] = (u - v) << 6; ;

            a_y[a_index + stripe * 5] = ((a_y[a_index + stripe * 5] & 255) - (a_y[a_index + stripe * 5] >> 8));
            a_y[a_index + stripe * 7] = ((a_y[a_index + stripe * 7] & 255) - (a_y[a_index + stripe * 7] >> 8));
            a_y[a_index + stripe * 13] = ((a_y[a_index + stripe * 13] & 255) - (a_y[a_index + stripe * 13] >> 8));
            a_y[a_index + stripe * 15] = ((a_y[a_index + stripe * 15] & 255) - (a_y[a_index + stripe * 15] >> 8));

            u = a_y[a_index + stripe * 0];
            v = a_y[a_index + stripe * 2];
            a_y[a_index + stripe * 0] = u + v;
            a_y[a_index + stripe * 2] = (u - v) << 0; ;
            u = a_y[a_index + stripe * 4];
            v = a_y[a_index + stripe * 6];
            a_y[a_index + stripe * 4] = u + v;
            a_y[a_index + stripe * 6] = (u - v) << 0; ;
            u = a_y[a_index + stripe * 8];
            v = a_y[a_index + stripe * 10];
            a_y[a_index + stripe * 8] = u + v;
            a_y[a_index + stripe * 10] = (u - v) << 0; ;
            u = a_y[a_index + stripe * 12];
            v = a_y[a_index + stripe * 14];
            a_y[a_index + stripe * 12] = u + v;
            a_y[a_index + stripe * 14] = (u - v) << 0; ;
            u = a_y[a_index + stripe * 1];
            v = a_y[a_index + stripe * 3];
            a_y[a_index + stripe * 1] = u + v;
            a_y[a_index + stripe * 3] = (u - v) << 4; ;
            u = a_y[a_index + stripe * 5];
            v = a_y[a_index + stripe * 7];
            a_y[a_index + stripe * 5] = u + v;
            a_y[a_index + stripe * 7] = (u - v) << 4; ;
            u = a_y[a_index + stripe * 9];
            v = a_y[a_index + stripe * 11];
            a_y[a_index + stripe * 9] = u + v;
            a_y[a_index + stripe * 11] = (u - v) << 4; ;
            u = a_y[a_index + stripe * 13];
            v = a_y[a_index + stripe * 15];
            a_y[a_index + stripe * 13] = u + v;
            a_y[a_index + stripe * 15] = (u - v) << 4; ;

            u = a_y[a_index + stripe * 0];
            v = a_y[a_index + stripe * 1];
            a_y[a_index + stripe * 0] = u + v;
            a_y[a_index + stripe * 1] = (u - v) << 0; ;
            u = a_y[a_index + stripe * 2];
            v = a_y[a_index + stripe * 3];
            a_y[a_index + stripe * 2] = u + v;
            a_y[a_index + stripe * 3] = (u - v) << 0; ;
            u = a_y[a_index + stripe * 4];
            v = a_y[a_index + stripe * 5];
            a_y[a_index + stripe * 4] = u + v;
            a_y[a_index + stripe * 5] = (u - v) << 0; ;
            u = a_y[a_index + stripe * 6];
            v = a_y[a_index + stripe * 7];
            a_y[a_index + stripe * 6] = u + v;
            a_y[a_index + stripe * 7] = (u - v) << 0; ;
            u = a_y[a_index + stripe * 8];
            v = a_y[a_index + stripe * 9];
            a_y[a_index + stripe * 8] = u + v;
            a_y[a_index + stripe * 9] = (u - v) << 0; ;
            u = a_y[a_index + stripe * 10];
            v = a_y[a_index + stripe * 11];
            a_y[a_index + stripe * 10] = u + v;
            a_y[a_index + stripe * 11] = (u - v) << 0; ;
            u = a_y[a_index + stripe * 12];
            v = a_y[a_index + stripe * 13];
            a_y[a_index + stripe * 12] = u + v;
            a_y[a_index + stripe * 13] = (u - v) << 0; ;
            u = a_y[a_index + stripe * 14];
            v = a_y[a_index + stripe * 15];
            a_y[a_index + stripe * 14] = u + v;
            a_y[a_index + stripe * 15] = (u - v) << 0; ;

            a_y[a_index + stripe * 0] = ((a_y[a_index + stripe * 0] & 255) - (a_y[a_index + stripe * 0] >> 8));
            a_y[a_index + stripe * 0] = (a_y[a_index + stripe * 0] <= 128 ?
                a_y[a_index + stripe * 0] : a_y[a_index + stripe * 0] - 257); ;
            a_y[a_index + stripe * 1] = ((a_y[a_index + stripe * 1] & 255) - (a_y[a_index + stripe * 1] >> 8));
            a_y[a_index + stripe * 1] = (a_y[a_index + stripe * 1] <= 128 ?
                a_y[a_index + stripe * 1] : a_y[a_index + stripe * 1] - 257); ;
            a_y[a_index + stripe * 2] = ((a_y[a_index + stripe * 2] & 255) - (a_y[a_index + stripe * 2] >> 8));
            a_y[a_index + stripe * 2] = (a_y[a_index + stripe * 2] <= 128 ?
                a_y[a_index + stripe * 2] : a_y[a_index + stripe * 2] - 257); ;
            a_y[a_index + stripe * 3] = ((a_y[a_index + stripe * 3] & 255) - (a_y[a_index + stripe * 3] >> 8));
            a_y[a_index + stripe * 3] = (a_y[a_index + stripe * 3] <= 128 ?
                a_y[a_index + stripe * 3] : a_y[a_index + stripe * 3] - 257); ;
            a_y[a_index + stripe * 4] = ((a_y[a_index + stripe * 4] & 255) - (a_y[a_index + stripe * 4] >> 8));
            a_y[a_index + stripe * 4] = (a_y[a_index + stripe * 4] <= 128 ?
                a_y[a_index + stripe * 4] : a_y[a_index + stripe * 4] - 257); ;
            a_y[a_index + stripe * 5] = ((a_y[a_index + stripe * 5] & 255) - (a_y[a_index + stripe * 5] >> 8));
            a_y[a_index + stripe * 5] = (a_y[a_index + stripe * 5] <= 128 ?
                a_y[a_index + stripe * 5] : a_y[a_index + stripe * 5] - 257); ;
            a_y[a_index + stripe * 6] = ((a_y[a_index + stripe * 6] & 255) - (a_y[a_index + stripe * 6] >> 8));
            a_y[a_index + stripe * 6] = (a_y[a_index + stripe * 6] <= 128 ?
                a_y[a_index + stripe * 6] : a_y[a_index + stripe * 6] - 257); ;
            a_y[a_index + stripe * 7] = ((a_y[a_index + stripe * 7] & 255) - (a_y[a_index + stripe * 7] >> 8));
            a_y[a_index + stripe * 7] = (a_y[a_index + stripe * 7] <= 128 ?
                a_y[a_index + stripe * 7] : a_y[a_index + stripe * 7] - 257); ;
            a_y[a_index + stripe * 8] = ((a_y[a_index + stripe * 8] & 255) - (a_y[a_index + stripe * 8] >> 8));
            a_y[a_index + stripe * 8] = (a_y[a_index + stripe * 8] <= 128 ?
                a_y[a_index + stripe * 8] : a_y[a_index + stripe * 8] - 257); ;
            a_y[a_index + stripe * 9] = ((a_y[a_index + stripe * 9] & 255) - (a_y[a_index + stripe * 9] >> 8));
            a_y[a_index + stripe * 9] = (a_y[a_index + stripe * 9] <= 128 ?
                a_y[a_index + stripe * 9] : a_y[a_index + stripe * 9] - 257); ;
            a_y[a_index + stripe * 10] = ((a_y[a_index + stripe * 10] & 255) - (a_y[a_index + stripe * 10] >> 8));
            a_y[a_index + stripe * 10] = (a_y[a_index + stripe * 10] <= 128 ?
                a_y[a_index + stripe * 10] : a_y[a_index + stripe * 10] - 257); ;
            a_y[a_index + stripe * 11] = ((a_y[a_index + stripe * 11] & 255) - (a_y[a_index + stripe * 11] >> 8));
            a_y[a_index + stripe * 11] = (a_y[a_index + stripe * 11] <= 128 ?
                a_y[a_index + stripe * 11] : a_y[a_index + stripe * 11] - 257); ;
            a_y[a_index + stripe * 12] = ((a_y[a_index + stripe * 12] & 255) - (a_y[a_index + stripe * 12] >> 8));
            a_y[a_index + stripe * 12] = (a_y[a_index + stripe * 12] <= 128 ?
                a_y[a_index + stripe * 12] : a_y[a_index + stripe * 12] - 257); ;
            a_y[a_index + stripe * 13] = ((a_y[a_index + stripe * 13] & 255) - (a_y[a_index + stripe * 13] >> 8));
            a_y[a_index + stripe * 13] = (a_y[a_index + stripe * 13] <= 128 ?
                a_y[a_index + stripe * 13] : a_y[a_index + stripe * 13] - 257); ;
            a_y[a_index + stripe * 14] = ((a_y[a_index + stripe * 14] & 255) - (a_y[a_index + stripe * 14] >> 8));
            a_y[a_index + stripe * 14] = (a_y[a_index + stripe * 14] <= 128 ?
                a_y[a_index + stripe * 14] : a_y[a_index + stripe * 14] - 257); ;
            a_y[a_index + stripe * 15] = ((a_y[a_index + stripe * 15] & 255) - (a_y[a_index + stripe * 15] >> 8));
            a_y[a_index + stripe * 15] = (a_y[a_index + stripe * 15] <= 128 ?
                a_y[a_index + stripe * 15] : a_y[a_index + stripe * 15] - 257); ;
        }

        private void FFT128(int[] a_y, int a_index)
        {
            for (int i = 0; i < 16; i++)
                FFT8(a_y, a_index + i, 16);

            for (int i = 0; i < 128; i++)
            {
                a_y[a_index + i] = ((a_y[a_index + i] * FFT128_8_16_Twiddle[i] & 255) -
                    (a_y[a_index + i] * FFT128_8_16_Twiddle[i] >> 8));
            }

            for (int i = 0; i < 8; i++)
                FFT16(a_y, a_index + 16 * i, 1);
        }

        protected override void TransformBlock(byte[] a_data, int a_index, bool a_final)
        {
            int[] y = new int[256];

            for (int i = 0; i < 128; i++)
                y[i] = a_data[i + a_index];

            int tmp = y[127];

            for (int i = 0; i < 127; i++)
                y[128 + i] = ((y[i] * FFT256_2_128_Twiddle[i] & 255) - (y[i] * FFT256_2_128_Twiddle[i] >> 8));

            if (a_final)
            {
                int tmp2 = y[125];
                y[125] = (((tmp2 + 1) & 255) - ((tmp2 + 1) >> 8));
                y[253] = (((tmp2 - 1) * FFT256_2_128_Twiddle[125] & 255) - ((tmp2 - 1) * FFT256_2_128_Twiddle[125] >> 8));
            }

            y[127] = (((tmp + 1) & 255) - ((tmp + 1) >> 8));
            y[255] = (((tmp - 1) * FFT256_2_128_Twiddle[127] & 255) - ((tmp - 1) * FFT256_2_128_Twiddle[127] >> 8));

            FFT128(y, 0);
            FFT128(y, 128);

            uint[] state = new uint[32];
            Array.Copy(m_state, 0, state, 0, 32);

            uint[] message = Converters.ConvertBytesToUInts(a_data, a_index, 128);

            for (int i = 0; i < 32; i++)
                m_state[i] ^= message[i];

            Round8(y, 0, 3, 23, 17, 27);
            Round8(y, 1, 28, 19, 22, 7);
            Round8(y, 2, 29, 9, 15, 5);
            Round8(y, 3, 4, 13, 10, 25);

            uint[] R = new uint[8];

            for (int j = 0; j < 8; j++)
                R[j] = ((m_state[0 + j] << 4) | (m_state[0 + j] >> (32 - 4)));

            for (int j = 0; j < 8; j++)
            {
                m_state[24 + j] = m_state[24 + j] + state[0 + j] + ((((m_state[8 + j]) ^ (m_state[16 + j])) &
                    (m_state[0 + j])) ^ (m_state[16 + j]));
                m_state[24 + j] = ((m_state[24 + j] << 13) | (m_state[24 + j] >> (32 - 13))) + R[j ^ p8_xor[(32) % 7]];
                m_state[0 + j] = R[j];
            }

            for (int j = 0; j < 8; j++)
                R[j] = ((m_state[24 + j] << 13) | (m_state[24 + j] >> (32 - 13)));

            for (int j = 0; j < 8; j++)
            {
                m_state[16 + j] = m_state[16 + j] + state[8 + j] + ((((m_state[0 + j]) ^ (m_state[8 + j])) &
                    (m_state[24 + j])) ^ (m_state[8 + j]));
                m_state[16 + j] = ((m_state[16 + j] << 10) | (m_state[16 + j] >> (32 - 10))) + R[j ^ p8_xor[(33) % 7]];
                m_state[24 + j] = R[j];
            }

            for (int j = 0; j < 8; j++)
                R[j] = ((m_state[16 + j] << 10) | (m_state[16 + j] >> (32 - 10)));

            for (int j = 0; j < 8; j++)
            {
                m_state[8 + j] = m_state[8 + j] + state[16 + j] + ((((m_state[24 + j]) ^ (m_state[0 + j])) &
                    (m_state[16 + j])) ^ (m_state[0 + j]));
                m_state[8 + j] = ((m_state[8 + j] << 25) | (m_state[8 + j] >> (32 - 25))) + R[j ^ p8_xor[(34) % 7]];
                m_state[16 + j] = R[j];
            }

            for (int j = 0; j < 8; j++)
                R[j] = ((m_state[8 + j] << 25) | (m_state[8 + j] >> (32 - 25)));

            for (int j = 0; j < 8; j++)
            {
                m_state[0 + j] = m_state[0 + j] + state[24 + j] + ((((m_state[16 + j]) ^ (m_state[24 + j])) &
                    (m_state[8 + j])) ^ (m_state[24 + j]));
                m_state[0 + j] = ((m_state[0 + j] << 4) | (m_state[0 + j] >> (32 - 4))) + R[j ^ p8_xor[(35) % 7]];
                m_state[8 + j] = R[j];
            }
        }

        public override void Initialize()
        {
            if (HashSize == 48)
                Array.Copy(IV_384, 0, m_state, 0, IV_384.Length);
            else
                Array.Copy(IV_512, 0, m_state, 0, IV_512.Length);

            base.Initialize();
        }
    }
}