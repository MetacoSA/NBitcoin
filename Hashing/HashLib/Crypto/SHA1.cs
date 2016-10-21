using System;

namespace HashLib.Crypto
{
    internal class SHA1 : SHA0
    {
        public SHA1() 
        {
        }

        protected override void Expand(uint[] a_data)
        {
            for (int i = 16; i < 80; i++)
            {
                uint T = a_data[i - 3] ^ a_data[i - 8] ^ a_data[i - 14] ^ a_data[i - 16];
                a_data[i] = ((T << 1) | (T >> 31));
            }
        }
    }
}