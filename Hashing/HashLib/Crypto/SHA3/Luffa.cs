using System;

namespace HashLib.Crypto.SHA3
{
    internal abstract class LuffaBase : BlockHash, ICryptoNotBuildIn
    {
        #region Consts

        private static readonly uint[] s_IV = 
        {
            0x6d251e69,0x44b051e0,0x4eaa6fb4,0xdbf78465,
            0x6e292011,0x90152df4,0xee058139,0xdef610bb,
            0xc3b44b95,0xd9d2f256,0x70eee9a0,0xde099fa3,
            0x5d9b0557,0x8fc944b3,0xcf1ccf0e,0x746cd581,
            0xf7efc89d,0x5dba5781,0x04016ce5,0xad659c05,
            0x0306194f,0x666d1836,0x24aa230a,0x8b264ae7,
            0x858075d5,0x36d79cce,0xe571f7d7,0x204b1f67,
            0x35870c6a,0x57e9e923,0x14bcb808,0x7cde72ce,
            0x6c68e9be,0x5ec41e22,0xc825b7c7,0xaffb4363,
            0xf5df3999,0x0fc688f1,0xb07224cc,0x03e86cea
        };

        protected static readonly uint[] s_CNS = 
        {
            0x303994a6,0xe0337818,0xc0e65299,0x441ba90d,
            0x6cc33a12,0x7f34d442,0xdc56983e,0x9389217f,
            0x1e00108f,0xe5a8bce6,0x7800423d,0x5274baf4,
            0x8f5b7882,0x26889ba7,0x96e1db12,0x9a226e9d,
            0xb6de10ed,0x01685f3d,0x70f47aae,0x05a17cf4,
            0x0707a3d4,0xbd09caca,0x1c1e8f51,0xf4272b28,
            0x707a3d45,0x144ae5cc,0xaeb28562,0xfaa7ae2b,
            0xbaca1589,0x2e48f1c1,0x40a46f3e,0xb923c704,
            0xfc20d9d2,0xe25e72c1,0x34552e25,0xe623bb72,
            0x7ad8818f,0x5c58a4a4,0x8438764a,0x1e38e2e7,
            0xbb6de032,0x78e38b9d,0xedb780c8,0x27586719,
            0xd9847356,0x36eda57f,0xa2c78434,0x703aace7,
            0xb213afa5,0xe028c9bf,0xc84ebe95,0x44756f91,
            0x4e608a22,0x7e8fce32,0x56d858fe,0x956548be,
            0x343b138f,0xfe191be2,0xd0ec4e3d,0x3cb226e5,
            0x2ceb4882,0x5944a28e,0xb3ad2208,0xa1c4c355,
            0xf0d2e9e3,0x5090d577,0xac11d7fa,0x2d1925ab,
            0x1bcb66f2,0xb46496ac,0x6f2d9bc9,0xd1925ab0,
            0x78602649,0x29131ab6,0x8edae952,0x0fc053c3,
            0x3b6ba548,0x3f014f0c,0xedae9520,0xfc053c31
        };

        #endregion

        protected readonly uint[] m_state = new uint[40];
        protected int m_result_blocks;
        protected int m_iv_length;

        public LuffaBase(HashLib.HashSize a_hash_size)
            : base((int)a_hash_size, 32)
        {
        }

        protected override byte[] GetResult()
        {
            byte[] zeroes = new byte[BlockSize];
            uint[] result = new uint[HashSize / 4];

            for (int i = 0; i < HashSize / 4; i++)
            {
                if (i % 8 == 0)
                    TransformBlock(zeroes, 0);

                for (int j = 0; j < m_result_blocks; j++)
                    result[i] ^= m_state[(i % 8) + 8 * j];
            }

            return Converters.ConvertUIntsToBytesSwapOrder(result);
        }

        protected override void Finish()
        {
            byte[] pad = new byte[BlockSize - m_buffer.Pos];
            pad[0] = 0x80;
            TransformBytes(pad, 0, pad.Length);
        }

        public override void Initialize()
        {
            Array.Copy(s_IV, 0, m_state, 0, m_iv_length);

            base.Initialize();
        }
    };

    internal abstract class Luffa256Base : LuffaBase
    {
        public Luffa256Base(HashLib.HashSize a_hash_size)
            : base(a_hash_size)
        {
            m_result_blocks = 3;
            m_iv_length = 24;

            Initialize();
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            uint chainv0, chainv1, chainv2, chainv3, chainv4, chainv5, chainv6, chainv7;
            uint tmp;

            uint[] data = Converters.ConvertBytesToUIntsSwapOrder(a_data, a_index, BlockSize);

            uint t0 = m_state[0] ^ m_state[0 + 8] ^ m_state[0 + 16];
            uint t1 = m_state[1] ^ m_state[1 + 8] ^ m_state[1 + 16];
            uint t2 = m_state[2] ^ m_state[2 + 8] ^ m_state[2 + 16];
            uint t3 = m_state[3] ^ m_state[3 + 8] ^ m_state[3 + 16];
            uint t4 = m_state[4] ^ m_state[4 + 8] ^ m_state[4 + 16];
            uint t5 = m_state[5] ^ m_state[5 + 8] ^ m_state[5 + 16];
            uint t6 = m_state[6] ^ m_state[6 + 8] ^ m_state[6 + 16];
            uint t7 = m_state[7] ^ m_state[7 + 8] ^ m_state[7 + 16];

            tmp = t7;
            t7 = t6;
            t6 = t5;
            t5 = t4;
            t4 = t3 ^ tmp;
            t3 = t2 ^ tmp;
            t2 = t1;
            t1 = t0 ^ tmp;
            t0 = tmp;

            for (int j = 0; j < 3; j++)
            {
                m_state[0 + 8 * j] ^= t0 ^ data[0];
                m_state[1 + 8 * j] ^= t1 ^ data[1];
                m_state[2 + 8 * j] ^= t2 ^ data[2];
                m_state[3 + 8 * j] ^= t3 ^ data[3];
                m_state[4 + 8 * j] ^= t4 ^ data[4];
                m_state[5 + 8 * j] ^= t5 ^ data[5];
                m_state[6 + 8 * j] ^= t6 ^ data[6];
                m_state[7 + 8 * j] ^= t7 ^ data[7];

                tmp = data[7];
                data[7] = data[6];
                data[6] = data[5];
                data[5] = data[4];
                data[4] = data[3] ^ tmp;
                data[3] = data[2] ^ tmp;
                data[2] = data[1];
                data[1] = data[0] ^ tmp;
                data[0] = tmp;
            }

            chainv0 = m_state[0];
            chainv1 = m_state[1];
            chainv2 = m_state[2];
            chainv3 = m_state[3];
            chainv4 = m_state[4];
            chainv5 = m_state[5];
            chainv6 = m_state[6];
            chainv7 = m_state[7];

            for (int i = 0; i < 8; i++)
            {
                tmp = chainv0;
                chainv0 |= chainv1;
                chainv2 ^= chainv3;
                chainv1 = ~chainv1;
                chainv0 ^= chainv3;
                chainv3 &= tmp;
                chainv1 ^= chainv3;
                chainv3 ^= chainv2;
                chainv2 &= chainv0;
                chainv0 = ~chainv0;
                chainv2 ^= chainv1;
                chainv1 |= chainv3; tmp ^= chainv1;
                chainv3 ^= chainv2;
                chainv2 &= chainv1;
                chainv1 ^= chainv0;
                chainv0 = tmp; tmp = chainv5;
                chainv5 |= chainv6;
                chainv7 ^= chainv4;
                chainv6 = ~chainv6;
                chainv5 ^= chainv4;
                chainv4 &= tmp;
                chainv6 ^= chainv4;
                chainv4 ^= chainv7;
                chainv7 &= chainv5;
                chainv5 = ~chainv5;
                chainv7 ^= chainv6;
                chainv6 |= chainv4; tmp ^= chainv6;
                chainv4 ^= chainv7;
                chainv7 &= chainv6;
                chainv6 ^= chainv5;
                chainv5 = tmp;
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 2) | (chainv0 >> 30);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 14) | (chainv4 >> 18);
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 10) | (chainv0 >> 22);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 1) | (chainv4 >> 31);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 2) | (chainv1 >> 30);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 14) | (chainv5 >> 18);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 10) | (chainv1 >> 22);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 1) | (chainv5 >> 31);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 2) | (chainv2 >> 30);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 14) | (chainv6 >> 18);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 10) | (chainv2 >> 22);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 1) | (chainv6 >> 31);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 2) | (chainv3 >> 30);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 14) | (chainv7 >> 18);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 10) | (chainv3 >> 22);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 1) | (chainv7 >> 31);
                chainv0 ^= s_CNS[(2 * i)];
                chainv4 ^= s_CNS[(2 * i) + 1];
            }

            m_state[0] = chainv0;
            chainv0 = m_state[0 + 8];
            m_state[1] = chainv1;
            chainv1 = m_state[1 + 8];
            m_state[2] = chainv2;
            chainv2 = m_state[2 + 8];
            m_state[3] = chainv3;
            chainv3 = m_state[3 + 8];
            m_state[4] = chainv4;
            chainv4 = m_state[4 + 8];
            m_state[5] = chainv5;
            chainv5 = m_state[5 + 8];
            m_state[6] = chainv6;
            chainv6 = m_state[6 + 8];
            m_state[7] = chainv7;
            chainv7 = m_state[7 + 8];

            chainv4 = (chainv4 << 1) | (chainv4 >> 31);
            chainv5 = (chainv5 << 1) | (chainv5 >> 31);
            chainv6 = (chainv6 << 1) | (chainv6 >> 31);
            chainv7 = (chainv7 << 1) | (chainv7 >> 31);

            for (int i = 0; i < 8; i++)
            {
                tmp = chainv0;
                chainv0 |= chainv1;
                chainv2 ^= chainv3;
                chainv1 = ~chainv1;
                chainv0 ^= chainv3;
                chainv3 &= tmp;
                chainv1 ^= chainv3;
                chainv3 ^= chainv2;
                chainv2 &= chainv0;
                chainv0 = ~chainv0;
                chainv2 ^= chainv1;
                chainv1 |= chainv3; tmp ^= chainv1;
                chainv3 ^= chainv2;
                chainv2 &= chainv1;
                chainv1 ^= chainv0;
                chainv0 = tmp; tmp = chainv5;
                chainv5 |= chainv6;
                chainv7 ^= chainv4;
                chainv6 = ~chainv6;
                chainv5 ^= chainv4;
                chainv4 &= tmp;
                chainv6 ^= chainv4;
                chainv4 ^= chainv7;
                chainv7 &= chainv5;
                chainv5 = ~chainv5;
                chainv7 ^= chainv6;
                chainv6 |= chainv4; tmp ^= chainv6;
                chainv4 ^= chainv7;
                chainv7 &= chainv6;
                chainv6 ^= chainv5;
                chainv5 = tmp;
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 2) | (chainv0 >> 30);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 14) | (chainv4 >> 18);
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 10) | (chainv0 >> 22);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 1) | (chainv4 >> 31);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 2) | (chainv1 >> 30);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 14) | (chainv5 >> 18);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 10) | (chainv1 >> 22);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 1) | (chainv5 >> 31);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 2) | (chainv2 >> 30);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 14) | (chainv6 >> 18);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 10) | (chainv2 >> 22);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 1) | (chainv6 >> 31);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 2) | (chainv3 >> 30);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 14) | (chainv7 >> 18);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 10) | (chainv3 >> 22);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 1) | (chainv7 >> 31);
                chainv0 ^= s_CNS[(2 * i) + 16];
                chainv4 ^= s_CNS[(2 * i) + 16 + 1];
            }

            m_state[8 + 0] = chainv0;
            chainv0 = m_state[ 0 + 16];
            m_state[8 + 1] = chainv1;
            chainv1 = m_state[1 + 16];
            m_state[8 + 2] = chainv2;
            chainv2 = m_state[2 + 16];
            m_state[8 + 3] = chainv3;
            chainv3 = m_state[3 + 16];
            m_state[8 + 4] = chainv4;
            chainv4 = m_state[4 + 16];
            m_state[8 + 5] = chainv5;
            chainv5 = m_state[5 + 16];
            m_state[8 + 6] = chainv6;
            chainv6 = m_state[6 + 16];
            m_state[8 + 7] = chainv7;
            chainv7 = m_state[7 + 16];

            chainv4 = (chainv4 << 2) | (chainv4 >> 30);
            chainv5 = (chainv5 << 2) | (chainv5 >> 30);
            chainv6 = (chainv6 << 2) | (chainv6 >> 30);
            chainv7 = (chainv7 << 2) | (chainv7 >> 30);

            for (int i = 0; i < 8; i++)
            {
                tmp = chainv0;
                chainv0 |= chainv1;
                chainv2 ^= chainv3;
                chainv1 = ~chainv1;
                chainv0 ^= chainv3;
                chainv3 &= tmp;
                chainv1 ^= chainv3;
                chainv3 ^= chainv2;
                chainv2 &= chainv0;
                chainv0 = ~chainv0;
                chainv2 ^= chainv1;
                chainv1 |= chainv3; tmp ^= chainv1;
                chainv3 ^= chainv2;
                chainv2 &= chainv1;
                chainv1 ^= chainv0;
                chainv0 = tmp; tmp = chainv5;
                chainv5 |= chainv6;
                chainv7 ^= chainv4;
                chainv6 = ~chainv6;
                chainv5 ^= chainv4;
                chainv4 &= tmp;
                chainv6 ^= chainv4;
                chainv4 ^= chainv7;
                chainv7 &= chainv5;
                chainv5 = ~chainv5;
                chainv7 ^= chainv6;
                chainv6 |= chainv4; tmp ^= chainv6;
                chainv4 ^= chainv7;
                chainv7 &= chainv6;
                chainv6 ^= chainv5;
                chainv5 = tmp;
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 2) | (chainv0 >> 30);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 14) | (chainv4 >> 18);
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 10) | (chainv0 >> 22);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 1) | (chainv4 >> 31);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 2) | (chainv1 >> 30);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 14) | (chainv5 >> 18);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 10) | (chainv1 >> 22);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 1) | (chainv5 >> 31);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 2) | (chainv2 >> 30);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 14) | (chainv6 >> 18);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 10) | (chainv2 >> 22);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 1) | (chainv6 >> 31);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 2) | (chainv3 >> 30);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 14) | (chainv7 >> 18);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 10) | (chainv3 >> 22);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 1) | (chainv7 >> 31);
                chainv0 ^= s_CNS[(2 * i) + 32];
                chainv4 ^= s_CNS[(2 * i) + 32 + 1];
            }

            m_state[0 + 16] = chainv0;
            m_state[1 + 16] = chainv1;
            m_state[2 + 16] = chainv2;
            m_state[3 + 16] = chainv3;
            m_state[4 + 16] = chainv4;
            m_state[5 + 16] = chainv5;
            m_state[6 + 16] = chainv6;
            m_state[7 + 16] = chainv7;
        }
    };

    internal class Luffa256 : Luffa256Base
    {
        public Luffa256()
            : base(HashLib.HashSize.HashSize256)
        {
        }
    };

    internal class Luffa224 : Luffa256Base
    {
        public Luffa224()
            : base(HashLib.HashSize.HashSize224)
        {
        }
    };

    internal class Luffa384 : LuffaBase
    {
        public Luffa384()
            : base(HashLib.HashSize.HashSize384)
        {
            m_result_blocks = 4;
            m_iv_length = 32;

            Initialize();
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            uint chainv0, chainv1, chainv2, chainv3, chainv4, chainv5, chainv6, chainv7;
            uint tmp;

            uint[] data = Converters.ConvertBytesToUIntsSwapOrder(a_data, a_index, BlockSize);

            uint[] t = new uint[32];

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 4; j++)
                    t[i] ^= m_state[i + 8 * j];
            }

            tmp = t[7];
            t[7] = t[6];
            t[6] = t[5];
            t[5] = t[4];
            t[4] = t[3] ^ tmp;
            t[3] = t[2] ^ tmp;
            t[2] = t[1];
            t[1] = t[0] ^ tmp;
            t[0] = tmp;

            for (int j = 0; j < 4; j++)
            {
                for (int i = 0; i < 8; i++)
                    m_state[i + 8 * j] ^= t[i];
            }

            for (int j = 0; j < 4; j++)
            {
                for (int i = 0; i < 8; i++)
                    t[i + 8 * j] = m_state[i + 8 * j];
            }

            for (int j = 0; j < 4; j++)
            {
                tmp = m_state[7 + (8 * j)];
                m_state[7 + (8 * j)] = m_state[6 + (8 * j)];
                m_state[6 + (8 * j)] = m_state[5 + (8 * j)];
                m_state[5 + (8 * j)] = m_state[4 + (8 * j)];
                m_state[4 + (8 * j)] = m_state[3 + (8 * j)] ^ tmp;
                m_state[3 + (8 * j)] = m_state[2 + (8 * j)] ^ tmp;
                m_state[2 + (8 * j)] = m_state[1 + (8 * j)];
                m_state[1 + (8 * j)] = m_state[0 + (8 * j)] ^ tmp;
                m_state[8 * j] = tmp;
            }

            for (int j = 0; j < 4; j++)
            {
                for (int i = 0; i < 8; i++)
                    m_state[8 * j + i] ^= t[8 * ((j + 3) % 4) + i];
            }

            for (int j = 0; j < 4; j++)
            {
                for (int i = 0; i < 8; i++)
                    m_state[i + 8 * j] ^= data[i];

                tmp = data[7];
                data[7] = data[6];
                data[6] = data[5];
                data[5] = data[4];
                data[4] = data[3] ^ tmp;
                data[3] = data[2] ^ tmp;
                data[2] = data[1];
                data[1] = data[0] ^ tmp;
                data[0] = tmp;
            }

            chainv0 = m_state[0];
            chainv1 = m_state[1];
            chainv2 = m_state[2];
            chainv3 = m_state[3];
            chainv4 = m_state[4];
            chainv5 = m_state[5];
            chainv6 = m_state[6];
            chainv7 = m_state[7];

            for (int i = 0; i < 8; i++)
            {
                tmp = chainv0;
                chainv0 |= chainv1;
                chainv2 ^= chainv3;
                chainv1 = ~chainv1;
                chainv0 ^= chainv3;
                chainv3 &= tmp;
                chainv1 ^= chainv3;
                chainv3 ^= chainv2;
                chainv2 &= chainv0;
                chainv0 = ~chainv0;
                chainv2 ^= chainv1;
                chainv1 |= chainv3; tmp ^= chainv1;
                chainv3 ^= chainv2;
                chainv2 &= chainv1;
                chainv1 ^= chainv0;
                chainv0 = tmp; tmp = chainv5;
                chainv5 |= chainv6;
                chainv7 ^= chainv4;
                chainv6 = ~chainv6;
                chainv5 ^= chainv4;
                chainv4 &= tmp;
                chainv6 ^= chainv4;
                chainv4 ^= chainv7;
                chainv7 &= chainv5;
                chainv5 = ~chainv5;
                chainv7 ^= chainv6;
                chainv6 |= chainv4; tmp ^= chainv6;
                chainv4 ^= chainv7;
                chainv7 &= chainv6;
                chainv6 ^= chainv5;
                chainv5 = tmp;
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 2) | (chainv0 >> 30);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 14) | (chainv4 >> 18);
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 10) | (chainv0 >> 22);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 1) | (chainv4 >> 31);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 2) | (chainv1 >> 30);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 14) | (chainv5 >> 18);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 10) | (chainv1 >> 22);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 1) | (chainv5 >> 31);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 2) | (chainv2 >> 30);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 14) | (chainv6 >> 18);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 10) | (chainv2 >> 22);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 1) | (chainv6 >> 31);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 2) | (chainv3 >> 30);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 14) | (chainv7 >> 18);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 10) | (chainv3 >> 22);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 1) | (chainv7 >> 31);
                chainv0 ^= s_CNS[(2 * i)];
                chainv4 ^= s_CNS[(2 * i) + 1];
            }

            m_state[0] = chainv0;
            chainv0 = m_state[0 + 8];
            m_state[1] = chainv1;
            chainv1 = m_state[1 + 8];
            m_state[2] = chainv2;
            chainv2 = m_state[2 + 8];
            m_state[3] = chainv3;
            chainv3 = m_state[3 + 8];
            m_state[4] = chainv4;
            chainv4 = m_state[4 + 8];
            m_state[5] = chainv5;
            chainv5 = m_state[5 + 8];
            m_state[6] = chainv6;
            chainv6 = m_state[6 + 8];
            m_state[7] = chainv7;
            chainv7 = m_state[7 + 8];

            chainv4 = (chainv4 << 1) | (chainv4 >> 31);
            chainv5 = (chainv5 << 1) | (chainv5 >> 31);
            chainv6 = (chainv6 << 1) | (chainv6 >> 31);
            chainv7 = (chainv7 << 1) | (chainv7 >> 31);

            for (int i = 0; i < 8; i++)
            {
                tmp = chainv0;
                chainv0 |= chainv1;
                chainv2 ^= chainv3;
                chainv1 = ~chainv1;
                chainv0 ^= chainv3;
                chainv3 &= tmp;
                chainv1 ^= chainv3;
                chainv3 ^= chainv2;
                chainv2 &= chainv0;
                chainv0 = ~chainv0;
                chainv2 ^= chainv1;
                chainv1 |= chainv3; tmp ^= chainv1;
                chainv3 ^= chainv2;
                chainv2 &= chainv1;
                chainv1 ^= chainv0;
                chainv0 = tmp; tmp = chainv5;
                chainv5 |= chainv6;
                chainv7 ^= chainv4;
                chainv6 = ~chainv6;
                chainv5 ^= chainv4;
                chainv4 &= tmp;
                chainv6 ^= chainv4;
                chainv4 ^= chainv7;
                chainv7 &= chainv5;
                chainv5 = ~chainv5;
                chainv7 ^= chainv6;
                chainv6 |= chainv4; tmp ^= chainv6;
                chainv4 ^= chainv7;
                chainv7 &= chainv6;
                chainv6 ^= chainv5;
                chainv5 = tmp;
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 2) | (chainv0 >> 30);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 14) | (chainv4 >> 18);
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 10) | (chainv0 >> 22);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 1) | (chainv4 >> 31);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 2) | (chainv1 >> 30);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 14) | (chainv5 >> 18);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 10) | (chainv1 >> 22);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 1) | (chainv5 >> 31);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 2) | (chainv2 >> 30);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 14) | (chainv6 >> 18);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 10) | (chainv2 >> 22);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 1) | (chainv6 >> 31);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 2) | (chainv3 >> 30);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 14) | (chainv7 >> 18);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 10) | (chainv3 >> 22);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 1) | (chainv7 >> 31);
                chainv0 ^= s_CNS[(2 * i) + 16];
                chainv4 ^= s_CNS[(2 * i) + 16 + 1];
            }

            m_state[8 + 0] = chainv0;
            chainv0 = m_state[0 + 16];
            m_state[8 + 1] = chainv1;
            chainv1 = m_state[1 + 16];
            m_state[8 + 2] = chainv2;
            chainv2 = m_state[2 + 16];
            m_state[8 + 3] = chainv3;
            chainv3 = m_state[3 + 16];
            m_state[8 + 4] = chainv4;
            chainv4 = m_state[4 + 16];
            m_state[8 + 5] = chainv5;
            chainv5 = m_state[5 + 16];
            m_state[8 + 6] = chainv6;
            chainv6 = m_state[6 + 16];
            m_state[8 + 7] = chainv7;
            chainv7 = m_state[7 + 16];

            chainv4 = (chainv4 << 2) | (chainv4 >> 30);
            chainv5 = (chainv5 << 2) | (chainv5 >> 30);
            chainv6 = (chainv6 << 2) | (chainv6 >> 30);
            chainv7 = (chainv7 << 2) | (chainv7 >> 30);

            for (int i = 0; i < 8; i++)
            {
                tmp = chainv0;
                chainv0 |= chainv1;
                chainv2 ^= chainv3;
                chainv1 = ~chainv1;
                chainv0 ^= chainv3;
                chainv3 &= tmp;
                chainv1 ^= chainv3;
                chainv3 ^= chainv2;
                chainv2 &= chainv0;
                chainv0 = ~chainv0;
                chainv2 ^= chainv1;
                chainv1 |= chainv3; tmp ^= chainv1;
                chainv3 ^= chainv2;
                chainv2 &= chainv1;
                chainv1 ^= chainv0;
                chainv0 = tmp; tmp = chainv5;
                chainv5 |= chainv6;
                chainv7 ^= chainv4;
                chainv6 = ~chainv6;
                chainv5 ^= chainv4;
                chainv4 &= tmp;
                chainv6 ^= chainv4;
                chainv4 ^= chainv7;
                chainv7 &= chainv5;
                chainv5 = ~chainv5;
                chainv7 ^= chainv6;
                chainv6 |= chainv4; tmp ^= chainv6;
                chainv4 ^= chainv7;
                chainv7 &= chainv6;
                chainv6 ^= chainv5;
                chainv5 = tmp;
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 2) | (chainv0 >> 30);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 14) | (chainv4 >> 18);
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 10) | (chainv0 >> 22);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 1) | (chainv4 >> 31);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 2) | (chainv1 >> 30);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 14) | (chainv5 >> 18);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 10) | (chainv1 >> 22);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 1) | (chainv5 >> 31);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 2) | (chainv2 >> 30);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 14) | (chainv6 >> 18);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 10) | (chainv2 >> 22);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 1) | (chainv6 >> 31);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 2) | (chainv3 >> 30);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 14) | (chainv7 >> 18);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 10) | (chainv3 >> 22);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 1) | (chainv7 >> 31);
                chainv0 ^= s_CNS[(2 * i) + 32];
                chainv4 ^= s_CNS[(2 * i) + 32 + 1];
            }

            m_state[16 + 0] = chainv0;
            chainv0 = m_state[0 + 24];
            m_state[16 + 1] = chainv1;
            chainv1 = m_state[1 + 24];
            m_state[16 + 2] = chainv2;
            chainv2 = m_state[2 + 24];
            m_state[16 + 3] = chainv3;
            chainv3 = m_state[3 + 24];
            m_state[16 + 4] = chainv4;
            chainv4 = m_state[4 + 24];
            m_state[16 + 5] = chainv5;
            chainv5 = m_state[5 + 24];
            m_state[16 + 6] = chainv6;
            chainv6 = m_state[6 + 24];
            m_state[16 + 7] = chainv7;
            chainv7 = m_state[7 + 24];

            chainv4 = (chainv4 << 3) | (chainv4 >> 29);
            chainv5 = (chainv5 << 3) | (chainv5 >> 29);
            chainv6 = (chainv6 << 3) | (chainv6 >> 29);
            chainv7 = (chainv7 << 3) | (chainv7 >> 29);

            for (int i = 0; i < 8; i++)
            {
                tmp = chainv0;
                chainv0 |= chainv1;
                chainv2 ^= chainv3;
                chainv1 = ~chainv1;
                chainv0 ^= chainv3;
                chainv3 &= tmp;
                chainv1 ^= chainv3;
                chainv3 ^= chainv2;
                chainv2 &= chainv0;
                chainv0 = ~chainv0;
                chainv2 ^= chainv1;
                chainv1 |= chainv3; tmp ^= chainv1;
                chainv3 ^= chainv2;
                chainv2 &= chainv1;
                chainv1 ^= chainv0;
                chainv0 = tmp; tmp = chainv5;
                chainv5 |= chainv6;
                chainv7 ^= chainv4;
                chainv6 = ~chainv6;
                chainv5 ^= chainv4;
                chainv4 &= tmp;
                chainv6 ^= chainv4;
                chainv4 ^= chainv7;
                chainv7 &= chainv5;
                chainv5 = ~chainv5;
                chainv7 ^= chainv6;
                chainv6 |= chainv4; tmp ^= chainv6;
                chainv4 ^= chainv7;
                chainv7 &= chainv6;
                chainv6 ^= chainv5;
                chainv5 = tmp;
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 2) | (chainv0 >> 30);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 14) | (chainv4 >> 18);
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 10) | (chainv0 >> 22);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 1) | (chainv4 >> 31);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 2) | (chainv1 >> 30);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 14) | (chainv5 >> 18);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 10) | (chainv1 >> 22);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 1) | (chainv5 >> 31);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 2) | (chainv2 >> 30);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 14) | (chainv6 >> 18);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 10) | (chainv2 >> 22);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 1) | (chainv6 >> 31);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 2) | (chainv3 >> 30);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 14) | (chainv7 >> 18);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 10) | (chainv3 >> 22);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 1) | (chainv7 >> 31);
                chainv0 ^= s_CNS[(2 * i) + 48];
                chainv4 ^= s_CNS[(2 * i) + 48 + 1];
            }

            m_state[0 + 24] = chainv0;
            m_state[1 + 24] = chainv1;
            m_state[2 + 24] = chainv2;
            m_state[3 + 24] = chainv3;
            m_state[4 + 24] = chainv4;
            m_state[5 + 24] = chainv5;
            m_state[6 + 24] = chainv6;
            m_state[7 + 24] = chainv7;
        }
    };

    internal class Luffa512 : LuffaBase
    {
        public Luffa512()
            : base(HashLib.HashSize.HashSize512)
        {
            m_result_blocks = 5;
            m_iv_length = 40;

            Initialize();
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            uint chainv0, chainv1, chainv2, chainv3, chainv4, chainv5, chainv6, chainv7;
            uint tmp;

            uint[] data = Converters.ConvertBytesToUIntsSwapOrder(a_data, a_index, BlockSize);

            uint[] t = new uint[40];

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 5; j++)
                    t[i] ^= m_state[i + 8 * j];
            }

            tmp = t[7];
            t[7] = t[6];
            t[6] = t[5];
            t[5] = t[4];
            t[4] = t[3] ^ tmp;
            t[3] = t[2] ^ tmp;
            t[2] = t[1];
            t[1] = t[0] ^ tmp;
            t[0] = tmp;

            for (int j = 0; j < 5; j++)
            {
                for (int i = 0; i < 8; i++)
                    m_state[i + 8 * j] ^= t[i];
            }

            for (int j = 0; j < 5; j++)
            {
                for (int i = 0; i < 8; i++)
                    t[i + 8 * j] = m_state[i + 8 * j];
            }

            for (int j = 0; j < 5; j++)
            {
                tmp = m_state[7 + (8 * j)];
                m_state[7 + (8 * j)] = m_state[6 + (8 * j)];
                m_state[6 + (8 * j)] = m_state[5 + (8 * j)];
                m_state[5 + (8 * j)] = m_state[4 + (8 * j)];
                m_state[4 + (8 * j)] = m_state[3 + (8 * j)] ^ tmp;
                m_state[3 + (8 * j)] = m_state[2 + (8 * j)] ^ tmp;
                m_state[2 + (8 * j)] = m_state[1 + (8 * j)];
                m_state[1 + (8 * j)] = m_state[0 + (8 * j)] ^ tmp;
                m_state[8 * j] = tmp;
            }

            for (int j = 0; j < 5; j++)
            {
                for (int i = 0; i < 8; i++)
                    m_state[8 * j + i] ^= t[8 * ((j + 1) % 5) + i];
            }

            for (int j = 0; j < 5; j++)
            {
                for (int i = 0; i < 8; i++)
                    t[i + 8 * j] = m_state[i + 8 * j];
            }

            for (int j = 0; j < 5; j++)
            {
                tmp = m_state[7 + (8 * j)];
                m_state[7 + (8 * j)] = m_state[6 + (8 * j)];
                m_state[6 + (8 * j)] = m_state[5 + (8 * j)];
                m_state[5 + (8 * j)] = m_state[4 + (8 * j)];
                m_state[4 + (8 * j)] = m_state[3 + (8 * j)] ^ tmp;
                m_state[3 + (8 * j)] = m_state[2 + (8 * j)] ^ tmp;
                m_state[2 + (8 * j)] = m_state[1 + (8 * j)];
                m_state[1 + (8 * j)] = m_state[0 + (8 * j)] ^ tmp;
                m_state[8 * j] = tmp;
            }

            for (int j = 0; j < 5; j++)
            {
                for (int i = 0; i < 8; i++)
                    m_state[8 * j + i] ^= t[8 * ((j + 4) % 5) + i];
            }

            for (int j = 0; j < 5; j++)
            {
                for (int i = 0; i < 8; i++)
                    m_state[i + 8 * j] ^= data[i];

                tmp = data[7];
                data[7] = data[6];
                data[6] = data[5];
                data[5] = data[4];
                data[4] = data[3] ^ tmp;
                data[3] = data[2] ^ tmp;
                data[2] = data[1];
                data[1] = data[0] ^ tmp;
                data[0] = tmp;
            }

            chainv0 = m_state[0];
            chainv1 = m_state[1];
            chainv2 = m_state[2];
            chainv3 = m_state[3];
            chainv4 = m_state[4];
            chainv5 = m_state[5];
            chainv6 = m_state[6];
            chainv7 = m_state[7];

            for (int i = 0; i < 8; i++)
            {
                tmp = chainv0;
                chainv0 |= chainv1;
                chainv2 ^= chainv3;
                chainv1 = ~chainv1;
                chainv0 ^= chainv3;
                chainv3 &= tmp;
                chainv1 ^= chainv3;
                chainv3 ^= chainv2;
                chainv2 &= chainv0;
                chainv0 = ~chainv0;
                chainv2 ^= chainv1;
                chainv1 |= chainv3; tmp ^= chainv1;
                chainv3 ^= chainv2;
                chainv2 &= chainv1;
                chainv1 ^= chainv0;
                chainv0 = tmp; tmp = chainv5;
                chainv5 |= chainv6;
                chainv7 ^= chainv4;
                chainv6 = ~chainv6;
                chainv5 ^= chainv4;
                chainv4 &= tmp;
                chainv6 ^= chainv4;
                chainv4 ^= chainv7;
                chainv7 &= chainv5;
                chainv5 = ~chainv5;
                chainv7 ^= chainv6;
                chainv6 |= chainv4; tmp ^= chainv6;
                chainv4 ^= chainv7;
                chainv7 &= chainv6;
                chainv6 ^= chainv5;
                chainv5 = tmp;
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 2) | (chainv0 >> 30);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 14) | (chainv4 >> 18);
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 10) | (chainv0 >> 22);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 1) | (chainv4 >> 31);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 2) | (chainv1 >> 30);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 14) | (chainv5 >> 18);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 10) | (chainv1 >> 22);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 1) | (chainv5 >> 31);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 2) | (chainv2 >> 30);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 14) | (chainv6 >> 18);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 10) | (chainv2 >> 22);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 1) | (chainv6 >> 31);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 2) | (chainv3 >> 30);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 14) | (chainv7 >> 18);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 10) | (chainv3 >> 22);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 1) | (chainv7 >> 31);
                chainv0 ^= s_CNS[(2 * i)];
                chainv4 ^= s_CNS[(2 * i) + 1];
            }

            m_state[0] = chainv0;
            chainv0 = m_state[0 + 8];
            m_state[1] = chainv1;
            chainv1 = m_state[1 + 8];
            m_state[2] = chainv2;
            chainv2 = m_state[2 + 8];
            m_state[3] = chainv3;
            chainv3 = m_state[3 + 8];
            m_state[4] = chainv4;
            chainv4 = m_state[4 + 8];
            m_state[5] = chainv5;
            chainv5 = m_state[5 + 8];
            m_state[6] = chainv6;
            chainv6 = m_state[6 + 8];
            m_state[7] = chainv7;
            chainv7 = m_state[7 + 8];

            chainv4 = (chainv4 << 1) | (chainv4 >> 31);
            chainv5 = (chainv5 << 1) | (chainv5 >> 31);
            chainv6 = (chainv6 << 1) | (chainv6 >> 31);
            chainv7 = (chainv7 << 1) | (chainv7 >> 31);

            for (int i = 0; i < 8; i++)
            {
                tmp = chainv0;
                chainv0 |= chainv1;
                chainv2 ^= chainv3;
                chainv1 = ~chainv1;
                chainv0 ^= chainv3;
                chainv3 &= tmp;
                chainv1 ^= chainv3;
                chainv3 ^= chainv2;
                chainv2 &= chainv0;
                chainv0 = ~chainv0;
                chainv2 ^= chainv1;
                chainv1 |= chainv3; tmp ^= chainv1;
                chainv3 ^= chainv2;
                chainv2 &= chainv1;
                chainv1 ^= chainv0;
                chainv0 = tmp; tmp = chainv5;
                chainv5 |= chainv6;
                chainv7 ^= chainv4;
                chainv6 = ~chainv6;
                chainv5 ^= chainv4;
                chainv4 &= tmp;
                chainv6 ^= chainv4;
                chainv4 ^= chainv7;
                chainv7 &= chainv5;
                chainv5 = ~chainv5;
                chainv7 ^= chainv6;
                chainv6 |= chainv4; tmp ^= chainv6;
                chainv4 ^= chainv7;
                chainv7 &= chainv6;
                chainv6 ^= chainv5;
                chainv5 = tmp;
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 2) | (chainv0 >> 30);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 14) | (chainv4 >> 18);
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 10) | (chainv0 >> 22);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 1) | (chainv4 >> 31);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 2) | (chainv1 >> 30);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 14) | (chainv5 >> 18);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 10) | (chainv1 >> 22);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 1) | (chainv5 >> 31);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 2) | (chainv2 >> 30);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 14) | (chainv6 >> 18);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 10) | (chainv2 >> 22);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 1) | (chainv6 >> 31);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 2) | (chainv3 >> 30);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 14) | (chainv7 >> 18);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 10) | (chainv3 >> 22);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 1) | (chainv7 >> 31);
                chainv0 ^= s_CNS[(2 * i) + 16];
                chainv4 ^= s_CNS[(2 * i) + 16 + 1];
            }

            m_state[8 + 0] = chainv0;
            chainv0 = m_state[0 + 16];
            m_state[8 + 1] = chainv1;
            chainv1 = m_state[1 + 16];
            m_state[8 + 2] = chainv2;
            chainv2 = m_state[2 + 16];
            m_state[8 + 3] = chainv3;
            chainv3 = m_state[3 + 16];
            m_state[8 + 4] = chainv4;
            chainv4 = m_state[4 + 16];
            m_state[8 + 5] = chainv5;
            chainv5 = m_state[5 + 16];
            m_state[8 + 6] = chainv6;
            chainv6 = m_state[6 + 16];
            m_state[8 + 7] = chainv7;
            chainv7 = m_state[7 + 16];

            chainv4 = (chainv4 << 2) | (chainv4 >> 30);
            chainv5 = (chainv5 << 2) | (chainv5 >> 30);
            chainv6 = (chainv6 << 2) | (chainv6 >> 30);
            chainv7 = (chainv7 << 2) | (chainv7 >> 30);

            for (int i = 0; i < 8; i++)
            {
                tmp = chainv0;
                chainv0 |= chainv1;
                chainv2 ^= chainv3;
                chainv1 = ~chainv1;
                chainv0 ^= chainv3;
                chainv3 &= tmp;
                chainv1 ^= chainv3;
                chainv3 ^= chainv2;
                chainv2 &= chainv0;
                chainv0 = ~chainv0;
                chainv2 ^= chainv1;
                chainv1 |= chainv3; tmp ^= chainv1;
                chainv3 ^= chainv2;
                chainv2 &= chainv1;
                chainv1 ^= chainv0;
                chainv0 = tmp; tmp = chainv5;
                chainv5 |= chainv6;
                chainv7 ^= chainv4;
                chainv6 = ~chainv6;
                chainv5 ^= chainv4;
                chainv4 &= tmp;
                chainv6 ^= chainv4;
                chainv4 ^= chainv7;
                chainv7 &= chainv5;
                chainv5 = ~chainv5;
                chainv7 ^= chainv6;
                chainv6 |= chainv4; tmp ^= chainv6;
                chainv4 ^= chainv7;
                chainv7 &= chainv6;
                chainv6 ^= chainv5;
                chainv5 = tmp;
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 2) | (chainv0 >> 30);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 14) | (chainv4 >> 18);
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 10) | (chainv0 >> 22);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 1) | (chainv4 >> 31);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 2) | (chainv1 >> 30);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 14) | (chainv5 >> 18);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 10) | (chainv1 >> 22);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 1) | (chainv5 >> 31);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 2) | (chainv2 >> 30);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 14) | (chainv6 >> 18);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 10) | (chainv2 >> 22);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 1) | (chainv6 >> 31);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 2) | (chainv3 >> 30);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 14) | (chainv7 >> 18);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 10) | (chainv3 >> 22);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 1) | (chainv7 >> 31);
                chainv0 ^= s_CNS[(2 * i) + 32];
                chainv4 ^= s_CNS[(2 * i) + 32 + 1];
            }

            m_state[16 + 0] = chainv0;
            chainv0 = m_state[0 + 24];
            m_state[16 + 1] = chainv1;
            chainv1 = m_state[1 + 24];
            m_state[16 + 2] = chainv2;
            chainv2 = m_state[2 + 24];
            m_state[16 + 3] = chainv3;
            chainv3 = m_state[3 + 24];
            m_state[16 + 4] = chainv4;
            chainv4 = m_state[4 + 24];
            m_state[16 + 5] = chainv5;
            chainv5 = m_state[5 + 24];
            m_state[16 + 6] = chainv6;
            chainv6 = m_state[6 + 24];
            m_state[16 + 7] = chainv7;
            chainv7 = m_state[7 + 24];

            chainv4 = (chainv4 << 3) | (chainv4 >> 29);
            chainv5 = (chainv5 << 3) | (chainv5 >> 29);
            chainv6 = (chainv6 << 3) | (chainv6 >> 29);
            chainv7 = (chainv7 << 3) | (chainv7 >> 29);

            for (int i = 0; i < 8; i++)
            {
                tmp = chainv0;
                chainv0 |= chainv1;
                chainv2 ^= chainv3;
                chainv1 = ~chainv1;
                chainv0 ^= chainv3;
                chainv3 &= tmp;
                chainv1 ^= chainv3;
                chainv3 ^= chainv2;
                chainv2 &= chainv0;
                chainv0 = ~chainv0;
                chainv2 ^= chainv1;
                chainv1 |= chainv3; tmp ^= chainv1;
                chainv3 ^= chainv2;
                chainv2 &= chainv1;
                chainv1 ^= chainv0;
                chainv0 = tmp; tmp = chainv5;
                chainv5 |= chainv6;
                chainv7 ^= chainv4;
                chainv6 = ~chainv6;
                chainv5 ^= chainv4;
                chainv4 &= tmp;
                chainv6 ^= chainv4;
                chainv4 ^= chainv7;
                chainv7 &= chainv5;
                chainv5 = ~chainv5;
                chainv7 ^= chainv6;
                chainv6 |= chainv4; tmp ^= chainv6;
                chainv4 ^= chainv7;
                chainv7 &= chainv6;
                chainv6 ^= chainv5;
                chainv5 = tmp;
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 2) | (chainv0 >> 30);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 14) | (chainv4 >> 18);
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 10) | (chainv0 >> 22);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 1) | (chainv4 >> 31);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 2) | (chainv1 >> 30);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 14) | (chainv5 >> 18);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 10) | (chainv1 >> 22);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 1) | (chainv5 >> 31);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 2) | (chainv2 >> 30);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 14) | (chainv6 >> 18);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 10) | (chainv2 >> 22);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 1) | (chainv6 >> 31);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 2) | (chainv3 >> 30);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 14) | (chainv7 >> 18);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 10) | (chainv3 >> 22);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 1) | (chainv7 >> 31);
                chainv0 ^= s_CNS[(2 * i) + 48];
                chainv4 ^= s_CNS[(2 * i) + 48 + 1];
            }

            m_state[24 + 0] = chainv0;
            chainv0 = m_state[0 + 32];
            m_state[24 + 1] = chainv1;
            chainv1 = m_state[1 + 32];
            m_state[24 + 2] = chainv2;
            chainv2 = m_state[2 + 32];
            m_state[24 + 3] = chainv3;
            chainv3 = m_state[3 + 32];
            m_state[24 + 4] = chainv4;
            chainv4 = m_state[4 + 32];
            m_state[24 + 5] = chainv5;
            chainv5 = m_state[5 + 32];
            m_state[24 + 6] = chainv6;
            chainv6 = m_state[6 + 32];
            m_state[24 + 7] = chainv7;
            chainv7 = m_state[7 + 32];

            chainv4 = (chainv4 << 4) | (chainv4 >> 28);
            chainv5 = (chainv5 << 4) | (chainv5 >> 28);
            chainv6 = (chainv6 << 4) | (chainv6 >> 28);
            chainv7 = (chainv7 << 4) | (chainv7 >> 28);

            for (int i = 0; i < 8; i++)
            {
                tmp = chainv0;
                chainv0 |= chainv1;
                chainv2 ^= chainv3;
                chainv1 = ~chainv1;
                chainv0 ^= chainv3;
                chainv3 &= tmp;
                chainv1 ^= chainv3;
                chainv3 ^= chainv2;
                chainv2 &= chainv0;
                chainv0 = ~chainv0;
                chainv2 ^= chainv1;
                chainv1 |= chainv3; tmp ^= chainv1;
                chainv3 ^= chainv2;
                chainv2 &= chainv1;
                chainv1 ^= chainv0;
                chainv0 = tmp; tmp = chainv5;
                chainv5 |= chainv6;
                chainv7 ^= chainv4;
                chainv6 = ~chainv6;
                chainv5 ^= chainv4;
                chainv4 &= tmp;
                chainv6 ^= chainv4;
                chainv4 ^= chainv7;
                chainv7 &= chainv5;
                chainv5 = ~chainv5;
                chainv7 ^= chainv6;
                chainv6 |= chainv4; tmp ^= chainv6;
                chainv4 ^= chainv7;
                chainv7 &= chainv6;
                chainv6 ^= chainv5;
                chainv5 = tmp;
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 2) | (chainv0 >> 30);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 14) | (chainv4 >> 18);
                chainv4 ^= chainv0;
                chainv0 = (chainv0 << 10) | (chainv0 >> 22);
                chainv0 ^= chainv4;
                chainv4 = (chainv4 << 1) | (chainv4 >> 31);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 2) | (chainv1 >> 30);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 14) | (chainv5 >> 18);
                chainv5 ^= chainv1;
                chainv1 = (chainv1 << 10) | (chainv1 >> 22);
                chainv1 ^= chainv5;
                chainv5 = (chainv5 << 1) | (chainv5 >> 31);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 2) | (chainv2 >> 30);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 14) | (chainv6 >> 18);
                chainv6 ^= chainv2;
                chainv2 = (chainv2 << 10) | (chainv2 >> 22);
                chainv2 ^= chainv6;
                chainv6 = (chainv6 << 1) | (chainv6 >> 31);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 2) | (chainv3 >> 30);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 14) | (chainv7 >> 18);
                chainv7 ^= chainv3;
                chainv3 = (chainv3 << 10) | (chainv3 >> 22);
                chainv3 ^= chainv7;
                chainv7 = (chainv7 << 1) | (chainv7 >> 31);
                chainv0 ^= s_CNS[(2 * i) + 64];
                chainv4 ^= s_CNS[(2 * i) + 64 + 1];
            }

            m_state[0 + 32] = chainv0;
            m_state[1 + 32] = chainv1;
            m_state[2 + 32] = chainv2;
            m_state[3 + 32] = chainv3;
            m_state[4 + 32] = chainv4;
            m_state[5 + 32] = chainv5;
            m_state[6 + 32] = chainv6;
            m_state[7 + 32] = chainv7;
        }
    }
}