namespace NBitcoin.BouncyCastle.Asn1
{
	internal class DerOctetString
		: Asn1OctetString
	{
		/// <param name="str">The octets making up the octet string.</param>
		public DerOctetString(
			byte[] str)
			: base(str)
		{
		}

		internal override void Encode(
			DerOutputStream derOut)
		{
			derOut.WriteEncoded(Asn1Tags.OctetString, str);
		}

		internal static void Encode(
			DerOutputStream derOut,
			byte[] bytes,
			int offset,
			int length)
		{
			derOut.WriteEncoded(Asn1Tags.OctetString, bytes, offset, length);
		}
	}
}
