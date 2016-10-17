using System;

namespace HashLib.Crypto.BuildIn
{
    internal class SHA1Managed : HashCryptoBuildIn, IHasHMACBuildIn
    {
        public SHA1Managed()
            : base(new System.Security.Cryptography.SHA1Managed(), 64)
        {
        }

        public virtual System.Security.Cryptography.HMAC GetBuildHMAC()
        {
            return new System.Security.Cryptography.HMACSHA1(new byte[0], true);
        }
    }
}
