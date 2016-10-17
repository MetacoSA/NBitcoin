using System;

namespace HashLib.Crypto
{
    internal class SHA256 : SHA256Base
    {
        public SHA256()
            : base(32)
        {
        }

        public override void Initialize()
        {
            m_state[0] = 0x6a09e667;
            m_state[1] = 0xbb67ae85;
            m_state[2] = 0x3c6ef372;
            m_state[3] = 0xa54ff53a;
            m_state[4] = 0x510e527f;
            m_state[5] = 0x9b05688c;
            m_state[6] = 0x1f83d9ab;
            m_state[7] = 0x5be0cd19;


            base.Initialize();
        }

        protected override byte[] GetResult()
        {
            return Converters.ConvertUIntsToBytesSwapOrder(m_state);
        }
    }
}
