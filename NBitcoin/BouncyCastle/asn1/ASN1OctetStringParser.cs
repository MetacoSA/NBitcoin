using System.IO;

namespace nStratis.BouncyCastle.asn1
{
	internal interface Asn1OctetStringParser
		: IAsn1Convertible
	{
		Stream GetOctetStream();
	}
}
