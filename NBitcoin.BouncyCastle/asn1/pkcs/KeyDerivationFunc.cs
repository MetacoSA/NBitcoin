using NBitcoin.BouncyCastle.Asn1;
using NBitcoin.BouncyCastle.Asn1.X509;

namespace NBitcoin.BouncyCastle.Asn1.Pkcs
{
	public class KeyDerivationFunc
		: AlgorithmIdentifier
	{
		internal KeyDerivationFunc(Asn1Sequence seq)
			: base(seq)
		{
		}

		public KeyDerivationFunc(
			DerObjectIdentifier	id,
			Asn1Encodable		parameters)
			: base(id, parameters)
		{
		}
	}
}