namespace nStratis.BouncyCastle.asn1
{
	internal interface Asn1SequenceParser
		: IAsn1Convertible
	{
		IAsn1Convertible ReadObject();
	}
}
