using System;

namespace HashLib.Crypto.BuildIn
{
    internal class MD5CryptoServiceProvider : HashCryptoBuildIn, IHasHMACBuildIn
    {
        public MD5CryptoServiceProvider() 
            : base(new System.Security.Cryptography.MD5CryptoServiceProvider(), 64)
        {
        }

        public virtual System.Security.Cryptography.HMAC GetBuildHMAC()
        {
            return new System.Security.Cryptography.HMACMD5();
        }
    }
}
