using System;

namespace HashLib.Crypto
{
    internal abstract class SHA512Base : BlockHash, ICryptoNotBuildIn
    {
        #region Consts
        protected static readonly ulong[] s_K =
        {
             0x428a2f98d728ae22, 0x7137449123ef65cd, 0xb5c0fbcfec4d3b2f, 0xe9b5dba58189dbbc,
             0x3956c25bf348b538, 0x59f111f1b605d019, 0x923f82a4af194f9b, 0xab1c5ed5da6d8118,
             0xd807aa98a3030242, 0x12835b0145706fbe, 0x243185be4ee4b28c, 0x550c7dc3d5ffb4e2,
             0x72be5d74f27b896f, 0x80deb1fe3b1696b1, 0x9bdc06a725c71235, 0xc19bf174cf692694,
             0xe49b69c19ef14ad2, 0xefbe4786384f25e3, 0x0fc19dc68b8cd5b5, 0x240ca1cc77ac9c65,
             0x2de92c6f592b0275, 0x4a7484aa6ea6e483, 0x5cb0a9dcbd41fbd4, 0x76f988da831153b5,
             0x983e5152ee66dfab, 0xa831c66d2db43210, 0xb00327c898fb213f, 0xbf597fc7beef0ee4,
             0xc6e00bf33da88fc2, 0xd5a79147930aa725, 0x06ca6351e003826f, 0x142929670a0e6e70,
             0x27b70a8546d22ffc, 0x2e1b21385c26c926, 0x4d2c6dfc5ac42aed, 0x53380d139d95b3df,
             0x650a73548baf63de, 0x766a0abb3c77b2a8, 0x81c2c92e47edaee6, 0x92722c851482353b,
             0xa2bfe8a14cf10364, 0xa81a664bbc423001, 0xc24b8b70d0f89791, 0xc76c51a30654be30,
             0xd192e819d6ef5218, 0xd69906245565a910, 0xf40e35855771202a, 0x106aa07032bbd1b8,
             0x19a4c116b8d2d0c8, 0x1e376c085141ab53, 0x2748774cdf8eeb99, 0x34b0bcb5e19b48a8,
             0x391c0cb3c5c95a63, 0x4ed8aa4ae3418acb, 0x5b9cca4f7763e373, 0x682e6ff3d6b2b8a3,
             0x748f82ee5defb2fc, 0x78a5636f43172f60, 0x84c87814a1f0ab72, 0x8cc702081a6439ec,
             0x90befffa23631e28, 0xa4506cebde82bde9, 0xbef9a3f7b2c67915, 0xc67178f2e372532b,
             0xca273eceea26619c, 0xd186b8c721c0c207, 0xeada7dd6cde0eb1e, 0xf57d4f7fee6ed178,
             0x06f067aa72176fba, 0x0a637dc5a2c898a6, 0x113f9804bef90dae, 0x1b710b35131c471b,
             0x28db77f523047d84, 0x32caab7b40c72493, 0x3c9ebe0a15c9bebc, 0x431d67c49c100d4c,
             0x4cc5d4becb3e42b6, 0x597f299cfc657e2a, 0x5fcb6fab3ad6faec, 0x6c44198c4a475817
        };
        #endregion

        protected readonly ulong[] m_state = new ulong[8];

        protected SHA512Base(int a_hash_size) 
            : base(a_hash_size, 128)
        {
            Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void Finish()
        {
            ulong lowBits = m_processed_bytes << 3;
            ulong hiBits = m_processed_bytes >> 61;

            int padindex = (m_buffer.Pos < 112) ? (111 - m_buffer.Pos) : (239 - m_buffer.Pos);

            padindex++;

            byte[] pad = new byte[padindex + 16];
            pad[0] = 0x80;

            Converters.ConvertULongToBytesSwapOrder(hiBits, pad, padindex);
            padindex += 8;

            Converters.ConvertULongToBytesSwapOrder(lowBits, pad, padindex);
            padindex += 8;

            TransformBytes(pad, 0, padindex);
        }

        protected override void TransformBlock(byte[] a_data, int a_index)
        {
            ulong[] data = new ulong[80];
            Converters.ConvertBytesToULongsSwapOrder(a_data, a_index, BlockSize, data);

            for (int i = 16; i <= 79; ++i)
            {
                ulong T0 = data[i - 15];
                ulong T1 = data[i - 2];

                data[i] = (((T1 << 45) | (T1 >> 19)) ^ ((T1 << 3) | (T1 >> 61)) ^ (T1 >> 6)) + data[i - 7] + 
                          (((T0 << 63) | (T0 >> 1)) ^ ((T0 << 56)| (T0 >> 8)) ^ (T0 >> 7)) + data[i - 16];
            }

            ulong a = m_state[0];
            ulong b = m_state[1];
            ulong c = m_state[2];
            ulong d = m_state[3];
            ulong e = m_state[4];
            ulong f = m_state[5];
            ulong g = m_state[6];
            ulong h = m_state[7];

            for(int i = 0, t = 0; i < 10; i ++)
            {
                h += s_K[t] + data[t++] + (((e << 50) | (e >> 14)) ^ ((e << 46) | (e >> 18)) ^ ((e << 23) | (e >> 41))) + 
                     ((e & f) ^ (~e & g));
                d += h;
                h += (((a << 36) | (a >> 28)) ^ ((a << 30) | (a >> 34)) ^ ((a << 25) | (a >> 39))) + 
                     ((a & b) ^ (a & c) ^ (b & c));

                g += s_K[t] + data[t++] + (((d << 50) | (d >> 14)) ^ ((d << 46) | (d >> 18)) ^ ((d << 23) | (d >> 41))) + 
                     ((d & e) ^ (~d & f));
                c += g;
                g += (((h << 36) | (h >> 28)) ^ ((h << 30) | (h >> 34)) ^ ((h << 25) | (h >> 39))) + 
                     ((h & a) ^ (h & b) ^ (a & b));

                f += s_K[t] + data[t++] + (((c << 50) | (c >> 14)) ^ ((c << 46) | (c >> 18)) ^ ((c << 23) | (c >> 41))) + 
                     ((c & d) ^ (~c & e));
                b += f;
                f += (((g << 36) | (g >> 28)) ^ ((g << 30) | (g >> 34)) ^ ((g << 25) | (g >> 39))) + 
                     ((g & h) ^ (g & a) ^ (h & a));

                e += s_K[t] + data[t++] + (((b << 50) | (b >> 14)) ^ ((b << 46) | (b >> 18)) ^ ((b << 23) | (b >> 41))) + 
                     ((b & c) ^ (~b & d));
                a += e;
                e += (((f << 36) | (f >> 28)) ^ ((f << 30) | (f >> 34)) ^ ((f << 25) | (f >> 39))) + 
                     ((f & g) ^ (f & h) ^ (g & h));

                d += s_K[t] + data[t++] + (((a << 50) | (a >> 14)) ^ ((a << 46) | (a >> 18)) ^ ((a << 23) | (a >> 41))) + 
                     ((a & b) ^ (~a & c));
                h += d;
                d += (((e << 36) | (e >> 28)) ^ ((e << 30) | (e >> 34)) ^ ((e << 25) | (e >> 39))) + 
                     ((e & f) ^ (e & g) ^ (f & g));

                c += s_K[t] + data[t++] + (((h << 50) | (h >> 14)) ^ ((h << 46) | (h >> 18)) ^ ((h << 23) | (h >> 41))) + 
                     ((h & a) ^ (~h & b));
                g += c;
                c += (((d << 36) | (d >> 28)) ^ ((d << 30) | (d >> 34)) ^ ((d << 25) | (d >> 39))) + 
                     ((d & e) ^ (d & f) ^ (e & f));

                b += s_K[t] + data[t++] + (((g << 50) | (g >> 14)) ^ ((g << 46) | (g >> 18)) ^ ((g << 23) | (g >> 41))) + 
                     ((g & h) ^ (~g & a));
                f += b;
                b += (((c << 36) | (c >> 28)) ^ ((c << 30) | (c >> 34)) ^ ((c << 25) | (c >> 39))) + 
                     ((c & d) ^ (c & e) ^ (d & e));

                a += s_K[t] + data[t++] + (((f << 50) | (f >> 14)) ^ ((f << 46) | (f >> 18)) ^ ((f << 23) | (f >> 41))) + 
                     ((f & g) ^ (~f & h));
                e += a;
                a += (((b << 36) | (b >> 28)) ^ ((b << 30) | (b >> 34)) ^ ((b << 25) | (b >> 39))) + 
                     ((b & c) ^ (b & d) ^ (c & d));
            }

            m_state[0] += a;
            m_state[1] += b;
            m_state[2] += c;
            m_state[3] += d;
            m_state[4] += e;
            m_state[5] += f;
            m_state[6] += g;
            m_state[7] += h;
        }
    }
}
