using System;

namespace HashLib.Hash32
{
    internal class SuperFast : MultipleTransformNonBlock, IHash32
    {
        public SuperFast()
            : base(4, 4)
        {
        }

        protected override HashResult ComputeAggregatedBytes(byte[] a_data)
        {
            int length = a_data.Length;

            if (length == 0)
                return new HashResult(0);

            uint hash = (UInt32)length;

            int currentIndex = 0;

            while (length >= 4)
            {
                hash += (ushort)(a_data[currentIndex++] | a_data[currentIndex++] << 8);
                uint tmp = (uint)((uint)(a_data[currentIndex++] | a_data[currentIndex++] << 8) << 11) ^ hash;
                hash = (hash << 16) ^ tmp;
                hash += hash >> 11;

                length -= 4;
            }

            switch (length)
            {
                case 3:
                    hash += (ushort)(a_data[currentIndex++] | a_data[currentIndex++] << 8);
                    hash ^= hash << 16;
                    hash ^= ((uint)a_data[currentIndex]) << 18;
                    hash += hash >> 11;
                    break;
                case 2:
                    hash += (ushort)(a_data[currentIndex++] | a_data[currentIndex] << 8);
                    hash ^= hash << 11;
                    hash += hash >> 17;
                    break;
                case 1:
                    hash += a_data[currentIndex];
                    hash ^= hash << 10;
                    hash += hash >> 1;
                    break;
                default:
                    break;
            }

            hash ^= hash << 3;
            hash += hash >> 5;
            hash ^= hash << 4;
            hash += hash >> 17;
            hash ^= hash << 25;
            hash += hash >> 6;

            return new HashResult(hash);
        }
    }
}
