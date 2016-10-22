using System;

namespace HashLib.Crypto.SHA3
{
    internal class CubeHash224 : CubeHash
    {
        public CubeHash224()
            : base(HashLib.HashSize.HashSize224)
        {
        }
    }

    internal class CubeHash256 : CubeHash
    {
        public CubeHash256()
            : base(HashLib.HashSize.HashSize256)
        {
        }
    }

    internal class CubeHash384 : CubeHash
    {
        public CubeHash384()
            : base(HashLib.HashSize.HashSize384)
        {
        }
    }

    internal class CubeHash512 : CubeHash
    {
        public CubeHash512()
            : base(HashLib.HashSize.HashSize512)
        {
        }
    }

    internal abstract class CubeHash : BlockHash, ICryptoNotBuildIn
    {
        private const int ROUNDS = 16;
        private readonly uint[] m_state = new uint[32];
        private static uint[][] m_inits;

        static CubeHash()
        {
            int[] hashes = new int[] { 28, 32, 48, 64 };
            uint[][] inits = new uint[65][];
            byte[] zeroes = new byte[32];

            foreach (int hashsize in hashes)
            {
                CubeHash ch = (CubeHash)HashFactory.Crypto.SHA3.CreateCubeHash(HashLib.HashSize.HashSize256);

                ch.m_state[0] = (uint)hashsize;
                ch.m_state[1] = 32;
                ch.m_state[2] = 16;

                for (int i = 0; i < 10; i++)
                    ch.TransformBlock(zeroes, 0);

                inits[hashsize] = new uint[32];
                Array.Copy(ch.m_state, inits[hashsize], 32);
            }

            m_inits = inits;
        }

        public CubeHash(HashLib.HashSize a_hash_size)
            : base((int)a_hash_size, 32)
        {
            Initialize();
        }

        protected override byte[] GetResult()
        {
            return Converters.ConvertUIntsToBytes(m_state, 0, HashSize / 4);
        }

        protected override void Finish()
        {
            byte[] pad = new byte[BlockSize + 1];
            pad[0] = 0x80;

            TransformBytes(pad, 0, BlockSize - m_buffer.Pos);

            m_state[31] ^= 1;

            for (int i = 0; i < 10; i++)
                TransformBlock(pad, 1);
        }

        public override void Initialize()
        {
            if (m_inits != null)
                Array.Copy(m_inits[HashSize], m_state, 32);

            base.Initialize();
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            uint[] temp = new uint[16];

            uint[] state = new uint[32];
            Array.Copy(m_state, state, 32);

            uint[] data = Converters.ConvertBytesToUInts(a_data, a_index, BlockSize);

            for (int i = 0; i < data.Length; i++)
                state[i] ^= data[i];

            for (int r = 0; r < ROUNDS; ++r)
            {
                state[16] += state[0];
                state[17] += state[1];
                state[18] += state[2];
                state[19] += state[3];
                state[20] += state[4];
                state[21] += state[5];
                state[22] += state[6];
                state[23] += state[7];
                state[24] += state[8];
                state[25] += state[9];
                state[26] += state[10];
                state[27] += state[11];
                state[28] += state[12];
                state[29] += state[13];
                state[30] += state[14];
                state[31] += state[15];

                temp[0 ^ 8] = state[0];
                temp[1 ^ 8] = state[1];
                temp[2 ^ 8] = state[2];
                temp[3 ^ 8] = state[3];
                temp[4 ^ 8] = state[4];
                temp[5 ^ 8] = state[5];
                temp[6 ^ 8] = state[6];
                temp[7 ^ 8] = state[7];
                temp[8 ^ 8] = state[8];
                temp[9 ^ 8] = state[9];
                temp[10 ^ 8] = state[10];
                temp[11 ^ 8] = state[11];
                temp[12 ^ 8] = state[12];
                temp[13 ^ 8] = state[13];
                temp[14 ^ 8] = state[14];
                temp[15 ^ 8] = state[15];

                for (int i = 0; i < 16; i++)
                    state[i] = (temp[i] << 7) | (temp[i] >> 25);

                state[0] ^= state[16];
                state[1] ^= state[17];
                state[2] ^= state[18];
                state[3] ^= state[19];
                state[4] ^= state[20];
                state[5] ^= state[21];
                state[6] ^= state[22];
                state[7] ^= state[23];
                state[8] ^= state[24];
                state[9] ^= state[25];
                state[10] ^= state[26];
                state[11] ^= state[27];
                state[12] ^= state[28];
                state[13] ^= state[29];
                state[14] ^= state[30];
                state[15] ^= state[31];

                temp[0 ^ 2] = state[16];
                temp[1 ^ 2] = state[17];
                temp[2 ^ 2] = state[18];
                temp[3 ^ 2] = state[19];
                temp[4 ^ 2] = state[20];
                temp[5 ^ 2] = state[21];
                temp[6 ^ 2] = state[22];
                temp[7 ^ 2] = state[23];
                temp[8 ^ 2] = state[24];
                temp[9 ^ 2] = state[25];
                temp[10 ^ 2] = state[26];
                temp[11 ^ 2] = state[27];
                temp[12 ^ 2] = state[28];
                temp[13 ^ 2] = state[29];
                temp[14 ^ 2] = state[30];
                temp[15 ^ 2] = state[31];

                state[16] = temp[0];
                state[17] = temp[1];
                state[18] = temp[2];
                state[19] = temp[3];
                state[20] = temp[4];
                state[21] = temp[5];
                state[22] = temp[6];
                state[23] = temp[7];
                state[24] = temp[8];
                state[25] = temp[9];
                state[26] = temp[10];
                state[27] = temp[11];
                state[28] = temp[12];
                state[29] = temp[13];
                state[30] = temp[14];
                state[31] = temp[15];

                state[16] += state[0];
                state[17] += state[1];
                state[18] += state[2];
                state[19] += state[3];
                state[20] += state[4];
                state[21] += state[5];
                state[22] += state[6];
                state[23] += state[7];
                state[24] += state[8];
                state[25] += state[9];
                state[26] += state[10];
                state[27] += state[11];
                state[28] += state[12];
                state[29] += state[13];
                state[30] += state[14];
                state[31] += state[15];

                temp[0 ^ 4] = state[0];
                temp[1 ^ 4] = state[1];
                temp[2 ^ 4] = state[2];
                temp[3 ^ 4] = state[3];
                temp[4 ^ 4] = state[4];
                temp[5 ^ 4] = state[5];
                temp[6 ^ 4] = state[6];
                temp[7 ^ 4] = state[7];
                temp[8 ^ 4] = state[8];
                temp[9 ^ 4] = state[9];
                temp[10 ^ 4] = state[10];
                temp[11 ^ 4] = state[11];
                temp[12 ^ 4] = state[12];
                temp[13 ^ 4] = state[13];
                temp[14 ^ 4] = state[14];
                temp[15 ^ 4] = state[15];
                                  
                for (int i = 0; i < 16; i++)
                    state[i] = (temp[i] << 11) | (temp[i] >> 21);

                state[0] ^= state[16];
                state[1] ^= state[17];
                state[2] ^= state[18];
                state[3] ^= state[19];
                state[4] ^= state[20];
                state[5] ^= state[21];
                state[6] ^= state[22];
                state[7] ^= state[23];
                state[8] ^= state[24];
                state[9] ^= state[25];
                state[10] ^= state[26];
                state[11] ^= state[27];
                state[12] ^= state[28];
                state[13] ^= state[29];
                state[14] ^= state[30];
                state[15] ^= state[31];

                temp[0 ^ 1] = state[16];
                temp[1 ^ 1] = state[17];
                temp[2 ^ 1] = state[18];
                temp[3 ^ 1] = state[19];
                temp[4 ^ 1] = state[20];
                temp[5 ^ 1] = state[21];
                temp[6 ^ 1] = state[22];
                temp[7 ^ 1] = state[23];
                temp[8 ^ 1] = state[24];
                temp[9 ^ 1] = state[25];
                temp[10 ^ 1] = state[26];
                temp[11 ^ 1] = state[27];
                temp[12 ^ 1] = state[28];
                temp[13 ^ 1] = state[29];
                temp[14 ^ 1] = state[30];
                temp[15 ^ 1] = state[31];

                state[16] = temp[0];
                state[17] = temp[1];
                state[18] = temp[2];
                state[19] = temp[3];
                state[20] = temp[4];
                state[21] = temp[5];
                state[22] = temp[6];
                state[23] = temp[7];
                state[24] = temp[8];
                state[25] = temp[9];
                state[26] = temp[10];
                state[27] = temp[11];
                state[28] = temp[12];
                state[29] = temp[13];
                state[30] = temp[14];
                state[31] = temp[15];
            }

            Array.Copy(state, m_state, 32);
        }
    }
}
