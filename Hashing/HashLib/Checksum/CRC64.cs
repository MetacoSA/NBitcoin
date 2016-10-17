using System;
using System.Diagnostics;

namespace HashLib.Checksum
{
    public static class CRC64Polynomials
    {
        public static ulong ISO = 0xD800000000000000;
        public static ulong ECMA_182 = 0xC96C5795D7870F42;
    }

    internal class CRC64_ISO : CRC64
    {
        public CRC64_ISO()
            : base(CRC64Polynomials.ISO)
        {
        }
    }

    internal class CRC64_ECMA : CRC64
    {
        public CRC64_ECMA()
            : base(CRC64Polynomials.ECMA_182)
        {
        }
    }

    internal class CRC64 : Hash, IChecksum, IBlockHash, IHash64
    {
        private ulong[] m_crc_tab = new ulong[256];

        private ulong m_hash;
        private ulong m_initial_value;
        private ulong m_final_xor;

        public CRC64(ulong a_polynomial, ulong a_initial_value = ulong.MaxValue, ulong a_final_xor = ulong.MaxValue)
            : base(8, 1)
        {
            m_initial_value = a_initial_value;
            m_final_xor = a_final_xor;

            GenerateCRCTable(a_polynomial);
        }

        private void GenerateCRCTable(ulong a_poly64)
        {
            for (uint i = 0; i < 256; ++i)
            {
                ulong crc = i;

                for (uint j = 0; j < 8; ++j)
                {
                    if ((crc & 1) == 1)
                        crc = (crc >> 1) ^ a_poly64;
                    else
                        crc >>= 1;
                }

                m_crc_tab[i] = crc;
            }
        }

        public override void Initialize()
        {
            m_hash = m_initial_value;
        }

        public override void TransformBytes(byte[] a_data, int a_index, int a_length)
        {
            Debug.Assert(a_index >= 0);
            Debug.Assert(a_length >= 0);
            Debug.Assert(a_index + a_length <= a_data.Length);

            for (int i = a_index; a_length > 0; i++, a_length--)
                m_hash = (m_hash >> 8) ^ m_crc_tab[(byte)m_hash ^ a_data[i]];
        }

        public override HashResult TransformFinal()
        {
            return new HashResult(m_hash ^ m_final_xor);
        }
    }
}
