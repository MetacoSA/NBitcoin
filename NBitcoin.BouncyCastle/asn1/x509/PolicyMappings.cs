using System.Collections;

namespace NBitcoin.BouncyCastle.Asn1.X509
{
	/**
	 * PolicyMappings V3 extension, described in RFC3280.
	 * <pre>
	 *    PolicyMappings ::= Sequence SIZE (1..MAX) OF Sequence {
	 *      issuerDomainPolicy      CertPolicyId,
	 *      subjectDomainPolicy     CertPolicyId }
	 * </pre>
	 *
	 * @see <a href="http://www.faqs.org/rfc/rfc3280.txt">RFC 3280, section 4.2.1.6</a>
	 */
	public class PolicyMappings
		: Asn1Encodable
	{
		private readonly Asn1Sequence seq;

		/**
		 * Creates a new <code>PolicyMappings</code> instance.
		 *
		 * @param seq an <code>Asn1Sequence</code> constructed as specified
		 * in RFC 3280
		 */
		public PolicyMappings(
			Asn1Sequence seq)
		{
			this.seq = seq;
		}

#if !(SILVERLIGHT || PORTABLE)
        public PolicyMappings(
            Hashtable mappings)
            : this((IDictionary)mappings)
        {
        }
#endif

        /**
		 * Creates a new <code>PolicyMappings</code> instance.
		 *
		 * @param mappings a <code>HashMap</code> value that maps
		 * <code>string</code> oids
		 * to other <code>string</code> oids.
		 */
		public PolicyMappings(
			IDictionary mappings)
		{
			Asn1EncodableVector v = new Asn1EncodableVector();

			foreach (string idp in mappings.Keys)
			{
				string sdp = (string) mappings[idp];

				v.Add(
					new DerSequence(
						new DerObjectIdentifier(idp),
						new DerObjectIdentifier(sdp)));
			}

			seq = new DerSequence(v);
		}

		public override Asn1Object ToAsn1Object()
		{
			return seq;
		}
	}
}
