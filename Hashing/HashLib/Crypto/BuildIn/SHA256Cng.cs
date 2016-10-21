using System;

namespace HashLib.Crypto.BuildIn
{
    internal class SHA256Cng : HashCryptoBuildIn
    {
        public SHA256Cng() 
            : base(new System.Security.Cryptography.SHA256Cng(), 64)
        {
        }
    }
}
