using System;

using NBitcoin.BouncyCastle.Utilities;

namespace NBitcoin.BouncyCastle.Asn1.CryptoPro
{
    public class Gost3410PublicKeyAlgParameters
        : Asn1Encodable
    {
        private DerObjectIdentifier	publicKeyParamSet;
        private DerObjectIdentifier	digestParamSet;
        private DerObjectIdentifier	encryptionParamSet;

		public static Gost3410PublicKeyAlgParameters GetInstance(
            Asn1TaggedObject	obj,
            bool				explicitly)
        {
            return GetInstance(Asn1Sequence.GetInstance(obj, explicitly));
        }

		public static Gost3410PublicKeyAlgParameters GetInstance(
            object obj)
        {
            if (obj == null || obj is Gost3410PublicKeyAlgParameters)
            {
                return (Gost3410PublicKeyAlgParameters) obj;
            }

			if (obj is Asn1Sequence)
            {
                return new Gost3410PublicKeyAlgParameters((Asn1Sequence) obj);
            }

            throw new ArgumentException("Invalid GOST3410Parameter: " + Platform.GetTypeName(obj));
        }

		public Gost3410PublicKeyAlgParameters(
            DerObjectIdentifier	publicKeyParamSet,
            DerObjectIdentifier	digestParamSet)
			: this (publicKeyParamSet, digestParamSet, null)
        {
        }

		public Gost3410PublicKeyAlgParameters(
            DerObjectIdentifier  publicKeyParamSet,
            DerObjectIdentifier  digestParamSet,
            DerObjectIdentifier  encryptionParamSet)
        {
			if (publicKeyParamSet == null)
				throw new ArgumentNullException("publicKeyParamSet");
			if (digestParamSet == null)
				throw new ArgumentNullException("digestParamSet");

			this.publicKeyParamSet = publicKeyParamSet;
            this.digestParamSet = digestParamSet;
            this.encryptionParamSet = encryptionParamSet;
        }

		public Gost3410PublicKeyAlgParameters(
            Asn1Sequence seq)
        {
            this.publicKeyParamSet = (DerObjectIdentifier) seq[0];
            this.digestParamSet = (DerObjectIdentifier) seq[1];

			if (seq.Count > 2)
            {
                this.encryptionParamSet = (DerObjectIdentifier) seq[2];
            }
        }

		public DerObjectIdentifier PublicKeyParamSet
		{
			get { return publicKeyParamSet; }
		}

		public DerObjectIdentifier DigestParamSet
		{
			get { return digestParamSet; }
		}

		public DerObjectIdentifier EncryptionParamSet
		{
			get { return encryptionParamSet; }
		}

		public override Asn1Object ToAsn1Object()
        {
            Asn1EncodableVector v = new Asn1EncodableVector(
				publicKeyParamSet, digestParamSet);

			if (encryptionParamSet != null)
            {
                v.Add(encryptionParamSet);
            }

			return new DerSequence(v);
        }
    }
}
