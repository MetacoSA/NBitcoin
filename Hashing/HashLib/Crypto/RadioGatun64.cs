using System;

namespace HashLib.Crypto
{
    internal class RadioGatun64 : BlockHash, ICryptoNotBuildIn
    {
        private const int MILL_SIZE = 19;
        private const int BELT_WIDTH = 3;
        private const int BELT_LENGTH = 13;
        private const int NUMBER_OF_BLANK_ITERATIONS = 16;

        private ulong[] m_mill = new ulong[MILL_SIZE];
        private ulong[][] m_belt;

        public RadioGatun64()
            : base(32, 8 * BELT_WIDTH)
        {
            m_belt = new ulong[BELT_LENGTH][];
            for (int i = 0; i < BELT_LENGTH; i++)
                m_belt[i] = new ulong[BELT_WIDTH];

            Initialize();
        }

        public override void Initialize()
        {
            m_mill.Clear();

            for (int i = 0; i < BELT_LENGTH; i++)
                m_belt[i].Clear();

            base.Initialize();
        }

        protected override void Finish()
        {
            int padding_size = BlockSize - (((int)m_processed_bytes) % BlockSize);

            byte[] pad = new byte[padding_size];
            pad[0] = 0x01;
            TransformBytes(pad, 0, padding_size);

            for (int i = 0; i < NUMBER_OF_BLANK_ITERATIONS; i++)
                RoundFunction();
        }

        protected override byte[] GetResult()
        {
            ulong[] result = new ulong[HashSize / 8];
            for (int i = 0; i < HashSize / 16; i++)
            {
                RoundFunction();
                Array.Copy(m_mill, 1, result, i * 2, 2);
            }

            return Converters.ConvertULongsToBytes(result);
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            ulong[] data = Converters.ConvertBytesToULongs(a_data, a_index, BlockSize);

            for (int i = 0; i < BELT_WIDTH; i++)
            {
                m_mill[i + 16] ^= data[i];
                m_belt[0][i] ^= data[i];
            }

            RoundFunction();
        }

        private void RoundFunction()
        {
            ulong[] q = m_belt[BELT_LENGTH - 1];
            for (int i = BELT_LENGTH - 1; i > 0; i--)
                m_belt[i] = m_belt[i - 1];
            m_belt[0] = q;

            for (int i = 0; i < 12; i++)
                m_belt[i + 1][i % BELT_WIDTH] ^= m_mill[i + 1];

            ulong[] a = new ulong[MILL_SIZE];

            for (int i = 0; i < MILL_SIZE; i++)
                a[i] = m_mill[i] ^ (m_mill[(i + 1) % MILL_SIZE] | ~m_mill[(i + 2) % MILL_SIZE]);

            for (int i = 0; i < MILL_SIZE; i++)
                m_mill[i] = Bits.RotateRight(a[(7 * i) % MILL_SIZE], i * (i + 1) / 2);

            for (int i = 0; i < MILL_SIZE; i++)
                a[i] = m_mill[i] ^ m_mill[(i + 1) % MILL_SIZE] ^ m_mill[(i + 4) % MILL_SIZE];

            a[0] ^= 1;
            for (int i = 0; i < MILL_SIZE; i++)
                m_mill[i] = a[i];

            for (int i = 0; i < BELT_WIDTH; i++)
                m_mill[i + 13] ^= q[i];
        }
    }
}
