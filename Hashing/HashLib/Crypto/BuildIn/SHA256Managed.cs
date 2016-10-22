using System;

namespace HashLib.Crypto.BuildIn
{
    internal class SHA256Managed : HashCryptoBuildIn, IHasHMACBuildIn
    {
        public SHA256Managed() 
            : base(new System.Security.Cryptography.SHA256Managed(), 64)
        {
        }

        public virtual System.Security.Cryptography.HMAC GetBuildHMAC()
        {
            return new System.Security.Cryptography.HMACSHA256();
        }
    }
}
