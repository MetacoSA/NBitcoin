#if !NETCORE
using System;

namespace HashLib.Crypto.BuildIn
{
    internal class SHA1CryptoServiceProvider : HashCryptoBuildIn, IHasHMACBuildIn
    {
        public SHA1CryptoServiceProvider() 
            : base(new System.Security.Cryptography.SHA1CryptoServiceProvider(), 64)
        {
        }

        public virtual System.Security.Cryptography.HMAC GetBuildHMAC()
        {
            return new System.Security.Cryptography.HMACSHA1(new byte[0], false);
        }
    }
}
#endif