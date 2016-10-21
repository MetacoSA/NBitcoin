using System;
using System.Diagnostics;
using System.Linq;

namespace HashLib.Checksum
{
    public static class CRC32Polynomials
    {
        public static uint IEEE_802_3 = 0xEDB88320;
        public static uint Castagnoli = 0x82F63B78;
        public static uint Koopman = 0xEB31D82E;
        public static uint CRC_32Q = 0xD5828281;
    }

    internal class CRC32_IEEE : CRC32
    {
        public CRC32_IEEE()
            : base(CRC32Polynomials.IEEE_802_3)
        {
        }
    }

    internal class CRC32_CASTAGNOLI : CRC32
    {
        public CRC32_CASTAGNOLI()
            : base(CRC32Polynomials.Castagnoli)
        {
        }
    }

    internal class CRC32_KOOPMAN : CRC32
    {
        public CRC32_KOOPMAN()
            : base(CRC32Polynomials.Koopman)
        {
        }
    }

    internal class CRC32_Q : CRC32
    {
        public CRC32_Q()
            : base(CRC32Polynomials.CRC_32Q)
        {
        }
    }

    internal class CRC32 : Hash, IChecksum, IBlockHash, IHash32
    {
        private uint[] m_crc_tab = new uint[256];

        private uint m_hash;
        private uint m_initial_value;
        private uint m_final_xor;

        public CRC32(uint a_polynomial, uint a_initial_value = uint.MaxValue, uint a_final_xor = uint.MaxValue)
            : base(4, 1)
        {
            m_initial_value = a_initial_value;
            m_final_xor = a_final_xor;

            GenerateCRCTable(a_polynomial);
        }

        private void GenerateCRCTable(uint a_poly32)
        {
            for (uint i = 0; i < 256; ++i)
            {
                uint crc = i;

                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) == 1)
                        crc = (crc >> 1) ^ a_poly32;
                    else
                        crc = crc >> 1;
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
