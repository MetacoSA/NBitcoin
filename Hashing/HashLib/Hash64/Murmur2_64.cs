using System;
using System.Diagnostics;

namespace HashLib.Hash64
{
    internal class Murmur2_64 : MultipleTransformNonBlock, IHash64, IHashWithKey
    {
        protected const uint KEY = 0xC58F1A7B;
        protected const ulong M = 0xC6A4A7935BD1E995;
        protected const int R = 47;

        private uint m_key = KEY;
        private uint m_working_key;

        public Murmur2_64()
            : base(8, 8)
        {
        }

        public override void Initialize()
        {
            m_working_key = m_key;

            base.Initialize();
        }

        protected override HashResult ComputeAggregatedBytes(byte[] a_data)
        {
            int length = a_data.Length;

            if (length == 0)
                return new HashResult((ulong)0);

            ulong h = m_working_key ^ (ulong)length;
            int current_index = 0;


            while (length >= 8)
            {
                ulong k = (ulong)a_data[current_index++] | (ulong)a_data[current_index++] << 8 | 
                          (ulong)a_data[current_index++] << 16 | (ulong)a_data[current_index++] << 24 | 
                          (ulong)a_data[current_index++] << 32 | (ulong)a_data[current_index++] << 40 |
                          (ulong)a_data[current_index++] << 48 | (ulong)a_data[current_index++] << 56;

                k *= M;
                k ^= k >> R;
                k *= M;

                h ^= k;
                h *= M;

                length -= 8;
            }

            switch (length)
            {
                case 7:
                    h ^= (ulong)a_data[current_index++] << 48 | (ulong)a_data[current_index++] << 40 |
                         (ulong)a_data[current_index++] << 32 | (ulong)a_data[current_index++] << 24 | 
                         (ulong)a_data[current_index++] << 16 | (ulong)a_data[current_index++] << 8 |
                         (ulong)a_data[current_index++];
                    h *= M;
                    break;
                case 6:
                    h ^= (ulong)a_data[current_index++] << 40 | (ulong)a_data[current_index++] << 32 | 
                         (ulong)a_data[current_index++] << 24 | (ulong)a_data[current_index++] << 16 | 
                         (ulong)a_data[current_index++] << 8 | (ulong)a_data[current_index++];
                    h *= M;
                    break;
                case 5:
                    h ^= (ulong)a_data[current_index++] << 32 | (ulong)a_data[current_index++] << 24 | 
                         (ulong)a_data[current_index++] << 16 | (ulong)a_data[current_index++] << 8 | 
                         (ulong)a_data[current_index++];
                    h *= M;
                    break;
                case 4:
                    h ^= (ulong)a_data[current_index++] << 24 | (ulong)a_data[current_index++] << 16 | 
                         (ulong)a_data[current_index++] << 8 | (ulong)a_data[current_index++];
                    h *= M;
                    break;
                case 3:
                    h ^= (ulong)a_data[current_index++] << 16 | (ulong)a_data[current_index++] << 8 | 
                         (ulong)a_data[current_index++];
                    h *= M;
                    break;
                case 2:
                    h ^= (ulong)a_data[current_index++] << 8 | (ulong)a_data[current_index++];
                    h *= M;
                    break;
                case 1:
                    h ^= (ulong)a_data[current_index++];
                    h *= M;
                    break;
            };

            h ^= h >> R;
            h *= M;
            h ^= h >> R;

            return new HashResult(h);
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
