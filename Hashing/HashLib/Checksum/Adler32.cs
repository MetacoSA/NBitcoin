using System;
using System.Diagnostics;

namespace HashLib.Checksum
{
    internal class Adler32 : Hash, IChecksum, IBlockHash, IHash32
    {
        private const uint MOD_ADLER = 65521;

        private uint m_a;
        private uint m_b;

        public Adler32()
            : base(4, 1)
        {
        }

        public override void Initialize()
        {
             m_a = 1;
             m_b = 0;
        }

        public override void TransformBytes(byte[] a_data, int a_index, int a_length)
        {
            Debug.Assert(a_index >= 0);
            Debug.Assert(a_length >= 0);
            Debug.Assert(a_index + a_length <= a_data.Length);

            for (int i = a_index; a_length > 0; i++, a_length--)
            {
                m_a = (m_a + a_data[i]) % MOD_ADLER;
                m_b = (m_b + m_a) % MOD_ADLER;

            }
        }

        public override HashResult TransformFinal()
        {
            return new HashResult((m_b << 16) | m_a);

        }
    }
}
