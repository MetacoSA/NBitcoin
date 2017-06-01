using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HashLib;

namespace NBitcoin.Crypto
{
	// this hashing class is not thread safe to use with static instances.
    // the hashing objects maintain state during hash calculation.
    // to use in a multi threaded environment create a new instance for every hash.

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
                HashFactory.Crypto.SHA3.CreateSkein512_Custom(),
                HashFactory.Crypto.SHA3.CreateJH512(),
                HashFactory.Crypto.SHA3.CreateKeccak512(),
                HashFactory.Crypto.SHA3.CreateLuffa512(),
                HashFactory.Crypto.SHA3.CreateCubeHash512(),
                HashFactory.Crypto.SHA3.CreateSHAvite3_512_Custom(),
                HashFactory.Crypto.SHA3.CreateSIMD512(),
                HashFactory.Crypto.SHA3.CreateEcho512(),
                HashFactory.Crypto.SHA3.CreateHamsi512(),
                HashFactory.Crypto.SHA3.CreateFugue512(),
            };

            this.Multiplier = 1;
        }

        public uint Multiplier { get; private set; }

        /// <summary>
        /// using the instance method is not thread safe. 
        /// to calling the hashing method in a multi threaded environment use the create() method
        /// </summary>
        public static HashX13 Instance => SingletonInstance.Value;

        public static HashX13 Create()
        {
            return new HashX13();
        }

        public uint256 Hash(byte[] input)
        {
            var buffer = input;

            foreach (var hasher in this.hashers)
            {
                buffer = hasher.ComputeBytes(buffer).GetBytes();
            }

            return new uint256(buffer.Take(32).ToArray());
        }
    }
}
