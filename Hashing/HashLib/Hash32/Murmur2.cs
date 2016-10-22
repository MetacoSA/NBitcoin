using System;
using System.Diagnostics;
using System.Text;

namespace HashLib.Hash32
{
    internal class Murmur2 : MultipleTransformNonBlock, IHash32, IFastHash32, IHashWithKey
    {
        private const uint KEY = 0xC58F1A7B;
        private const uint M = 0x5BD1E995;
        private const int R = 24;

        private uint m_key = KEY;
        private uint m_working_key;
        private uint m_h;

        public Murmur2()
            : base(4, 4)
        {
        }

        public override void Initialize()
        {
            m_working_key = m_key;

            base.Initialize();
        }

        private int InternalComputeBytes(byte[] a_data)
        {
            int length = a_data.Length;

            if (length == 0)
                return 0;

            m_h = m_working_key ^ (uint)length;
            int current_index = 0;

            while (length >= 4)
            {
                uint k = (uint)a_data[current_index++] |
                         ((uint)a_data[current_index++] << 8) |
                         ((uint)a_data[current_index++] << 16) |
                         ((uint)a_data[current_index++] << 24);

                TransformUIntFast(k);

                length -= 4;
            }

            switch (length)
            {
                case 3:
                    m_h ^= (uint)(a_data[current_index++] | a_data[current_index++] << 8);
                    m_h ^= (uint)(a_data[current_index] << 16);
                    m_h *= M;
                    break;
                case 2:
                    m_h ^= (uint)(a_data[current_index++] | a_data[current_index] << 8);
                    m_h *= M;
                    break;
                case 1:
                    m_h ^= a_data[current_index];
                    m_h *= M;
                    break;
                default:
                    break;
            }

            m_h ^= m_h >> 13;
            m_h *= M;
            m_h ^= m_h >> 15;

            return unchecked((int)m_h);
        }

        protected override HashResult ComputeAggregatedBytes(byte[] a_data)
        {
            return new HashResult(InternalComputeBytes(a_data));
        }

        public int ComputeBytesFast(byte[] a_data)
        {
            Initialize();

            return InternalComputeBytes(a_data);
        }

        public int ComputeByteFast(byte a_data)
        {
            Initialize();

            m_h = m_working_key ^ 1;

            m_h ^= a_data;
            m_h *= M;

            m_h ^= m_h >> 13;
            m_h *= M;
            m_h ^= m_h >> 15;

            return unchecked((int)m_h);
        }

        public int ComputeCharFast(char a_data)
        {
            return ComputeUShortFast((ushort)a_data);
        }

        public int ComputeShortFast(short a_data)
        {
            return ComputeUShortFast(unchecked((ushort)a_data));
        }

        public int ComputeUShortFast(ushort a_data)
        {
            Initialize();

            m_h = m_working_key ^ 2;

            m_h ^= a_data;
            m_h *= M;

            m_h ^= m_h >> 13;
            m_h *= M;
            m_h ^= m_h >> 15;

            return unchecked((int)m_h);
        }

        public int ComputeIntFast(int a_data)
        {
            return ComputeUIntFast(unchecked((uint)a_data));
        }

        public int ComputeUIntFast(uint a_data)
        {
            Initialize();

            m_h = m_working_key ^ 4;

            uint k = a_data;
            k *= M;
            k ^= k >> R;
            k *= M;

            m_h *= M;
            m_h ^= k;

            m_h ^= m_h >> 13;
            m_h *= M;
            m_h ^= m_h >> 15;

            return unchecked((int)m_h);
        }

        public int ComputeLongFast(long a_data)
        {
            return ComputeULongFast(unchecked((ulong)a_data));
        }

        public int ComputeULongFast(ulong a_data)
        {
            Initialize();

            m_h = m_working_key ^ 8;

            uint k = (uint)a_data;
            k *= M;
            k ^= k >> R;
            k *= M;

            m_h *= M;
            m_h ^= k;

            k = (uint)(a_data >> 32);
            k *= M;
            k ^= k >> R;
            k *= M;

            m_h *= M;
            m_h ^= k;

            m_h ^= m_h >> 13;
            m_h *= M;
            m_h ^= m_h >> 15;

            return unchecked((int)m_h);
        }

        public int ComputeStringFast(string a_data)
        {
            Initialize();

            int length = a_data.Length * 2;

            if (length == 0)
                return 0;

            m_h = m_working_key ^ (uint)length;
            int current_index = 0;

            while (length >= 4)
            {
                uint k = (uint)a_data[current_index++] |
                        ((uint)a_data[current_index++] << 16);

                TransformUIntFast(k);

                length -= 4;
            }

            if (length == 2)
            {
                m_h ^= (uint)a_data[current_index];
                m_h *= M;
            }

            m_h ^= m_h >> 13;
            m_h *= M;
            m_h ^= m_h >> 15;

            return unchecked((int)m_h);
        }

        public int ComputeCharsFast(char[] a_data)
        {
            Initialize();

            int length = a_data.Length * sizeof(char);

            if (length == 0)
                return 0;

            m_h = m_working_key ^ (uint)length;
            int current_index = 0;

            while (length >= 4)
            {
                uint k = (uint)a_data[current_index++] | 
                        ((uint)a_data[current_index++] << 16);

                TransformUIntFast(k);

                length -= 4;
            }

            if (length == 2)
            {
                m_h ^= (uint)a_data[current_index];
                m_h *= M;
            }

            m_h ^= m_h >> 13;
            m_h *= M;
            m_h ^= m_h >> 15;

            return unchecked((int)m_h);
        }

        public int ComputeShortsFast(short[] a_data)
        {
            Initialize();

            int length = a_data.Length * 2;

            if (length == 0)
                return 0;

            m_h = m_working_key ^ (uint)length;
            int current_index = 0;

            while (length >= 4)
            {
                uint k = (uint)(unchecked((ushort)a_data[current_index++])) |
                        ((uint)(unchecked((ushort)a_data[current_index++])) << 16);

                TransformUIntFast(k);

                length -= 4;
            }

            if (length == 2)
            {
                m_h ^= unchecked((ushort)a_data[current_index++]);
                m_h *= M;
            }

            m_h ^= m_h >> 13;
            m_h *= M;
            m_h ^= m_h >> 15;

            return unchecked((int)m_h);
        }

        public int ComputeUShortsFast(ushort[] a_data)
        {
            Initialize();

            int length = a_data.Length * 2;

            if (length == 0)
                return 0;

            m_h = m_working_key ^ (uint)length;
            int current_index = 0;

            while (length >= 4)
            {
                uint k =  (uint)a_data[current_index++] |
                         ((uint)a_data[current_index++] << 16);

                TransformUIntFast(k);

                length -= 4;
            }

            if (length == 2)
            {
                m_h ^= (uint)a_data[current_index++];
                m_h *= M;
            }

            m_h ^= m_h >> 13;
            m_h *= M;
            m_h ^= m_h >> 15;

            return unchecked((int)m_h);
        }

        public int ComputeIntsFast(int[] a_data)
        {
            Initialize();

            int length = a_data.Length * 4;

            if (length == 0)
                return 0;

            m_h = m_working_key ^ (uint)length;
            int current_index = 0;

            while (length >= 4)
            {
                uint k = unchecked((uint)a_data[current_index++]);

                TransformUIntFast(k);

                length -= 4;
            }

            m_h ^= m_h >> 13;
            m_h *= M;
            m_h ^= m_h >> 15;

            return unchecked((int)m_h);
        }

        public int ComputeUIntsFast(uint[] a_data)
        {
            Initialize();

            int length = a_data.Length * 4;

            if (length == 0)
                return 0;

            m_h = m_working_key ^ (uint)length;
            int current_index = 0;

            while (length >= 4)
            {
                uint k = a_data[current_index++];

                TransformUIntFast(k);

                length -= 4;
            }

            m_h ^= m_h >> 13;
            m_h *= M;
            m_h ^= m_h >> 15;

            return unchecked((int)m_h);
        }

        public int ComputeLongsFast(long[] a_data)
        {
            Initialize();

            int length = a_data.Length * 8;

            if (length == 0)
                return 0;

            m_h = m_working_key ^ (uint)length;
            int current_index = 0;

            while (length >= 8)
            {
                TransformULongFast(unchecked((ulong)a_data[current_index++]));

                length -= 8;
            }

            m_h ^= m_h >> 13;
            m_h *= M;
            m_h ^= m_h >> 15;

            return unchecked((int)m_h);
        }

        public int ComputeULongsFast(ulong[] a_data)
        {
            Initialize();

            int length = a_data.Length * 8;

            if (length == 0)
                return 0;

            m_h = m_working_key ^ (uint)length;
            int current_index = 0;

            while (length >= 8)
            {
                TransformULongFast(unchecked((ulong)a_data[current_index++]));

                length -= 8;
            }

            m_h ^= m_h >> 13;
            m_h *= M;
            m_h ^= m_h >> 15;

            return unchecked((int)m_h);
        }

        public int ComputeFloatFast(float a_data)
        {
            return ComputeUIntFast(BitConverter.ToUInt32(BitConverter.GetBytes(a_data), 0));
        }

        public int ComputeFloatsFast(float[] a_data)
        {
            Initialize();

            int length = a_data.Length * 4;

            if (length == 0)
                return 0;

            m_h = m_working_key ^ (uint)length;

            int current_index = a_data.Length / (sizeof(ulong) / sizeof(float)) * (sizeof(ulong) / sizeof(float));
            length = a_data.Length * sizeof(float) - current_index * sizeof(float);

            TransformULongsFast(Converters.ConvertFloatsToULongs(a_data, 0, current_index));

            while (length >= 4)
            {
                uint k = BitConverter.ToUInt32(BitConverter.GetBytes(a_data[current_index++]), 0);

                TransformUIntFast(k);

                length -= 4;
            }

            m_h ^= m_h >> 13;
            m_h *= M;
            m_h ^= m_h >> 15;

            return unchecked((int)m_h);
        }

        public int ComputeDoubleFast(double a_data)
        {
            return ComputeLongFast(BitConverter.DoubleToInt64Bits(a_data));
        }

        public int ComputeDoublesFast(double[] a_data)
        {
            Initialize();

            int length = a_data.Length * 8;

            if (length == 0)
                return 0;

            m_h = m_working_key ^ (uint)length;
            int current_index = 0;

            while (length >= 8)
            {
                long k = BitConverter.DoubleToInt64Bits(a_data[current_index++]);

                TransformULongFast(unchecked((ulong)k));

                length -= 8;
            }

            m_h ^= m_h >> 13;
            m_h *= M;
            m_h ^= m_h >> 15;

            return unchecked((int)m_h);
        }

        private void TransformUIntFast(uint a_data)
        {
            a_data *= M;
            a_data ^= a_data >> R;
            a_data *= M;

            m_h *= M;
            m_h ^= a_data;
        }

        private void TransformULongFast(ulong a_data)
        {
            uint k = (uint)a_data;
            k *= M;
            k ^= k >> R;
            k *= M;

            m_h *= M;
            m_h ^= k;

            k = (uint)(a_data >> 32);
            k *= M;
            k ^= k >> R;
            k *= M;

            m_h *= M;
            m_h ^= k;
        }

        public void TransformULongsFast(ulong[] a_data)
        {
            int length = a_data.Length;
            int current_index = 0;

            while (length > 0)
            {
                TransformULongFast(a_data[current_index++]);
                length--;
            }
        }

        public byte[] Key
        {
            get
            {
                return Converters.ConvertUIntToBytes(m_key);
            }
            set
            {
                if (value == null)
                {
                    m_key = KEY;
                }
                else
                {
                    Debug.Assert(value.Length == KeyLength);

                    m_key = Converters.ConvertBytesToUInt(value);
                }
            }
        }

        public int? KeyLength
        {
            get
            {
                return 4;
            }
        }
    }
}
