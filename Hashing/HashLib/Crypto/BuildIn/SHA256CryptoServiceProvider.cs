#if !NETCORE

using System;

namespace HashLib.Crypto.BuildIn
{
    internal class SHA256CryptoServiceProvider : HashCryptoBuildIn
    {
        public SHA256CryptoServiceProvider() 
            : base(new System.Security.Cryptography.SHA256CryptoServiceProvider(), 64)
        {
        }
    }
}
#endif