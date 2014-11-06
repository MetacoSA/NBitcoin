using System;
using System.Threading;

using NBitcoin.BouncyCastle.Crypto;
using NBitcoin.BouncyCastle.Crypto.Digests;
using NBitcoin.BouncyCastle.Crypto.Prng;
using NBitcoin.BouncyCastle.Utilities;

namespace NBitcoin.BouncyCastle.Security
{
    public class SecureRandom
        : Random
    {
        private static long counter = Times.NanoTime();

        private static long NextCounterValue()
        {
            return Interlocked.Increment(ref counter);
        }

#if NETCF_1_0
        private static readonly SecureRandom[] master = { null };
        private static SecureRandom Master
        {
            get
            {
                lock (master)
                {
                    if (master[0] == null)
                    {
                        SecureRandom sr = master[0] = GetInstance("SHA256PRNG", false);

                        // Even though Ticks has at most 8 or 14 bits of entropy, there's no harm in adding it.
                        sr.SetSeed(DateTime.Now.Ticks);

                        // 32 will be enough when ThreadedSeedGenerator is fixed.  Until then, ThreadedSeedGenerator returns low
                        // entropy, and this is not sufficient to be secure. http://www.bouncycastle.org/csharpdevmailarchive/msg00814.html
                        sr.SetSeed(new ThreadedSeedGenerator().GenerateSeed(32, true));
                    }

                    return master[0];
                }
            }
        }
#else
        private static readonly SecureRandom master = new SecureRandom(new CryptoApiRandomGenerator());
        private static SecureRandom Master
        {
            get { return master; }
        }
#endif

        private static DigestRandomGenerator CreatePrng(string digestName, bool autoSeed)
        {
            IDigest digest = DigestUtilities.GetDigest(digestName);
            if (digest == null)
                return null;
            DigestRandomGenerator prng = new DigestRandomGenerator(digest);
            if (autoSeed)
            {
                prng.AddSeedMaterial(NextCounterValue());
                prng.AddSeedMaterial(GetSeed(digest.GetDigestSize()));
            }
            return prng;
        }

        /// <summary>
        /// Create and auto-seed an instance based on the given algorithm.
        /// </summary>
        /// <remarks>Equivalent to GetInstance(algorithm, true)</remarks>
        /// <param name="algorithm">e.g. "SHA256PRNG"</param>
        public static SecureRandom GetInstance(string algorithm)
        {
            return GetInstance(algorithm, true);
        }

        /// <summary>
        /// Create an instance based on the given algorithm, with optional auto-seeding
        /// </summary>
        /// <param name="algorithm">e.g. "SHA256PRNG"</param>
        /// <param name="autoSeed">If true, the instance will be auto-seeded.</param>
        public static SecureRandom GetInstance(string algorithm, bool autoSeed)
        {
            string upper = Platform.ToUpperInvariant(algorithm);
            if (upper.EndsWith("PRNG"))
            {
                string digestName = upper.Substring(0, upper.Length - "PRNG".Length);
                DigestRandomGenerator prng = CreatePrng(digestName, autoSeed);
                if (prng != null)
                {
                    return new SecureRandom(prng);
                }
            }

            throw new ArgumentException("Unrecognised PRNG algorithm: " + algorithm, "algorithm");
        }

        public static byte[] GetSeed(int length)
        {
#if NETCF_1_0
            lock (master)
#endif
            return Master.GenerateSeed(length);
        }

        protected readonly IRandomGenerator generator;

        public SecureRandom()
            : this(CreatePrng("SHA256", true))
        {
        }

        /// <remarks>
        /// To replicate existing predictable output, replace with GetInstance("SHA1PRNG", false), followed by SetSeed(seed)
        /// </remarks>
        [Obsolete("Use GetInstance/SetSeed instead")]
        public SecureRandom(byte[] seed)
            : this(CreatePrng("SHA1", false))
        {
            SetSeed(seed);
        }

        /// <summary>Use the specified instance of IRandomGenerator as random source.</summary>
        /// <remarks>
        /// This constructor performs no seeding of either the <c>IRandomGenerator</c> or the
        /// constructed <c>SecureRandom</c>. It is the responsibility of the client to provide
        /// proper seed material as necessary/appropriate for the given <c>IRandomGenerator</c>
        /// implementation.
        /// </remarks>
        /// <param name="generator">The source to generate all random bytes from.</param>
        public SecureRandom(IRandomGenerator generator)
            : base(0)
        {
            this.generator = generator;
        }

        public virtual byte[] GenerateSeed(int length)
        {
            SetSeed(DateTime.Now.Ticks);

            byte[] rv = new byte[length];
            NextBytes(rv);
            return rv;
        }

        public virtual void SetSeed(byte[] seed)
        {
            generator.AddSeedMaterial(seed);
        }

        public virtual void SetSeed(long seed)
        {
            generator.AddSeedMaterial(seed);
        }

        public override int Next()
        {
            for (;;)
            {
                int i = NextInt() & int.MaxValue;

                if (i != int.MaxValue)
                    return i;
            }
        }

        public override int Next(int maxValue)
        {
            if (maxValue < 2)
            {
                if (maxValue < 0)
                    throw new ArgumentOutOfRangeException("maxValue", "cannot be negative");

                return 0;
            }

            // Test whether maxValue is a power of 2
            if ((maxValue & -maxValue) == maxValue)
            {
                int val = NextInt() & int.MaxValue;
                long lr = ((long) maxValue * (long) val) >> 31;
                return (int) lr;
            }

            int bits, result;
            do
            {
                bits = NextInt() & int.MaxValue;
                result = bits % maxValue;
            }
            while (bits - result + (maxValue - 1) < 0); // Ignore results near overflow

            return result;
        }

        public override int Next(int minValue, int maxValue)
        {
            if (maxValue <= minValue)
            {
                if (maxValue == minValue)
                    return minValue;

                throw new ArgumentException("maxValue cannot be less than minValue");
            }

            int diff = maxValue - minValue;
            if (diff > 0)
                return minValue + Next(diff);

            for (;;)
            {
                int i = NextInt();

                if (i >= minValue && i < maxValue)
                    return i;
            }
        }

        public override void NextBytes(byte[] buf)
        {
            generator.NextBytes(buf);
        }

        public virtual void NextBytes(byte[] buf, int off, int len)
        {
            generator.NextBytes(buf, off, len);
        }

        private static readonly double DoubleScale = System.Math.Pow(2.0, 64.0);

        public override double NextDouble()
        {
            return Convert.ToDouble((ulong) NextLong()) / DoubleScale;
        }

        public virtual int NextInt()
        {
            byte[] intBytes = new byte[4];
            NextBytes(intBytes);

            int result = 0;
            for (int i = 0; i < 4; i++)
            {
                result = (result << 8) + (intBytes[i] & 0xff);
            }

            return result;
        }

        public virtual long NextLong()
        {
            return ((long)(uint) NextInt() << 32) | (long)(uint) NextInt();
        }
    }
}
