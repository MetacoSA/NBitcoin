using System;
using NBitcoin.Altcoins.HashX11;

namespace NBitcoin.Altcoins.ArgoneumInternals
{
    public class Phi2
    {
        private IHash cubehash512, jh512, echo512, skein512;
        private Lyra2Broken.Lyra2 lyra2;
        private Gost gost;

        public Phi2()
        {
            cubehash512 = HashFactory.Crypto.SHA3.CreateCubeHash512();
            jh512 = HashFactory.Crypto.SHA3.CreateJH512();
            echo512 = HashFactory.Crypto.SHA3.CreateEcho512();
            skein512 = HashFactory.Crypto.SHA3.CreateSkein512_Custom();
            lyra2 = new Lyra2Broken.Lyra2();
            gost = new Gost();
        }

        public byte[] ComputeHash(byte [] input)
        {
            byte[] hash = cubehash512.ComputeBytes(input).GetBytes();

            byte[] hashA1 = new byte[32];
            byte[] hashA2 = new byte[32];
            byte[] hashB1 = new byte[32];
            byte[] hashB2 = new byte[32];

            Array.Copy(hash, 0, hashB1, 0, 32);
            Array.Copy(hash, 32, hashB2, 0, 32);
            lyra2.Calculate(hashA1, hashB1, hashB1, 1, 8, 8);
            lyra2.Calculate(hashA2, hashB2, hashB2, 1, 8, 8);
            Array.Copy(hashA1, 0, hash, 0, 32);
            Array.Copy(hashA2, 0, hash, 32, 32);

            hash = jh512.ComputeBytes(hash).GetBytes();

            if ((hash[0] & 1) == 1)
            {
                hash = gost.ComputeHash512(hash);
            }
            else
            {
                hash = echo512.ComputeBytes(hash).GetBytes();
                hash = echo512.ComputeBytes(hash).GetBytes();
            }

            hash = skein512.ComputeBytes(hash).GetBytes();

            byte[] hash32 = new byte[32];

            for (int i = 0; i < 32; i++)
            {
                hash32[i] = Convert.ToByte(hash[i] ^ hash[i + 32]);
            }

            return hash32;
        }
    }
}
