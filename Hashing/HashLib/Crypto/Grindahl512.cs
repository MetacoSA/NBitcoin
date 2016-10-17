using System;

namespace HashLib.Crypto
{
    internal class Grindahl512 : BlockHash, ICryptoNotBuildIn
    {
        #region Consts

        private const int ROWS = 8;
        private const int COLUMNS = 13;
        private const int BLANK_ROUNDS = 8;

        static readonly ulong[] s_table_0;
        static readonly ulong[] s_table_1;
        static readonly ulong[] s_table_2;
        static readonly ulong[] s_table_3;
        static readonly ulong[] s_table_4;
        static readonly ulong[] s_table_5;
        static readonly ulong[] s_table_6;
        static readonly ulong[] s_table_7;

        static readonly ulong[] s_master_table =
        {
            0xc6636397633551a2, 0xf87c7ceb7ccd1326, 0xee7777c777952952, 0xf67b7bf77bf50102, 
            0xfff2f2e5f2d11a34, 0xd66b6bb76b7561c2, 0xde6f6fa76f5579f2, 0x91c5c539c572a84b, 
            0x603030c0309ba05b, 0x020101040108060c, 0xce67678767154992, 0x562b2bac2b43faef, 
            0xe7fefed5feb13264, 0xb5d7d771d7e2c493, 0x4dabab9aab2fd7b5, 0xec7676c3769d2f5e, 
            0x8fcaca05ca0a8a0f, 0x1f82823e827c2142, 0x89c9c909c912801b, 0xfa7d7def7dc5152a, 
            0xeffafac5fa912a54, 0xb259597f59fecd81, 0x8e474707470e8909, 0xfbf0f0edf0c1162c, 
            0x41adad82ad1fc39d, 0xb3d4d47dd4face87, 0x5fa2a2bea267e1d9, 0x45afaf8aaf0fcf85, 
            0x239c9c469c8c65ca, 0x53a4a4a6a457f5f1, 0xe47272d372bd376e, 0x9bc0c02dc05ab677, 
            0x75b7b7eab7cf9f25, 0xe1fdfdd9fda93870, 0x3d93937a93f4478e, 0x4c262698262bd4b3, 
            0x6c3636d836abb473, 0x7e3f3ffc3fe3821f, 0xf5f7f7f1f7f90408, 0x83cccc1dcc3a9e27, 
            0x683434d034bbb86b, 0x51a5a5a2a55ff3fd, 0xd1e5e5b9e56968d0, 0xf9f1f1e9f1c91020, 
            0xe27171df71a53d7a, 0xabd8d84dd89ae6d7, 0x623131c43193a657, 0x2a15155415a87efc, 
            0x0804041004201830, 0x95c7c731c762a453, 0x4623238c2303ca8f, 0x9dc3c321c342bc63, 
            0x3018186018c050a0, 0x3796966e96dc59b2, 0x0a05051405281e3c, 0x2f9a9a5e9abc71e2, 
            0x0e07071c07381224, 0x2412124812906cd8, 0x1b808036806c2d5a, 0xdfe2e2a5e2517af4, 
            0xcdebeb81eb194c98, 0x4e27279c2723d2bf, 0x7fb2b2feb2e78119, 0xea7575cf7585254a, 
            0x120909240948366c, 0x1d83833a8374274e, 0x582c2cb02c7be8cb, 0x341a1a681ad05cb8, 
            0x361b1b6c1bd85ab4, 0xdc6e6ea36e5d7ffe, 0xb45a5a735ae6c795, 0x5ba0a0b6a077edc1, 
            0xa452525352a6f7f5, 0x763b3bec3bc39a2f, 0xb7d6d675d6eac29f, 0x7db3b3fab3ef8715, 
            0x522929a42953f6f7, 0xdde3e3a1e3597cf8, 0x5e2f2fbc2f63e2df, 0x13848426844c356a, 
            0xa653535753aef1f9, 0xb9d1d169d1d2d0bb, 0x0000000000000000, 0xc1eded99ed2958b0, 
            0x40202080201bc09b, 0xe3fcfcddfca13e7c, 0x79b1b1f2b1ff8b0d, 0xb65b5b775beec199, 
            0xd46a6ab36a7d67ce, 0x8dcbcb01cb028c03, 0x67bebecebe87a949, 0x723939e439d39637, 
            0x944a4a334a66a755, 0x984c4c2b4c56b37d, 0xb058587b58f6cb8d, 0x85cfcf11cf229433, 
            0xbbd0d06dd0dad6b7, 0xc5efef91ef3954a8, 0x4faaaa9eaa27d1b9, 0xedfbfbc1fb992c58, 
            0x86434317432e9139, 0x9a4d4d2f4d5eb571, 0x663333cc3383aa4f, 0x1185852285443366, 
            0x8a45450f451e8511, 0xe9f9f9c9f9892040, 0x0402020802100c18, 0xfe7f7fe77fd51932, 
            0xa050505b50b6fbed, 0x783c3cf03cfb880b, 0x259f9f4a9f946fde, 0x4ba8a896a837dda1, 
            0xa251515f51befde1, 0x5da3a3baa36fe7d5, 0x8040401b40369b2d, 0x058f8f0a8f140f1e, 
            0x3f92927e92fc4182, 0x219d9d429d8463c6, 0x703838e038db903b, 0xf1f5f5f9f5e90810, 
            0x63bcbcc6bc97a551, 0x77b6b6eeb6c79929, 0xafdada45da8aeacf, 0x422121842113c697, 
            0x20101040108060c0, 0xe5ffffd1ffb93468, 0xfdf3f3e1f3d91c38, 0xbfd2d265d2cadaaf, 
            0x81cdcd19cd32982b, 0x180c0c300c602850, 0x2613134c13986ad4, 0xc3ecec9dec215ebc, 
            0xbe5f5f675fced9a9, 0x3597976a97d45fbe, 0x8844440b4416831d, 0x2e17175c17b872e4, 
            0x93c4c43dc47aae47, 0x55a7a7aaa74fffe5, 0xfc7e7ee37edd1f3e, 0x7a3d3df43df38e07, 
            0xc864648b640d4386, 0xba5d5d6f5dded5b1, 0x3219196419c856ac, 0xe67373d773b53162, 
            0xc060609b602d5bb6, 0x1981813281642b56, 0x9e4f4f274f4eb969, 0xa3dcdc5ddcbafee7, 
            0x44222288220bcc83, 0x542a2aa82a4bfce3, 0x3b90907690ec4d9a, 0x0b888816882c1d3a, 
            0x8c46460346068f05, 0xc7eeee95ee3152a4, 0x6bb8b8d6b8b7bd61, 0x2814145014a078f0, 
            0xa7dede55deaaf2ff, 0xbc5e5e635ec6dfa5, 0x160b0b2c0b583a74, 0xaddbdb41db82ecc3, 
            0xdbe0e0ade04176ec, 0x643232c8328bac43, 0x743a3ae83acb9c23, 0x140a0a280a503c78, 
            0x9249493f497ead41, 0x0c06061806301428, 0x48242490243bd8ab, 0xb85c5c6b5cd6d3bd, 
            0x9fc2c225c24aba6f, 0xbdd3d361d3c2dca3, 0x43acac86ac17c591, 0xc4626293623d57ae, 
            0x3991917291e44b96, 0x3195956295c453a6, 0xd3e4e4bde4616edc, 0xf27979ff79e50d1a, 
            0xd5e7e7b1e77964c8, 0x8bc8c80dc81a8617, 0x6e3737dc37a3b27f, 0xda6d6daf6d4575ea, 
            0x018d8d028d040306, 0xb1d5d579d5f2c88b, 0x9c4e4e234e46bf65, 0x49a9a992a93fdbad, 
            0xd86c6cab6c4d73e6, 0xac5656435686efc5, 0xf3f4f4fdf4e10e1c, 0xcfeaea85ea114a94, 
            0xca65658f6505458a, 0xf47a7af37afd070e, 0x47aeae8eae07c989, 0x1008082008403060, 
            0x6fbabadebaa7b179, 0xf07878fb78ed0b16, 0x4a2525942533dea7, 0x5c2e2eb82e6be4d3, 
            0x381c1c701ce04890, 0x57a6a6aea647f9e9, 0x73b4b4e6b4d79531, 0x97c6c635c66aa25f, 
            0xcbe8e88de801468c, 0xa1dddd59ddb2f8eb, 0xe87474cb748d2346, 0x3e1f1f7c1ff84284, 
            0x964b4b374b6ea159, 0x61bdbdc2bd9fa35d, 0x0d8b8b1a8b34172e, 0x0f8a8a1e8a3c1122, 
            0xe07070db70ad3b76, 0x7c3e3ef83eeb8413, 0x71b5b5e2b5df933d, 0xcc666683661d4f9e, 
            0x9048483b4876ab4d, 0x0603030c03180a14, 0xf7f6f6f5f6f10204, 0x1c0e0e380e702448, 
            0xc261619f61255dba, 0x6a3535d435b3be67, 0xae575747578ee9c9, 0x69b9b9d2b9bfbb6d, 
            0x1786862e865c3972, 0x99c1c129c152b07b, 0x3a1d1d741de84e9c, 0x279e9e4e9e9c69d2, 
            0xd9e1e1a9e14970e0, 0xebf8f8cdf881264c, 0x2b98985698ac7dfa, 0x22111144118866cc, 
            0xd26969bf69656dda, 0xa9d9d949d992e0db, 0x078e8e0e8e1c0912, 0x3394946694cc55aa, 
            0x2d9b9b5a9bb477ee, 0x3c1e1e781ef04488, 0x1587872a87543f7e, 0xc9e9e989e9094080, 
            0x87cece15ce2a923f, 0xaa55554f559ee5d1, 0x502828a0285bf0fb, 0xa5dfdf51dfa2f4f3, 
            0x038c8c068c0c050a, 0x59a1a1b2a17febcd, 0x0989891289241b36, 0x1a0d0d340d682e5c, 
            0x65bfbfcabf8faf45, 0xd7e6e6b5e67162c4, 0x8442421342269735, 0xd06868bb686d6bd6, 
            0x8241411f413e9d21, 0x2999995299a47bf6, 0x5a2d2db42d73eec7, 0x1e0f0f3c0f782244, 
            0x7bb0b0f6b0f78d01, 0xa854544b5496e3dd, 0x6dbbbbdabbafb775, 0x2c16165816b074e8
        };

        static Grindahl512()
        {
            s_table_0 = s_master_table;
            s_table_1 = CalcTable(1);
            s_table_2 = CalcTable(2);
            s_table_3 = CalcTable(3);
            s_table_4 = CalcTable(4);
            s_table_5 = CalcTable(5);
            s_table_6 = CalcTable(6);
            s_table_7 = CalcTable(7);
        }

        private static ulong[] CalcTable(int i)
        {
            var result = new ulong[256];
            for (int j = 0; j < 256; j++)
                result[j] = Bits.RotateRight(s_master_table[j], i * 8);
            return result;
        }

        #endregion

        private ulong[] m_state = new ulong[ROWS * COLUMNS / 8];
        private ulong[] m_temp = new ulong[ROWS * COLUMNS / 8];

        public Grindahl512()
            : base(64, 8)
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
            int padding_size = 2 * BlockSize - (int)(m_processed_bytes % (uint)BlockSize);
            ulong msg_length = (m_processed_bytes / (ulong)ROWS) + 1;
            byte[] pad = new byte[padding_size];
            pad[0] = 0x80;

            Converters.ConvertULongToBytesSwapOrder(msg_length, pad, padding_size - 8);
            TransformBytes(pad, 0, padding_size - BlockSize);

            m_state[0] = Converters.ConvertBytesToULongSwapOrder(pad, padding_size - BlockSize);
            InjectMsg(true);

            for (int i = 0; i < BLANK_ROUNDS; i++)
                InjectMsg(true);
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            m_state[0] = Converters.ConvertBytesToULongSwapOrder(a_data, a_index);
            InjectMsg(false);
        }

        protected override byte[] GetResult()
        {
            return Converters.ConvertULongsToBytesSwapOrder(m_state, COLUMNS - HashSize / ROWS, HashSize / ROWS);
        }

        private void InjectMsg(bool a_full_process)
        {
            m_state[ROWS * COLUMNS / 8 - 1] ^= 0x01;

            if (a_full_process)
            {
                m_temp[0] = 
                    s_table_0[(byte)(m_state[12] >> 56)] ^
                    s_table_1[(byte)(m_state[11] >> 48)] ^
                    s_table_2[(byte)(m_state[10] >> 40)] ^
                    s_table_3[(byte)(m_state[9] >> 32)] ^
                    s_table_4[(byte)(m_state[8] >> 24)] ^
                    s_table_5[(byte)(m_state[7] >> 16)] ^
                    s_table_6[(byte)(m_state[6] >> 8)] ^
                    s_table_7[(byte)(m_state[5])];
            }

            m_temp[1] = 
              s_table_0[(byte)(m_state[0] >> 56)] ^
              s_table_1[(byte)(m_state[12] >> 48)] ^
              s_table_2[(byte)(m_state[11] >> 40)] ^
              s_table_3[(byte)(m_state[10] >> 32)] ^
              s_table_4[(byte)(m_state[9] >> 24)] ^
              s_table_5[(byte)(m_state[8] >> 16)] ^
              s_table_6[(byte)(m_state[7] >> 8)] ^
              s_table_7[(byte)(m_state[6])];

            m_temp[2] = 
              s_table_0[(byte)(m_state[1] >> 56)] ^
              s_table_1[(byte)(m_state[0] >> 48)] ^
              s_table_2[(byte)(m_state[12] >> 40)] ^
              s_table_3[(byte)(m_state[11] >> 32)] ^
              s_table_4[(byte)(m_state[10] >> 24)] ^
              s_table_5[(byte)(m_state[9] >> 16)] ^
              s_table_6[(byte)(m_state[8] >> 8)] ^
              s_table_7[(byte)(m_state[7])];

            m_temp[3] = 
              s_table_0[(byte)(m_state[2] >> 56)] ^
              s_table_1[(byte)(m_state[1] >> 48)] ^
              s_table_2[(byte)(m_state[0] >> 40)] ^
              s_table_3[(byte)(m_state[12] >> 32)] ^
              s_table_4[(byte)(m_state[11] >> 24)] ^
              s_table_5[(byte)(m_state[10] >> 16)] ^
              s_table_6[(byte)(m_state[9] >> 8)] ^
              s_table_7[(byte)(m_state[8])];

            m_temp[4] = 
              s_table_0[(byte)(m_state[3] >> 56)] ^
              s_table_1[(byte)(m_state[2] >> 48)] ^
              s_table_2[(byte)(m_state[1] >> 40)] ^
              s_table_3[(byte)(m_state[0] >> 32)] ^
              s_table_4[(byte)(m_state[12] >> 24)] ^
              s_table_5[(byte)(m_state[11] >> 16)] ^
              s_table_6[(byte)(m_state[10] >> 8)] ^
              s_table_7[(byte)(m_state[9])];

            m_temp[5] = 
              s_table_0[(byte)(m_state[4] >> 56)] ^
              s_table_1[(byte)(m_state[3] >> 48)] ^
              s_table_2[(byte)(m_state[2] >> 40)] ^
              s_table_3[(byte)(m_state[1] >> 32)] ^
              s_table_4[(byte)(m_state[0] >> 24)] ^
              s_table_5[(byte)(m_state[12] >> 16)] ^
              s_table_6[(byte)(m_state[11] >> 8)] ^
              s_table_7[(byte)(m_state[10])];

            m_temp[6] = 
              s_table_0[(byte)(m_state[5] >> 56)] ^
              s_table_1[(byte)(m_state[4] >> 48)] ^
              s_table_2[(byte)(m_state[3] >> 40)] ^
              s_table_3[(byte)(m_state[2] >> 32)] ^
              s_table_4[(byte)(m_state[1] >> 24)] ^
              s_table_5[(byte)(m_state[0] >> 16)] ^
              s_table_6[(byte)(m_state[12] >> 8)] ^
              s_table_7[(byte)(m_state[11])];

            m_temp[7] = 
              s_table_0[(byte)(m_state[6] >> 56)] ^
              s_table_1[(byte)(m_state[5] >> 48)] ^
              s_table_2[(byte)(m_state[4] >> 40)] ^
              s_table_3[(byte)(m_state[3] >> 32)] ^
              s_table_4[(byte)(m_state[2] >> 24)] ^
              s_table_5[(byte)(m_state[1] >> 16)] ^
              s_table_6[(byte)(m_state[0] >> 8)] ^
              s_table_7[(byte)(m_state[12])];

            m_temp[8] = 
              s_table_0[(byte)(m_state[7] >> 56)] ^
              s_table_1[(byte)(m_state[6] >> 48)] ^
              s_table_2[(byte)(m_state[5] >> 40)] ^
              s_table_3[(byte)(m_state[4] >> 32)] ^
              s_table_4[(byte)(m_state[3] >> 24)] ^
              s_table_5[(byte)(m_state[2] >> 16)] ^
              s_table_6[(byte)(m_state[1] >> 8)] ^
              s_table_7[(byte)(m_state[0])];

            m_temp[9] = 
              s_table_0[(byte)(m_state[8] >> 56)] ^
              s_table_1[(byte)(m_state[7] >> 48)] ^
              s_table_2[(byte)(m_state[6] >> 40)] ^
              s_table_3[(byte)(m_state[5] >> 32)] ^
              s_table_4[(byte)(m_state[4] >> 24)] ^
              s_table_5[(byte)(m_state[3] >> 16)] ^
              s_table_6[(byte)(m_state[2] >> 8)] ^
              s_table_7[(byte)(m_state[1])];

            m_temp[10] = 
              s_table_0[(byte)(m_state[9] >> 56)] ^
              s_table_1[(byte)(m_state[8] >> 48)] ^
              s_table_2[(byte)(m_state[7] >> 40)] ^
              s_table_3[(byte)(m_state[6] >> 32)] ^
              s_table_4[(byte)(m_state[5] >> 24)] ^
              s_table_5[(byte)(m_state[4] >> 16)] ^
              s_table_6[(byte)(m_state[3] >> 8)] ^
              s_table_7[(byte)(m_state[2])];

            m_temp[11] = 
              s_table_0[(byte)(m_state[10] >> 56)] ^
              s_table_1[(byte)(m_state[9] >> 48)] ^
              s_table_2[(byte)(m_state[8] >> 40)] ^
              s_table_3[(byte)(m_state[7] >> 32)] ^
              s_table_4[(byte)(m_state[6] >> 24)] ^
              s_table_5[(byte)(m_state[5] >> 16)] ^
              s_table_6[(byte)(m_state[4] >> 8)] ^
              s_table_7[(byte)(m_state[3])];

            m_temp[12] = 
              s_table_0[(byte)(m_state[11] >> 56)] ^
              s_table_1[(byte)(m_state[10] >> 48)] ^
              s_table_2[(byte)(m_state[9] >> 40)] ^
              s_table_3[(byte)(m_state[8] >> 32)] ^
              s_table_4[(byte)(m_state[7] >> 24)] ^
              s_table_5[(byte)(m_state[6] >> 16)] ^
              s_table_6[(byte)(m_state[5] >> 8)] ^
              s_table_7[(byte)(m_state[4])];

            ulong[] u = m_temp;
            m_temp = m_state;
            m_state = u;
        }
    }
}