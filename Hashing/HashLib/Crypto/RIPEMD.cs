using System;

namespace HashLib.Crypto
{
    internal class RIPEMD : MDBase
    {
        public RIPEMD()
            : base(4, 16)
        {
            Initialize();
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

            uint a = m_state[0];
            uint b = m_state[1];
            uint c = m_state[2];
            uint d = m_state[3];
            uint aa = a;
            uint bb = b;
            uint cc = c;
            uint dd = d;

            a = Bits.RotateLeft(P1(b, c, d) + a + data0, 11);
            d = Bits.RotateLeft(P1(a, b, c) + d + data1, 14);
            c = Bits.RotateLeft(P1(d, a, b) + c + data2, 15);
            b = Bits.RotateLeft(P1(c, d, a) + b + data3, 12);
            a = Bits.RotateLeft(P1(b, c, d) + a + data4, 5);
            d = Bits.RotateLeft(P1(a, b, c) + d + data5, 8);
            c = Bits.RotateLeft(P1(d, a, b) + c + data6, 7);
            b = Bits.RotateLeft(P1(c, d, a) + b + data7, 9);
            a = Bits.RotateLeft(P1(b, c, d) + a + data8, 11);
            d = Bits.RotateLeft(P1(a, b, c) + d + data9, 13);
            c = Bits.RotateLeft(P1(d, a, b) + c + data10, 14);
            b = Bits.RotateLeft(P1(c, d, a) + b + data11, 15);
            a = Bits.RotateLeft(P1(b, c, d) + a + data12, 6);
            d = Bits.RotateLeft(P1(a, b, c) + d + data13, 7);
            c = Bits.RotateLeft(P1(d, a, b) + c + data14, 9);
            b = Bits.RotateLeft(P1(c, d, a) + b + data15, 8);

            a = Bits.RotateLeft(P2(b, c, d) + a + data7 + C2, 7);
            d = Bits.RotateLeft(P2(a, b, c) + d + data4 + C2, 6);
            c = Bits.RotateLeft(P2(d, a, b) + c + data13 + C2, 8);
            b = Bits.RotateLeft(P2(c, d, a) + b + data1 + C2, 13);
            a = Bits.RotateLeft(P2(b, c, d) + a + data10 + C2, 11);
            d = Bits.RotateLeft(P2(a, b, c) + d + data6 + C2, 9);
            c = Bits.RotateLeft(P2(d, a, b) + c + data15 + C2, 7);
            b = Bits.RotateLeft(P2(c, d, a) + b + data3 + C2, 15);
            a = Bits.RotateLeft(P2(b, c, d) + a + data12 + C2, 7);
            d = Bits.RotateLeft(P2(a, b, c) + d + data0 + C2, 12);
            c = Bits.RotateLeft(P2(d, a, b) + c + data9 + C2, 15);
            b = Bits.RotateLeft(P2(c, d, a) + b + data5 + C2, 9);
            a = Bits.RotateLeft(P2(b, c, d) + a + data14 + C2, 7);
            d = Bits.RotateLeft(P2(a, b, c) + d + data2 + C2, 11);
            c = Bits.RotateLeft(P2(d, a, b) + c + data11 + C2, 13);
            b = Bits.RotateLeft(P2(c, d, a) + b + data8 + C2, 12);

            a = Bits.RotateLeft(P3(b, c, d) + a + data3 + C4, 11);
            d = Bits.RotateLeft(P3(a, b, c) + d + data10 + C4, 13);
            c = Bits.RotateLeft(P3(d, a, b) + c + data2 + C4, 14);
            b = Bits.RotateLeft(P3(c, d, a) + b + data4 + C4, 7);
            a = Bits.RotateLeft(P3(b, c, d) + a + data9 + C4, 14);
            d = Bits.RotateLeft(P3(a, b, c) + d + data15 + C4, 9);
            c = Bits.RotateLeft(P3(d, a, b) + c + data8 + C4, 13);
            b = Bits.RotateLeft(P3(c, d, a) + b + data1 + C4, 15);
            a = Bits.RotateLeft(P3(b, c, d) + a + data14 + C4, 6);
            d = Bits.RotateLeft(P3(a, b, c) + d + data7 + C4, 8);
            c = Bits.RotateLeft(P3(d, a, b) + c + data0 + C4, 13);
            b = Bits.RotateLeft(P3(c, d, a) + b + data6 + C4, 6);
            a = Bits.RotateLeft(P3(b, c, d) + a + data11 + C4, 12);
            d = Bits.RotateLeft(P3(a, b, c) + d + data13 + C4, 5);
            c = Bits.RotateLeft(P3(d, a, b) + c + data5 + C4, 7);
            b = Bits.RotateLeft(P3(c, d, a) + b + data12 + C4, 5);

            aa = Bits.RotateLeft(P1(bb, cc, dd) + aa + data0 + C1, 11);
            dd = Bits.RotateLeft(P1(aa, bb, cc) + dd + data1 + C1, 14);
            cc = Bits.RotateLeft(P1(dd, aa, bb) + cc + data2 + C1, 15);
            bb = Bits.RotateLeft(P1(cc, dd, aa) + bb + data3 + C1, 12);
            aa = Bits.RotateLeft(P1(bb, cc, dd) + aa + data4 + C1, 5);
            dd = Bits.RotateLeft(P1(aa, bb, cc) + dd + data5 + C1, 8);
            cc = Bits.RotateLeft(P1(dd, aa, bb) + cc + data6 + C1, 7);
            bb = Bits.RotateLeft(P1(cc, dd, aa) + bb + data7 + C1, 9);
            aa = Bits.RotateLeft(P1(bb, cc, dd) + aa + data8 + C1, 11);
            dd = Bits.RotateLeft(P1(aa, bb, cc) + dd + data9 + C1, 13);
            cc = Bits.RotateLeft(P1(dd, aa, bb) + cc + data10 + C1, 14);
            bb = Bits.RotateLeft(P1(cc, dd, aa) + bb + data11 + C1, 15);
            aa = Bits.RotateLeft(P1(bb, cc, dd) + aa + data12 + C1, 6);
            dd = Bits.RotateLeft(P1(aa, bb, cc) + dd + data13 + C1, 7);
            cc = Bits.RotateLeft(P1(dd, aa, bb) + cc + data14 + C1, 9);
            bb = Bits.RotateLeft(P1(cc, dd, aa) + bb + data15 + C1, 8);

            aa = Bits.RotateLeft(P2(bb, cc, dd) + aa + data7, 7);
            dd = Bits.RotateLeft(P2(aa, bb, cc) + dd + data4, 6);
            cc = Bits.RotateLeft(P2(dd, aa, bb) + cc + data13, 8);
            bb = Bits.RotateLeft(P2(cc, dd, aa) + bb + data1, 13);
            aa = Bits.RotateLeft(P2(bb, cc, dd) + aa + data10, 11);
            dd = Bits.RotateLeft(P2(aa, bb, cc) + dd + data6, 9);
            cc = Bits.RotateLeft(P2(dd, aa, bb) + cc + data15, 7);
            bb = Bits.RotateLeft(P2(cc, dd, aa) + bb + data3, 15);
            aa = Bits.RotateLeft(P2(bb, cc, dd) + aa + data12, 7);
            dd = Bits.RotateLeft(P2(aa, bb, cc) + dd + data0, 12);
            cc = Bits.RotateLeft(P2(dd, aa, bb) + cc + data9, 15);
            bb = Bits.RotateLeft(P2(cc, dd, aa) + bb + data5, 9);
            aa = Bits.RotateLeft(P2(bb, cc, dd) + aa + data14, 7);
            dd = Bits.RotateLeft(P2(aa, bb, cc) + dd + data2, 11);
            cc = Bits.RotateLeft(P2(dd, aa, bb) + cc + data11, 13);
            bb = Bits.RotateLeft(P2(cc, dd, aa) + bb + data8, 12);

            aa = Bits.RotateLeft(P3(bb, cc, dd) + aa + data3 + C3, 11);
            dd = Bits.RotateLeft(P3(aa, bb, cc) + dd + data10 + C3, 13);
            cc = Bits.RotateLeft(P3(dd, aa, bb) + cc + data2 + C3, 14);
            bb = Bits.RotateLeft(P3(cc, dd, aa) + bb + data4 + C3, 7);
            aa = Bits.RotateLeft(P3(bb, cc, dd) + aa + data9 + C3, 14);
            dd = Bits.RotateLeft(P3(aa, bb, cc) + dd + data15 + C3, 9);
            cc = Bits.RotateLeft(P3(dd, aa, bb) + cc + data8 + C3, 13);
            bb = Bits.RotateLeft(P3(cc, dd, aa) + bb + data1 + C3, 15);
            aa = Bits.RotateLeft(P3(bb, cc, dd) + aa + data14 + C3, 6);
            dd = Bits.RotateLeft(P3(aa, bb, cc) + dd + data7 + C3, 8);
            cc = Bits.RotateLeft(P3(dd, aa, bb) + cc + data0 + C3, 13);
            bb = Bits.RotateLeft(P3(cc, dd, aa) + bb + data6 + C3, 6);
            aa = Bits.RotateLeft(P3(bb, cc, dd) + aa + data11 + C3, 12);
            dd = Bits.RotateLeft(P3(aa, bb, cc) + dd + data13 + C3, 5);
            cc = Bits.RotateLeft(P3(dd, aa, bb) + cc + data5 + C3, 7);
            bb = Bits.RotateLeft(P3(cc, dd, aa) + bb + data12 + C3, 5);

            cc += m_state[0] + b;
            m_state[0] = m_state[1] + c + dd;
            m_state[1] = m_state[2] + d + aa;
            m_state[2] = m_state[3] + a + bb;
            m_state[3] = cc;
        }

        private static uint P1(uint a, uint b, uint c)
        {
            return (a & b) | (~a & c);
        }

        private static uint P2(uint a, uint b, uint c)
        {
            return (a & b) | (a & c) | (b & c);
        }

        private static uint P3(uint a, uint b, uint c)
        {
            return a ^ b ^ c;
        }
    }
}