using System;

namespace HashLib.Crypto
{
    internal class Gost : BlockHash, ICryptoNotBuildIn
    {
        #region Consts
        private static readonly uint[] s_sbox1 = new uint[256];
        private static readonly uint[] s_sbox2 = new uint[256];
        private static readonly uint[] s_sbox3 = new uint[256];
        private static readonly uint[] s_sbox4 = new uint[256];

        static Gost()
        {
            uint[,] sbox = new uint[8, 16]
                {
                    {  4, 10,  9,  2, 13,  8,  0, 14,  6, 11,  1, 12,  7, 15,  5,  3 },
                    { 14, 11,  4, 12,  6, 13, 15, 10,  2,  3,  8,  1,  0,  7,  5,  9 },
                    {  5,  8,  1, 13, 10,  3,  4,  2, 14, 15, 12,  7,  6,  0,  9, 11 },
                    {  7, 13, 10,  1,  0,  8,  9, 15, 14,  4,  6, 12, 11,  2,  5,  3 },
                    {  6, 12,  7,  1,  5, 15, 13,  8,  4, 10,  9, 14,  0,  3, 11,  2 },
                    {  4, 11, 10,  0,  7,  2,  1, 13,  3,  6,  8,  5,  9, 12, 15, 14 },
                    { 13, 11,  4,  1,  3, 15,  5,  9,  0, 10, 14,  7,  6,  8,  2, 12 },
                    {  1, 15, 13,  0,  5,  7, 10,  4,  9,  2,  3, 14,  6, 11,  8, 12 }  
                };

            int i = 0;
            for (int a = 0; a < 16; a++)
            {
                uint ax = sbox[1, a] << 15;
                uint bx = sbox[3, a] << 23;
                uint cx = sbox[5, a];
                cx = (cx >> 1) | (cx << 31);
                uint dx = sbox[7, a] << 7;

                for (int b = 0; b < 16; b++)
                {
                    s_sbox1[i] = ax | (sbox[0, b] << 11);
                    s_sbox2[i] = bx | (sbox[2, b] << 19);
                    s_sbox3[i] = cx | (sbox[4, b] << 27);
                    s_sbox4[i++] = dx | (sbox[6, b] << 3);
                }
            }
        }
        #endregion

        private uint[] m_state = new uint[8];
        private uint[] m_hash = new uint[8];

        public Gost()
            : base(32, 32)
        {
            Initialize();
        }

        private void Compress(uint[] a_m)
        {
            uint[] s = new uint[8];

            uint u0 = m_hash[0];
            uint u1 = m_hash[1];
            uint u2 = m_hash[2];
            uint u3 = m_hash[3];
            uint u4 = m_hash[4];
            uint u5 = m_hash[5];
            uint u6 = m_hash[6];
            uint u7 = m_hash[7];

            uint v0 = a_m[0];
            uint v1 = a_m[1];
            uint v2 = a_m[2];
            uint v3 = a_m[3];
            uint v4 = a_m[4];
            uint v5 = a_m[5];
            uint v6 = a_m[6];
            uint v7 = a_m[7];

            for (int i = 0; i < 8; i += 2)
            {
                uint w0 = u0 ^ v0;
                uint w1 = u1 ^ v1;
                uint w2 = u2 ^ v2;
                uint w3 = u3 ^ v3;
                uint w4 = u4 ^ v4;
                uint w5 = u5 ^ v5;
                uint w6 = u6 ^ v6;
                uint w7 = u7 ^ v7;

                uint key0 = (uint)(byte)w0 | ((uint)(byte)w2 << 8) |
                    ((uint)(byte)w4 << 16) | ((uint)(byte)w6 << 24);
                uint key1 = (uint)(byte)(w0 >> 8) | (w2 & 0x0000ff00) |
                    ((w4 & 0x0000ff00) << 8) | ((w6 & 0x0000ff00) << 16);
                uint key2 = (uint)(byte)(w0 >> 16) | ((w2 & 0x00ff0000) >> 8) |
                    (w4 & 0x00ff0000) | ((w6 & 0x00ff0000) << 8);
                uint key3 = (w0 >> 24) | ((w2 & 0xff000000) >> 16) |
                    ((w4 & 0xff000000) >> 8) | (w6 & 0xff000000);
                uint key4 = (uint)(byte)w1 | ((w3 & 0x000000ff) << 8) |
                    ((w5 & 0x000000ff) << 16) | ((w7 & 0x000000ff) << 24);
                uint key5 = (uint)(byte)(w1 >> 8) | (w3 & 0x0000ff00) |
                    ((w5 & 0x0000ff00) << 8) | ((w7 & 0x0000ff00) << 16);
                uint key6 = (uint)(byte)(w1 >> 16) | ((w3 & 0x00ff0000) >> 8) |
                    (w5 & 0x00ff0000) | ((w7 & 0x00ff0000) << 8);
                uint key7 = (w1 >> 24) | ((w3 & 0xff000000) >> 16) |
                    ((w5 & 0xff000000) >> 8) | (w7 & 0xff000000);

                uint r = m_hash[i];
                uint l = m_hash[i + 1];

                uint t = key0 + r;
                l ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key1 + l;
                r ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key2 + r;
                l ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key3 + l;
                r ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key4 + r;
                l ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key5 + l;
                r ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key6 + r;
                l ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key7 + l;
                r ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key0 + r;
                l ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key1 + l;
                r ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key2 + r;
                l ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key3 + l;
                r ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key4 + r;
                l ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key5 + l;
                r ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key6 + r;
                l ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key7 + l;
                r ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key0 + r;
                l ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key1 + l;
                r ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key2 + r;
                l ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key3 + l;
                r ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key4 + r;
                l ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key5 + l;
                r ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key6 + r;
                l ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key7 + l;
                r ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key7 + r;
                l ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key6 + l;
                r ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key5 + r;
                l ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key4 + l;
                r ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key3 + r;
                l ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key2 + l;
                r ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key1 + r;
                l ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];
                t = key0 + l;
                r ^= s_sbox1[(byte)t] ^ s_sbox2[(byte)(t >> 8)] ^ s_sbox3[(byte)(t >> 16)] ^ s_sbox4[t >> 24];

                t = r;
                r = l;
                l = t;

                s[i] = r;
                s[i + 1] = l;

                if (i == 6)
                    break;

                l = u0 ^ u2;
                r = u1 ^ u3;
                u0 = u2;
                u1 = u3;
                u2 = u4;
                u3 = u5;
                u4 = u6;
                u5 = u7;
                u6 = l;
                u7 = r; 

                if (i == 2)
                {
                    u0 ^= 0xff00ff00;
                    u1 ^= 0xff00ff00;
                    u2 ^= 0x00ff00ff;
                    u3 ^= 0x00ff00ff;
                    u4 ^= 0x00ffff00;
                    u5 ^= 0xff0000ff;
                    u6 ^= 0x000000ff;
                    u7 ^= 0xff00ffff;
                }

                l = v0;
                r = v2;
                v0 = v4;
                v2 = v6;
                v4 = l ^ r;
                v6 = v0 ^ r;
                l = v1;
                r = v3;
                v1 = v5;
                v3 = v7;
                v5 = l ^ r;
                v7 = v1 ^ r;
            }

            u0 = a_m[0] ^ s[6];
            u1 = a_m[1] ^ s[7];
            u2 = a_m[2] ^ (s[0] << 16) ^ (s[0] >> 16) ^ (s[0] & 0xffff) ^
                (s[1] & 0xffff) ^ (s[1] >> 16) ^ (s[2] << 16) ^ s[6] ^ (s[6] << 16) ^
                (s[7] & 0xffff0000) ^ (s[7] >> 16);
            u3 = a_m[3] ^ (s[0] & 0xffff) ^ (s[0] << 16) ^ (s[1] & 0xffff) ^
                (s[1] << 16) ^ (s[1] >> 16) ^ (s[2] << 16) ^ (s[2] >> 16) ^
                (s[3] << 16) ^ s[6] ^ (s[6] << 16) ^ (s[6] >> 16) ^ (s[7] & 0xffff) ^
                (s[7] << 16) ^ (s[7] >> 16);
            u4 = a_m[4] ^
                (s[0] & 0xffff0000) ^ (s[0] << 16) ^ (s[0] >> 16) ^
                (s[1] & 0xffff0000) ^ (s[1] >> 16) ^ (s[2] << 16) ^ (s[2] >> 16) ^
                (s[3] << 16) ^ (s[3] >> 16) ^ (s[4] << 16) ^ (s[6] << 16) ^
                (s[6] >> 16) ^ (s[7] & 0xffff) ^ (s[7] << 16) ^ (s[7] >> 16);
            u5 = a_m[5] ^ (s[0] << 16) ^ (s[0] >> 16) ^ (s[0] & 0xffff0000) ^
                (s[1] & 0xffff) ^ s[2] ^ (s[2] >> 16) ^ (s[3] << 16) ^ (s[3] >> 16) ^
                (s[4] << 16) ^ (s[4] >> 16) ^ (s[5] << 16) ^ (s[6] << 16) ^
                (s[6] >> 16) ^ (s[7] & 0xffff0000) ^ (s[7] << 16) ^ (s[7] >> 16);
            u6 = a_m[6] ^ s[0] ^ (s[1] >> 16) ^ (s[2] << 16) ^ s[3] ^ (s[3] >> 16) ^
                (s[4] << 16) ^ (s[4] >> 16) ^ (s[5] << 16) ^ (s[5] >> 16) ^ s[6] ^
                (s[6] << 16) ^ (s[6] >> 16) ^ (s[7] << 16);
            u7 = a_m[7] ^ (s[0] & 0xffff0000) ^ (s[0] << 16) ^ (s[1] & 0xffff) ^
                (s[1] << 16) ^ (s[2] >> 16) ^ (s[3] << 16) ^ s[4] ^ (s[4] >> 16) ^
                (s[5] << 16) ^ (s[5] >> 16) ^ (s[6] >> 16) ^ (s[7] & 0xffff) ^
                (s[7] << 16) ^ (s[7] >> 16);

            v0 = m_hash[0] ^ (u1 << 16) ^ (u0 >> 16);
            v1 = m_hash[1] ^ (u2 << 16) ^ (u1 >> 16);
            v2 = m_hash[2] ^ (u3 << 16) ^ (u2 >> 16);
            v3 = m_hash[3] ^ (u4 << 16) ^ (u3 >> 16);
            v4 = m_hash[4] ^ (u5 << 16) ^ (u4 >> 16);
            v5 = m_hash[5] ^ (u6 << 16) ^ (u5 >> 16);
            v6 = m_hash[6] ^ (u7 << 16) ^ (u6 >> 16);
            v7 = m_hash[7] ^ (u0 & 0xffff0000) ^ (u0 << 16) ^ (u7 >> 16) ^
                (u1 & 0xffff0000) ^ (u1 << 16) ^ (u6 << 16) ^ (u7 & 0xffff0000);

            m_hash[0] = (v0 & 0xffff0000) ^ (v0 << 16) ^ (v0 >> 16) ^ (v1 >> 16) ^
                (v1 & 0xffff0000) ^ (v2 << 16) ^ (v3 >> 16) ^ (v4 << 16) ^
                (v5 >> 16) ^ v5 ^ (v6 >> 16) ^ (v7 << 16) ^ (v7 >> 16) ^
                (v7 & 0xffff);
            m_hash[1] = (v0 << 16) ^ (v0 >> 16) ^ (v0 & 0xffff0000) ^ (v1 & 0xffff) ^
                v2 ^ (v2 >> 16) ^ (v3 << 16) ^ (v4 >> 16) ^ (v5 << 16) ^
                (v6 << 16) ^ v6 ^ (v7 & 0xffff0000) ^ (v7 >> 16);
            m_hash[2] = (v0 & 0xffff) ^ (v0 << 16) ^ (v1 << 16) ^ (v1 >> 16) ^
                (v1 & 0xffff0000) ^ (v2 << 16) ^ (v3 >> 16) ^ v3 ^ (v4 << 16) ^
                (v5 >> 16) ^ v6 ^ (v6 >> 16) ^ (v7 & 0xffff) ^ (v7 << 16) ^
                (v7 >> 16);
            m_hash[3] = (v0 << 16) ^ (v0 >> 16) ^ (v0 & 0xffff0000) ^
                (v1 & 0xffff0000) ^ (v1 >> 16) ^ (v2 << 16) ^ (v2 >> 16) ^ v2 ^
                (v3 << 16) ^ (v4 >> 16) ^ v4 ^ (v5 << 16) ^ (v6 << 16) ^
                (v7 & 0xffff) ^ (v7 >> 16);
            m_hash[4] = (v0 >> 16) ^ (v1 << 16) ^ v1 ^ (v2 >> 16) ^ v2 ^
                (v3 << 16) ^ (v3 >> 16) ^ v3 ^ (v4 << 16) ^ (v5 >> 16) ^
                v5 ^ (v6 << 16) ^ (v6 >> 16) ^ (v7 << 16);
            m_hash[5] = (v0 << 16) ^ (v0 & 0xffff0000) ^ (v1 << 16) ^ (v1 >> 16) ^
                (v1 & 0xffff0000) ^ (v2 << 16) ^ v2 ^ (v3 >> 16) ^ v3 ^
                (v4 << 16) ^ (v4 >> 16) ^ v4 ^ (v5 << 16) ^ (v6 << 16) ^
                (v6 >> 16) ^ v6 ^ (v7 << 16) ^ (v7 >> 16) ^ (v7 & 0xffff0000);
            m_hash[6] = v0 ^ v2 ^ (v2 >> 16) ^ v3 ^ (v3 << 16) ^ v4 ^
                (v4 >> 16) ^ (v5 << 16) ^ (v5 >> 16) ^ v5 ^ (v6 << 16) ^
                (v6 >> 16) ^ v6 ^ (v7 << 16) ^ v7;
            m_hash[7] = v0 ^ (v0 >> 16) ^ (v1 << 16) ^ (v1 >> 16) ^ (v2 << 16) ^
                (v3 >> 16) ^ v3 ^ (v4 << 16) ^ v4 ^ (v5 >> 16) ^ v5 ^
                (v6 << 16) ^ (v6 >> 16) ^ (v7 << 16) ^ v7;
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            uint[] m = new uint[8];

            uint c = 0;

            uint[] data = Converters.ConvertBytesToUInts(a_data, a_index, 32);

            for (int i = 0; i < 8; i++)
            {
                uint a = data[i];
                m[i] = a;
                uint b = m_state[i];
                c = a + c + m_state[i];
                m_state[i] = c;
                c = (uint)(((c < a) || (c < b)) ? 1 : 0);
            }

            Compress(m);
        }

        protected override byte[] GetResult()
        {
            return Converters.ConvertUIntsToBytes(m_hash);
        }

        protected override void Finish()
        {
            ulong bits = m_processed_bytes * 8;

            if (m_buffer.Pos > 0)
            {
                byte[] pad = new byte[32 - m_buffer.Pos];
                TransformBytes(pad, 0, 32 - m_buffer.Pos);
            }

            uint[] m_length = new uint[8];
            m_length[0] = (uint)bits;
            m_length[1] = (uint)(bits >> 32);

            Compress(m_length);

            Compress(m_state);
        }

        public override void Initialize()
        {
            m_state.Clear();
            m_hash.Clear();

            base.Initialize();
        }
    };
}
