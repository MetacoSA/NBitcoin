using System;
using System.Diagnostics;

namespace HashLib.Crypto
{
    internal class Haval_3_128 : Haval3
    {
        public Haval_3_128()
            : base(HashLib.HashSize.HashSize128)
        {
        }
    }

    internal class Haval_4_128 : Haval4
    {
        public Haval_4_128()
            : base(HashLib.HashSize.HashSize128)
        {
        }
    }

    internal class Haval_5_128 : Haval5
    {
        public Haval_5_128()
            : base(HashLib.HashSize.HashSize128)
        {
        }
    }

    internal class Haval_3_160 : Haval3
    {
        public Haval_3_160()
            : base(HashLib.HashSize.HashSize160)
        {
        }
    }

    internal class Haval_4_160 : Haval4
    {
        public Haval_4_160()
            : base(HashLib.HashSize.HashSize160)
        {
        }
    }

    internal class Haval_5_160 : Haval5
    {
        public Haval_5_160()
            : base(HashLib.HashSize.HashSize160)
        {
        }
    }

    internal class Haval_3_192 : Haval3
    {
        public Haval_3_192()
            : base(HashLib.HashSize.HashSize192)
        {
        }
    }

    internal class Haval_4_192 : Haval4
    {
        public Haval_4_192()
            : base(HashLib.HashSize.HashSize192)
        {
        }
    }

    internal class Haval_5_192 : Haval5
    {
        public Haval_5_192()
            : base(HashLib.HashSize.HashSize192)
        {
        }
    }

    internal class Haval_3_224 : Haval3
    {
        public Haval_3_224()
            : base(HashLib.HashSize.HashSize224)
        {
        }
    }

    internal class Haval_4_224 : Haval4
    {
        public Haval_4_224()
            : base(HashLib.HashSize.HashSize224)
        {
        }
    }

    internal class Haval_5_224 : Haval5
    {
        public Haval_5_224()
            : base(HashLib.HashSize.HashSize224)
        {
        }
    }

    internal class Haval_3_256 : Haval3
    {
        public Haval_3_256()
            : base(HashLib.HashSize.HashSize256)
        {
        }
    }

    internal class Haval_4_256 : Haval4
    {
        public Haval_4_256()
            : base(HashLib.HashSize.HashSize256)
        {
        }
    }

    internal class Haval_5_256 : Haval5
    {
        public Haval_5_256()
            : base(HashLib.HashSize.HashSize256)
        {
        }
    }

    internal abstract class Haval : BlockHash, ICryptoNotBuildIn
    {
        public const int HAVAL_VERSION = 1;

        protected readonly int m_rounds;
        protected readonly uint[] m_hash = new uint[8];

        internal Haval(HashRounds a_rounds, HashSize a_hash_size) 
            : base((int)a_hash_size, 128)
        {
            m_rounds = (int)a_rounds;

            Initialize();
        }

        protected override void Finish()
        {
            ulong bits = m_processed_bytes * 8;
            int padindex = (m_buffer.Pos < 118) ? (118 - m_buffer.Pos) : (246 - m_buffer.Pos);

            byte[] pad = new byte[padindex + 10];
            pad[0] = (byte)0x01;

            pad[padindex++] = (byte)((m_rounds << 3) | (HAVAL_VERSION & 0x07));
            pad[padindex++] = (byte)(HashSize << 1);

            Converters.ConvertULongToBytes(bits, pad, padindex);
            padindex += 8;

            TransformBytes(pad, 0, padindex);
        }

        protected override byte[] GetResult()
        {
            TailorDigestBits();

            return Converters.ConvertUIntsToBytes(m_hash, 0, HashSize / 4);
        }

        public override void Initialize()
        {
            m_hash[0] = 0x243F6A88;
            m_hash[1] = 0x85A308D3;
            m_hash[2] = 0x13198A2E;
            m_hash[3] = 0x03707344;
            m_hash[4] = 0xA4093822;
            m_hash[5] = 0x299F31D0;
            m_hash[6] = 0x082EFA98;
            m_hash[7] = 0xEC4E6C89;

            base.Initialize();
        }

        private void TailorDigestBits()
        {
            uint t;

            switch (HashSize)
            {
                case 16:
                    t = (m_hash[7] & 0x000000FF) | (m_hash[6] & 0xFF000000) | (m_hash[5] & 0x00FF0000) | (m_hash[4] & 0x0000FF00);
                    m_hash[0] += t >> 8 | t << 24;
                    t = (m_hash[7] & 0x0000FF00) | (m_hash[6] & 0x000000FF) | (m_hash[5] & 0xFF000000) | (m_hash[4] & 0x00FF0000);
                    m_hash[1] += t >> 16 | t << 16;
                    t = (m_hash[7] & 0x00FF0000) | (m_hash[6] & 0x0000FF00) | (m_hash[5] & 0x000000FF) | (m_hash[4] & 0xFF000000);
                    m_hash[2] += t >> 24 | t << 8;
                    t = (m_hash[7] & 0xFF000000) | (m_hash[6] & 0x00FF0000) | (m_hash[5] & 0x0000FF00) | (m_hash[4] & 0x000000FF);
                    m_hash[3] += t;
                    break;
                case 20:
                    t = (uint)(m_hash[7] & 0x3F) | (uint)(m_hash[6] & (0x7F << 25)) | (uint)(m_hash[5] & (0x3F << 19));
                    m_hash[0] += t >> 19 | t << 13;
                    t = (uint)(m_hash[7] & (0x3F << 6)) | (uint)(m_hash[6] & 0x3F) | (uint)(m_hash[5] & (0x7F << 25));
                    m_hash[1] += t >> 25 | t << 7;
                    t = (m_hash[7] & (0x7F << 12)) | (m_hash[6] & (0x3F << 6)) | (m_hash[5] & 0x3F);
                    m_hash[2] += t;
                    t = (m_hash[7] & (0x3F << 19)) | (m_hash[6] & (0x7F << 12)) | (m_hash[5] & (0x3F << 6));
                    m_hash[3] += (t >> 6);
                    t = (m_hash[7] & ((uint)0x7F << 25)) | (uint)(m_hash[6] & (0x3F << 19)) | (uint)(m_hash[5] & (0x7F << 12));
                    m_hash[4] += (t >> 12);
                    break;
                case 24:
                    t = (uint)(m_hash[7] & 0x1F) | (uint)(m_hash[6] & (0x3F << 26));
                    m_hash[0] += t >> 26 | t << 6;
                    t = (m_hash[7] & (0x1F << 5)) | (m_hash[6] & 0x1F);
                    m_hash[1] += t;
                    t = (m_hash[7] & (0x3F << 10)) | (m_hash[6] & (0x1F << 5));
                    m_hash[2] += (t >> 5);
                    t = (m_hash[7] & (0x1F << 16)) | (m_hash[6] & (0x3F << 10));
                    m_hash[3] += (t >> 10);
                    t = (m_hash[7] & (0x1F << 21)) | (m_hash[6] & (0x1F << 16));
                    m_hash[4] += (t >> 16);
                    t = (uint)(m_hash[7] & (0x3F << 26)) | (uint)(m_hash[6] & (0x1F << 21));
                    m_hash[5] += (t >> 21);
                    break;
                case 28:
                    m_hash[0] += ((m_hash[7] >> 27) & 0x1F);
                    m_hash[1] += ((m_hash[7] >> 22) & 0x1F);
                    m_hash[2] += ((m_hash[7] >> 18) & 0x0F);
                    m_hash[3] += ((m_hash[7] >> 13) & 0x1F);
                    m_hash[4] += ((m_hash[7] >> 9) & 0x0F);
                    m_hash[5] += ((m_hash[7] >> 4) & 0x1F);
                    m_hash[6] += (m_hash[7] & 0x0F);
                    break;
            }
        }
    }

    internal abstract class Haval3 : Haval
    {
        internal Haval3(HashSize a_hash_size)
            : base(HashRounds.Rounds3, a_hash_size)
        {
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            uint[] temp = Converters.ConvertBytesToUInts(a_data, a_index, BlockSize);

            uint a = m_hash[0];
            uint b = m_hash[1];
            uint c = m_hash[2];
            uint d = m_hash[3];
            uint e = m_hash[4];
            uint f = m_hash[5];
            uint g = m_hash[6];
            uint h = m_hash[7];

            uint t = 0;

            t = c & (e ^ d) ^ g & a ^ f & b ^ e;
            h = temp[0] + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = b & (d ^ c) ^ f & h ^ e & a ^ d;
            g = temp[1] + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = a & (c ^ b) ^ e & g ^ d & h ^ c;
            f = temp[2] + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = h & (b ^ a) ^ d & f ^ c & g ^ b;
            e = temp[3] + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = g & (a ^ h) ^ c & e ^ b & f ^ a;
            d = temp[4] + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = f & (h ^ g) ^ b & d ^ a & e ^ h;
            c = temp[5] + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = e & (g ^ f) ^ a & c ^ h & d ^ g;
            b = temp[6] + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = d & (f ^ e) ^ h & b ^ g & c ^ f;
            a = temp[7] + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = c & (e ^ d) ^ g & a ^ f & b ^ e;
            h = temp[8] + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = b & (d ^ c) ^ f & h ^ e & a ^ d;
            g = temp[9] + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = a & (c ^ b) ^ e & g ^ d & h ^ c;
            f = temp[10] + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = h & (b ^ a) ^ d & f ^ c & g ^ b;
            e = temp[11] + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = g & (a ^ h) ^ c & e ^ b & f ^ a;
            d = temp[12] + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = f & (h ^ g) ^ b & d ^ a & e ^ h;
            c = temp[13] + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = e & (g ^ f) ^ a & c ^ h & d ^ g;
            b = temp[14] + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = d & (f ^ e) ^ h & b ^ g & c ^ f;
            a = temp[15] + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = c & (e ^ d) ^ g & a ^ f & b ^ e;
            h = temp[16] + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = b & (d ^ c) ^ f & h ^ e & a ^ d;
            g = temp[17] + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = a & (c ^ b) ^ e & g ^ d & h ^ c;
            f = temp[18] + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = h & (b ^ a) ^ d & f ^ c & g ^ b;
            e = temp[19] + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = g & (a ^ h) ^ c & e ^ b & f ^ a;
            d = temp[20] + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = f & (h ^ g) ^ b & d ^ a & e ^ h;
            c = temp[21] + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = e & (g ^ f) ^ a & c ^ h & d ^ g;
            b = temp[22] + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = d & (f ^ e) ^ h & b ^ g & c ^ f;
            a = temp[23] + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = c & (e ^ d) ^ g & a ^ f & b ^ e;
            h = temp[24] + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = b & (d ^ c) ^ f & h ^ e & a ^ d;
            g = temp[25] + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = a & (c ^ b) ^ e & g ^ d & h ^ c;
            f = temp[26] + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = h & (b ^ a) ^ d & f ^ c & g ^ b;
            e = temp[27] + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = g & (a ^ h) ^ c & e ^ b & f ^ a;
            d = temp[28] + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = f & (h ^ g) ^ b & d ^ a & e ^ h;
            c = temp[29] + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = e & (g ^ f) ^ a & c ^ h & d ^ g;
            b = temp[30] + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = d & (f ^ e) ^ h & b ^ g & c ^ f;
            a = temp[31] + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = f & (d & ~a ^ b & c ^ e ^ g) ^ b & (d ^ c) ^ a & c ^ g;
            h = temp[5] + 0x452821E6 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = e & (c & ~h ^ a & b ^ d ^ f) ^ a & (c ^ b) ^ h & b ^ f;
            g = temp[14] + 0x38D01377 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = d & (b & ~g ^ h & a ^ c ^ e) ^ h & (b ^ a) ^ g & a ^ e;
            f = temp[26] + 0xBE5466CF + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = c & (a & ~f ^ g & h ^ b ^ d) ^ g & (a ^ h) ^ f & h ^ d;
            e = temp[18] + 0x34E90C6C + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = b & (h & ~e ^ f & g ^ a ^ c) ^ f & (h ^ g) ^ e & g ^ c;
            d = temp[11] + 0xC0AC29B7 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = a & (g & ~d ^ e & f ^ h ^ b) ^ e & (g ^ f) ^ d & f ^ b;
            c = temp[28] + 0xC97C50DD + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = h & (f & ~c ^ d & e ^ g ^ a) ^ d & (f ^ e) ^ c & e ^ a;
            b = temp[7] + 0x3F84D5B5 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = g & (e & ~b ^ c & d ^ f ^ h) ^ c & (e ^ d) ^ b & d ^ h;
            a = temp[16] + 0xB5470917 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = f & (d & ~a ^ b & c ^ e ^ g) ^ b & (d ^ c) ^ a & c ^ g;
            h = temp[0] + 0x9216D5D9 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = e & (c & ~h ^ a & b ^ d ^ f) ^ a & (c ^ b) ^ h & b ^ f;
            g = temp[23] + 0x8979FB1B + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = d & (b & ~g ^ h & a ^ c ^ e) ^ h & (b ^ a) ^ g & a ^ e;
            f = temp[20] + 0xD1310BA6 + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = c & (a & ~f ^ g & h ^ b ^ d) ^ g & (a ^ h) ^ f & h ^ d;
            e = temp[22] + 0x98DFB5AC + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = b & (h & ~e ^ f & g ^ a ^ c) ^ f & (h ^ g) ^ e & g ^ c;
            d = temp[1] + 0x2FFD72DB + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = a & (g & ~d ^ e & f ^ h ^ b) ^ e & (g ^ f) ^ d & f ^ b;
            c = temp[10] + 0xD01ADFB7 + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = h & (f & ~c ^ d & e ^ g ^ a) ^ d & (f ^ e) ^ c & e ^ a;
            b = temp[4] + 0xB8E1AFED + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = g & (e & ~b ^ c & d ^ f ^ h) ^ c & (e ^ d) ^ b & d ^ h;
            a = temp[8] + 0x6A267E96 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = f & (d & ~a ^ b & c ^ e ^ g) ^ b & (d ^ c) ^ a & c ^ g;
            h = temp[30] + 0xBA7C9045 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = e & (c & ~h ^ a & b ^ d ^ f) ^ a & (c ^ b) ^ h & b ^ f;
            g = temp[3] + 0xF12C7F99 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = d & (b & ~g ^ h & a ^ c ^ e) ^ h & (b ^ a) ^ g & a ^ e;
            f = temp[21] + 0x24A19947 + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = c & (a & ~f ^ g & h ^ b ^ d) ^ g & (a ^ h) ^ f & h ^ d;
            e = temp[9] + 0xB3916CF7 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = b & (h & ~e ^ f & g ^ a ^ c) ^ f & (h ^ g) ^ e & g ^ c;
            d = temp[17] + 0x0801F2E2 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = a & (g & ~d ^ e & f ^ h ^ b) ^ e & (g ^ f) ^ d & f ^ b;
            c = temp[24] + 0x858EFC16 + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = h & (f & ~c ^ d & e ^ g ^ a) ^ d & (f ^ e) ^ c & e ^ a;
            b = temp[29] + 0x636920D8 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = g & (e & ~b ^ c & d ^ f ^ h) ^ c & (e ^ d) ^ b & d ^ h;
            a = temp[6] + 0x71574E69 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = f & (d & ~a ^ b & c ^ e ^ g) ^ b & (d ^ c) ^ a & c ^ g;
            h = temp[19] + 0xA458FEA3 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = e & (c & ~h ^ a & b ^ d ^ f) ^ a & (c ^ b) ^ h & b ^ f;
            g = temp[12] + 0xF4933D7E + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = d & (b & ~g ^ h & a ^ c ^ e) ^ h & (b ^ a) ^ g & a ^ e;
            f = temp[15] + 0x0D95748F + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = c & (a & ~f ^ g & h ^ b ^ d) ^ g & (a ^ h) ^ f & h ^ d;
            e = temp[13] + 0x728EB658 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = b & (h & ~e ^ f & g ^ a ^ c) ^ f & (h ^ g) ^ e & g ^ c;
            d = temp[2] + 0x718BCD58 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = a & (g & ~d ^ e & f ^ h ^ b) ^ e & (g ^ f) ^ d & f ^ b;
            c = temp[25] + 0x82154AEE + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = h & (f & ~c ^ d & e ^ g ^ a) ^ d & (f ^ e) ^ c & e ^ a;
            b = temp[31] + 0x7B54A41D + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = g & (e & ~b ^ c & d ^ f ^ h) ^ c & (e ^ d) ^ b & d ^ h;
            a = temp[27] + 0xC25A59B5 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = d & (f & e ^ g ^ a) ^ f & c ^ e & b ^ a;
            h = temp[19] + 0x9C30D539 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = c & (e & d ^ f ^ h) ^ e & b ^ d & a ^ h;
            g = temp[9] + 0x2AF26013 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = b & (d & c ^ e ^ g) ^ d & a ^ c & h ^ g;
            f = temp[4] + 0xC5D1B023 + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = a & (c & b ^ d ^ f) ^ c & h ^ b & g ^ f;
            e = temp[20] + 0x286085F0 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = h & (b & a ^ c ^ e) ^ b & g ^ a & f ^ e;
            d = temp[28] + 0xCA417918 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = g & (a & h ^ b ^ d) ^ a & f ^ h & e ^ d;
            c = temp[17] + 0xB8DB38EF + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = f & (h & g ^ a ^ c) ^ h & e ^ g & d ^ c;
            b = temp[8] + 0x8E79DCB0 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = e & (g & f ^ h ^ b) ^ g & d ^ f & c ^ b;
            a = temp[22] + 0x603A180E + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = d & (f & e ^ g ^ a) ^ f & c ^ e & b ^ a;
            h = temp[29] + 0x6C9E0E8B + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = c & (e & d ^ f ^ h) ^ e & b ^ d & a ^ h;
            g = temp[14] + 0xB01E8A3E + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = b & (d & c ^ e ^ g) ^ d & a ^ c & h ^ g;
            f = temp[25] + 0xD71577C1 + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = a & (c & b ^ d ^ f) ^ c & h ^ b & g ^ f;
            e = temp[12] + 0xBD314B27 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = h & (b & a ^ c ^ e) ^ b & g ^ a & f ^ e;
            d = temp[24] + 0x78AF2FDA + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = g & (a & h ^ b ^ d) ^ a & f ^ h & e ^ d;
            c = temp[30] + 0x55605C60 + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = f & (h & g ^ a ^ c) ^ h & e ^ g & d ^ c;
            b = temp[16] + 0xE65525F3 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = e & (g & f ^ h ^ b) ^ g & d ^ f & c ^ b;
            a = temp[26] + 0xAA55AB94 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = d & (f & e ^ g ^ a) ^ f & c ^ e & b ^ a;
            h = temp[31] + 0x57489862 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = c & (e & d ^ f ^ h) ^ e & b ^ d & a ^ h;
            g = temp[15] + 0x63E81440 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = b & (d & c ^ e ^ g) ^ d & a ^ c & h ^ g;
            f = temp[7] + 0x55CA396A + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = a & (c & b ^ d ^ f) ^ c & h ^ b & g ^ f;
            e = temp[3] + 0x2AAB10B6 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = h & (b & a ^ c ^ e) ^ b & g ^ a & f ^ e;
            d = temp[1] + 0xB4CC5C34 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = g & (a & h ^ b ^ d) ^ a & f ^ h & e ^ d;
            c = temp[0] + 0x1141E8CE + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = f & (h & g ^ a ^ c) ^ h & e ^ g & d ^ c;
            b = temp[18] + 0xA15486AF + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = e & (g & f ^ h ^ b) ^ g & d ^ f & c ^ b;
            a = temp[27] + 0x7C72E993 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = d & (f & e ^ g ^ a) ^ f & c ^ e & b ^ a;
            h = temp[13] + 0xB3EE1411 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = c & (e & d ^ f ^ h) ^ e & b ^ d & a ^ h;
            g = temp[6] + 0x636FBC2A + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = b & (d & c ^ e ^ g) ^ d & a ^ c & h ^ g;
            f = temp[21] + 0x2BA9C55D + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = a & (c & b ^ d ^ f) ^ c & h ^ b & g ^ f;
            e = temp[10] + 0x741831F6 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = h & (b & a ^ c ^ e) ^ b & g ^ a & f ^ e;
            d = temp[23] + 0xCE5C3E16 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = g & (a & h ^ b ^ d) ^ a & f ^ h & e ^ d;
            c = temp[11] + 0x9B87931E + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = f & (h & g ^ a ^ c) ^ h & e ^ g & d ^ c;
            b = temp[5] + 0xAFD6BA33 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = e & (g & f ^ h ^ b) ^ g & d ^ f & c ^ b;
            a = temp[2] + 0x6C24CF5C + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            m_hash[0] += a;
            m_hash[1] += b;
            m_hash[2] += c;
            m_hash[3] += d;
            m_hash[4] += e;
            m_hash[5] += f;
            m_hash[6] += g;
            m_hash[7] += h;
        }
    }

    internal abstract class Haval4 : Haval
    {
        internal Haval4(HashSize a_hash_size)
            : base(HashRounds.Rounds4, a_hash_size)
        {
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            uint[] temp = Converters.ConvertBytesToUInts(a_data, a_index, BlockSize);

            uint a = m_hash[0];
            uint b = m_hash[1];
            uint c = m_hash[2];
            uint d = m_hash[3];
            uint e = m_hash[4];
            uint f = m_hash[5];
            uint g = m_hash[6];
            uint h = m_hash[7];

            uint t = 0;

            t = d & (a ^ b) ^ f & g ^ e & c ^ a;
            h = temp[0] + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = c & (h ^ a) ^ e & f ^ d & b ^ h;
            g = temp[1] + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = b & (g ^ h) ^ d & e ^ c & a ^ g;
            f = temp[2] + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = a & (f ^ g) ^ c & d ^ b & h ^ f;
            e = temp[3] + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = h & (e ^ f) ^ b & c ^ a & g ^ e;
            d = temp[4] + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = g & (d ^ e) ^ a & b ^ h & f ^ d;
            c = temp[5] + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = f & (c ^ d) ^ h & a ^ g & e ^ c;
            b = temp[6] + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = e & (b ^ c) ^ g & h ^ f & d ^ b;
            a = temp[7] + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = d & (a ^ b) ^ f & g ^ e & c ^ a;
            h = temp[8] + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = c & (h ^ a) ^ e & f ^ d & b ^ h;
            g = temp[9] + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = b & (g ^ h) ^ d & e ^ c & a ^ g;
            f = temp[10] + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = a & (f ^ g) ^ c & d ^ b & h ^ f;
            e = temp[11] + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = h & (e ^ f) ^ b & c ^ a & g ^ e;
            d = temp[12] + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = g & (d ^ e) ^ a & b ^ h & f ^ d;
            c = temp[13] + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = f & (c ^ d) ^ h & a ^ g & e ^ c;
            b = temp[14] + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = e & (b ^ c) ^ g & h ^ f & d ^ b;
            a = temp[15] + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = d & (a ^ b) ^ f & g ^ e & c ^ a;
            h = temp[16] + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = c & (h ^ a) ^ e & f ^ d & b ^ h;
            g = temp[17] + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = b & (g ^ h) ^ d & e ^ c & a ^ g;
            f = temp[18] + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = a & (f ^ g) ^ c & d ^ b & h ^ f;
            e = temp[19] + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = h & (e ^ f) ^ b & c ^ a & g ^ e;
            d = temp[20] + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = g & (d ^ e) ^ a & b ^ h & f ^ d;
            c = temp[21] + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = f & (c ^ d) ^ h & a ^ g & e ^ c;
            b = temp[22] + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = e & (b ^ c) ^ g & h ^ f & d ^ b;
            a = temp[23] + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = d & (a ^ b) ^ f & g ^ e & c ^ a;
            h = temp[24] + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = c & (h ^ a) ^ e & f ^ d & b ^ h;
            g = temp[25] + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = b & (g ^ h) ^ d & e ^ c & a ^ g;
            f = temp[26] + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = a & (f ^ g) ^ c & d ^ b & h ^ f;
            e = temp[27] + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = h & (e ^ f) ^ b & c ^ a & g ^ e;
            d = temp[28] + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = g & (d ^ e) ^ a & b ^ h & f ^ d;
            c = temp[29] + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = f & (c ^ d) ^ h & a ^ g & e ^ c;
            b = temp[30] + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = e & (b ^ c) ^ g & h ^ f & d ^ b;
            a = temp[31] + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = b & (g & ~a ^ c & f ^ d ^ e) ^ c & (g ^ f) ^ a & f ^ e;
            h = temp[5] + 0x452821E6 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = a & (f & ~h ^ b & e ^ c ^ d) ^ b & (f ^ e) ^ h & e ^ d;
            g = temp[14] + 0x38D01377 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = h & (e & ~g ^ a & d ^ b ^ c) ^ a & (e ^ d) ^ g & d ^ c;
            f = temp[26] + 0xBE5466CF + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = g & (d & ~f ^ h & c ^ a ^ b) ^ h & (d ^ c) ^ f & c ^ b;
            e = temp[18] + 0x34E90C6C + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = f & (c & ~e ^ g & b ^ h ^ a) ^ g & (c ^ b) ^ e & b ^ a;
            d = temp[11] + 0xC0AC29B7 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = e & (b & ~d ^ f & a ^ g ^ h) ^ f & (b ^ a) ^ d & a ^ h;
            c = temp[28] + 0xC97C50DD + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = d & (a & ~c ^ e & h ^ f ^ g) ^ e & (a ^ h) ^ c & h ^ g;
            b = temp[7] + 0x3F84D5B5 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = c & (h & ~b ^ d & g ^ e ^ f) ^ d & (h ^ g) ^ b & g ^ f;
            a = temp[16] + 0xB5470917 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = b & (g & ~a ^ c & f ^ d ^ e) ^ c & (g ^ f) ^ a & f ^ e;
            h = temp[0] + 0x9216D5D9 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = a & (f & ~h ^ b & e ^ c ^ d) ^ b & (f ^ e) ^ h & e ^ d;
            g = temp[23] + 0x8979FB1B + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = h & (e & ~g ^ a & d ^ b ^ c) ^ a & (e ^ d) ^ g & d ^ c;
            f = temp[20] + 0xD1310BA6 + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = g & (d & ~f ^ h & c ^ a ^ b) ^ h & (d ^ c) ^ f & c ^ b;
            e = temp[22] + 0x98DFB5AC + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = f & (c & ~e ^ g & b ^ h ^ a) ^ g & (c ^ b) ^ e & b ^ a;
            d = temp[1] + 0x2FFD72DB + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = e & (b & ~d ^ f & a ^ g ^ h) ^ f & (b ^ a) ^ d & a ^ h;
            c = temp[10] + 0xD01ADFB7 + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = d & (a & ~c ^ e & h ^ f ^ g) ^ e & (a ^ h) ^ c & h ^ g;
            b = temp[4] + 0xB8E1AFED + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = c & (h & ~b ^ d & g ^ e ^ f) ^ d & (h ^ g) ^ b & g ^ f;
            a = temp[8] + 0x6A267E96 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = b & (g & ~a ^ c & f ^ d ^ e) ^ c & (g ^ f) ^ a & f ^ e;
            h = temp[30] + 0xBA7C9045 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = a & (f & ~h ^ b & e ^ c ^ d) ^ b & (f ^ e) ^ h & e ^ d;
            g = temp[3] + 0xF12C7F99 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = h & (e & ~g ^ a & d ^ b ^ c) ^ a & (e ^ d) ^ g & d ^ c;
            f = temp[21] + 0x24A19947 + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = g & (d & ~f ^ h & c ^ a ^ b) ^ h & (d ^ c) ^ f & c ^ b;
            e = temp[9] + 0xB3916CF7 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = f & (c & ~e ^ g & b ^ h ^ a) ^ g & (c ^ b) ^ e & b ^ a;
            d = temp[17] + 0x0801F2E2 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = e & (b & ~d ^ f & a ^ g ^ h) ^ f & (b ^ a) ^ d & a ^ h;
            c = temp[24] + 0x858EFC16 + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = d & (a & ~c ^ e & h ^ f ^ g) ^ e & (a ^ h) ^ c & h ^ g;
            b = temp[29] + 0x636920D8 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = c & (h & ~b ^ d & g ^ e ^ f) ^ d & (h ^ g) ^ b & g ^ f;
            a = temp[6] + 0x71574E69 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = b & (g & ~a ^ c & f ^ d ^ e) ^ c & (g ^ f) ^ a & f ^ e;
            h = temp[19] + 0xA458FEA3 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = a & (f & ~h ^ b & e ^ c ^ d) ^ b & (f ^ e) ^ h & e ^ d;
            g = temp[12] + 0xF4933D7E + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = h & (e & ~g ^ a & d ^ b ^ c) ^ a & (e ^ d) ^ g & d ^ c;
            f = temp[15] + 0x0D95748F + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = g & (d & ~f ^ h & c ^ a ^ b) ^ h & (d ^ c) ^ f & c ^ b;
            e = temp[13] + 0x728EB658 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = f & (c & ~e ^ g & b ^ h ^ a) ^ g & (c ^ b) ^ e & b ^ a;
            d = temp[2] + 0x718BCD58 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = e & (b & ~d ^ f & a ^ g ^ h) ^ f & (b ^ a) ^ d & a ^ h;
            c = temp[25] + 0x82154AEE + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = d & (a & ~c ^ e & h ^ f ^ g) ^ e & (a ^ h) ^ c & h ^ g;
            b = temp[31] + 0x7B54A41D + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = c & (h & ~b ^ d & g ^ e ^ f) ^ d & (h ^ g) ^ b & g ^ f;
            a = temp[27] + 0xC25A59B5 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = g & (c & a ^ b ^ f) ^ c & d ^ a & e ^ f;
            h = temp[19] + 0x9C30D539 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = f & (b & h ^ a ^ e) ^ b & c ^ h & d ^ e;
            g = temp[9] + 0x2AF26013 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = e & (a & g ^ h ^ d) ^ a & b ^ g & c ^ d;
            f = temp[4] + 0xC5D1B023 + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = d & (h & f ^ g ^ c) ^ h & a ^ f & b ^ c;
            e = temp[20] + 0x286085F0 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = c & (g & e ^ f ^ b) ^ g & h ^ e & a ^ b;
            d = temp[28] + 0xCA417918 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = b & (f & d ^ e ^ a) ^ f & g ^ d & h ^ a;
            c = temp[17] + 0xB8DB38EF + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = a & (e & c ^ d ^ h) ^ e & f ^ c & g ^ h;
            b = temp[8] + 0x8E79DCB0 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = h & (d & b ^ c ^ g) ^ d & e ^ b & f ^ g;
            a = temp[22] + 0x603A180E + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = g & (c & a ^ b ^ f) ^ c & d ^ a & e ^ f;
            h = temp[29] + 0x6C9E0E8B + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = f & (b & h ^ a ^ e) ^ b & c ^ h & d ^ e;
            g = temp[14] + 0xB01E8A3E + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = e & (a & g ^ h ^ d) ^ a & b ^ g & c ^ d;
            f = temp[25] + 0xD71577C1 + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = d & (h & f ^ g ^ c) ^ h & a ^ f & b ^ c;
            e = temp[12] + 0xBD314B27 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = c & (g & e ^ f ^ b) ^ g & h ^ e & a ^ b;
            d = temp[24] + 0x78AF2FDA + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = b & (f & d ^ e ^ a) ^ f & g ^ d & h ^ a;
            c = temp[30] + 0x55605C60 + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = a & (e & c ^ d ^ h) ^ e & f ^ c & g ^ h;
            b = temp[16] + 0xE65525F3 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = h & (d & b ^ c ^ g) ^ d & e ^ b & f ^ g;
            a = temp[26] + 0xAA55AB94 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = g & (c & a ^ b ^ f) ^ c & d ^ a & e ^ f;
            h = temp[31] + 0x57489862 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = f & (b & h ^ a ^ e) ^ b & c ^ h & d ^ e;
            g = temp[15] + 0x63E81440 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = e & (a & g ^ h ^ d) ^ a & b ^ g & c ^ d;
            f = temp[7] + 0x55CA396A + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = d & (h & f ^ g ^ c) ^ h & a ^ f & b ^ c;
            e = temp[3] + 0x2AAB10B6 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = c & (g & e ^ f ^ b) ^ g & h ^ e & a ^ b;
            d = temp[1] + 0xB4CC5C34 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = b & (f & d ^ e ^ a) ^ f & g ^ d & h ^ a;
            c = temp[0] + 0x1141E8CE + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = a & (e & c ^ d ^ h) ^ e & f ^ c & g ^ h;
            b = temp[18] + 0xA15486AF + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = h & (d & b ^ c ^ g) ^ d & e ^ b & f ^ g;
            a = temp[27] + 0x7C72E993 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = g & (c & a ^ b ^ f) ^ c & d ^ a & e ^ f;
            h = temp[13] + 0xB3EE1411 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = f & (b & h ^ a ^ e) ^ b & c ^ h & d ^ e;
            g = temp[6] + 0x636FBC2A + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = e & (a & g ^ h ^ d) ^ a & b ^ g & c ^ d;
            f = temp[21] + 0x2BA9C55D + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = d & (h & f ^ g ^ c) ^ h & a ^ f & b ^ c;
            e = temp[10] + 0x741831F6 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = c & (g & e ^ f ^ b) ^ g & h ^ e & a ^ b;
            d = temp[23] + 0xCE5C3E16 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = b & (f & d ^ e ^ a) ^ f & g ^ d & h ^ a;
            c = temp[11] + 0x9B87931E + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = a & (e & c ^ d ^ h) ^ e & f ^ c & g ^ h;
            b = temp[5] + 0xAFD6BA33 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = h & (d & b ^ c ^ g) ^ d & e ^ b & f ^ g;
            a = temp[2] + 0x6C24CF5C + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = a & (e & ~c ^ f & ~g ^ b ^ g ^ d) ^ f & (b & c ^ e ^ g) ^ c & g ^ d;
            h = temp[24] + 0x7A325381 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = h & (d & ~b ^ e & ~f ^ a ^ f ^ c) ^ e & (a & b ^ d ^ f) ^ b & f ^ c;
            g = temp[4] + 0x28958677 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = g & (c & ~a ^ d & ~e ^ h ^ e ^ b) ^ d & (h & a ^ c ^ e) ^ a & e ^ b;
            f = temp[0] + 0x3B8F4898 + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = f & (b & ~h ^ c & ~d ^ g ^ d ^ a) ^ c & (g & h ^ b ^ d) ^ h & d ^ a;
            e = temp[14] + 0x6B4BB9AF + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = e & (a & ~g ^ b & ~c ^ f ^ c ^ h) ^ b & (f & g ^ a ^ c) ^ g & c ^ h;
            d = temp[2] + 0xC4BFE81B + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = d & (h & ~f ^ a & ~b ^ e ^ b ^ g) ^ a & (e & f ^ h ^ b) ^ f & b ^ g;
            c = temp[7] + 0x66282193 + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = c & (g & ~e ^ h & ~a ^ d ^ a ^ f) ^ h & (d & e ^ g ^ a) ^ e & a ^ f;
            b = temp[28] + 0x61D809CC + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = b & (f & ~d ^ g & ~h ^ c ^ h ^ e) ^ g & (c & d ^ f ^ h) ^ d & h ^ e;
            a = temp[23] + 0xFB21A991 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = a & (e & ~c ^ f & ~g ^ b ^ g ^ d) ^ f & (b & c ^ e ^ g) ^ c & g ^ d;
            h = temp[26] + 0x487CAC60 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = h & (d & ~b ^ e & ~f ^ a ^ f ^ c) ^ e & (a & b ^ d ^ f) ^ b & f ^ c;
            g = temp[6] + 0x5DEC8032 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = g & (c & ~a ^ d & ~e ^ h ^ e ^ b) ^ d & (h & a ^ c ^ e) ^ a & e ^ b;
            f = temp[30] + 0xEF845D5D + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = f & (b & ~h ^ c & ~d ^ g ^ d ^ a) ^ c & (g & h ^ b ^ d) ^ h & d ^ a;
            e = temp[20] + 0xE98575B1 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = e & (a & ~g ^ b & ~c ^ f ^ c ^ h) ^ b & (f & g ^ a ^ c) ^ g & c ^ h;
            d = temp[18] + 0xDC262302 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = d & (h & ~f ^ a & ~b ^ e ^ b ^ g) ^ a & (e & f ^ h ^ b) ^ f & b ^ g;
            c = temp[25] + 0xEB651B88 + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = c & (g & ~e ^ h & ~a ^ d ^ a ^ f) ^ h & (d & e ^ g ^ a) ^ e & a ^ f;
            b = temp[19] + 0x23893E81 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = b & (f & ~d ^ g & ~h ^ c ^ h ^ e) ^ g & (c & d ^ f ^ h) ^ d & h ^ e;
            a = temp[3] + 0xD396ACC5 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = a & (e & ~c ^ f & ~g ^ b ^ g ^ d) ^ f & (b & c ^ e ^ g) ^ c & g ^ d;
            h = temp[22] + 0x0F6D6FF3 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = h & (d & ~b ^ e & ~f ^ a ^ f ^ c) ^ e & (a & b ^ d ^ f) ^ b & f ^ c;
            g = temp[11] + 0x83F44239 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = g & (c & ~a ^ d & ~e ^ h ^ e ^ b) ^ d & (h & a ^ c ^ e) ^ a & e ^ b;
            f = temp[31] + 0x2E0B4482 + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = f & (b & ~h ^ c & ~d ^ g ^ d ^ a) ^ c & (g & h ^ b ^ d) ^ h & d ^ a;
            e = temp[21] + 0xA4842004 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = e & (a & ~g ^ b & ~c ^ f ^ c ^ h) ^ b & (f & g ^ a ^ c) ^ g & c ^ h;
            d = temp[8] + 0x69C8F04A + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = d & (h & ~f ^ a & ~b ^ e ^ b ^ g) ^ a & (e & f ^ h ^ b) ^ f & b ^ g;
            c = temp[27] + 0x9E1F9B5E + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = c & (g & ~e ^ h & ~a ^ d ^ a ^ f) ^ h & (d & e ^ g ^ a) ^ e & a ^ f;
            b = temp[12] + 0x21C66842 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = b & (f & ~d ^ g & ~h ^ c ^ h ^ e) ^ g & (c & d ^ f ^ h) ^ d & h ^ e;
            a = temp[9] + 0xF6E96C9A + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = a & (e & ~c ^ f & ~g ^ b ^ g ^ d) ^ f & (b & c ^ e ^ g) ^ c & g ^ d;
            h = temp[1] + 0x670C9C61 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = h & (d & ~b ^ e & ~f ^ a ^ f ^ c) ^ e & (a & b ^ d ^ f) ^ b & f ^ c;
            g = temp[29] + 0xABD388F0 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = g & (c & ~a ^ d & ~e ^ h ^ e ^ b) ^ d & (h & a ^ c ^ e) ^ a & e ^ b;
            f = temp[5] + 0x6A51A0D2 + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = f & (b & ~h ^ c & ~d ^ g ^ d ^ a) ^ c & (g & h ^ b ^ d) ^ h & d ^ a;
            e = temp[15] + 0xD8542F68 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = e & (a & ~g ^ b & ~c ^ f ^ c ^ h) ^ b & (f & g ^ a ^ c) ^ g & c ^ h;
            d = temp[17] + 0x960FA728 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = d & (h & ~f ^ a & ~b ^ e ^ b ^ g) ^ a & (e & f ^ h ^ b) ^ f & b ^ g;
            c = temp[10] + 0xAB5133A3 + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = c & (g & ~e ^ h & ~a ^ d ^ a ^ f) ^ h & (d & e ^ g ^ a) ^ e & a ^ f;
            b = temp[16] + 0x6EEF0B6C + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = b & (f & ~d ^ g & ~h ^ c ^ h ^ e) ^ g & (c & d ^ f ^ h) ^ d & h ^ e;
            a = temp[13] + 0x137A3BE4 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            m_hash[0] += a;
            m_hash[1] += b;
            m_hash[2] += c;
            m_hash[3] += d;
            m_hash[4] += e;
            m_hash[5] += f;
            m_hash[6] += g;
            m_hash[7] += h;
        }
    }

    internal abstract class Haval5 : Haval
    {
        internal Haval5(HashSize a_hash_size)
            : base(HashRounds.Rounds5, a_hash_size)
        {
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            uint[] temp = Converters.ConvertBytesToUInts(a_data, a_index, BlockSize);

            uint a = m_hash[0];
            uint b = m_hash[1];
            uint c = m_hash[2];
            uint d = m_hash[3];
            uint e = m_hash[4];
            uint f = m_hash[5];
            uint g = m_hash[6];
            uint h = m_hash[7];

            uint t = 0;

            t = c & (g ^ b) ^ f & e ^ a & d ^ g;
            h = temp[0] + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = b & (f ^ a) ^ e & d ^ h & c ^ f;
            g = temp[1] + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = a & (e ^ h) ^ d & c ^ g & b ^ e;
            f = temp[2] + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = h & (d ^ g) ^ c & b ^ f & a ^ d;
            e = temp[3] + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = g & (c ^ f) ^ b & a ^ e & h ^ c;
            d = temp[4] + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = f & (b ^ e) ^ a & h ^ d & g ^ b;
            c = temp[5] + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = e & (a ^ d) ^ h & g ^ c & f ^ a;
            b = temp[6] + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = d & (h ^ c) ^ g & f ^ b & e ^ h;
            a = temp[7] + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = c & (g ^ b) ^ f & e ^ a & d ^ g;
            h = temp[8] + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = b & (f ^ a) ^ e & d ^ h & c ^ f;
            g = temp[9] + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = a & (e ^ h) ^ d & c ^ g & b ^ e;
            f = temp[10] + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = h & (d ^ g) ^ c & b ^ f & a ^ d;
            e = temp[11] + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = g & (c ^ f) ^ b & a ^ e & h ^ c;
            d = temp[12] + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = f & (b ^ e) ^ a & h ^ d & g ^ b;
            c = temp[13] + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = e & (a ^ d) ^ h & g ^ c & f ^ a;
            b = temp[14] + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = d & (h ^ c) ^ g & f ^ b & e ^ h;
            a = temp[15] + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = c & (g ^ b) ^ f & e ^ a & d ^ g;
            h = temp[16] + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = b & (f ^ a) ^ e & d ^ h & c ^ f;
            g = temp[17] + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = a & (e ^ h) ^ d & c ^ g & b ^ e;
            f = temp[18] + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = h & (d ^ g) ^ c & b ^ f & a ^ d;
            e = temp[19] + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = g & (c ^ f) ^ b & a ^ e & h ^ c;
            d = temp[20] + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = f & (b ^ e) ^ a & h ^ d & g ^ b;
            c = temp[21] + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = e & (a ^ d) ^ h & g ^ c & f ^ a;
            b = temp[22] + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = d & (h ^ c) ^ g & f ^ b & e ^ h;
            a = temp[23] + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = c & (g ^ b) ^ f & e ^ a & d ^ g;
            h = temp[24] + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = b & (f ^ a) ^ e & d ^ h & c ^ f;
            g = temp[25] + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = a & (e ^ h) ^ d & c ^ g & b ^ e;
            f = temp[26] + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = h & (d ^ g) ^ c & b ^ f & a ^ d;
            e = temp[27] + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = g & (c ^ f) ^ b & a ^ e & h ^ c;
            d = temp[28] + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = f & (b ^ e) ^ a & h ^ d & g ^ b;
            c = temp[29] + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = e & (a ^ d) ^ h & g ^ c & f ^ a;
            b = temp[30] + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = d & (h ^ c) ^ g & f ^ b & e ^ h;
            a = temp[31] + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = d & (e & ~a ^ b & c ^ g ^ f) ^ b & (e ^ c) ^ a & c ^ f;
            h = temp[5] + 0x452821E6 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = c & (d & ~h ^ a & b ^ f ^ e) ^ a & (d ^ b) ^ h & b ^ e;
            g = temp[14] + 0x38D01377 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = b & (c & ~g ^ h & a ^ e ^ d) ^ h & (c ^ a) ^ g & a ^ d;
            f = temp[26] + 0xBE5466CF + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = a & (b & ~f ^ g & h ^ d ^ c) ^ g & (b ^ h) ^ f & h ^ c;
            e = temp[18] + 0x34E90C6C + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = h & (a & ~e ^ f & g ^ c ^ b) ^ f & (a ^ g) ^ e & g ^ b;
            d = temp[11] + 0xC0AC29B7 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = g & (h & ~d ^ e & f ^ b ^ a) ^ e & (h ^ f) ^ d & f ^ a;
            c = temp[28] + 0xC97C50DD + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = f & (g & ~c ^ d & e ^ a ^ h) ^ d & (g ^ e) ^ c & e ^ h;
            b = temp[7] + 0x3F84D5B5 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = e & (f & ~b ^ c & d ^ h ^ g) ^ c & (f ^ d) ^ b & d ^ g;
            a = temp[16] + 0xB5470917 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = d & (e & ~a ^ b & c ^ g ^ f) ^ b & (e ^ c) ^ a & c ^ f;
            h = temp[0] + 0x9216D5D9 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = c & (d & ~h ^ a & b ^ f ^ e) ^ a & (d ^ b) ^ h & b ^ e;
            g = temp[23] + 0x8979FB1B + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = b & (c & ~g ^ h & a ^ e ^ d) ^ h & (c ^ a) ^ g & a ^ d;
            f = temp[20] + 0xD1310BA6 + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = a & (b & ~f ^ g & h ^ d ^ c) ^ g & (b ^ h) ^ f & h ^ c;
            e = temp[22] + 0x98DFB5AC + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = h & (a & ~e ^ f & g ^ c ^ b) ^ f & (a ^ g) ^ e & g ^ b;
            d = temp[1] + 0x2FFD72DB + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = g & (h & ~d ^ e & f ^ b ^ a) ^ e & (h ^ f) ^ d & f ^ a;
            c = temp[10] + 0xD01ADFB7 + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = f & (g & ~c ^ d & e ^ a ^ h) ^ d & (g ^ e) ^ c & e ^ h;
            b = temp[4] + 0xB8E1AFED + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = e & (f & ~b ^ c & d ^ h ^ g) ^ c & (f ^ d) ^ b & d ^ g;
            a = temp[8] + 0x6A267E96 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = d & (e & ~a ^ b & c ^ g ^ f) ^ b & (e ^ c) ^ a & c ^ f;
            h = temp[30] + 0xBA7C9045 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = c & (d & ~h ^ a & b ^ f ^ e) ^ a & (d ^ b) ^ h & b ^ e;
            g = temp[3] + 0xF12C7F99 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = b & (c & ~g ^ h & a ^ e ^ d) ^ h & (c ^ a) ^ g & a ^ d;
            f = temp[21] + 0x24A19947 + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = a & (b & ~f ^ g & h ^ d ^ c) ^ g & (b ^ h) ^ f & h ^ c;
            e = temp[9] + 0xB3916CF7 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = h & (a & ~e ^ f & g ^ c ^ b) ^ f & (a ^ g) ^ e & g ^ b;
            d = temp[17] + 0x0801F2E2 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = g & (h & ~d ^ e & f ^ b ^ a) ^ e & (h ^ f) ^ d & f ^ a;
            c = temp[24] + 0x858EFC16 + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = f & (g & ~c ^ d & e ^ a ^ h) ^ d & (g ^ e) ^ c & e ^ h;
            b = temp[29] + 0x636920D8 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = e & (f & ~b ^ c & d ^ h ^ g) ^ c & (f ^ d) ^ b & d ^ g;
            a = temp[6] + 0x71574E69 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = d & (e & ~a ^ b & c ^ g ^ f) ^ b & (e ^ c) ^ a & c ^ f;
            h = temp[19] + 0xA458FEA3 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = c & (d & ~h ^ a & b ^ f ^ e) ^ a & (d ^ b) ^ h & b ^ e;
            g = temp[12] + 0xF4933D7E + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = b & (c & ~g ^ h & a ^ e ^ d) ^ h & (c ^ a) ^ g & a ^ d;
            f = temp[15] + 0x0D95748F + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = a & (b & ~f ^ g & h ^ d ^ c) ^ g & (b ^ h) ^ f & h ^ c;
            e = temp[13] + 0x728EB658 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = h & (a & ~e ^ f & g ^ c ^ b) ^ f & (a ^ g) ^ e & g ^ b;
            d = temp[2] + 0x718BCD58 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = g & (h & ~d ^ e & f ^ b ^ a) ^ e & (h ^ f) ^ d & f ^ a;
            c = temp[25] + 0x82154AEE + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = f & (g & ~c ^ d & e ^ a ^ h) ^ d & (g ^ e) ^ c & e ^ h;
            b = temp[31] + 0x7B54A41D + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = e & (f & ~b ^ c & d ^ h ^ g) ^ c & (f ^ d) ^ b & d ^ g;
            a = temp[27] + 0xC25A59B5 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = e & (b & d ^ c ^ f) ^ b & a ^ d & g ^ f;
            h = temp[19] + 0x9C30D539 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = d & (a & c ^ b ^ e) ^ a & h ^ c & f ^ e;
            g = temp[9] + 0x2AF26013 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = c & (h & b ^ a ^ d) ^ h & g ^ b & e ^ d;
            f = temp[4] + 0xC5D1B023 + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = b & (g & a ^ h ^ c) ^ g & f ^ a & d ^ c;
            e = temp[20] + 0x286085F0 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = a & (f & h ^ g ^ b) ^ f & e ^ h & c ^ b;
            d = temp[28] + 0xCA417918 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = h & (e & g ^ f ^ a) ^ e & d ^ g & b ^ a;
            c = temp[17] + 0xB8DB38EF + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = g & (d & f ^ e ^ h) ^ d & c ^ f & a ^ h;
            b = temp[8] + 0x8E79DCB0 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = f & (c & e ^ d ^ g) ^ c & b ^ e & h ^ g;
            a = temp[22] + 0x603A180E + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = e & (b & d ^ c ^ f) ^ b & a ^ d & g ^ f;
            h = temp[29] + 0x6C9E0E8B + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = d & (a & c ^ b ^ e) ^ a & h ^ c & f ^ e;
            g = temp[14] + 0xB01E8A3E + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = c & (h & b ^ a ^ d) ^ h & g ^ b & e ^ d;
            f = temp[25] + 0xD71577C1 + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = b & (g & a ^ h ^ c) ^ g & f ^ a & d ^ c;
            e = temp[12] + 0xBD314B27 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = a & (f & h ^ g ^ b) ^ f & e ^ h & c ^ b;
            d = temp[24] + 0x78AF2FDA + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = h & (e & g ^ f ^ a) ^ e & d ^ g & b ^ a;
            c = temp[30] + 0x55605C60 + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = g & (d & f ^ e ^ h) ^ d & c ^ f & a ^ h;
            b = temp[16] + 0xE65525F3 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = f & (c & e ^ d ^ g) ^ c & b ^ e & h ^ g;
            a = temp[26] + 0xAA55AB94 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = e & (b & d ^ c ^ f) ^ b & a ^ d & g ^ f;
            h = temp[31] + 0x57489862 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = d & (a & c ^ b ^ e) ^ a & h ^ c & f ^ e;
            g = temp[15] + 0x63E81440 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = c & (h & b ^ a ^ d) ^ h & g ^ b & e ^ d;
            f = temp[7] + 0x55CA396A + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = b & (g & a ^ h ^ c) ^ g & f ^ a & d ^ c;
            e = temp[3] + 0x2AAB10B6 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = a & (f & h ^ g ^ b) ^ f & e ^ h & c ^ b;
            d = temp[1] + 0xB4CC5C34 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = h & (e & g ^ f ^ a) ^ e & d ^ g & b ^ a;
            c = temp[0] + 0x1141E8CE + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = g & (d & f ^ e ^ h) ^ d & c ^ f & a ^ h;
            b = temp[18] + 0xA15486AF + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = f & (c & e ^ d ^ g) ^ c & b ^ e & h ^ g;
            a = temp[27] + 0x7C72E993 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = e & (b & d ^ c ^ f) ^ b & a ^ d & g ^ f;
            h = temp[13] + 0xB3EE1411 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = d & (a & c ^ b ^ e) ^ a & h ^ c & f ^ e;
            g = temp[6] + 0x636FBC2A + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = c & (h & b ^ a ^ d) ^ h & g ^ b & e ^ d;
            f = temp[21] + 0x2BA9C55D + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = b & (g & a ^ h ^ c) ^ g & f ^ a & d ^ c;
            e = temp[10] + 0x741831F6 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = a & (f & h ^ g ^ b) ^ f & e ^ h & c ^ b;
            d = temp[23] + 0xCE5C3E16 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = h & (e & g ^ f ^ a) ^ e & d ^ g & b ^ a;
            c = temp[11] + 0x9B87931E + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = g & (d & f ^ e ^ h) ^ d & c ^ f & a ^ h;
            b = temp[5] + 0xAFD6BA33 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = f & (c & e ^ d ^ g) ^ c & b ^ e & h ^ g;
            a = temp[2] + 0x6C24CF5C + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = d & (f & ~a ^ c & ~b ^ e ^ b ^ g) ^ c & (e & a ^ f ^ b) ^ a & b ^ g;
            h = temp[24] + 0x7A325381 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = c & (e & ~h ^ b & ~a ^ d ^ a ^ f) ^ b & (d & h ^ e ^ a) ^ h & a ^ f;
            g = temp[4] + 0x28958677 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = b & (d & ~g ^ a & ~h ^ c ^ h ^ e) ^ a & (c & g ^ d ^ h) ^ g & h ^ e;
            f = temp[0] + 0x3B8F4898 + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = a & (c & ~f ^ h & ~g ^ b ^ g ^ d) ^ h & (b & f ^ c ^ g) ^ f & g ^ d;
            e = temp[14] + 0x6B4BB9AF + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = h & (b & ~e ^ g & ~f ^ a ^ f ^ c) ^ g & (a & e ^ b ^ f) ^ e & f ^ c;
            d = temp[2] + 0xC4BFE81B + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = g & (a & ~d ^ f & ~e ^ h ^ e ^ b) ^ f & (h & d ^ a ^ e) ^ d & e ^ b;
            c = temp[7] + 0x66282193 + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = f & (h & ~c ^ e & ~d ^ g ^ d ^ a) ^ e & (g & c ^ h ^ d) ^ c & d ^ a;
            b = temp[28] + 0x61D809CC + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = e & (g & ~b ^ d & ~c ^ f ^ c ^ h) ^ d & (f & b ^ g ^ c) ^ b & c ^ h;
            a = temp[23] + 0xFB21A991 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = d & (f & ~a ^ c & ~b ^ e ^ b ^ g) ^ c & (e & a ^ f ^ b) ^ a & b ^ g;
            h = temp[26] + 0x487CAC60 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = c & (e & ~h ^ b & ~a ^ d ^ a ^ f) ^ b & (d & h ^ e ^ a) ^ h & a ^ f;
            g = temp[6] + 0x5DEC8032 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = b & (d & ~g ^ a & ~h ^ c ^ h ^ e) ^ a & (c & g ^ d ^ h) ^ g & h ^ e;
            f = temp[30] + 0xEF845D5D + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = a & (c & ~f ^ h & ~g ^ b ^ g ^ d) ^ h & (b & f ^ c ^ g) ^ f & g ^ d;
            e = temp[20] + 0xE98575B1 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = h & (b & ~e ^ g & ~f ^ a ^ f ^ c) ^ g & (a & e ^ b ^ f) ^ e & f ^ c;
            d = temp[18] + 0xDC262302 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = g & (a & ~d ^ f & ~e ^ h ^ e ^ b) ^ f & (h & d ^ a ^ e) ^ d & e ^ b;
            c = temp[25] + 0xEB651B88 + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = f & (h & ~c ^ e & ~d ^ g ^ d ^ a) ^ e & (g & c ^ h ^ d) ^ c & d ^ a;
            b = temp[19] + 0x23893E81 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = e & (g & ~b ^ d & ~c ^ f ^ c ^ h) ^ d & (f & b ^ g ^ c) ^ b & c ^ h;
            a = temp[3] + 0xD396ACC5 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = d & (f & ~a ^ c & ~b ^ e ^ b ^ g) ^ c & (e & a ^ f ^ b) ^ a & b ^ g;
            h = temp[22] + 0x0F6D6FF3 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = c & (e & ~h ^ b & ~a ^ d ^ a ^ f) ^ b & (d & h ^ e ^ a) ^ h & a ^ f;
            g = temp[11] + 0x83F44239 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = b & (d & ~g ^ a & ~h ^ c ^ h ^ e) ^ a & (c & g ^ d ^ h) ^ g & h ^ e;
            f = temp[31] + 0x2E0B4482 + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = a & (c & ~f ^ h & ~g ^ b ^ g ^ d) ^ h & (b & f ^ c ^ g) ^ f & g ^ d;
            e = temp[21] + 0xA4842004 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = h & (b & ~e ^ g & ~f ^ a ^ f ^ c) ^ g & (a & e ^ b ^ f) ^ e & f ^ c;
            d = temp[8] + 0x69C8F04A + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = g & (a & ~d ^ f & ~e ^ h ^ e ^ b) ^ f & (h & d ^ a ^ e) ^ d & e ^ b;
            c = temp[27] + 0x9E1F9B5E + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = f & (h & ~c ^ e & ~d ^ g ^ d ^ a) ^ e & (g & c ^ h ^ d) ^ c & d ^ a;
            b = temp[12] + 0x21C66842 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = e & (g & ~b ^ d & ~c ^ f ^ c ^ h) ^ d & (f & b ^ g ^ c) ^ b & c ^ h;
            a = temp[9] + 0xF6E96C9A + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = d & (f & ~a ^ c & ~b ^ e ^ b ^ g) ^ c & (e & a ^ f ^ b) ^ a & b ^ g;
            h = temp[1] + 0x670C9C61 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = c & (e & ~h ^ b & ~a ^ d ^ a ^ f) ^ b & (d & h ^ e ^ a) ^ h & a ^ f;
            g = temp[29] + 0xABD388F0 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = b & (d & ~g ^ a & ~h ^ c ^ h ^ e) ^ a & (c & g ^ d ^ h) ^ g & h ^ e;
            f = temp[5] + 0x6A51A0D2 + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = a & (c & ~f ^ h & ~g ^ b ^ g ^ d) ^ h & (b & f ^ c ^ g) ^ f & g ^ d;
            e = temp[15] + 0xD8542F68 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = h & (b & ~e ^ g & ~f ^ a ^ f ^ c) ^ g & (a & e ^ b ^ f) ^ e & f ^ c;
            d = temp[17] + 0x960FA728 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = g & (a & ~d ^ f & ~e ^ h ^ e ^ b) ^ f & (h & d ^ a ^ e) ^ d & e ^ b;
            c = temp[10] + 0xAB5133A3 + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = f & (h & ~c ^ e & ~d ^ g ^ d ^ a) ^ e & (g & c ^ h ^ d) ^ c & d ^ a;
            b = temp[16] + 0x6EEF0B6C + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = e & (g & ~b ^ d & ~c ^ f ^ c ^ h) ^ d & (f & b ^ g ^ c) ^ b & c ^ h;
            a = temp[13] + 0x137A3BE4 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = b & (d & e & g ^ ~f) ^ d & a ^ e & f ^ g & c;
            h = temp[27] + 0xBA3BF050 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = a & (c & d & f ^ ~e) ^ c & h ^ d & e ^ f & b;
            g = temp[3] + 0x7EFB2A98 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = h & (b & c & e ^ ~d) ^ b & g ^ c & d ^ e & a;
            f = temp[21] + 0xA1F1651D + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = g & (a & b & d ^ ~c) ^ a & f ^ b & c ^ d & h;
            e = temp[26] + 0x39AF0176 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = f & (h & a & c ^ ~b) ^ h & e ^ a & b ^ c & g;
            d = temp[17] + 0x66CA593E + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = e & (g & h & b ^ ~a) ^ g & d ^ h & a ^ b & f;
            c = temp[11] + 0x82430E88 + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = d & (f & g & a ^ ~h) ^ f & c ^ g & h ^ a & e;
            b = temp[20] + 0x8CEE8619 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = c & (e & f & h ^ ~g) ^ e & b ^ f & g ^ h & d;
            a = temp[29] + 0x456F9FB4 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = b & (d & e & g ^ ~f) ^ d & a ^ e & f ^ g & c;
            h = temp[19] + 0x7D84A5C3 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = a & (c & d & f ^ ~e) ^ c & h ^ d & e ^ f & b;
            g = temp[0] + 0x3B8B5EBE + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = h & (b & c & e ^ ~d) ^ b & g ^ c & d ^ e & a;
            f = temp[12] + 0xE06F75D8 + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = g & (a & b & d ^ ~c) ^ a & f ^ b & c ^ d & h;
            e = temp[7] + 0x85C12073 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = f & (h & a & c ^ ~b) ^ h & e ^ a & b ^ c & g;
            d = temp[13] + 0x401A449F + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = e & (g & h & b ^ ~a) ^ g & d ^ h & a ^ b & f;
            c = temp[8] + 0x56C16AA6 + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = d & (f & g & a ^ ~h) ^ f & c ^ g & h ^ a & e;
            b = temp[31] + 0x4ED3AA62 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = c & (e & f & h ^ ~g) ^ e & b ^ f & g ^ h & d;
            a = temp[10] + 0x363F7706 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = b & (d & e & g ^ ~f) ^ d & a ^ e & f ^ g & c;
            h = temp[5] + 0x1BFEDF72 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = a & (c & d & f ^ ~e) ^ c & h ^ d & e ^ f & b;
            g = temp[9] + 0x429B023D + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = h & (b & c & e ^ ~d) ^ b & g ^ c & d ^ e & a;
            f = temp[14] + 0x37D0D724 + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = g & (a & b & d ^ ~c) ^ a & f ^ b & c ^ d & h;
            e = temp[30] + 0xD00A1248 + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = f & (h & a & c ^ ~b) ^ h & e ^ a & b ^ c & g;
            d = temp[18] + 0xDB0FEAD3 + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = e & (g & h & b ^ ~a) ^ g & d ^ h & a ^ b & f;
            c = temp[6] + 0x49F1C09B + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = d & (f & g & a ^ ~h) ^ f & c ^ g & h ^ a & e;
            b = temp[28] + 0x075372C9 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = c & (e & f & h ^ ~g) ^ e & b ^ f & g ^ h & d;
            a = temp[24] + 0x80991B7B + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            t = b & (d & e & g ^ ~f) ^ d & a ^ e & f ^ g & c;
            h = temp[2] + 0x25D479D8 + (t >> 7 | t << 25) + (h >> 11 | h << 21);

            t = a & (c & d & f ^ ~e) ^ c & h ^ d & e ^ f & b;
            g = temp[23] + 0xF6E8DEF7 + (t >> 7 | t << 25) + (g >> 11 | g << 21);

            t = h & (b & c & e ^ ~d) ^ b & g ^ c & d ^ e & a;
            f = temp[16] + 0xE3FE501A + (t >> 7 | t << 25) + (f >> 11 | f << 21);

            t = g & (a & b & d ^ ~c) ^ a & f ^ b & c ^ d & h;
            e = temp[22] + 0xB6794C3B + (t >> 7 | t << 25) + (e >> 11 | e << 21);

            t = f & (h & a & c ^ ~b) ^ h & e ^ a & b ^ c & g;
            d = temp[4] + 0x976CE0BD + (t >> 7 | t << 25) + (d >> 11 | d << 21);

            t = e & (g & h & b ^ ~a) ^ g & d ^ h & a ^ b & f;
            c = temp[1] + 0x04C006BA + (t >> 7 | t << 25) + (c >> 11 | c << 21);

            t = d & (f & g & a ^ ~h) ^ f & c ^ g & h ^ a & e;
            b = temp[25] + 0xC1A94FB6 + (t >> 7 | t << 25) + (b >> 11 | b << 21);

            t = c & (e & f & h ^ ~g) ^ e & b ^ f & g ^ h & d;
            a = temp[15] + 0x409F60C4 + (t >> 7 | t << 25) + (a >> 11 | a << 21);

            m_hash[0] += a;
            m_hash[1] += b;
            m_hash[2] += c;
            m_hash[3] += d;
            m_hash[4] += e;
            m_hash[5] += f;
            m_hash[6] += g;
            m_hash[7] += h;
        }
    }

}
