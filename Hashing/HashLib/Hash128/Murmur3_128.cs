using System;
using System.Diagnostics;
using System.Text;

namespace HashLib.Hash128
{
    internal class Murmur3_128 : BlockHash, IHash128, IHashWithKey
    {
        private const uint KEY = 0xC58F1A7B;
        private const ulong C1 = 0x87C37B91114253D5;
        private const ulong C2 = 0x4CF5AD432745937F;
        private const uint C3 = 0x52DCE729;
        private const uint C4 = 0x38495AB5;
        private const ulong C5 = 0xFF51AFD7ED558CCD;
        private const ulong C6 = 0xC4CEB9FE1A85EC53;

        private ulong m_h1;
        private ulong m_h2;
        private uint m_key = KEY;

        public Murmur3_128()
            : base(16, 16)
        {
        }

        public override void Initialize()
        {
            m_h1 = m_key;
            m_h2 = m_key;

            base.Initialize();
        }

        protected override void Finish()
        {
            int length = m_buffer.Pos;
            byte[] data = m_buffer.GetBytesZeroPadded();

            switch (length)
            {
                case 15:
                    {
                        ulong k2 = (ulong)(data[14]) << 48;
                        k2 ^= (ulong)(data[13]) << 40;
                        k2 ^= (ulong)(data[12]) << 32;
                        k2 ^= (ulong)(data[11]) << 24;
                        k2 ^= (ulong)(data[10]) << 16;
                        k2 ^= (ulong)(data[9]) << 8;
                        k2 ^= (ulong)(data[8]) << 0;
                        k2 *= C2;
                        k2 = (k2 << 33) | (k2 >> 31);
                        k2 *= C1;
                        m_h2 ^= k2;
                        break;
                    }
                case 14:
                    {
                        ulong k2 = (ulong)(data[13]) << 40;
                        k2 ^= (ulong)(data[12]) << 32;
                        k2 ^= (ulong)(data[11]) << 24;
                        k2 ^= (ulong)(data[10]) << 16;
                        k2 ^= (ulong)(data[9]) << 8;
                        k2 ^= (ulong)(data[8]) << 0;
                        k2 *= C2;
                        k2 = (k2 << 33) | (k2 >> 31);
                        k2 *= C1;
                        m_h2 ^= k2;
                        break;
                    }
                case 13:
                    {
                        ulong k2 = (ulong)(data[12]) << 32;
                        k2 ^= (ulong)(data[11]) << 24;
                        k2 ^= (ulong)(data[10]) << 16;
                        k2 ^= (ulong)(data[9]) << 8;
                        k2 ^= (ulong)(data[8]) << 0;
                        k2 *= C2;
                        k2 = (k2 << 33) | (k2 >> 31);
                        k2 *= C1;
                        m_h2 ^= k2;
                        break;
                    }
                case 12:
                    {
                        ulong k2 = (ulong)(data[11]) << 24;
                        k2 ^= (ulong)(data[10]) << 16;
                        k2 ^= (ulong)(data[9]) << 8;
                        k2 ^= (ulong)(data[8]) << 0;
                        k2 *= C2;
                        k2 = (k2 << 33) | (k2 >> 31);
                        k2 *= C1;
                        m_h2 ^= k2;
                        break;
                    }
                case 11:
                    {
                        ulong k2 = (ulong)(data[10]) << 16;
                        k2 ^= (ulong)(data[9]) << 8;
                        k2 ^= (ulong)(data[8]) << 0;
                        k2 *= C2;
                        k2 = (k2 << 33) | (k2 >> 31);
                        k2 *= C1;
                        m_h2 ^= k2;
                        break;
                    }
                case 10:
                    {
                        ulong k2 = (ulong)(data[9]) << 8;
                        k2 ^= (ulong)(data[8]) << 0;
                        k2 *= C2;
                        k2 = (k2 << 33) | (k2 >> 31);
                        k2 *= C1;
                        m_h2 ^= k2;
                        break;
                    }
                case 9:
                    {
                        ulong k2 = (ulong)(data[8]) << 0;
                        k2 *= C2;
                        k2 = (k2 << 33) | (k2 >> 31);
                        k2 *= C1;
                        m_h2 ^= k2;
                        break;
                    }
            }

            if (length > 8)
                length = 8;

            switch (length)
            {
                case 8:
                    {
                        ulong k1 = (ulong)(data[7]) << 56;
                        k1 ^= (ulong)(data[6]) << 48;
                        k1 ^= (ulong)(data[5]) << 40;
                        k1 ^= (ulong)(data[4]) << 32;
                        k1 ^= (ulong)(data[3]) << 24;
                        k1 ^= (ulong)(data[2]) << 16;
                        k1 ^= (ulong)(data[1]) << 8;
                        k1 ^= (ulong)(data[0]) << 0;
                        k1 *= C1;
                        k1 = (k1 << 31) | (k1 >> 33);
                        k1 *= C2;
                        m_h1 ^= k1;
                        break;
                    }
                case 7:
                    {
                        ulong k1 = (ulong)(data[6]) << 48;
                        k1 ^= (ulong)(data[5]) << 40;
                        k1 ^= (ulong)(data[4]) << 32;
                        k1 ^= (ulong)(data[3]) << 24;
                        k1 ^= (ulong)(data[2]) << 16;
                        k1 ^= (ulong)(data[1]) << 8;
                        k1 ^= (ulong)(data[0]) << 0;
                        k1 *= C1;
                        k1 = (k1 << 31) | (k1 >> 33);
                        k1 *= C2;
                        m_h1 ^= k1;
                        break;
                    }
                case 6:
                    {
                        ulong k1 = (ulong)(data[5]) << 40;
                        k1 ^= (ulong)(data[4]) << 32;
                        k1 ^= (ulong)(data[3]) << 24;
                        k1 ^= (ulong)(data[2]) << 16;
                        k1 ^= (ulong)(data[1]) << 8;
                        k1 ^= (ulong)(data[0]) << 0;
                        k1 *= C1;
                        k1 = (k1 << 31) | (k1 >> 33);
                        k1 *= C2;
                        m_h1 ^= k1;
                        break;
                    }
                case 5:
                    {
                        ulong k1 = (ulong)(data[4]) << 32;
                        k1 ^= (ulong)(data[3]) << 24;
                        k1 ^= (ulong)(data[2]) << 16;
                        k1 ^= (ulong)(data[1]) << 8;
                        k1 ^= (ulong)(data[0]) << 0;
                        k1 *= C1;
                        k1 = (k1 << 31) | (k1 >> 33);
                        k1 *= C2;
                        m_h1 ^= k1;
                        break;
                    }
                case 4:
                    {
                        ulong k1 = (ulong)(data[3]) << 24;
                        k1 ^= (ulong)(data[2]) << 16;
                        k1 ^= (ulong)(data[1]) << 8;
                        k1 ^= (ulong)(data[0]) << 0;
                        k1 *= C1;
                        k1 = (k1 << 31) | (k1 >> 33);
                        k1 *= C2;
                        m_h1 ^= k1;
                        break;
                    }
                case 3:
                    {
                        ulong k1 = (ulong)(data[2]) << 16;
                        k1 ^= (ulong)(data[1]) << 8;
                        k1 ^= (ulong)(data[0]) << 0;
                        k1 *= C1;
                        k1 = (k1 << 31) | (k1 >> 33);
                        k1 *= C2;
                        m_h1 ^= k1;
                        break;
                    }
                case 2:
                    {
                        ulong k1 = (ulong)(data[1]) << 8;
                        k1 ^= (ulong)(data[0]) << 0;
                        k1 *= C1;
                        k1 = (k1 << 31) | (k1 >> 33);
                        k1 *= C2;
                        m_h1 ^= k1;
                        break;
                    }
                case 1:
                    {
                        ulong k1 = (ulong)(data[0]) << 0;
                        k1 *= C1;
                        k1 = (k1 << 31) | (k1 >> 33);
                        k1 *= C2;
                        m_h1 ^= k1;
                        break;
                    }
            };

            m_h1 ^= m_processed_bytes;
            m_h2 ^= m_processed_bytes;

            m_h1 += m_h2;
            m_h2 += m_h1;

            m_h1 ^= m_h1 >> 33;
            m_h1 *= C5;
            m_h1 ^= m_h1 >> 33;
            m_h1 *= C6;
            m_h1 ^= m_h1 >> 33;

            m_h2 ^= m_h2 >> 33;
            m_h2 *= C5;
            m_h2 ^= m_h2 >> 33;
            m_h2 *= C6;
            m_h2 ^= m_h2 >> 33;

            m_h1 += m_h2;
            m_h2 += m_h1;
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            ulong k1 = (ulong)a_data[a_index++] | (ulong)a_data[a_index++] << 8 |
                       (ulong)a_data[a_index++] << 16 | (ulong)a_data[a_index++] << 24 |
                       (ulong)a_data[a_index++] << 32 | (ulong)a_data[a_index++] << 40 |
                       (ulong)a_data[a_index++] << 48 | (ulong)a_data[a_index++] << 56;

            k1 *= C1;
            k1 = (k1 << 31) | (k1 >> 33);
            k1 *= C2;
            m_h1 ^= k1;

            m_h1 = (m_h1 << 27) | (m_h1 >> 37);
            m_h1 += m_h2;
            m_h1 = m_h1 * 5 + C3;

            ulong k2 = (ulong)a_data[a_index++] | (ulong)a_data[a_index++] << 8 |
                       (ulong)a_data[a_index++] << 16 | (ulong)a_data[a_index++] << 24 |
                       (ulong)a_data[a_index++] << 32 | (ulong)a_data[a_index++] << 40 |
                       (ulong)a_data[a_index++] << 48 | (ulong)a_data[a_index++] << 56;

            k2 *= C2;
            k2 = (k2 << 33) | (k2 >> 31);
            k2 *= C1;
            m_h2 ^= k2;

            m_h2 = (m_h2 << 31) | (m_h2 >> 33);
            m_h2 += m_h1;
            m_h2 = m_h2 * 5 + C4;
        }

        protected override byte[] GetResult()
        {
            byte[] result = new byte[16];

            Converters.ConvertULongToBytes(m_h1, result, 0);
            Converters.ConvertULongToBytes(m_h2, result, 8);

            return result;
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
