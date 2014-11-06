using System;

using NBitcoin.BouncyCastle.Crypto.Parameters;
using NBitcoin.BouncyCastle.Math;

namespace NBitcoin.BouncyCastle.Crypto.Generators
{
    /**
     * a ElGamal key pair generator.
     * <p>
     * This Generates keys consistent for use with ElGamal as described in
     * page 164 of "Handbook of Applied Cryptography".</p>
     */
    public class ElGamalKeyPairGenerator
		: IAsymmetricCipherKeyPairGenerator
    {
        private ElGamalKeyGenerationParameters param;

        public void Init(
			KeyGenerationParameters parameters)
        {
            this.param = (ElGamalKeyGenerationParameters) parameters;
        }

        public AsymmetricCipherKeyPair GenerateKeyPair()
        {
			DHKeyGeneratorHelper helper = DHKeyGeneratorHelper.Instance;
			ElGamalParameters egp = param.Parameters;
			DHParameters dhp = new DHParameters(egp.P, egp.G, null, 0, egp.L);

			BigInteger x = helper.CalculatePrivate(dhp, param.Random);
			BigInteger y = helper.CalculatePublic(dhp, x);

			return new AsymmetricCipherKeyPair(
                new ElGamalPublicKeyParameters(y, egp),
                new ElGamalPrivateKeyParameters(x, egp));
        }
    }

}
