using System;

namespace HashLib.Crypto.SHA3
{
    internal class JH224 : JHBase
    {
        public JH224() :
            base(HashLib.HashSize.HashSize224)
        {
        }
    }

    internal class JH256 : JHBase
    {
        public JH256() :
            base(HashLib.HashSize.HashSize256)
        {
        }
    }

    internal class JH384 : JHBase
    {
        public JH384() :
            base(HashLib.HashSize.HashSize384)
        {
        }
    }

    internal class JH512 : JHBase
    {
        public JH512() :
            base(HashLib.HashSize.HashSize512)
        {
        }
    }

    internal abstract class JHBase : BlockHash, ICryptoNotBuildIn
    {
        #region Consts
        private static readonly ulong[] s_bitslices =
        {
            0x67F815DFA2DED572, 0x571523B70A15847B, 0xF6875A4D90D6AB81, 0x402BD1C3C54F9F4E,
            0x9CFA455CE03A98EA, 0x9A99B26699D2C503, 0x8A53BBF2B4960266, 0x31A2DB881A1456B5,
            0xDB0E199A5C5AA303, 0x1044C1870AB23F40, 0x1D959E848019051C, 0xDCCDE75EADEB336F,
            0x416BBF029213BA10, 0xD027BBF7156578DC, 0x5078AA3739812C0A, 0xD3910041D2BF1A3F,
            0x907ECCF60D5A2D42, 0xCE97C0929C9F62DD, 0xAC442BC70BA75C18, 0x23FCC663D665DFD1,
            0x1AB8E09E036C6E97, 0xA8EC6C447E450521, 0xFA618E5DBB03F1EE, 0x97818394B29796FD,
            0x2F3003DB37858E4A, 0x956A9FFB2D8D672A, 0x6C69B8F88173FE8A, 0x14427FC04672C78A,
            0xC45EC7BD8F15F4C5, 0x80BB118FA76F4475, 0xBC88E4AEB775DE52, 0xF4A3A6981E00B882,
            0x1563A3A9338FF48E, 0x89F9B7D524565FAA, 0xFDE05A7C20EDF1B6, 0x362C42065AE9CA36,
            0x3D98FE4E433529CE, 0xA74B9A7374F93A53, 0x86814E6F591FF5D0, 0x9F5AD8AF81AD9D0E,
            0x6A6234EE670605A7, 0x2717B96EBE280B8B, 0x3F1080C626077447, 0x7B487EC66F7EA0E0,
            0xC0A4F84AA50A550D, 0x9EF18E979FE7E391, 0xD48D605081727686, 0x62B0E5F3415A9E7E,
            0x7A205440EC1F9FFC, 0x84C9F4CE001AE4E3, 0xD895FA9DF594D74F, 0xA554C324117E2E55,
            0x286EFEBD2872DF5B, 0xB2C4A50FE27FF578, 0x2ED349EEEF7C8905, 0x7F5928EB85937E44,
            0x4A3124B337695F70, 0x65E4D61DF128865E, 0xE720B95104771BC7, 0x8A87D423E843FE74,
            0xF2947692A3E8297D, 0xC1D9309B097ACBDD, 0xE01BDC5BFB301B1D, 0xBF829CF24F4924DA,
            0xFFBF70B431BAE7A4, 0x48BCF8DE0544320D, 0x39D3BB5332FCAE3B, 0xA08B29E0C1C39F45,
            0x0F09AEF7FD05C9E5, 0x34F1904212347094, 0x95ED44E301B771A2, 0x4A982F4F368E3BE9,
            0x15F66CA0631D4088, 0xFFAF52874B44C147, 0x30C60AE2F14ABB7E, 0xE68C6ECCC5B67046,
            0x00CA4FBD56A4D5A4, 0xAE183EC84B849DDA, 0xADD1643045CE5773, 0x67255C1468CEA6E8,
            0x16E10ECBF28CDAA3, 0x9A99949A5806E933, 0x7B846FC220B2601F, 0x1885D1A07FACCED1,
            0xD319DD8DA15B5932, 0x46B4A5AAC01C9A50, 0xBA6B04E467633D9F, 0x7EEE560BAB19CAF6,
            0x742128A9EA79B11F, 0xEE51363B35F7BDE9, 0x76D350755AAC571D, 0x01707DA3FEC2463A,
            0x42D8A498AFC135F7, 0x79676B9E20ECED78, 0xA8DB3AEA15638341, 0x832C83324D3BC3FA,
            0xF347271C1F3B40A7, 0x9A762DB734F04059, 0xFD4F21D26C4E3EE7, 0xEF5957DC398DFDB8,
            0xDAEB492B490C9B8D, 0x0D70F36849D7A25B, 0x84558D7AD0AE3B7D, 0x658EF8E4F0E9A5F5,
            0x533B1036F4A2B8A0, 0x5AEC3E759E07A80C, 0x4F88E85692946891, 0x4CBCBAF8555CB05B,
            0x7B9487F3993BBBE3, 0x5D1C6B72D6F4DA75, 0x6DB334DC28ACAE64, 0x71DB28B850A5346C,
            0x2A518D10F2E261F8, 0xFC75DD593364DBE3, 0xA23FCE43F1BCAC1C, 0xB043E8023CD1BB67,
            0x75A12988CA5B0A33, 0x5C5316B44D19347F, 0x1E4D790EC3943B92, 0x3FAFEEB6D7757479,
            0x21391ABEF7D4A8EA, 0x5127234C097EF45C, 0xD23C32BA5324A326, 0xADD5A66D4A17A344,
            0x08C9F2AFA63E1DB5, 0x563C6B91983D5983, 0x4D608672A17CF84C, 0xF6C76E08CC3EE246,
            0x5E76BCB1B333982F, 0x2AE6C4EFA566D62B, 0x36D4C1BEE8B6F406, 0x6321EFBC1582EE74,
            0x69C953F40D4EC1FD, 0x26585806C45A7DA7, 0x16FAE0061614C17E, 0x3F9D63283DAF907E,
            0x0CD29B00E3F2C9D2, 0x300CD4B730CEAA5F, 0x9832E0F216512A74, 0x9AF8CEE3D830EB0D,
            0x9279F1B57B9EC54B, 0xD36886046EE651FF, 0x316796E6574D239B, 0x05750A17F3A6E6CC,
            0xCE6C3213D98176B1, 0x62A205F88452173C, 0x47154778B3CB2BF4, 0x486A9323825446FF,
            0x65655E4E0758DF38, 0x8E5086FC897CFCF2, 0x86CA0BD0442E7031, 0x4E477830A20940F0,
            0x8338F7D139EEA065, 0xBD3A2CE437E95EF7, 0x6FF8130126B29721, 0xE7DE9FEFD1ED44A3,
            0xD992257615DFA08B, 0xBE42DC12F6F7853C, 0x7EB027AB7CECA7D8, 0xDEA83EAADA7D8D53,
            0xD86902BD93CE25AA, 0xF908731AFD43F65A, 0xA5194A17DAEF5FC0, 0x6A21FD4C33664D97,
            0x701541DB3198B435, 0x9B54CDEDBB0F1EEA, 0x72409751A163D09A, 0xE26F4791BF9D75F6
        };

        private static readonly ulong[][] s_initial_states = 
        {
            new ulong [] 
            { 
                0xac989af962ddfe2d, 0xe734d619d6ac7cae, 0x161230bc051083a4, 0x941466c9c63860b8,
                0x6f7080259f89d966, 0xdc1a9b1d1ba39ece, 0x106e367b5f32e811, 0xc106fa027f8594f9,
                0xb340c8d85c1b4f1b, 0x9980736e7fa1f697, 0xd3a3eaada593dfdc, 0x689a53c9dee831a4,
                0xe4a186ec8aa9b422, 0xf06ce59c95ac74d5, 0xbf2babb5ea0d9615, 0x6eea64ddf0dc1196
            }, 
            new ulong [] 
            { 
                0xebd3202c41a398eb, 0xc145b29c7bbecd92, 0xfac7d4609151931c, 0x038a507ed6820026,
                0x45b92677269e23a4, 0x77941ad4481afbe0, 0x7a176b0226abb5cd, 0xa82fff0f4224f056,
                0x754d2e7f8996a371, 0x62e27df70849141d, 0x948f2476f7957627, 0x6c29804757b6d587,
                0x6c0d8eac2d275e5c, 0x0f7a0557c6508451, 0xea12247067d3e47b, 0x69d71cd313abe389
            }, 
            new ulong [] 
            { 
                0x8a3913d8c63b1e48, 0x9b87de4a895e3b6d, 0x2ead80d468eafa63, 0x67820f4821cb2c33,
                0x28b982904dc8ae98, 0x4942114130ea55d4, 0xec474892b255f536, 0xe13cf4ba930a25c7,
                0x4c45db278a7f9b56, 0x0eaf976349bdfc9e, 0xcd80aa267dc29f58, 0xda2eeb9d8c8bc080,
                0x3a37d5f8e881798a, 0x717ad1ddad6739f4, 0x94d375a4bdd3b4a9, 0x7f734298ba3f6c97
            }, 
            new ulong [] 
            { 
                0x17aa003e964bd16f, 0x43d5157a052e6a63, 0x0bef970c8d5e228a, 0x61c3b3f2591234e9,
                0x1e806f53c1a01d89, 0x806d2bea6b05a92a, 0xa6ba7520dbcc8e58, 0xf73bf8ba763a0fa9,
                0x694ae34105e66901, 0x5ae66f2e8e8ab546, 0x243c84c1d0a74710, 0x99c15a2db1716e3b,
                0x56f8b19decf657cf, 0x56b116577c8806a7, 0xfb1785e6dffcc2e3, 0x4bdd8ccc78465a54
            }
        };
        #endregion

        protected readonly ulong[] m_state = new ulong[16];

        public JHBase(HashSize a_hash_size) :
            base((int)a_hash_size, 128, 64)
        {
            Initialize();
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            ulong[] data = Converters.ConvertBytesToULongs(a_data, a_index, 64);

            const ulong c_2_1 = 0x5555555555555555;
            const ulong c_2_2 = 0xaaaaaaaaaaaaaaaa;
            const ulong c_4_1 = 0x3333333333333333;
            const ulong c_4_2 = 0xcccccccccccccccc;
            const ulong c_8_1 = 0x0f0f0f0f0f0f0f0f;
            const ulong c_8_2 = 0xf0f0f0f0f0f0f0f0;
            const ulong c_16_1 = 0x00ff00ff00ff00ff;
            const ulong c_16_2 = 0xff00ff00ff00ff00;
            const ulong c_32_1 = 0x0000ffff0000ffff;
            const ulong c_32_2 = 0xffff0000ffff0000;

            ulong m0 = m_state[0] ^ data[0];
            ulong m1 = m_state[1] ^ data[1];
            ulong m2 = m_state[2] ^ data[2];
            ulong m3 = m_state[3] ^ data[3];
            ulong m4 = m_state[4] ^ data[4];
            ulong m5 = m_state[5] ^ data[5];
            ulong m6 = m_state[6] ^ data[6];
            ulong m7 = m_state[7] ^ data[7];

            ulong m8 = m_state[8];
            ulong m9 = m_state[9];
            ulong m10 = m_state[10];
            ulong m11 = m_state[11];
            ulong m12 = m_state[12];
            ulong m13 = m_state[13];
            ulong m14 = m_state[14];
            ulong m15 = m_state[15];

            ulong t0;
            ulong t1;

            for (int r = 0; r < 42; r = r + 7)
            {
                m12 = ~m12;
                m14 = ~m14;
                m0 ^= ~m8 & s_bitslices[r * 4];
                m2 ^= ~m10 & s_bitslices[r * 4 + 2];
                t0 = s_bitslices[r * 4] ^ (m0 & m4);
                t1 = s_bitslices[r * 4 + 2] ^ (m2 & m6);
                m0 ^= m8 & m12;
                m2 ^= m10 & m14;
                m12 ^= ~m4 & m8;
                m14 ^= ~m6 & m10;
                m4 ^= m0 & m8;
                m6 ^= m2 & m10;
                m8 ^= m0 & ~m12;
                m10 ^= m2 & ~m14;
                m0 ^= m4 | m12;
                m2 ^= m6 | m14;
                m12 ^= m4 & m8;
                m14 ^= m6 & m10;
                m4 ^= t0 & m0;
                m6 ^= t1 & m2;
                m8 ^= t0;
                m10 ^= t1;

                m2 ^= m4;
                m6 ^= m8;
                m10 ^= m0 ^ m12;
                m14 ^= m0;
                m0 ^= m6;
                m4 ^= m10;
                m8 ^= m2 ^ m14;
                m12 ^= m2;

                m2 = ((m2 & c_2_1) << 1) | ((m2 & c_2_2) >> 1);
                m6 = ((m6 & c_2_1) << 1) | ((m6 & c_2_2) >> 1);
                m10 = ((m10 & c_2_1) << 1) | ((m10 & c_2_2) >> 1);
                m14 = ((m14 & c_2_1) << 1) | ((m14 & c_2_2) >> 1);

                m13 = ~m13;
                m15 = ~m15;
                m1 ^= ~m9 & s_bitslices[r * 4 + 1];
                m3 ^= ~m11 & s_bitslices[r * 4 + 1 + 2];
                t0 = s_bitslices[r * 4 + 1] ^ (m1 & m5);
                t1 = s_bitslices[r * 4 + 1 + 2] ^ (m3 & m7);
                m1 ^= m9 & m13;
                m3 ^= m11 & m15;
                m13 ^= ~m5 & m9;
                m15 ^= ~m7 & m11;
                m5 ^= m1 & m9;
                m7 ^= m3 & m11;
                m9 ^= m1 & ~m13;
                m11 ^= m3 & ~m15;
                m1 ^= m5 | m13;
                m3 ^= m7 | m15;
                m13 ^= m5 & m9;
                m15 ^= m7 & m11;
                m5 ^= t0 & m1;
                m7 ^= t1 & m3;
                m9 ^= t0;
                m11 ^= t1;

                m3 ^= m5;
                m7 ^= m9;
                m11 ^= m1 ^ m13;
                m15 ^= m1;
                m1 ^= m7;
                m5 ^= m11;
                m9 ^= m3 ^ m15;
                m13 ^= m3;

                m3 = ((m3 & c_2_1) << 1) | ((m3 & c_2_2) >> 1);
                m7 = ((m7 & c_2_1) << 1) | ((m7 & c_2_2) >> 1);
                m11 = ((m11 & c_2_1) << 1) | ((m11 & c_2_2) >> 1);
                m15 = ((m15 & c_2_1) << 1) | ((m15 & c_2_2) >> 1);

                m12 = ~m12;
                m14 = ~m14;
                m0 ^= ~m8 & s_bitslices[(r + 1) * 4];
                m2 ^= ~m10 & s_bitslices[(r + 1) * 4 + 2];
                t0 = s_bitslices[(r + 1) * 4] ^ (m0 & m4);
                t1 = s_bitslices[(r + 1) * 4 + 2] ^ (m2 & m6);
                m0 ^= m8 & m12;
                m2 ^= m10 & m14;
                m12 ^= ~m4 & m8;
                m14 ^= ~m6 & m10;
                m4 ^= m0 & m8;
                m6 ^= m2 & m10;
                m8 ^= m0 & ~m12;
                m10 ^= m2 & ~m14;
                m0 ^= m4 | m12;
                m2 ^= m6 | m14;
                m12 ^= m4 & m8;
                m14 ^= m6 & m10;
                m4 ^= t0 & m0;
                m6 ^= t1 & m2;
                m8 ^= t0;
                m10 ^= t1;

                m2 ^= m4;
                m6 ^= m8;
                m10 ^= m0 ^ m12;
                m14 ^= m0;
                m0 ^= m6;
                m4 ^= m10;
                m8 ^= m2 ^ m14;
                m12 ^= m2;

                m2 = ((m2 & c_4_1) << 2) | ((m2 & c_4_2) >> 2);
                m6 = ((m6 & c_4_1) << 2) | ((m6 & c_4_2) >> 2);
                m10 = ((m10 & c_4_1) << 2) | ((m10 & c_4_2) >> 2);
                m14 = ((m14 & c_4_1) << 2) | ((m14 & c_4_2) >> 2);

                m13 = ~m13;
                m15 = ~m15;
                m1 ^= ~m9 & s_bitslices[(r + 1) * 4 + 1];
                m3 ^= ~m11 & s_bitslices[(r + 1) * 4 + 1 + 2];
                t0 = s_bitslices[(r + 1) * 4 + 1] ^ (m1 & m5);
                t1 = s_bitslices[(r + 1) * 4 + 1 + 2] ^ (m3 & m7);
                m1 ^= m9 & m13;
                m3 ^= m11 & m15;
                m13 ^= ~m5 & m9;
                m15 ^= ~m7 & m11;
                m5 ^= m1 & m9;
                m7 ^= m3 & m11;
                m9 ^= m1 & ~m13;
                m11 ^= m3 & ~m15;
                m1 ^= m5 | m13;
                m3 ^= m7 | m15;
                m13 ^= m5 & m9;
                m15 ^= m7 & m11;
                m5 ^= t0 & m1;
                m7 ^= t1 & m3;
                m9 ^= t0;
                m11 ^= t1;

                m3 ^= m5;
                m7 ^= m9;
                m11 ^= m1 ^ m13;
                m15 ^= m1;
                m1 ^= m7;
                m5 ^= m11;
                m9 ^= m3 ^ m15;
                m13 ^= m3;

                m3 = ((m3 & c_4_1) << 2) | ((m3 & c_4_2) >> 2);
                m7 = ((m7 & c_4_1) << 2) | ((m7 & c_4_2) >> 2);
                m11 = ((m11 & c_4_1) << 2) | ((m11 & c_4_2) >> 2);
                m15 = ((m15 & c_4_1) << 2) | ((m15 & c_4_2) >> 2);

                m12 = ~m12;
                m14 = ~m14;
                m0 ^= ~m8 & s_bitslices[(r + 2) * 4];
                m2 ^= ~m10 & s_bitslices[(r + 2) * 4 + 2];
                t0 = s_bitslices[(r + 2) * 4] ^ (m0 & m4);
                t1 = s_bitslices[(r + 2) * 4 + 2] ^ (m2 & m6);
                m0 ^= m8 & m12;
                m2 ^= m10 & m14;
                m12 ^= ~m4 & m8;
                m14 ^= ~m6 & m10;
                m4 ^= m0 & m8;
                m6 ^= m2 & m10;
                m8 ^= m0 & ~m12;
                m10 ^= m2 & ~m14;
                m0 ^= m4 | m12;
                m2 ^= m6 | m14;
                m12 ^= m4 & m8;
                m14 ^= m6 & m10;
                m4 ^= t0 & m0;
                m6 ^= t1 & m2;
                m8 ^= t0;
                m10 ^= t1;

                m2 ^= m4;
                m6 ^= m8;
                m10 ^= m0 ^ m12;
                m14 ^= m0;
                m0 ^= m6;
                m4 ^= m10;
                m8 ^= m2 ^ m14;
                m12 ^= m2;

                m2 = ((m2 & c_8_1) << 4) | ((m2 & c_8_2) >> 4);
                m6 = ((m6 & c_8_1) << 4) | ((m6 & c_8_2) >> 4);
                m10 = ((m10 & c_8_1) << 4) | ((m10 & c_8_2) >> 4);
                m14 = ((m14 & c_8_1) << 4) | ((m14 & c_8_2) >> 4);

                m13 = ~m13;
                m15 = ~m15;
                m1 ^= ~m9 & s_bitslices[(r + 2) * 4 + 1];
                m3 ^= ~m11 & s_bitslices[(r + 2) * 4 + 1 + 2];
                t0 = s_bitslices[(r + 2) * 4 + 1] ^ (m1 & m5);
                t1 = s_bitslices[(r + 2) * 4 + 1 + 2] ^ (m3 & m7);
                m1 ^= m9 & m13;
                m3 ^= m11 & m15;
                m13 ^= ~m5 & m9;
                m15 ^= ~m7 & m11;
                m5 ^= m1 & m9;
                m7 ^= m3 & m11;
                m9 ^= m1 & ~m13;
                m11 ^= m3 & ~m15;
                m1 ^= m5 | m13;
                m3 ^= m7 | m15;
                m13 ^= m5 & m9;
                m15 ^= m7 & m11;
                m5 ^= t0 & m1;
                m7 ^= t1 & m3;
                m9 ^= t0;
                m11 ^= t1;

                m3 ^= m5;
                m7 ^= m9;
                m11 ^= m1 ^ m13;
                m15 ^= m1;
                m1 ^= m7;
                m5 ^= m11;
                m9 ^= m3 ^ m15;
                m13 ^= m3;

                m3 = ((m3 & c_8_1) << 4) | ((m3 & c_8_2) >> 4);
                m7 = ((m7 & c_8_1) << 4) | ((m7 & c_8_2) >> 4);
                m11 = ((m11 & c_8_1) << 4) | ((m11 & c_8_2) >> 4);
                m15 = ((m15 & c_8_1) << 4) | ((m15 & c_8_2) >> 4);

                m12 = ~m12;
                m14 = ~m14;
                m0 ^= ~m8 & s_bitslices[(r + 3) * 4];
                m2 ^= ~m10 & s_bitslices[(r + 3) * 4 + 2];
                t0 = s_bitslices[(r + 3) * 4] ^ (m0 & m4);
                t1 = s_bitslices[(r + 3) * 4 + 2] ^ (m2 & m6);
                m0 ^= m8 & m12;
                m2 ^= m10 & m14;
                m12 ^= ~m4 & m8;
                m14 ^= ~m6 & m10;
                m4 ^= m0 & m8;
                m6 ^= m2 & m10;
                m8 ^= m0 & ~m12;
                m10 ^= m2 & ~m14;
                m0 ^= m4 | m12;
                m2 ^= m6 | m14;
                m12 ^= m4 & m8;
                m14 ^= m6 & m10;
                m4 ^= t0 & m0;
                m6 ^= t1 & m2;
                m8 ^= t0;
                m10 ^= t1;

                m2 ^= m4;
                m6 ^= m8;
                m10 ^= m0 ^ m12;
                m14 ^= m0;
                m0 ^= m6;
                m4 ^= m10;
                m8 ^= m2 ^ m14;
                m12 ^= m2;

                m2 = ((m2 & c_16_1) << 8) | ((m2 & c_16_2) >> 8);
                m6 = ((m6 & c_16_1) << 8) | ((m6 & c_16_2) >> 8);
                m10 = ((m10 & c_16_1) << 8) | ((m10 & c_16_2) >> 8);
                m14 = ((m14 & c_16_1) << 8) | ((m14 & c_16_2) >> 8);

                m13 = ~m13;
                m15 = ~m15;
                m1 ^= ~m9 & s_bitslices[(r + 3) * 4 + 1];
                m3 ^= ~m11 & s_bitslices[(r + 3) * 4 + 1 + 2];
                t0 = s_bitslices[(r + 3) * 4 + 1] ^ (m1 & m5);
                t1 = s_bitslices[(r + 3) * 4 + 1 + 2] ^ (m3 & m7);
                m1 ^= m9 & m13;
                m3 ^= m11 & m15;
                m13 ^= ~m5 & m9;
                m15 ^= ~m7 & m11;
                m5 ^= m1 & m9;
                m7 ^= m3 & m11;
                m9 ^= m1 & ~m13;
                m11 ^= m3 & ~m15;
                m1 ^= m5 | m13;
                m3 ^= m7 | m15;
                m13 ^= m5 & m9;
                m15 ^= m7 & m11;
                m5 ^= t0 & m1;
                m7 ^= t1 & m3;
                m9 ^= t0;
                m11 ^= t1;

                m3 ^= m5;
                m7 ^= m9;
                m11 ^= m1 ^ m13;
                m15 ^= m1;
                m1 ^= m7;
                m5 ^= m11;
                m9 ^= m3 ^ m15;
                m13 ^= m3;

                m3 = ((m3 & c_16_1) << 8) | ((m3 & c_16_2) >> 8);
                m7 = ((m7 & c_16_1) << 8) | ((m7 & c_16_2) >> 8);
                m11 = ((m11 & c_16_1) << 8) | ((m11 & c_16_2) >> 8);
                m15 = ((m15 & c_16_1) << 8) | ((m15 & c_16_2) >> 8);

                m12 = ~m12;
                m14 = ~m14;
                m0 ^= ~m8 & s_bitslices[(r + 4) * 4];
                m2 ^= ~m10 & s_bitslices[(r + 4) * 4 + 2];
                t0 = s_bitslices[(r + 4) * 4] ^ (m0 & m4);
                t1 = s_bitslices[(r + 4) * 4 + 2] ^ (m2 & m6);
                m0 ^= m8 & m12;
                m2 ^= m10 & m14;
                m12 ^= ~m4 & m8;
                m14 ^= ~m6 & m10;
                m4 ^= m0 & m8;
                m6 ^= m2 & m10;
                m8 ^= m0 & ~m12;
                m10 ^= m2 & ~m14;
                m0 ^= m4 | m12;
                m2 ^= m6 | m14;
                m12 ^= m4 & m8;
                m14 ^= m6 & m10;
                m4 ^= t0 & m0;
                m6 ^= t1 & m2;
                m8 ^= t0;
                m10 ^= t1;

                m2 ^= m4;
                m6 ^= m8;
                m10 ^= m0 ^ m12;
                m14 ^= m0;
                m0 ^= m6;
                m4 ^= m10;
                m8 ^= m2 ^ m14;
                m12 ^= m2;

                m2 = ((m2 & c_32_1) << 16) | ((m2 & c_32_2) >> 16);
                m6 = ((m6 & c_32_1) << 16) | ((m6 & c_32_2) >> 16);
                m10 = ((m10 & c_32_1) << 16) | ((m10 & c_32_2) >> 16);
                m14 = ((m14 & c_32_1) << 16) | ((m14 & c_32_2) >> 16);

                m13 = ~m13;
                m15 = ~m15;
                m1 ^= ~m9 & s_bitslices[(r + 4) * 4 + 1];
                m3 ^= ~m11 & s_bitslices[(r + 4) * 4 + 1 + 2];
                t0 = s_bitslices[(r + 4) * 4 + 1] ^ (m1 & m5);
                t1 = s_bitslices[(r + 4) * 4 + 1 + 2] ^ (m3 & m7);
                m1 ^= m9 & m13;
                m3 ^= m11 & m15;
                m13 ^= ~m5 & m9;
                m15 ^= ~m7 & m11;
                m5 ^= m1 & m9;
                m7 ^= m3 & m11;
                m9 ^= m1 & ~m13;
                m11 ^= m3 & ~m15;
                m1 ^= m5 | m13;
                m3 ^= m7 | m15;
                m13 ^= m5 & m9;
                m15 ^= m7 & m11;
                m5 ^= t0 & m1;
                m7 ^= t1 & m3;
                m9 ^= t0;
                m11 ^= t1;

                m3 ^= m5;
                m7 ^= m9;
                m11 ^= m1 ^ m13;
                m15 ^= m1;
                m1 ^= m7;
                m5 ^= m11;
                m9 ^= m3 ^ m15;
                m13 ^= m3;

                m3 = ((m3 & c_32_1) << 16) | ((m3 & c_32_2) >> 16);
                m7 = ((m7 & c_32_1) << 16) | ((m7 & c_32_2) >> 16);
                m11 = ((m11 & c_32_1) << 16) | ((m11 & c_32_2) >> 16);
                m15 = ((m15 & c_32_1) << 16) | ((m15 & c_32_2) >> 16);

                m12 = ~m12;
                m14 = ~m14;
                m0 ^= ~m8 & s_bitslices[(r + 5) * 4];
                m2 ^= ~m10 & s_bitslices[(r + 5) * 4 + 2];
                t0 = s_bitslices[(r + 5) * 4] ^ (m0 & m4);
                t1 = s_bitslices[(r + 5) * 4 + 2] ^ (m2 & m6);
                m0 ^= m8 & m12;
                m2 ^= m10 & m14;
                m12 ^= ~m4 & m8;
                m14 ^= ~m6 & m10;
                m4 ^= m0 & m8;
                m6 ^= m2 & m10;
                m8 ^= m0 & ~m12;
                m10 ^= m2 & ~m14;
                m0 ^= m4 | m12;
                m2 ^= m6 | m14;
                m12 ^= m4 & m8;
                m14 ^= m6 & m10;
                m4 ^= t0 & m0;
                m6 ^= t1 & m2;
                m8 ^= t0;
                m10 ^= t1;

                m2 ^= m4;
                m6 ^= m8;
                m10 ^= m0 ^ m12;
                m14 ^= m0;
                m0 ^= m6;
                m4 ^= m10;
                m8 ^= m2 ^ m14;
                m12 ^= m2;

                m2 = (m2 << 32) | (m2 >> 32);
                m6 = (m6 << 32) | (m6 >> 32);
                m10 = (m10 << 32) | (m10 >> 32);
                m14 = (m14 << 32) | (m14 >> 32);

                m13 = ~m13;
                m15 = ~m15;
                m1 ^= ~m9 & s_bitslices[(r + 5) * 4 + 1];
                m3 ^= ~m11 & s_bitslices[(r + 5) * 4 + 1 + 2];
                t0 = s_bitslices[(r + 5) * 4 + 1] ^ (m1 & m5);
                t1 = s_bitslices[(r + 5) * 4 + 1 + 2] ^ (m3 & m7);
                m1 ^= m9 & m13;
                m3 ^= m11 & m15;
                m13 ^= ~m5 & m9;
                m15 ^= ~m7 & m11;
                m5 ^= m1 & m9;
                m7 ^= m3 & m11;
                m9 ^= m1 & ~m13;
                m11 ^= m3 & ~m15;
                m1 ^= m5 | m13;
                m3 ^= m7 | m15;
                m13 ^= m5 & m9;
                m15 ^= m7 & m11;
                m5 ^= t0 & m1;
                m7 ^= t1 & m3;
                m9 ^= t0;
                m11 ^= t1;

                m3 ^= m5;
                m7 ^= m9;
                m11 ^= m1 ^ m13;
                m15 ^= m1;
                m1 ^= m7;
                m5 ^= m11;
                m9 ^= m3 ^ m15;
                m13 ^= m3;

                m3 = (m3 << 32) | (m3 >> 32);
                m7 = (m7 << 32) | (m7 >> 32);
                m11 = (m11 << 32) | (m11 >> 32);
                m15 = (m15 << 32) | (m15 >> 32);

                m12 = ~m12;
                m14 = ~m14;
                m0 ^= ~m8 & s_bitslices[(r + 6) * 4];
                m2 ^= ~m10 & s_bitslices[(r + 6) * 4 + 2];
                t0 = s_bitslices[(r + 6) * 4] ^ (m0 & m4);
                t1 = s_bitslices[(r + 6) * 4 + 2] ^ (m2 & m6);
                m0 ^= m8 & m12;
                m2 ^= m10 & m14;
                m12 ^= ~m4 & m8;
                m14 ^= ~m6 & m10;
                m4 ^= m0 & m8;
                m6 ^= m2 & m10;
                m8 ^= m0 & ~m12;
                m10 ^= m2 & ~m14;
                m0 ^= m4 | m12;
                m2 ^= m6 | m14;
                m12 ^= m4 & m8;
                m14 ^= m6 & m10;
                m4 ^= t0 & m0;
                m6 ^= t1 & m2;
                m8 ^= t0;
                m10 ^= t1;

                m2 ^= m4;
                m6 ^= m8;
                m10 ^= m0 ^ m12;
                m14 ^= m0;
                m0 ^= m6;
                m4 ^= m10;
                m8 ^= m2 ^ m14;
                m12 ^= m2;

                m13 = ~m13;
                m15 = ~m15;
                m1 ^= ~m9 & s_bitslices[(r + 6) * 4 + 1];
                m3 ^= ~m11 & s_bitslices[(r + 6) * 4 + 1 + 2];
                t0 = s_bitslices[(r + 6) * 4 + 1] ^ (m1 & m5);
                t1 = s_bitslices[(r + 6) * 4 + 1 + 2] ^ (m3 & m7);
                m1 ^= m9 & m13;
                m3 ^= m11 & m15;
                m13 ^= ~m5 & m9;
                m15 ^= ~m7 & m11;
                m5 ^= m1 & m9;
                m7 ^= m3 & m11;
                m9 ^= m1 & ~m13;
                m11 ^= m3 & ~m15;
                m1 ^= m5 | m13;
                m3 ^= m7 | m15;
                m13 ^= m5 & m9;
                m15 ^= m7 & m11;
                m5 ^= t0 & m1;
                m7 ^= t1 & m3;
                m9 ^= t0;
                m11 ^= t1;

                m3 ^= m5;
                m7 ^= m9;
                m11 ^= m1 ^ m13;
                m15 ^= m1;
                m1 ^= m7;
                m5 ^= m11;
                m9 ^= m3 ^ m15;
                m13 ^= m3;

                t0 = m2;
                m2 = m3;
                m3 = t0;
                t0 = m6;
                m6 = m7;
                m7 = t0;
                t0 = m10;
                m10 = m11;
                m11 = t0;
                t0 = m14;
                m14 = m15;
                m15 = t0;
            }

            m_state[0] = m0;
            m_state[1] = m1;
            m_state[2] = m2;
            m_state[3] = m3;
            m_state[4] = m4;
            m_state[5] = m5;
            m_state[6] = m6;
            m_state[7] = m7;

            m_state[8] = m8 ^ data[0];
            m_state[9] = m9 ^ data[1];
            m_state[10] = m10 ^ data[2];
            m_state[11] = m11 ^ data[3];
            m_state[12] = m12 ^ data[4];
            m_state[13] = m13 ^ data[5];
            m_state[14] = m14 ^ data[6];
            m_state[15] = m15 ^ data[7];
        }

        protected override void Finish()
        {
            ulong bits = m_processed_bytes * 8;

            int padindex = 56;

            if (m_processed_bytes % 64 != 0)
                padindex += m_buffer.Length - m_buffer.Pos;

            byte[] pad = new byte[padindex + 8];

            pad[0] = 0x80;

            Converters.ConvertULongToBytesSwapOrder(bits, pad, padindex);
            padindex += 8;

            TransformBytes(pad, 0, padindex);
        }

        protected override byte[] GetResult()
        {
            return Converters.ConvertULongsToBytes(m_state, 8, 8).SubArray(64 - HashSize, HashSize);
        }

        public override void Initialize()
        {
            switch (HashSize)
            {
                case 28:
                    Array.Copy(s_initial_states[0], m_state, m_state.Length);
                    break;
                case 32:
                    Array.Copy(s_initial_states[1], m_state, m_state.Length);
                    break;
                case 48:
                    Array.Copy(s_initial_states[2], m_state, m_state.Length);
                    break;
                case 64:
                    Array.Copy(s_initial_states[3], m_state, m_state.Length);
                    break;
            }

            base.Initialize();
        }
    }
}
