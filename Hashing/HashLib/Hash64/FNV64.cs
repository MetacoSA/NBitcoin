using System;
using System.Diagnostics;

namespace HashLib.Hash64
{
    internal class FNV64 : Hash, IHash64, IBlockHash
    {
        private ulong m_hash;

        public FNV64()
            : base(8, 1)
        {
        }

        public override void Initialize()
        {
            m_hash = 14695981039346656037;
        }

        public override void TransformBytes(byte[] a_data, int a_index, int a_length)
        {
            Debug.Assert(a_index >= 0);
            Debug.Assert(a_length >= 0);
            Debug.Assert(a_index + a_length <= a_data.Length);

            for (int i = a_index; a_length > 0; i++, a_length--)
                m_hash = (m_hash * 1099511628211) ^ a_data[i];
        }

        public override HashResult TransformFinal()
        {
            return new HashResult(m_hash);
        }
    }
}
