using System;

namespace HashLib.Crypto.BuildIn
{
    internal class SHA512Cng : HashCryptoBuildIn
    {
        public SHA512Cng() 
            : base(new System.Security.Cryptography.SHA512Cng(), 128)
        {
        }
    }
}
