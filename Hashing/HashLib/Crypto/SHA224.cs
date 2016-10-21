using System;

namespace HashLib.Crypto
{
    internal class SHA224 : SHA256Base
    {
        public SHA224()
            : base(28)
        {
        }

        protected override byte[] GetResult()
        {
            return Converters.ConvertUIntsToBytesSwapOrder(m_state, 0, 7);
        }

        public override void Initialize()
        {
            m_state[0] = 0xc1059ed8;
            m_state[1] = 0x367cd507;
            m_state[2] = 0x3070dd17;
            m_state[3] = 0xf70e5939;
            m_state[4] = 0xffc00b31;
            m_state[5] = 0x68581511;
            m_state[6] = 0x64f98fa7;
            m_state[7] = 0xbefa4fa4;

            base.Initialize();
        }
    }
}
