using System;
using System.Diagnostics;
using System.Text;

namespace HashLib.Hash32
{
    internal class Murmur3 : BlockHash, IHash32, IFastHash32, IHashWithKey
    {
        private const uint KEY = 0xC58F1A7B;
        private const uint M = 0x5BD1E995;
        private const int R = 24;

        private const uint C1 = 0xCC9E2D51;
        private const uint C2 = 0x1B873593;
        private const uint C3 = 0xE6546B64;
        private const uint C4 = 0x85EBCA6B;
        private const uint C5 = 0xC2B2AE35;

        private uint m_h;
        private uint m_key = KEY;

        public Murmur3()
            : base(4, 4)
        {
        }

        public override void Initialize()
        {
            m_h = m_key;

            base.Initialize();
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            uint k = (uint)a_data[a_index++] |
                      ((uint)a_data[a_index++] << 8) |
                      ((uint)a_data[a_index++] << 16) |
                      ((uint)a_data[a_index++] << 24);

            TransformUIntFast(k);
        }

        protected override void Finish()
        {
            ulong left = m_processed_bytes % (ulong)BlockSize;

            if (left != 0)
            {
                byte[] buffer = m_buffer.GetBytesZeroPadded();

                switch (left)
                {
                    case 3:
                    {
                        uint k = (uint)buffer[2] << 16 | ((uint)buffer[1] << 8) | (uint)buffer[0];
                        k *= C1;
                        k = (k << 15) | (k >> 17);
                        k *= C2;
                        m_h ^= k;
                        break;
                    }
                    case 2:
                    {
                        uint k = ((uint)buffer[1] << 8) | (uint)buffer[0];
                        k *= C1;
                        k = (k << 15) | (k >> 17);
                        k *= C2;
                        m_h ^= k;
                        break;
                    }
                    case 1:
                    {
                        uint k = buffer[0];
                        k *= C1;
                        k = (k << 15) | (k >> 17);
                        k *= C2;
                        m_h ^= k;
                        break;
                    }
                };
            }

            m_h ^= (uint)m_processed_bytes;

            m_h ^= m_h >> 16;
            m_h *= C4;
            m_h ^= m_h >> 13;
            m_h *= C5;
            m_h ^= m_h >> 16;
        }

        protected override byte[] GetResult()
        {
            byte[] result = new byte[4];

            result[0] = (byte)m_h;
            result[1] = (byte)(m_h >> 8);
            result[2] = (byte)(m_h >> 16);
            result[3] = (byte)(m_h >> 24);

            return result;
        }

        public int ComputeByteFast(byte a_data)
        {
            m_h = m_key;

            uint k = a_data;
            k *= C1;
            k = (k << 15) | (k >> 17);
            k *= C2;
            m_h ^= k;

            m_h ^= 1;

            m_h ^= m_h >> 16;
            m_h *= C4;
            m_h ^= m_h >> 13;
            m_h *= C5;
            m_h ^= m_h >> 16;

            var result = unchecked((int)m_h);
            Initialize();
            return result;
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
            m_h = m_key;

            uint k = a_data;
            k *= C1;
            k = (k << 15) | (k >> 17);
            k *= C2;
            m_h ^= k;

            m_h ^= 2;

            m_h ^= m_h >> 16;
            m_h *= C4;
            m_h ^= m_h >> 13;
            m_h *= C5;
            m_h ^= m_h >> 16;

            var result = unchecked((int)m_h);
            Initialize();
            return result;
        }

        public int ComputeIntFast(int a_data)
        {
            return ComputeUIntFast(unchecked((uint)a_data));
        }

        public int ComputeUIntFast(uint a_data)
        {
            m_h = m_key;

            TransformUIntFast(a_data);

            m_h ^= 4;

            m_h ^= m_h >> 16;
            m_h *= C4;
            m_h ^= m_h >> 13;
            m_h *= C5;
            m_h ^= m_h >> 16;

            var result = unchecked((int)m_h);
            Initialize();
            return result;
        }

        public int ComputeLongFast(long a_data)
        {
            return ComputeULongFast(unchecked((ulong)a_data));
        }

        public int ComputeULongFast(ulong a_data)
        {
            m_h = m_key;

            TransformULongFast(a_data);

            m_h ^= 8;

            m_h ^= m_h >> 16;
            m_h *= C4;
            m_h ^= m_h >> 13;
            m_h *= C5;
            m_h ^= m_h >> 16;

            var result = unchecked((int)m_h);
            Initialize();
            return result;
        }

        public int ComputeFloatFast(float a_data)
        {
            return ComputeUIntFast(BitConverter.ToUInt32(BitConverter.GetBytes(a_data), 0));
        }

        public int ComputeDoubleFast(double a_data)
        {
            return ComputeLongFast(BitConverter.DoubleToInt64Bits(a_data));
        }

        public int ComputeBytesFast(byte[] a_data)
        {
            m_h = m_key;

            int current_index = 0;
            int length = a_data.Length;

            while (length >= 4)
            {
                uint k = (uint)a_data[current_index] |
                          ((uint)a_data[current_index + 1] << 8) |
                          ((uint)a_data[current_index + 2] << 16) |
                          ((uint)a_data[current_index + 3] << 24);

                k *= C1;
                k = (k << 15) | (k >> 17);
                k *= C2;

                m_h ^= k;
                m_h = (m_h << 13) | (m_h >> 19);
                m_h = m_h * 5 + C3;

                current_index += 4;
                length -= 4;
            }

            switch (length)
            {
                case 3:
                {
                    uint k = (uint)a_data[current_index + 2] << 16 | ((uint)a_data[current_index + 1] << 8) | (uint)a_data[current_index];
                    k *= C1;
                    k = (k << 15) | (k >> 17);
                    k *= C2;
                    m_h ^= k;
                    break;
                }
                case 2:
                {
                    uint k = ((uint)a_data[current_index + 1] << 8) | (uint)a_data[current_index];
                    k *= C1;
                    k = (k << 15) | (k >> 17);
                    k *= C2;
                    m_h ^= k;
                    break;
                }
                case 1:
                {
                    uint k = a_data[current_index];
                    k *= C1;
                    k = (k << 15) | (k >> 17);
                    k *= C2;
                    m_h ^= k;
                    break;
                }
            };

            m_h ^= (uint)a_data.Length;

            m_h ^= m_h >> 16;
            m_h *= C4;
            m_h ^= m_h >> 13;
            m_h *= C5;
            m_h ^= m_h >> 16;

            var result = unchecked((int)m_h);
            Initialize();
            return result;
        }

        public int ComputeUIntsFast(uint[] a_data)
        {
            m_h = m_key;

            for (int i=0; i<a_data.Length; i++)
                TransformUIntFast(a_data[i]);

            m_h ^= (uint)a_data.Length * 4;

            m_h ^= m_h >> 16;
            m_h *= C4;
            m_h ^= m_h >> 13;
            m_h *= C5;
            m_h ^= m_h >> 16;

            var result = unchecked((int)m_h);
            Initialize();
            return result;
        }

        public int ComputeIntsFast(int[] a_data)
        {
            m_h = m_key;

            for (int i = 0; i < a_data.Length; i++)
                TransformUIntFast(unchecked((uint)a_data[i]));

            m_h ^= (uint)a_data.Length * 4;

            m_h ^= m_h >> 16;
            m_h *= C4;
            m_h ^= m_h >> 13;
            m_h *= C5;
            m_h ^= m_h >> 16;

            var result = unchecked((int)m_h);
            Initialize();
            return result;
        }

        public int ComputeLongsFast(long[] a_data)
        {
            m_h = m_key;

            for (int i = 0; i < a_data.Length; i++)
                TransformULongFast(unchecked((ulong)a_data[i]));

            m_h ^= (uint)a_data.Length * 8;

            m_h ^= m_h >> 16;
            m_h *= C4;
            m_h ^= m_h >> 13;
            m_h *= C5;
            m_h ^= m_h >> 16;

            var result = unchecked((int)m_h);
            Initialize();
            return result;
        }

        public int ComputeULongsFast(ulong[] a_data)
        {
            m_h = m_key;

            TransformULongsFast(a_data);

            m_h ^= (uint)a_data.Length * 8;

            m_h ^= m_h >> 16;
            m_h *= C4;
            m_h ^= m_h >> 13;
            m_h *= C5;
            m_h ^= m_h >> 16;

            var result = unchecked((int)m_h);
            Initialize();
            return result;
        }

        public int ComputeDoublesFast(double[] a_data)
        {
            m_h = m_key;

            for (int i = 0; i < a_data.Length; i++)
            {
                long k = BitConverter.DoubleToInt64Bits(a_data[i]);
                TransformULongFast(unchecked((ulong)k));
            }

            m_h ^= (uint)a_data.Length * 8;

            m_h ^= m_h >> 16;
            m_h *= C4;
            m_h ^= m_h >> 13;
            m_h *= C5;
            m_h ^= m_h >> 16;

            var result = unchecked((int)m_h);
            Initialize();
            return result;
        }

        public int ComputeFloatsFast(float[] a_data)
        {
            m_h = m_key;

            int current_index = a_data.Length / (sizeof(ulong) / sizeof(float)) * (sizeof(ulong) / sizeof(float));
            int length = a_data.Length * sizeof(float) - current_index * sizeof(float);

            TransformULongsFast(Converters.ConvertFloatsToULongs(a_data, 0, current_index));

            if (length == 4)
                TransformUIntFast(BitConverter.ToUInt32(BitConverter.GetBytes(a_data[current_index]), 0));

            m_h ^= (uint)a_data.Length * sizeof(float);

            m_h ^= m_h >> 16;
            m_h *= C4;
            m_h ^= m_h >> 13;
            m_h *= C5;
            m_h ^= m_h >> 16;

            var result = unchecked((int)m_h);
            Initialize();
            return result;
        }

        public int ComputeStringFast(string a_data)
        {
            m_h = m_key;
            int length = a_data.Length * sizeof(char);
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
                uint k = (uint)a_data[current_index++];
                k *= C1;
                k = (k << 15) | (k >> 17);
                k *= C2;
                m_h ^= k;
            };

            m_h ^= (uint)a_data.Length * sizeof(char);

            m_h ^= m_h >> 16;
            m_h *= C4;
            m_h ^= m_h >> 13;
            m_h *= C5;
            m_h ^= m_h >> 16;

            var result = unchecked((int)m_h);
            Initialize();
            return result;
        }

        public int ComputeCharsFast(char[] a_data)
        {
            m_h = m_key;
            int length = a_data.Length * sizeof(char);
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
                uint k = (uint)a_data[current_index++];
                k *= C1;
                k = (k << 15) | (k >> 17);
                k *= C2;
                m_h ^= k;
            };

            m_h ^= (uint)a_data.Length * sizeof(char);

            m_h ^= m_h >> 16;
            m_h *= C4;
            m_h ^= m_h >> 13;
            m_h *= C5;
            m_h ^= m_h >> 16;

            var result = unchecked((int)m_h);
            Initialize();
            return result;
        }

        public int ComputeShortsFast(short[] a_data)
        {
            m_h = m_key;

            int length = a_data.Length * sizeof(char);
            int current_index = 0;

            while (length >= 4)
            {
                uint k = (uint)(unchecked((ushort)a_data[current_index++])) |
                         (uint)(unchecked((ushort)a_data[current_index++] << 16));

                TransformUIntFast(k);

                length -= 4;
            }

            if (length == 2)
            {
                uint k = unchecked((ushort)a_data[current_index++]);
                k *= C1;
                k = (k << 15) | (k >> 17);
                k *= C2;
                m_h ^= k;
            };

            m_h ^= (uint)a_data.Length * sizeof(short);

            m_h ^= m_h >> 16;
            m_h *= C4;
            m_h ^= m_h >> 13;
            m_h *= C5;
            m_h ^= m_h >> 16;

            var result = unchecked((int)m_h);
            Initialize();
            return result;
        }

        public int ComputeUShortsFast(ushort[] a_data)
        {
            m_h = m_key;

            int length = a_data.Length * sizeof(char);
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
                uint k = (uint)a_data[current_index++];
                k *= C1;
                k = (k << 15) | (k >> 17);
                k *= C2;
                m_h ^= k;
            };

            m_h ^= (uint)a_data.Length * sizeof(ushort);

            m_h ^= m_h >> 16;
            m_h *= C4;
            m_h ^= m_h >> 13;
            m_h *= C5;
            m_h ^= m_h >> 16;

            var result = unchecked((int)m_h);
            Initialize();
            return result;
        }

        private void TransformUIntFast(uint a_data)
        {
            uint k = a_data;

            k *= C1;
            k = (k << 15) | (k >> 17);
            k *= C2;

            m_h ^= k;
            m_h = (m_h << 13) | (m_h >> 19);
            m_h = m_h * 5 + C3;
        }

        private void TransformULongFast(ulong a_data)
        {
            uint k = (uint)a_data;

            k *= C1;
            k = (k << 15) | (k >> 17);
            k *= C2;

            m_h ^= k;
            m_h = (m_h << 13) | (m_h >> 19);
            m_h = m_h * 5 + C3;

            k = (uint)(a_data >> 32);

            k *= C1;
            k = (k << 15) | (k >> 17);
            k *= C2;

            m_h ^= k;
            m_h = (m_h << 13) | (m_h >> 19);
            m_h = m_h * 5 + C3;
        }

        private void TransformULongsFast(ulong[] a_data)
        {
            for (int i = 0; i < a_data.Length; i++)
                TransformULongFast(a_data[i]);
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
