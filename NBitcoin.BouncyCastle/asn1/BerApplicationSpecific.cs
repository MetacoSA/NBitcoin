using System;

namespace NBitcoin.BouncyCastle.Asn1
{
	public class BerApplicationSpecific
		: DerApplicationSpecific
	{
		public BerApplicationSpecific(
			int					tagNo,
			Asn1EncodableVector	vec)
			: base(tagNo, vec)
		{
		}
	}
}
