using System;
using System.Diagnostics;

namespace HashLib.Hash64
{
    internal class SipHash : BlockHash, IHash64, IHashWithKey
    {
        #region Consts
        private const ulong V0 = 0x736f6d6570736575;
        private const ulong V1 = 0x646f72616e646f6d;
        private const ulong V2 = 0x6c7967656e657261;
        private const ulong V3 = 0x7465646279746573;

        private const ulong KEY0 = 0x0706050403020100;
        private const ulong KEY1 = 0x0F0E0D0C0B0A0908;
        #endregion

        private ulong m_v0;
        private ulong m_v1;
        private ulong m_v2;
        private ulong m_v3;
        private ulong m_key0;
        private ulong m_key1;

        public SipHash()
            : base(8, 8)
        {
            m_key0 = KEY0;
            m_key1 = KEY1;

            Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();

            m_v0 = V0;
            m_v1 = V1;
            m_v2 = V2;
            m_v3 = V3;

            m_v3 ^= m_key1;
            m_v2 ^= m_key0;
            m_v1 ^= m_key1;
            m_v0 ^= m_key0;
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            ulong m = Converters.ConvertBytesToULong(a_data, a_index);

            m_v3 ^= m;

            m_v0 += m_v1;
            m_v1 = (m_v1 << 13) | (m_v1 >> (64 - 13));
            m_v1 ^= m_v0;
            m_v0 = (m_v0 << 32) | (m_v0 >> (64 - 32));
            m_v2 += m_v3;
            m_v3 = (m_v3 << 16) | (m_v3 >> (64 - 16));
            m_v3 ^= m_v2;
            m_v0 += m_v3;
            m_v3 = (m_v3 << 21) | (m_v3 >> (64 - 21));
            m_v3 ^= m_v0;
            m_v2 += m_v1;
            m_v1 = (m_v1 << 17) | (m_v1 >> (64 - 17));
            m_v1 ^= m_v2;
            m_v2 = (m_v2 << 32) | (m_v2 >> (64 - 32));

            m_v0 += m_v1;
            m_v1 = (m_v1 << 13) | (m_v1 >> (64 - 13));
            m_v1 ^= m_v0;
            m_v0 = (m_v0 << 32) | (m_v0 >> (64 - 32));
            m_v2 += m_v3;
            m_v3 = (m_v3 << 16) | (m_v3 >> (64 - 16));
            m_v3 ^= m_v2;
            m_v0 += m_v3;
            m_v3 = (m_v3 << 21) | (m_v3 >> (64 - 21));
            m_v3 ^= m_v0;
            m_v2 += m_v1;
            m_v1 = (m_v1 << 17) | (m_v1 >> (64 - 17));
            m_v1 ^= m_v2;
            m_v2 = (m_v2 << 32) | (m_v2 >> (64 - 32));

            m_v0 ^= m;
        }

        protected override byte[] GetResult()
        {
            ulong b = m_v0 ^ m_v1 ^ m_v2 ^ m_v3;
            return Converters.ConvertULongToBytes(b);
        }

        protected override void Finish()
        {
            ulong left = m_processed_bytes % (ulong)BlockSize;
            ulong b = m_processed_bytes << 56;

            byte[] buffer = m_buffer.GetBytesZeroPadded();
            b |= Converters.ConvertBytesToULong(buffer, 0);

            m_v3 ^= b;

            m_v0 += m_v1;
            m_v1 = (m_v1 << 13) | (m_v1 >> (64 - 13));
            m_v1 ^= m_v0;
            m_v0 = (m_v0 << 32) | (m_v0 >> (64 - 32));
            m_v2 += m_v3;
            m_v3 = (m_v3 << 16) | (m_v3 >> (64 - 16));
            m_v3 ^= m_v2;
            m_v0 += m_v3;
            m_v3 = (m_v3 << 21) | (m_v3 >> (64 - 21));
            m_v3 ^= m_v0;
            m_v2 += m_v1;
            m_v1 = (m_v1 << 17) | (m_v1 >> (64 - 17));
            m_v1 ^= m_v2;
            m_v2 = (m_v2 << 32) | (m_v2 >> (64 - 32));

            m_v0 += m_v1;
            m_v1 = (m_v1 << 13) | (m_v1 >> (64 - 13));
            m_v1 ^= m_v0;
            m_v0 = (m_v0 << 32) | (m_v0 >> (64 - 32));
            m_v2 += m_v3;
            m_v3 = (m_v3 << 16) | (m_v3 >> (64 - 16));
            m_v3 ^= m_v2;
            m_v0 += m_v3;
            m_v3 = (m_v3 << 21) | (m_v3 >> (64 - 21));
            m_v3 ^= m_v0;
            m_v2 += m_v1;
            m_v1 = (m_v1 << 17) | (m_v1 >> (64 - 17));
            m_v1 ^= m_v2;
            m_v2 = (m_v2 << 32) | (m_v2 >> (64 - 32));

            m_v0 ^= b;
            m_v2 ^= 0xff;

            m_v0 += m_v1;
            m_v1 = (m_v1 << 13) | (m_v1 >> (64 - 13));
            m_v1 ^= m_v0;
            m_v0 = (m_v0 << 32) | (m_v0 >> (64 - 32));
            m_v2 += m_v3;
            m_v3 = (m_v3 << 16) | (m_v3 >> (64 - 16));
            m_v3 ^= m_v2;
            m_v0 += m_v3;
            m_v3 = (m_v3 << 21) | (m_v3 >> (64 - 21));
            m_v3 ^= m_v0;
            m_v2 += m_v1;
            m_v1 = (m_v1 << 17) | (m_v1 >> (64 - 17));
            m_v1 ^= m_v2;
            m_v2 = (m_v2 << 32) | (m_v2 >> (64 - 32));

            m_v0 += m_v1;
            m_v1 = (m_v1 << 13) | (m_v1 >> (64 - 13));
            m_v1 ^= m_v0;
            m_v0 = (m_v0 << 32) | (m_v0 >> (64 - 32));
            m_v2 += m_v3;
            m_v3 = (m_v3 << 16) | (m_v3 >> (64 - 16));
            m_v3 ^= m_v2;
            m_v0 += m_v3;
            m_v3 = (m_v3 << 21) | (m_v3 >> (64 - 21));
            m_v3 ^= m_v0;
            m_v2 += m_v1;
            m_v1 = (m_v1 << 17) | (m_v1 >> (64 - 17));
            m_v1 ^= m_v2;
            m_v2 = (m_v2 << 32) | (m_v2 >> (64 - 32));

            m_v0 += m_v1;
            m_v1 = (m_v1 << 13) | (m_v1 >> (64 - 13));
            m_v1 ^= m_v0;
            m_v0 = (m_v0 << 32) | (m_v0 >> (64 - 32));
            m_v2 += m_v3;
            m_v3 = (m_v3 << 16) | (m_v3 >> (64 - 16));
            m_v3 ^= m_v2;
            m_v0 += m_v3;
            m_v3 = (m_v3 << 21) | (m_v3 >> (64 - 21));
            m_v3 ^= m_v0;
            m_v2 += m_v1;
            m_v1 = (m_v1 << 17) | (m_v1 >> (64 - 17));
            m_v1 ^= m_v2;
            m_v2 = (m_v2 << 32) | (m_v2 >> (64 - 32));

            m_v0 += m_v1;
            m_v1 = (m_v1 << 13) | (m_v1 >> (64 - 13));
            m_v1 ^= m_v0;
            m_v0 = (m_v0 << 32) | (m_v0 >> (64 - 32));
            m_v2 += m_v3;
            m_v3 = (m_v3 << 16) | (m_v3 >> (64 - 16));
            m_v3 ^= m_v2;
            m_v0 += m_v3;
            m_v3 = (m_v3 << 21) | (m_v3 >> (64 - 21));
            m_v3 ^= m_v0;
            m_v2 += m_v1;
            m_v1 = (m_v1 << 17) | (m_v1 >> (64 - 17));
            m_v1 ^= m_v2;
            m_v2 = (m_v2 << 32) | (m_v2 >> (64 - 32));
        }

        public byte[] Key
        {
            get
            {
                byte[] key = new byte[KeyLength.Value];

                Converters.ConvertULongToBytes(m_key0, key, 0);
                Converters.ConvertULongToBytes(m_key1, key, 8);

                return key;
            }
            set
            {
                if (value == null)
                {
                    m_key0 = KEY0;
                    m_key1 = KEY1;
                }
                else
                {
                    Debug.Assert(value.Length == KeyLength.Value);

                    m_key0 = Converters.ConvertBytesToULong(value, 0);
                    m_key1 = Converters.ConvertBytesToULong(value, 8);
                }
            }
        }

        public int? KeyLength
        {
            get
            {
                return 16;
            }
        }
    }
}
