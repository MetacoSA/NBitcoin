
using System;

namespace HashLib.Crypto
{
    internal class Grindahl256 : BlockHash, ICryptoNotBuildIn
    {
        #region Consts

        private const int ROWS = 4;
        private const int COLUMNS = 13;
        private const int BLANK_ROUNDS = 8;

        private static readonly uint[] s_table_0;
        private static readonly uint[] s_table_1;
        private static readonly uint[] s_table_2;
        private static readonly uint[] s_table_3;

        private static readonly uint[] s_master_table =
        {
            0xc66363a5, 0xf87c7c84, 0xee777799, 0xf67b7b8d, 0xfff2f20d, 0xd66b6bbd, 0xde6f6fb1, 0x91c5c554, 
            0x60303050, 0x02010103, 0xce6767a9, 0x562b2b7d, 0xe7fefe19, 0xb5d7d762, 0x4dababe6, 0xec76769a, 
            0x8fcaca45, 0x1f82829d, 0x89c9c940, 0xfa7d7d87, 0xeffafa15, 0xb25959eb, 0x8e4747c9, 0xfbf0f00b, 
            0x41adadec, 0xb3d4d467, 0x5fa2a2fd, 0x45afafea, 0x239c9cbf, 0x53a4a4f7, 0xe4727296, 0x9bc0c05b, 
            0x75b7b7c2, 0xe1fdfd1c, 0x3d9393ae, 0x4c26266a, 0x6c36365a, 0x7e3f3f41, 0xf5f7f702, 0x83cccc4f, 
            0x6834345c, 0x51a5a5f4, 0xd1e5e534, 0xf9f1f108, 0xe2717193, 0xabd8d873, 0x62313153, 0x2a15153f, 
            0x0804040c, 0x95c7c752, 0x46232365, 0x9dc3c35e, 0x30181828, 0x379696a1, 0x0a05050f, 0x2f9a9ab5, 
            0x0e070709, 0x24121236, 0x1b80809b, 0xdfe2e23d, 0xcdebeb26, 0x4e272769, 0x7fb2b2cd, 0xea75759f, 
            0x1209091b, 0x1d83839e, 0x582c2c74, 0x341a1a2e, 0x361b1b2d, 0xdc6e6eb2, 0xb45a5aee, 0x5ba0a0fb, 
            0xa45252f6, 0x763b3b4d, 0xb7d6d661, 0x7db3b3ce, 0x5229297b, 0xdde3e33e, 0x5e2f2f71, 0x13848497, 
            0xa65353f5, 0xb9d1d168, 0x00000000, 0xc1eded2c, 0x40202060, 0xe3fcfc1f, 0x79b1b1c8, 0xb65b5bed, 
            0xd46a6abe, 0x8dcbcb46, 0x67bebed9, 0x7239394b, 0x944a4ade, 0x984c4cd4, 0xb05858e8, 0x85cfcf4a, 
            0xbbd0d06b, 0xc5efef2a, 0x4faaaae5, 0xedfbfb16, 0x864343c5, 0x9a4d4dd7, 0x66333355, 0x11858594, 
            0x8a4545cf, 0xe9f9f910, 0x04020206, 0xfe7f7f81, 0xa05050f0, 0x783c3c44, 0x259f9fba, 0x4ba8a8e3, 
            0xa25151f3, 0x5da3a3fe, 0x804040c0, 0x058f8f8a, 0x3f9292ad, 0x219d9dbc, 0x70383848, 0xf1f5f504, 
            0x63bcbcdf, 0x77b6b6c1, 0xafdada75, 0x42212163, 0x20101030, 0xe5ffff1a, 0xfdf3f30e, 0xbfd2d26d, 
            0x81cdcd4c, 0x180c0c14, 0x26131335, 0xc3ecec2f, 0xbe5f5fe1, 0x359797a2, 0x884444cc, 0x2e171739, 
            0x93c4c457, 0x55a7a7f2, 0xfc7e7e82, 0x7a3d3d47, 0xc86464ac, 0xba5d5de7, 0x3219192b, 0xe6737395, 
            0xc06060a0, 0x19818198, 0x9e4f4fd1, 0xa3dcdc7f, 0x44222266, 0x542a2a7e, 0x3b9090ab, 0x0b888883, 
            0x8c4646ca, 0xc7eeee29, 0x6bb8b8d3, 0x2814143c, 0xa7dede79, 0xbc5e5ee2, 0x160b0b1d, 0xaddbdb76, 
            0xdbe0e03b, 0x64323256, 0x743a3a4e, 0x140a0a1e, 0x924949db, 0x0c06060a, 0x4824246c, 0xb85c5ce4, 
            0x9fc2c25d, 0xbdd3d36e, 0x43acacef, 0xc46262a6, 0x399191a8, 0x319595a4, 0xd3e4e437, 0xf279798b, 
            0xd5e7e732, 0x8bc8c843, 0x6e373759, 0xda6d6db7, 0x018d8d8c, 0xb1d5d564, 0x9c4e4ed2, 0x49a9a9e0, 
            0xd86c6cb4, 0xac5656fa, 0xf3f4f407, 0xcfeaea25, 0xca6565af, 0xf47a7a8e, 0x47aeaee9, 0x10080818, 
            0x6fbabad5, 0xf0787888, 0x4a25256f, 0x5c2e2e72, 0x381c1c24, 0x57a6a6f1, 0x73b4b4c7, 0x97c6c651, 
            0xcbe8e823, 0xa1dddd7c, 0xe874749c, 0x3e1f1f21, 0x964b4bdd, 0x61bdbddc, 0x0d8b8b86, 0x0f8a8a85, 
            0xe0707090, 0x7c3e3e42, 0x71b5b5c4, 0xcc6666aa, 0x904848d8, 0x06030305, 0xf7f6f601, 0x1c0e0e12, 
            0xc26161a3, 0x6a35355f, 0xae5757f9, 0x69b9b9d0, 0x17868691, 0x99c1c158, 0x3a1d1d27, 0x279e9eb9, 
            0xd9e1e138, 0xebf8f813, 0x2b9898b3, 0x22111133, 0xd26969bb, 0xa9d9d970, 0x078e8e89, 0x339494a7, 
            0x2d9b9bb6, 0x3c1e1e22, 0x15878792, 0xc9e9e920, 0x87cece49, 0xaa5555ff, 0x50282878, 0xa5dfdf7a, 
            0x038c8c8f, 0x59a1a1f8, 0x09898980, 0x1a0d0d17, 0x65bfbfda, 0xd7e6e631, 0x844242c6, 0xd06868b8, 
            0x824141c3, 0x299999b0, 0x5a2d2d77, 0x1e0f0f11, 0x7bb0b0cb, 0xa85454fc, 0x6dbbbbd6, 0x2c16163a
        };

        static Grindahl256()
        {
            s_table_0 = s_master_table;

            s_table_1 = CalcTable(1);
            s_table_2 = CalcTable(2);
            s_table_3 = CalcTable(3);
        }

        private static uint[] CalcTable(int i)
        {
            var result = new uint[256];
            for (int j = 0; j < 256; j++)
                result[j] = (uint)((s_master_table[j] >> i * 8) | (s_master_table[j] << (32 - i * 8)));
            return result;
        }

        #endregion
    
        private uint[] m_state = new uint[ROWS * COLUMNS / 4];
        private uint[] m_temp = new uint[ROWS * COLUMNS / 4];

        public Grindahl256()
            : base(32, 4)
        {
            Initialize();
        }

        public override void Initialize()
        {
            m_state.Clear();
            m_temp.Clear();

            base.Initialize();
        }

        protected override void Finish()
        {
            int padding_size = 3 * BlockSize - (int)(m_processed_bytes % (uint)BlockSize);
            ulong msg_length = (m_processed_bytes / (ulong)ROWS) + 1;

            byte[] pad = new byte[padding_size];
            pad[0] = 0x80;

            Converters.ConvertULongToBytesSwapOrder(msg_length, pad, padding_size - 8);
            TransformBytes(pad, 0, padding_size - BlockSize);

            m_state[0] = Converters.ConvertBytesToUIntSwapOrder(pad, padding_size - BlockSize);
            InjectMsg(true);

            for (int i = 0; i < BLANK_ROUNDS; i++)
                InjectMsg(true);
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            m_state[0] = Converters.ConvertBytesToUIntSwapOrder(a_data, a_index);
            InjectMsg(false);
        }

        protected override byte[] GetResult()
        {
            return Converters.ConvertUIntsToBytesSwapOrder(m_state, COLUMNS - HashSize / ROWS, HashSize / ROWS);
        }

        private void InjectMsg(bool a_full_process)
        {
            m_state[ROWS * COLUMNS / 4 - 1] ^= 0x01;

            if (a_full_process)
            {
                m_temp[0] =
                  s_table_0[(byte)(m_state[12] >> 24)] ^
                  s_table_1[(byte)(m_state[11] >> 16)] ^
                  s_table_2[(byte)(m_state[9] >> 8)] ^
                  s_table_3[(byte)m_state[3]];
            }

            m_temp[1] =
              s_table_0[(byte)(m_state[0] >> 24)] ^
              s_table_1[(byte)(m_state[12] >> 16)] ^
              s_table_2[(byte)(m_state[10] >> 8)] ^
              s_table_3[(byte)m_state[4]];

            m_temp[2] =
              s_table_0[(byte)(m_state[1] >> 24)] ^
              s_table_1[(byte)(m_state[0] >> 16)] ^
              s_table_2[(byte)(m_state[11] >> 8)] ^
              s_table_3[(byte)m_state[5]];

            m_temp[3] =
              s_table_0[(byte)(m_state[2] >> 24)] ^
              s_table_1[(byte)(m_state[1] >> 16)] ^
              s_table_2[(byte)(m_state[12] >> 8)] ^
              s_table_3[(byte)m_state[6]];

            m_temp[4] =
              s_table_0[(byte)(m_state[3] >> 24)] ^
              s_table_1[(byte)(m_state[2] >> 16)] ^
              s_table_2[(byte)(m_state[0] >> 8)] ^
              s_table_3[(byte)m_state[7]];

            m_temp[5] =
              s_table_0[(byte)(m_state[4] >> 24)] ^
              s_table_1[(byte)(m_state[3] >> 16)] ^
              s_table_2[(byte)(m_state[1] >> 8)] ^
              s_table_3[(byte)m_state[8]];

            m_temp[6] =
              s_table_0[(byte)(m_state[5] >> 24)] ^
              s_table_1[(byte)(m_state[4] >> 16)] ^
              s_table_2[(byte)(m_state[2] >> 8)] ^
              s_table_3[(byte)m_state[9]];

            m_temp[7] =
              s_table_0[(byte)(m_state[6] >> 24)] ^
              s_table_1[(byte)(m_state[5] >> 16)] ^
              s_table_2[(byte)(m_state[3] >> 8)] ^
              s_table_3[(byte)m_state[10]];

            m_temp[8] =
              s_table_0[(byte)(m_state[7] >> 24)] ^
              s_table_1[(byte)(m_state[6] >> 16)] ^
              s_table_2[(byte)(m_state[4] >> 8)] ^
              s_table_3[(byte)m_state[11]];

            m_temp[9] =
              s_table_0[(byte)(m_state[8] >> 24)] ^
              s_table_1[(byte)(m_state[7] >> 16)] ^
              s_table_2[(byte)(m_state[5] >> 8)] ^
              s_table_3[(byte)m_state[12]];

            m_temp[10] =
              s_table_0[(byte)(m_state[9] >> 24)] ^
              s_table_1[(byte)(m_state[8] >> 16)] ^
              s_table_2[(byte)(m_state[6] >> 8)] ^
              s_table_3[(byte)m_state[0]];

            m_temp[11] =
              s_table_0[(byte)(m_state[10] >> 24)] ^
              s_table_1[(byte)(m_state[9] >> 16)] ^
              s_table_2[(byte)(m_state[7] >> 8)] ^
              s_table_3[(byte)m_state[1]];

            m_temp[12] =
              s_table_0[(byte)(m_state[11] >> 24)] ^
              s_table_1[(byte)(m_state[10] >> 16)] ^
              s_table_2[(byte)(m_state[8] >> 8)] ^
              s_table_3[(byte)m_state[2]];

            uint[] u = m_temp;
            m_temp = m_state;
            m_state = u;
        }
    }
}