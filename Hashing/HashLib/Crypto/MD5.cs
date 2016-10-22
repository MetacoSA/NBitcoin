using System;

namespace HashLib.Crypto
{
    internal class MD5 : MDBase
    {
        public MD5() 
            : base(4, 16)
        {
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            uint data0 = Converters.ConvertBytesToUInt(a_data, a_index + 4 * 0);
            uint data1 = Converters.ConvertBytesToUInt(a_data, a_index + 4 * 1);
            uint data2 = Converters.ConvertBytesToUInt(a_data, a_index + 4 * 2);
            uint data3 = Converters.ConvertBytesToUInt(a_data, a_index + 4 * 3);
            uint data4 = Converters.ConvertBytesToUInt(a_data, a_index + 4 * 4);
            uint data5 = Converters.ConvertBytesToUInt(a_data, a_index + 4 * 5);
            uint data6 = Converters.ConvertBytesToUInt(a_data, a_index + 4 * 6);
            uint data7 = Converters.ConvertBytesToUInt(a_data, a_index + 4 * 7);
            uint data8 = Converters.ConvertBytesToUInt(a_data, a_index + 4 * 8);
            uint data9 = Converters.ConvertBytesToUInt(a_data, a_index + 4 * 9);
            uint data10 = Converters.ConvertBytesToUInt(a_data, a_index + 4 * 10);
            uint data11 = Converters.ConvertBytesToUInt(a_data, a_index + 4 * 11);
            uint data12 = Converters.ConvertBytesToUInt(a_data, a_index + 4 * 12);
            uint data13 = Converters.ConvertBytesToUInt(a_data, a_index + 4 * 13);
            uint data14 = Converters.ConvertBytesToUInt(a_data, a_index + 4 * 14);
            uint data15 = Converters.ConvertBytesToUInt(a_data, a_index + 4 * 15);
            
            uint A = m_state[0];
            uint B = m_state[1];
            uint C = m_state[2];
            uint D = m_state[3];

            A = data0 + 0xd76aa478 + A + ((B & C) | (~B & D));
            A = ((A << 7) | (A >> (32 - 7))) + B;
            D = data1 + 0xe8c7b756 + D + ((A & B) | (~A & C));
            D = ((D << 12) | (D >> (32 - 12))) + A;
            C = data2 + 0x242070db + C + ((D & A) | (~D & B));
            C = ((C << 17) | (C >> (32 - 17))) + D;
            B = data3 + 0xc1bdceee + B + ((C & D) | (~C & A));
            B = ((B << 22) | (B >> (32 - 22))) + C;
            A = data4 + 0xf57c0faf + A + ((B & C) | (~B & D));
            A = ((A << 7) | (A >> (32 - 7))) + B;
            D = data5 + 0x4787c62a + D + ((A & B) | (~A & C));
            D = ((D << 12) | (D >> (32 - 12))) + A;
            C = data6 + 0xa8304613 + C + ((D & A) | (~D & B));
            C = ((C << 17) | (C >> (32 - 17))) + D;
            B = data7 + 0xfd469501 + B + ((C & D) | (~C & A));
            B = ((B << 22) | (B >> (32 - 22))) + C;
            A = data8 + 0x698098d8 + A + ((B & C) | (~B & D));
            A = ((A << 7) | (A >> (32 - 7))) + B;
            D = data9 + 0x8b44f7af + D + ((A & B) | (~A & C));
            D = ((D << 12) | (D >> (32 - 12))) + A;
            C = data10 + 0xffff5bb1 + C + ((D & A) | (~D & B));
            C = ((C << 17) | (C >> (32 - 17))) + D;
            B = data11 + 0x895cd7be + B + ((C & D) | (~C & A));
            B = ((B << 22) | (B >> (32 - 22))) + C;
            A = data12 + 0x6b901122 + A + ((B & C) | (~B & D));
            A = ((A << 7) | (A >> (32 - 7))) + B;
            D = data13 + 0xfd987193 + D + ((A & B) | (~A & C));
            D = ((D << 12) | (D >> (32 - 12))) + A;
            C = data14 + 0xa679438e + C + ((D & A) | (~D & B));
            C = ((C << 17) | (C >> (32 - 17))) + D;
            B = data15 + 0x49b40821 + B + ((C & D) | (~C & A));
            B = ((B << 22) | (B >> (32 - 22))) + C;
            
            A = data1 + 0xf61e2562 + A + ((B & D) | (C & ~D));
            A = ((A << 5) | (A >> (32 - 5))) + B;
            D = data6 + 0xc040b340 + D + ((A & C) | (B & ~C));
            D = ((D << 9) | (D >> (32 - 9))) + A;
            C = data11 + 0x265e5a51 + C + ((D & B) | (A & ~B));
            C = ((C << 14) | (C >> (32 - 14))) + D;
            B = data0 + 0xe9b6c7aa + B + ((C & A) | (D & ~A));
            B = ((B << 20) | (B >> (32 - 20))) + C;
            A = data5 + 0xd62f105d + A + ((B & D) | (C & ~D));
            A = ((A << 5) | (A >> (32 - 5))) + B;
            D = data10 + 0x2441453 + D + ((A & C) | (B & ~C));
            D = ((D << 9) | (D >> (32 - 9))) + A;
            C = data15 + 0xd8a1e681 + C + ((D & B) | (A & ~B));
            C = ((C << 14) | (C >> (32 - 14))) + D;
            B = data4 + 0xe7d3fbc8 + B + ((C & A) | (D & ~A));
            B = ((B << 20) | (B >> (32 - 20))) + C;
            A = data9 + 0x21e1cde6 + A + ((B & D) | (C & ~D));
            A = ((A << 5) | (A >> (32 - 5))) + B;
            D = data14 + 0xc33707d6 + D + ((A & C) | (B & ~C));
            D = ((D << 9) | (D >> (32 - 9))) + A;
            C = data3 + 0xf4d50d87 + C + ((D & B) | (A & ~B));
            C = ((C << 14) | (C >> (32 - 14))) + D;
            B = data8 + 0x455a14ed + B + ((C & A) | (D & ~A));
            B = ((B << 20) | (B >> (32 - 20))) + C;
            A = data13 + 0xa9e3e905 + A + ((B & D) | (C & ~D));
            A = ((A << 5) | (A >> (32 - 5))) + B;
            D = data2 + 0xfcefa3f8 + D + ((A & C) | (B & ~C));
            D = ((D << 9) | (D >> (32 - 9))) + A;
            C = data7 + 0x676f02d9 + C + ((D & B) | (A & ~B));
            C = ((C << 14) | (C >> (32 - 14))) + D;
            B = data12 + 0x8d2a4c8a + B + ((C & A) | (D & ~A));
            B = ((B << 20) | (B >> (32 - 20))) + C;

            A = data5 + 0xfffa3942 + A + (B ^ C ^ D);
            A = ((A << 4) | (A >> (32 - 4))) + B;
            D = data8 + 0x8771f681 + D + (A ^ B ^ C);
            D = ((D << 11) | (D >> (32 - 11))) + A;
            C = data11 + 0x6d9d6122 + C + (D ^ A ^ B);
            C = ((C << 16) | (C >> (32 - 16))) + D;
            B = data14 + 0xfde5380c + B + (C ^ D ^ A);
            B = ((B << 23) | (B >> (32 - 23))) + C;
            A = data1 + 0xa4beea44 + A + (B ^ C ^ D);
            A = ((A << 4) | (A >> (32 - 4))) + B;
            D = data4 + 0x4bdecfa9 + D + (A ^ B ^ C);
            D = ((D << 11) | (D >> (32 - 11))) + A;
            C = data7 + 0xf6bb4b60 + C + (D ^ A ^ B);
            C = ((C << 16) | (C >> (32 - 16))) + D;
            B = data10 + 0xbebfbc70 + B + (C ^ D ^ A);
            B = ((B << 23) | (B >> (32 - 23))) + C;
            A = data13 + 0x289b7ec6 + A + (B ^ C ^ D);
            A = ((A << 4) | (A >> (32 - 4))) + B;
            D = data0 + 0xeaa127fa + D + (A ^ B ^ C);
            D = ((D << 11) | (D >> (32 - 11))) + A;
            C = data3 + 0xd4ef3085 + C + (D ^ A ^ B);
            C = ((C << 16) | (C >> (32 - 16))) + D;
            B = data6 + 0x4881d05 + B + (C ^ D ^ A);
            B = ((B << 23) | (B >> (32 - 23))) + C;
            A = data9 + 0xd9d4d039 + A + (B ^ C ^ D);
            A = ((A << 4) | (A >> (32 - 4))) + B;
            D = data12 + 0xe6db99e5 + D + (A ^ B ^ C);
            D = ((D << 11) | (D >> (32 - 11))) + A;
            C = data15 + 0x1fa27cf8 + C + (D ^ A ^ B);
            C = ((C << 16) | (C >> (32 - 16))) + D;
            B = data2 + 0xc4ac5665 + B + (C ^ D ^ A);
            B = ((B << 23) | (B >> (32 - 23))) + C;

            A = data0 + 0xf4292244 + A + (C ^ (B | ~D));
            A = ((A << 6) | (A >> (32 - 6))) + B;
            D = data7 + 0x432aff97 + D + (B ^ (A | ~C));
            D = ((D << 10) | (D >> (32 - 10))) + A;
            C = data14 + 0xab9423a7 + C + (A ^ (D | ~B));
            C = ((C << 15) | (C >> (32 - 15))) + D;
            B = data5 + 0xfc93a039 + B + (D ^ (C | ~A));
            B = ((B << 21) | (B >> (32 - 21))) + C;
            A = data12 + 0x655b59c3 + A + (C ^ (B | ~D));
            A = ((A << 6) | (A >> (32 - 6))) + B;
            D = data3 + 0x8f0ccc92 + D + (B ^ (A | ~C));
            D = ((D << 10) | (D >> (32 - 10))) + A;
            C = data10 + 0xffeff47d + C + (A ^ (D | ~B));
            C = ((C << 15) | (C >> (32 - 15))) + D;
            B = data1 + 0x85845dd1 + B + (D ^ (C | ~A));
            B = ((B << 21) | (B >> (32 - 21))) + C;
            A = data8 + 0x6fa87e4f + A + (C ^ (B | ~D));
            A = ((A << 6) | (A >> (32 - 6))) + B;
            D = data15 + 0xfe2ce6e0 + D + (B ^ (A | ~C));
            D = ((D << 10) | (D >> (32 - 10))) + A;
            C = data6 + 0xa3014314 + C + (A ^ (D | ~B));
            C = ((C << 15) | (C >> (32 - 15))) + D;
            B = data13 + 0x4e0811a1 + B + (D ^ (C | ~A));
            B = ((B << 21) | (B >> (32 - 21))) + C;
            A = data4 + 0xf7537e82 + A + (C ^ (B | ~D));
            A = ((A << 6) | (A >> (32 - 6))) + B;
            D = data11 + 0xbd3af235 + D + (B ^ (A | ~C));
            D = ((D << 10) | (D >> (32 - 10))) + A;
            C = data2 + 0x2ad7d2bb + C + (A ^ (D | ~B));
            C = ((C << 15) | (C >> (32 - 15))) + D;
            B = data9 + 0xeb86d391 + B + (D ^ (C | ~A));
            B = ((B << 21) | (B >> (32 - 21))) + C;

            m_state[0] += A;
            m_state[1] += B;
            m_state[2] += C;
            m_state[3] += D;
        }
    }
}
