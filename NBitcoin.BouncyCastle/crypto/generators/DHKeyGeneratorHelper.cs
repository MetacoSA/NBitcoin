using System;

using NBitcoin.BouncyCastle.Crypto.Parameters;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Math.EC.Multiplier;
using NBitcoin.BouncyCastle.Security;
using NBitcoin.BouncyCastle.Utilities;

namespace NBitcoin.BouncyCastle.Crypto.Generators
{
    class DHKeyGeneratorHelper
    {
        internal static readonly DHKeyGeneratorHelper Instance = new DHKeyGeneratorHelper();

        private DHKeyGeneratorHelper()
        {
        }

        internal BigInteger CalculatePrivate(
            DHParameters	dhParams,
            SecureRandom	random)
        {
            int limit = dhParams.L;

            if (limit != 0)
            {
                int minWeight = limit >> 2;
                for (;;)
                {
                    BigInteger x = new BigInteger(limit, random).SetBit(limit - 1);
                    if (WNafUtilities.GetNafWeight(x) >= minWeight)
                    {
                        return x;
                    }
                }
            }

            BigInteger min = BigInteger.Two;
            int m = dhParams.M;
            if (m != 0)
            {
                min = BigInteger.One.ShiftLeft(m - 1);
            }

            BigInteger q = dhParams.Q;
            if (q == null)
            {
                q = dhParams.P;
            }
            BigInteger max = q.Subtract(BigInteger.Two);

            {
                int minWeight = max.BitLength >> 2;
                for (;;)
                {
                    BigInteger x = BigIntegers.CreateRandomInRange(min, max, random);
                    if (WNafUtilities.GetNafWeight(x) >= minWeight)
                    {
                        return x;
                    }
                }
            }
        }

        internal BigInteger CalculatePublic(
            DHParameters	dhParams,
            BigInteger		x)
        {
            return dhParams.G.ModPow(x, dhParams.P);
        }
    }
}
