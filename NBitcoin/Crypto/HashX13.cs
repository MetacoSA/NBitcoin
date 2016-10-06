using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Crypto
{
    using System.Threading;

    using HashLib;

    public sealed class HashX13
    {
        private readonly List<IHash> hashers;

        private static readonly Lazy<HashX13> SingletonInstance = new Lazy<HashX13>(LazyThreadSafetyMode.PublicationOnly);

        public HashX13()
        {
            this.hashers = new List<IHash>
            {
                HashFactory.Crypto.SHA3.CreateBlake512(),
                HashFactory.Crypto.SHA3.CreateBlueMidnightWish512(),
                HashFactory.Crypto.SHA3.CreateGroestl512(),
                HashFactory.Crypto.SHA3.CreateSkein512(),
                HashFactory.Crypto.SHA3.CreateJH512(),
                HashFactory.Crypto.SHA3.CreateKeccak512(),
                HashFactory.Crypto.SHA3.CreateLuffa512(),
                HashFactory.Crypto.SHA3.CreateCubeHash512(),
                HashFactory.Crypto.SHA3.CreateSHAvite3_512(),
                HashFactory.Crypto.SHA3.CreateSIMD512(),
                HashFactory.Crypto.SHA3.CreateEcho512(),
                HashFactory.Crypto.SHA3.CreateHamsi512(),
                HashFactory.Crypto.SHA3.CreateFugue512(),
            };

            this.Multiplier = 1;
        }

        public uint Multiplier { get; private set; }

        public static HashX13 Instance => SingletonInstance.Value;

        public uint256 Hash(byte[] input)
        {
            var buffer = input;

            foreach (var hasher in this.hashers)
            {
                buffer = hasher.ComputeBytes(buffer).GetBytes();
            }

            return new uint256(buffer);
        }
    }
}
