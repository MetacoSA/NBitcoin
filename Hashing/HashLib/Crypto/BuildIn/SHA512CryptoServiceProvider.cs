using System;

namespace HashLib.Crypto.BuildIn
{
    internal class SHA512CryptoServiceProvider : HashCryptoBuildIn
    {
        public SHA512CryptoServiceProvider() 
            : base(new System.Security.Cryptography.SHA512CryptoServiceProvider(), 128)
        {
        }
    }
}
