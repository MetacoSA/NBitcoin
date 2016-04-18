using System;
using System.Collections;

using NBitcoin.BouncyCastle.Asn1.X509;
using NBitcoin.BouncyCastle.Utilities;
using NBitcoin.BouncyCastle.Utilities.Collections;

namespace NBitcoin.BouncyCastle.Asn1.Esf
{
	/// <remarks>
	/// RFC 3126: 4.3.1 Certificate Values Attribute Definition
	/// <code>
	/// CertificateValues ::= SEQUENCE OF Certificate
	/// </code>
	/// </remarks>
	public class CertificateValues
		: Asn1Encodable
	{
		private readonly Asn1Sequence certificates;

		public static CertificateValues GetInstance(
			object obj)
		{
			if (obj == null || obj is CertificateValues)
				return (CertificateValues) obj;

			if (obj is Asn1Sequence)
				return new CertificateValues((Asn1Sequence) obj);

			throw new ArgumentException(
				"Unknown object in 'CertificateValues' factory: "
                    + Platform.GetTypeName(obj),
				"obj");
		}

		private CertificateValues(
			Asn1Sequence seq)
		{
			if (seq == null)
				throw new ArgumentNullException("seq");

			foreach (Asn1Encodable ae in seq)
			{
				X509CertificateStructure.GetInstance(ae.ToAsn1Object());
			}

			this.certificates = seq;
		}

		public CertificateValues(
			params X509CertificateStructure[] certificates)
		{
			if (certificates == null)
				throw new ArgumentNullException("certificates");

			this.certificates = new DerSequence(certificates);
		}

		public CertificateValues(
			IEnumerable certificates)
		{
			if (certificates == null)
				throw new ArgumentNullException("certificates");
			if (!CollectionUtilities.CheckElementsAreOfType(certificates, typeof(X509CertificateStructure)))
				throw new ArgumentException("Must contain only 'X509CertificateStructure' objects", "certificates");

			this.certificates = new DerSequence(
				Asn1EncodableVector.FromEnumerable(certificates));
		}

		public X509CertificateStructure[] GetCertificates()
		{
			X509CertificateStructure[] result = new X509CertificateStructure[certificates.Count];
			for (int i = 0; i < certificates.Count; ++i)
			{
				result[i] = X509CertificateStructure.GetInstance(certificates[i]);
			}
			return result;
		}

		public override Asn1Object ToAsn1Object()
		{
			return certificates;
		}
 	}
}
