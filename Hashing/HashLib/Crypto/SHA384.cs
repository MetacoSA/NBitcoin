using System;

namespace HashLib.Crypto
{
    internal class SHA384 : SHA512Base
    {
        public SHA384()
            : base(48)
        {
        }

        protected override byte[] GetResult()
        {
            return Converters.ConvertULongsToBytesSwapOrder(m_state, 0, 6);
        }

        public override void Initialize()
        {
            m_state[0] = 0xcbbb9d5dc1059ed8;
            m_state[1] = 0x629a292a367cd507;
            m_state[2] = 0x9159015a3070dd17;
            m_state[3] = 0x152fecd8f70e5939;
            m_state[4] = 0x67332667ffc00b31;
            m_state[5] = 0x8eb44a8768581511;
            m_state[6] = 0xdb0c2e0d64f98fa7;
            m_state[7] = 0x47b5481dbefa4fa4;

            base.Initialize();
        }
    }
}
