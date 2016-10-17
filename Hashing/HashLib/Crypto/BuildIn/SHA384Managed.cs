using System;

namespace HashLib.Crypto.BuildIn
{
    internal class SHA384Managed : HashCryptoBuildIn, IHasHMACBuildIn
    {
        public SHA384Managed() 
            : base(new System.Security.Cryptography.SHA384Managed(), 128)
        {
        }

        public virtual System.Security.Cryptography.HMAC GetBuildHMAC()
        {
            return new System.Security.Cryptography.HMACSHA384();
        }
    }
}
