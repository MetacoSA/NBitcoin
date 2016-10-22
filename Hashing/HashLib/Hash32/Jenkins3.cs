using System;

namespace HashLib.Hash32
{
    internal class Jenkins3 : MultipleTransformNonBlock, IHash32
    {
        public Jenkins3()
            : base(4, 12)
        {
        }

        protected override HashResult ComputeAggregatedBytes(byte[] a_data)
        {
            int length = a_data.Length;

            if (length == 0)
                return new HashResult((uint)0);

            uint a, b, c; 
            a = b = c = 0xdeadbeef + (uint)length;

            int currentIndex = 0;

            while (length > 12)
            {
                a += (uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8 | a_data[currentIndex++] << 16 | a_data[currentIndex++] << 24);
                b += (uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8 | a_data[currentIndex++] << 16 | a_data[currentIndex++] << 24);
                c += (uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8 | a_data[currentIndex++] << 16 | a_data[currentIndex++] << 24);

                a -= c;
                a ^= (c << 4) | (c >> (32 - 4));
                c += b;
                b -= a;
                b ^= (a << 6) | (a >> (32 - 6));
                a += c;
                c -= b;
                c ^= (b << 8) | (b >> (32 - 8));
                b += a;
                a -= c;
                a ^= (c << 16) | (c >> (32 - 16));
                c += b;
                b -= a;
                b ^= (a << 19) | (a >> (32 - 19));
                a += c;
                c -= b;
                c ^= (b << 4) | (b >> (32 - 4));
                b += a;

                length -= 12;
            }

            switch (length)
            {
                case 12:
                    a += (uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8 | a_data[currentIndex++] << 16 | a_data[currentIndex++] << 24);
                    b += (uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8 | a_data[currentIndex++] << 16 | a_data[currentIndex++] << 24);
                    c += (uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8 | a_data[currentIndex++] << 16 | a_data[currentIndex] << 24);
                    break;
                case 11:
                    a += (uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8 | a_data[currentIndex++] << 16 | a_data[currentIndex++] << 24);
                    b += (uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8 | a_data[currentIndex++] << 16 | a_data[currentIndex++] << 24);
                    c += (uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8 | a_data[currentIndex] << 16);
                    break;
                case 10:
                    a += (uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8 | a_data[currentIndex++] << 16 | a_data[currentIndex++] << 24);
                    b += (uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8 | a_data[currentIndex++] << 16 | a_data[currentIndex++] << 24);
                    c += (uint)(a_data[currentIndex++] | a_data[currentIndex] << 8);
                    break;
                case 9:
                    a += (uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8 | a_data[currentIndex++] << 16 | a_data[currentIndex++] << 24);
                    b += (uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8 | a_data[currentIndex++] << 16 | a_data[currentIndex++] << 24);
                    c += (uint)a_data[currentIndex];
                    break;
                case 8:
                    a += (uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8 | a_data[currentIndex++] << 16 | a_data[currentIndex++] << 24);
                    b += (uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8 | a_data[currentIndex++] << 16 | a_data[currentIndex] << 24);
                    break;
                case 7:
                    a += (uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8 | a_data[currentIndex++] << 16 | a_data[currentIndex++] << 24);
                    b += (uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8 | a_data[currentIndex] << 16);
                    break;
                case 6:
                    a += (uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8 | a_data[currentIndex++] << 16 | a_data[currentIndex++] << 24);
                    b += (uint)(a_data[currentIndex++] | a_data[currentIndex] << 8);
                    break;
                case 5:
                    a += (uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8 | a_data[currentIndex++] << 16 | a_data[currentIndex++] << 24);
                    b += (uint)a_data[currentIndex];
                    break;
                case 4:
                    a += (uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8 | a_data[currentIndex++] << 16 | a_data[currentIndex] << 24);
                    break;
                case 3:
                    a += (uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8 | a_data[currentIndex] << 16);
                    break;
                case 2:
                    a += (uint)(a_data[currentIndex++] | a_data[currentIndex] << 8);
                    break;
                case 1:
                    a += (uint)a_data[currentIndex];
                    break;
            }

            c ^= b;
            c = c - ((b << 14) | (b >> (32 - 14)));
            a ^= c;
            a = a - ((c << 11) | (c >> (32 - 11)));
            b ^= a;
            b = b - ((a << 25) | (a >> (32 - 25)));
            c ^= b;
            c = c - ((b << 16) | (b >> (32 - 16)));
            a ^= c;
            a = a - ((c << 4) | (c >> (32 - 4)));
            b ^= a;
            b = b - ((a << 14) | (a >> (32 - 14)));
            c ^= b;
            c = c - ((b << 24) | (b >> (32 - 24)));

            return new HashResult(c);
        }
    }
}
