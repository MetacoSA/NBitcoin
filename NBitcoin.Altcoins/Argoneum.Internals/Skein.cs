using System;
using NBitcoin.Crypto;
using NBitcoin.Altcoins.HashX11;

namespace NBitcoin.Altcoins.ArgoneumInternals
{
    public class Skein
    {
        private IHash skein512;

        public Skein()
        {
            skein512 = HashFactory.Crypto.SHA3.CreateSkein512_Custom();
        }

        public byte[] ComputeBytes(byte[] input)
        {
            var skeinResult = skein512.ComputeBytes(input).GetBytes();
            return Hashes.SHA256(skeinResult);
        }
    }
}
