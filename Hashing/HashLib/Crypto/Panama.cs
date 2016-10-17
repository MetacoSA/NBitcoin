using System;

namespace HashLib.Crypto
{
    internal class Panama : BlockHash, ICryptoNotBuildIn
    {
        private const int COLUMNS = 17;

        private uint[] m_state = new uint[COLUMNS];
        private uint[,] m_stages = new uint[32, 8];
        private int m_tap;

        public Panama()
            : base(32, 32)
        {
            Initialize();
        }

        public override void Initialize()
        {
            m_state.Clear();
            m_stages.Clear();

            base.Initialize();
        }

        protected override void Finish()
        {
            int padding_size = BlockSize - (((int)m_processed_bytes) % BlockSize);

            byte[] pad = new byte[padding_size];
            pad[0] = 0x01;
            TransformBytes(pad, 0, padding_size);

            uint[] theta = new uint[COLUMNS];

            for (int i = 0; i < 32; i++)
            {
                int tap4 = (m_tap + 4) & 0x1F;
                int tap16 = (m_tap + 16) & 0x1F;

                m_tap = (m_tap - 1) & 0x1F;
                int tap25 = (m_tap + 25) & 0x1F;

                GPT(theta);

                m_stages[tap25, 0] ^= m_stages[m_tap, 2];
                m_stages[tap25, 1] ^= m_stages[m_tap, 3];
                m_stages[tap25, 2] ^= m_stages[m_tap, 4];
                m_stages[tap25, 3] ^= m_stages[m_tap, 5];
                m_stages[tap25, 4] ^= m_stages[m_tap, 6];
                m_stages[tap25, 5] ^= m_stages[m_tap, 7];
                m_stages[tap25, 6] ^= m_stages[m_tap, 0];
                m_stages[tap25, 7] ^= m_stages[m_tap, 1];
                m_stages[m_tap, 0] ^= m_state[1];
                m_stages[m_tap, 1] ^= m_state[2];
                m_stages[m_tap, 2] ^= m_state[3];
                m_stages[m_tap, 3] ^= m_state[4];
                m_stages[m_tap, 4] ^= m_state[5];
                m_stages[m_tap, 5] ^= m_state[6];
                m_stages[m_tap, 6] ^= m_state[7];
                m_stages[m_tap, 7] ^= m_state[8];

                m_state[0] = theta[0] ^ 0x01;
                m_state[1] = theta[1] ^ m_stages[tap4, 0];
                m_state[2] = theta[2] ^ m_stages[tap4, 1];
                m_state[3] = theta[3] ^ m_stages[tap4, 2];
                m_state[4] = theta[4] ^ m_stages[tap4, 3];
                m_state[5] = theta[5] ^ m_stages[tap4, 4];
                m_state[6] = theta[6] ^ m_stages[tap4, 5];
                m_state[7] = theta[7] ^ m_stages[tap4, 6];
                m_state[8] = theta[8] ^ m_stages[tap4, 7];
                m_state[9] = theta[9] ^ m_stages[tap16, 0];
                m_state[10] = theta[10] ^ m_stages[tap16, 1];
                m_state[11] = theta[11] ^ m_stages[tap16, 2];
                m_state[12] = theta[12] ^ m_stages[tap16, 3];
                m_state[13] = theta[13] ^ m_stages[tap16, 4];
                m_state[14] = theta[14] ^ m_stages[tap16, 5];
                m_state[15] = theta[15] ^ m_stages[tap16, 6];
                m_state[16] = theta[16] ^ m_stages[tap16, 7];
            }
        }

        private void GPT(uint[] theta)
        {
            uint[] gamma = new uint[COLUMNS];
            uint[] pi = new uint[COLUMNS];

            gamma[0] = m_state[0] ^ (m_state[1] | ~m_state[2]);
            gamma[1] = m_state[1] ^ (m_state[2] | ~m_state[3]);
            gamma[2] = m_state[2] ^ (m_state[3] | ~m_state[4]);
            gamma[3] = m_state[3] ^ (m_state[4] | ~m_state[5]);
            gamma[4] = m_state[4] ^ (m_state[5] | ~m_state[6]);
            gamma[5] = m_state[5] ^ (m_state[6] | ~m_state[7]);
            gamma[6] = m_state[6] ^ (m_state[7] | ~m_state[8]);
            gamma[7] = m_state[7] ^ (m_state[8] | ~m_state[9]);
            gamma[8] = m_state[8] ^ (m_state[9] | ~m_state[10]);
            gamma[9] = m_state[9] ^ (m_state[10] | ~m_state[11]);
            gamma[10] = m_state[10] ^ (m_state[11] | ~m_state[12]);
            gamma[11] = m_state[11] ^ (m_state[12] | ~m_state[13]);
            gamma[12] = m_state[12] ^ (m_state[13] | ~m_state[14]);
            gamma[13] = m_state[13] ^ (m_state[14] | ~m_state[15]);
            gamma[14] = m_state[14] ^ (m_state[15] | ~m_state[16]);
            gamma[15] = m_state[15] ^ (m_state[16] | ~m_state[0]);
            gamma[16] = m_state[16] ^ (m_state[0] | ~m_state[1]);

            pi[0] = gamma[0];
            pi[1] = Bits.RotateLeft(gamma[7], 1);
            pi[2] = Bits.RotateLeft(gamma[14], 3);
            pi[3] = Bits.RotateLeft(gamma[4], 6);
            pi[4] = Bits.RotateLeft(gamma[11], 10);
            pi[5] = Bits.RotateLeft(gamma[1], 15);
            pi[6] = Bits.RotateLeft(gamma[8], 21);
            pi[7] = Bits.RotateLeft(gamma[15], 28);
            pi[8] = Bits.RotateLeft(gamma[5], 4);
            pi[9] = Bits.RotateLeft(gamma[12], 13);
            pi[10] = Bits.RotateLeft(gamma[2], 23);
            pi[11] = Bits.RotateLeft(gamma[9], 2);
            pi[12] = Bits.RotateLeft(gamma[16], 14);
            pi[13] = Bits.RotateLeft(gamma[6], 27);
            pi[14] = Bits.RotateLeft(gamma[13], 9);
            pi[15] = Bits.RotateLeft(gamma[3], 24);
            pi[16] = Bits.RotateLeft(gamma[10], 8);

            theta[0] = pi[0] ^ pi[1] ^ pi[4];
            theta[1] = pi[1] ^ pi[2] ^ pi[5];
            theta[2] = pi[2] ^ pi[3] ^ pi[6];
            theta[3] = pi[3] ^ pi[4] ^ pi[7];
            theta[4] = pi[4] ^ pi[5] ^ pi[8];
            theta[5] = pi[5] ^ pi[6] ^ pi[9];
            theta[6] = pi[6] ^ pi[7] ^ pi[10];
            theta[7] = pi[7] ^ pi[8] ^ pi[11];
            theta[8] = pi[8] ^ pi[9] ^ pi[12];
            theta[9] = pi[9] ^ pi[10] ^ pi[13];
            theta[10] = pi[10] ^ pi[11] ^ pi[14];
            theta[11] = pi[11] ^ pi[12] ^ pi[15];
            theta[12] = pi[12] ^ pi[13] ^ pi[16];
            theta[13] = pi[13] ^ pi[14] ^ pi[0];
            theta[14] = pi[14] ^ pi[15] ^ pi[1];
            theta[15] = pi[15] ^ pi[16] ^ pi[2];
            theta[16] = pi[16] ^ pi[0] ^ pi[3];
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            uint[] work_buffer = Converters.ConvertBytesToUInts(a_data, a_index, BlockSize);
            uint[] theta = new uint[COLUMNS];

            int tap16 = (m_tap + 16) & 0x1F;

            m_tap = (m_tap - 1) & 0x1F;
            int tap25 = (m_tap + 25) & 0x1F;

            GPT(theta);

            m_stages[tap25, 0] ^= m_stages[m_tap, 2];
            m_stages[tap25, 1] ^= m_stages[m_tap, 3];
            m_stages[tap25, 2] ^= m_stages[m_tap, 4];
            m_stages[tap25, 3] ^= m_stages[m_tap, 5];
            m_stages[tap25, 4] ^= m_stages[m_tap, 6];
            m_stages[tap25, 5] ^= m_stages[m_tap, 7];
            m_stages[tap25, 6] ^= m_stages[m_tap, 0];
            m_stages[tap25, 7] ^= m_stages[m_tap, 1];
            m_stages[m_tap, 0] ^= work_buffer[0];
            m_stages[m_tap, 1] ^= work_buffer[1];
            m_stages[m_tap, 2] ^= work_buffer[2];
            m_stages[m_tap, 3] ^= work_buffer[3];
            m_stages[m_tap, 4] ^= work_buffer[4];
            m_stages[m_tap, 5] ^= work_buffer[5];
            m_stages[m_tap, 6] ^= work_buffer[6];
            m_stages[m_tap, 7] ^= work_buffer[7];

            m_state[0] = theta[0] ^ 0x01;
            m_state[1] = theta[1] ^ work_buffer[0];
            m_state[2] = theta[2] ^ work_buffer[1];
            m_state[3] = theta[3] ^ work_buffer[2];
            m_state[4] = theta[4] ^ work_buffer[3];
            m_state[5] = theta[5] ^ work_buffer[4];
            m_state[6] = theta[6] ^ work_buffer[5];
            m_state[7] = theta[7] ^ work_buffer[6];
            m_state[8] = theta[8] ^ work_buffer[7];
            m_state[9] = theta[9] ^ m_stages[tap16, 0];
            m_state[10] = theta[10] ^ m_stages[tap16, 1];
            m_state[11] = theta[11] ^ m_stages[tap16, 2];
            m_state[12] = theta[12] ^ m_stages[tap16, 3];
            m_state[13] = theta[13] ^ m_stages[tap16, 4];
            m_state[14] = theta[14] ^ m_stages[tap16, 5];
            m_state[15] = theta[15] ^ m_stages[tap16, 6];
            m_state[16] = theta[16] ^ m_stages[tap16, 7];
        }

        protected override byte[] GetResult()
        {
            return Converters.ConvertUIntsToBytes(m_state, 9, 8);
        }
    }
}