using System;

using NBitcoin.BouncyCastle.Asn1;
using NBitcoin.BouncyCastle.Asn1.X509;
using NBitcoin.BouncyCastle.Utilities;

namespace NBitcoin.BouncyCastle.Asn1.Ocsp
{
    public class BasicOcspResponse
        : Asn1Encodable
    {
        private readonly ResponseData			tbsResponseData;
        private readonly AlgorithmIdentifier	signatureAlgorithm;
        private readonly DerBitString			signature;
        private readonly Asn1Sequence			certs;

		public static BasicOcspResponse GetInstance(
			Asn1TaggedObject	obj,
			bool				explicitly)
		{
			return GetInstance(Asn1Sequence.GetInstance(obj, explicitly));
		}

		public static BasicOcspResponse GetInstance(
			object obj)
		{
			if (obj == null || obj is BasicOcspResponse)
			{
				return (BasicOcspResponse)obj;
			}

			if (obj is Asn1Sequence)
			{
				return new BasicOcspResponse((Asn1Sequence)obj);
			}

            throw new ArgumentException("unknown object in factory: " + Platform.GetTypeName(obj), "obj");
		}

		public BasicOcspResponse(
            ResponseData		tbsResponseData,
            AlgorithmIdentifier	signatureAlgorithm,
            DerBitString		signature,
            Asn1Sequence		certs)
        {
            this.tbsResponseData = tbsResponseData;
            this.signatureAlgorithm = signatureAlgorithm;
            this.signature = signature;
            this.certs = certs;
        }

		private BasicOcspResponse(
            Asn1Sequence seq)
        {
            this.tbsResponseData = ResponseData.GetInstance(seq[0]);
            this.signatureAlgorithm = AlgorithmIdentifier.GetInstance(seq[1]);
            this.signature = (DerBitString)seq[2];

			if (seq.Count > 3)
            {
                this.certs = Asn1Sequence.GetInstance((Asn1TaggedObject)seq[3], true);
            }
        }

		[Obsolete("Use TbsResponseData property instead")]
		public ResponseData GetTbsResponseData()
        {
            return tbsResponseData;
        }

		public ResponseData TbsResponseData
		{
			get { return tbsResponseData; }
		}

		[Obsolete("Use SignatureAlgorithm property instead")]
		public AlgorithmIdentifier GetSignatureAlgorithm()
        {
            return signatureAlgorithm;
        }

		public AlgorithmIdentifier SignatureAlgorithm
		{
			get { return signatureAlgorithm; }
		}

		[Obsolete("Use Signature property instead")]
		public DerBitString GetSignature()
        {
            return signature;
        }

		public DerBitString Signature
		{
			get { return signature; }
		}

        public byte[] GetSignatureOctets()
        {
            return signature.GetOctets();
        }

        [Obsolete("Use Certs property instead")]
		public Asn1Sequence GetCerts()
        {
            return certs;
        }

		public Asn1Sequence Certs
		{
			get { return certs; }
		}

		/**
         * Produce an object suitable for an Asn1OutputStream.
         * <pre>
         * BasicOcspResponse       ::= Sequence {
         *      tbsResponseData      ResponseData,
         *      signatureAlgorithm   AlgorithmIdentifier,
         *      signature            BIT STRING,
         *      certs                [0] EXPLICIT Sequence OF Certificate OPTIONAL }
         * </pre>
         */
        public override Asn1Object ToAsn1Object()
        {
            Asn1EncodableVector v = new Asn1EncodableVector(
				tbsResponseData, signatureAlgorithm, signature);

			if (certs != null)
            {
                v.Add(new DerTaggedObject(true, 0, certs));
            }

			return new DerSequence(v);
        }
    }
}
