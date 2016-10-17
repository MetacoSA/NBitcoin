using System;

namespace HashLib.Crypto
{
    internal class HAS160 : BlockHash, ICryptoNotBuildIn
    {
        #region Consts
        private static readonly int[] s_rot = new int[]
        {
            5, 11,  7, 15,  6, 13,  8, 14,  7, 12,  9, 11,  8, 15,  6, 12,  9, 14,  5, 13
        };

        private static readonly int[] s_tor = new int[] 
        {
            27, 21, 25, 17, 26, 19, 24, 18, 25, 20, 23, 21, 24, 17, 26, 20, 23, 18, 27, 19
        };

        private static readonly int[] s_index = new int[]
        {
            18,  0,  1,  2,  3, 19,  4,  5, 6,  7, 16,  8,  9, 10, 11, 17, 12, 13, 14, 15,
            18,  3,  6,  9, 12, 19, 15,  2, 5,  8, 16, 11, 14,  1,  4, 17,  7, 10, 13,  0,
            18, 12,  5, 14,  7, 19,  0,  9, 2, 11, 16,  4, 13,  6, 15, 17,  8,  1, 10,  3,
            18,  7,  2, 13,  8, 19,  3, 14, 9,  4, 16, 15, 10,  5,  0, 17, 11,  6,  1, 12
        };
        #endregion

        private readonly uint[] m_hash = new uint[5];

        public HAS160() 
            : base(20, 64)
        {
            Initialize();
        }

        protected override void Finish()
        {
            ulong bits = m_processed_bytes * 8;
            int pad_index = (m_buffer.Pos < 56) ? (56 - m_buffer.Pos) : (120 - m_buffer.Pos);

            byte[] pad = new byte[pad_index + 8];
            pad[0] = 0x80;

            Converters.ConvertULongToBytes(bits, pad, pad_index);
            pad_index += 8;

            TransformBytes(pad, 0, pad_index);
        }

        protected override byte[] GetResult()
        {
            return Converters.ConvertUIntsToBytes(m_hash);
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            uint A = m_hash[0];
            uint B = m_hash[1];
            uint C = m_hash[2];
            uint D = m_hash[3];
            uint E = m_hash[4];

            uint[] data = new uint[20];
            Converters.ConvertBytesToUInts(a_data, a_index, BlockSize, data);

            data[16] = data[0] ^ data[1] ^ data[2] ^ data[3];
            data[17] = data[4] ^ data[5] ^ data[6] ^ data[7];
            data[18] = data[8] ^ data[9] ^ data[10] ^ data[11];
            data[19] = data[12] ^ data[13] ^ data[14] ^ data[15];

            for (int r = 0; r < 20; r++)
            {
                uint T = data[s_index[r]] + (A << s_rot[r] | A >> s_tor[r]) + ((B & C) | (~B & D)) + E;
                E = D;
                D = C;
                C = B << 10 | B >> 22;
                B = A;
                A = T;
            }

            data[16] = data[3] ^ data[6] ^ data[9] ^ data[12];
            data[17] = data[2] ^ data[5] ^ data[8] ^ data[15];
            data[18] = data[1] ^ data[4] ^ data[11] ^ data[14];
            data[19] = data[0] ^ data[7] ^ data[10] ^ data[13];

            for (int r = 20; r < 40; r++)
            {
                uint T = data[s_index[r]] + 0x5A827999 + (A << s_rot[r - 20] | A >> s_tor[r - 20]) + (B ^ C ^ D) + E;
                E = D;
                D = C;
                C = B << 17 | B >> 15;
                B = A;
                A = T;
            }

            data[16] = data[5] ^ data[7] ^ data[12] ^ data[14];
            data[17] = data[0] ^ data[2] ^ data[9] ^ data[11];
            data[18] = data[4] ^ data[6] ^ data[13] ^ data[15];
            data[19] = data[1] ^ data[3] ^ data[8] ^ data[10];

            for (int r = 40; r < 60; r++)
            {
                uint T = data[s_index[r]] + 0x6ED9EBA1 + (A << s_rot[r - 40] | A >> s_tor[r - 40]) + (C ^ (B | ~D)) + E;
                E = D;
                D = C;
                C = B << 25 | B >> 7;
                B = A;
                A = T;
            }

            data[16] = data[2] ^ data[7] ^ data[8] ^ data[13];
            data[17] = data[3] ^ data[4] ^ data[9] ^ data[14];
            data[18] = data[0] ^ data[5] ^ data[10] ^ data[15];
            data[19] = data[1] ^ data[6] ^ data[11] ^ data[12];

            for (int r = 60; r < 80; r++)
            {
                uint T = data[s_index[r]] + 0x8F1BBCDC + (A << s_rot[r - 60] | A >> s_tor[r - 60]) + (B ^ C ^ D) + E;
                E = D;
                D = C;
                C = B << 30 | B >> 2;
                B = A;
                A = T;
            }

            m_hash[0] += A;
            m_hash[1] += B;
            m_hash[2] += C;
            m_hash[3] += D;
            m_hash[4] += E;
        }

        public override void Initialize()
        {
            m_hash[0] = 0x67452301;
            m_hash[1] = 0xEFCDAB89;
            m_hash[2] = 0x98BADCFE;
            m_hash[3] = 0x10325476;
            m_hash[4] = 0xC3D2E1F0;

            base.Initialize();
        }
    }
}
