using System;
using System.Diagnostics;

namespace HashLib.Hash32
{
    internal class PJW : Hash, IHash32, IBlockHash
    {
        private const int BitsInUnsignedInt = sizeof(uint) * 8;
        private const int ThreeQuarters = (BitsInUnsignedInt * 3) / 4;
        private const int OneEighth = BitsInUnsignedInt / 8;
        private const uint HighBits = uint.MaxValue << (BitsInUnsignedInt - OneEighth);

        private uint m_hash;

        public PJW()
            : base(4, 1)
        {
        }

        public override void Initialize()
        {
            m_hash = 0;
        }

        public override void TransformBytes(byte[] a_data, int a_index, int a_length)
        {
            Debug.Assert(a_index >= 0);
            Debug.Assert(a_length >= 0);
            Debug.Assert(a_index + a_length <= a_data.Length);

            for (int i = a_index; a_length > 0; i++, a_length--)
            {
                m_hash = (m_hash << OneEighth) + a_data[i];

                uint test = m_hash & HighBits;
                if (test != 0)
                    m_hash = ((m_hash ^ (test >> ThreeQuarters)) & (~HighBits));
            }
        }

        public override HashResult TransformFinal()
        {
            return new HashResult(m_hash);
        }
    }
}
