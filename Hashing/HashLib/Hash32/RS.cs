using System;
using System.Diagnostics;

namespace HashLib.Hash32
{
    internal class RS : Hash, IHash32, IBlockHash
    {
        private const uint B = 378551;

        private uint m_a = 63689;
        private uint m_hash;
        
        public RS()
            : base(4, 1)
        {
        }

        public override void Initialize()
        {
            m_hash = 0;
            m_a = 63689;
        }

        public override void TransformBytes(byte[] a_data, int a_index, int a_length)
        {
            Debug.Assert(a_index >= 0);
            Debug.Assert(a_length >= 0);
            Debug.Assert(a_index + a_length <= a_data.Length);

            for (int i = a_index; a_length > 0; i++, a_length--)
            {
                m_hash = (m_hash * m_a) + a_data[i];
                m_a = m_a * B;
            }
        }

        public override HashResult TransformFinal()
        {
            return new HashResult(m_hash);
        }
    }
}
