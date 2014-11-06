using System;
using System.Collections;
using System.IO;

using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Org.BouncyCastle.Crypto.Tls
{
    /// <summary>(D)TLS PSK key exchange (RFC 4279).</summary>
    public class TlsPskKeyExchange
        :   AbstractTlsKeyExchange
    {
        protected TlsPskIdentity mPskIdentity;
        protected DHParameters mDHParameters;
        protected int[] mNamedCurves;
        protected byte[] mClientECPointFormats, mServerECPointFormats;

        protected byte[] mPskIdentityHint = null;

        protected DHPrivateKeyParameters mDHAgreePrivateKey = null;
        protected DHPublicKeyParameters mDHAgreePublicKey = null;

        protected AsymmetricKeyParameter mServerPublicKey = null;
        protected RsaKeyParameters mRsaServerPublicKey = null;
        protected TlsEncryptionCredentials mServerCredentials = null;
        protected byte[] mPremasterSecret;

        public TlsPskKeyExchange(int keyExchange, IList supportedSignatureAlgorithms, TlsPskIdentity pskIdentity,
            DHParameters dhParameters, int[] namedCurves, byte[] clientECPointFormats, byte[] serverECPointFormats)
            :   base(keyExchange, supportedSignatureAlgorithms)
        {
            switch (keyExchange)
            {
            case KeyExchangeAlgorithm.DHE_PSK:
            case KeyExchangeAlgorithm.ECDHE_PSK:
            case KeyExchangeAlgorithm.PSK:
            case KeyExchangeAlgorithm.RSA_PSK:
                break;
            default:
                throw new InvalidOperationException("unsupported key exchange algorithm");
            }

            this.mPskIdentity = pskIdentity;
            this.mDHParameters = dhParameters;
            this.mNamedCurves = namedCurves;
            this.mClientECPointFormats = clientECPointFormats;
            this.mServerECPointFormats = serverECPointFormats;
        }

        public override void SkipServerCredentials()
        {
            if (mKeyExchange == KeyExchangeAlgorithm.RSA_PSK)
                throw new TlsFatalAlert(AlertDescription.unexpected_message);
        }

        public override void ProcessServerCredentials(TlsCredentials serverCredentials)
        {
            if (!(serverCredentials is TlsEncryptionCredentials))
                throw new TlsFatalAlert(AlertDescription.internal_error);

            ProcessServerCertificate(serverCredentials.Certificate);

            this.mServerCredentials = (TlsEncryptionCredentials)serverCredentials;
        }

        public override byte[] GenerateServerKeyExchange()
        {
            // TODO[RFC 4279] Need a server-side PSK API to determine hint and resolve identities to keys
            this.mPskIdentityHint = null;

            if (this.mPskIdentityHint == null && !RequiresServerKeyExchange)
                return null;

            MemoryStream buf = new MemoryStream();

            if (this.mPskIdentityHint == null)
            {
                TlsUtilities.WriteOpaque16(TlsUtilities.EmptyBytes, buf);
            }
            else
            {
                TlsUtilities.WriteOpaque16(this.mPskIdentityHint, buf);
            }

            if (this.mKeyExchange == KeyExchangeAlgorithm.DHE_PSK)
            {
                if (this.mDHParameters == null)
                    throw new TlsFatalAlert(AlertDescription.internal_error);

                this.mDHAgreePrivateKey = TlsDHUtilities.GenerateEphemeralServerKeyExchange(context.SecureRandom,
                    this.mDHParameters, buf);
            }
            else if (this.mKeyExchange == KeyExchangeAlgorithm.ECDHE_PSK)
            {
                // TODO[RFC 5489]
            }

            return buf.ToArray();
        }

        public override void ProcessServerCertificate(Certificate serverCertificate)
        {
            if (mKeyExchange != KeyExchangeAlgorithm.RSA_PSK)
                throw new TlsFatalAlert(AlertDescription.unexpected_message);
            if (serverCertificate.IsEmpty)
                throw new TlsFatalAlert(AlertDescription.bad_certificate);

            X509CertificateStructure x509Cert = serverCertificate.GetCertificateAt(0);

            SubjectPublicKeyInfo keyInfo = x509Cert.SubjectPublicKeyInfo;
            try
            {
                this.mServerPublicKey = PublicKeyFactory.CreateKey(keyInfo);
            }
            catch (Exception e)
            {
                throw new TlsFatalAlert(AlertDescription.unsupported_certificate, e);
            }

            // Sanity check the PublicKeyFactory
            if (this.mServerPublicKey.IsPrivate)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            this.mRsaServerPublicKey = ValidateRsaPublicKey((RsaKeyParameters)this.mServerPublicKey);

            TlsUtilities.ValidateKeyUsage(x509Cert, KeyUsage.KeyEncipherment);

            base.ProcessServerCertificate(serverCertificate);
        }

        public override bool RequiresServerKeyExchange
        {
            get
            {
                switch (mKeyExchange)
                {
                case KeyExchangeAlgorithm.DHE_PSK:
                case KeyExchangeAlgorithm.ECDHE_PSK:
                    return true;
                default:
                    return false;
                }
            }
        }

        public override void ProcessServerKeyExchange(Stream input)
        {
            this.mPskIdentityHint = TlsUtilities.ReadOpaque16(input);

            if (this.mKeyExchange == KeyExchangeAlgorithm.DHE_PSK)
            {
                ServerDHParams serverDHParams = ServerDHParams.Parse(input);

                this.mDHAgreePublicKey = TlsDHUtilities.ValidateDHPublicKey(serverDHParams.PublicKey);
            }
            else if (this.mKeyExchange == KeyExchangeAlgorithm.ECDHE_PSK)
            {
                // TODO[RFC 5489]
            }
        }

        public override void ValidateCertificateRequest(CertificateRequest certificateRequest)
        {
            throw new TlsFatalAlert(AlertDescription.unexpected_message);
        }

        public override void ProcessClientCredentials(TlsCredentials clientCredentials)
        {
            throw new TlsFatalAlert(AlertDescription.internal_error);
        }

        public override void GenerateClientKeyExchange(Stream output)
        {
            if (mPskIdentityHint == null)
            {
                mPskIdentity.SkipIdentityHint();
            }
            else
            {
                mPskIdentity.NotifyIdentityHint(mPskIdentityHint);
            }

            byte[] psk_identity = mPskIdentity.GetPskIdentity();

            TlsUtilities.WriteOpaque16(psk_identity, output);

            if (this.mKeyExchange == KeyExchangeAlgorithm.DHE_PSK)
            {
                this.mDHAgreePrivateKey = TlsDHUtilities.GenerateEphemeralClientKeyExchange(context.SecureRandom,
                    mDHAgreePublicKey.Parameters, output);
            }
            else if (this.mKeyExchange == KeyExchangeAlgorithm.ECDHE_PSK)
            {
                // TODO[RFC 5489]
                throw new TlsFatalAlert(AlertDescription.internal_error);
            }
            else if (this.mKeyExchange == KeyExchangeAlgorithm.RSA_PSK)
            {
                this.mPremasterSecret = TlsRsaUtilities.GenerateEncryptedPreMasterSecret(context,
                    this.mRsaServerPublicKey, output);
            }
        }

        public override byte[] GeneratePremasterSecret()
        {
            byte[] psk = mPskIdentity.GetPsk();
            byte[] other_secret = GenerateOtherSecret(psk.Length);

            MemoryStream buf = new MemoryStream(4 + other_secret.Length + psk.Length);
            TlsUtilities.WriteOpaque16(other_secret, buf);
            TlsUtilities.WriteOpaque16(psk, buf);
            return buf.ToArray();
        }

        protected virtual byte[] GenerateOtherSecret(int pskLength)
        {
            if (this.mKeyExchange == KeyExchangeAlgorithm.DHE_PSK)
            {
                if (mDHAgreePrivateKey != null)
                {
                    return TlsDHUtilities.CalculateDHBasicAgreement(mDHAgreePublicKey, mDHAgreePrivateKey);
                }

                throw new TlsFatalAlert(AlertDescription.internal_error);
            }

            if (this.mKeyExchange == KeyExchangeAlgorithm.ECDHE_PSK)
            {
                // TODO[RFC 5489]
                throw new TlsFatalAlert(AlertDescription.internal_error);
            }

            if (this.mKeyExchange == KeyExchangeAlgorithm.RSA_PSK)
            {
                return this.mPremasterSecret;
            }

            return new byte[pskLength];
        }

        protected virtual RsaKeyParameters ValidateRsaPublicKey(RsaKeyParameters key)
        {
            // TODO What is the minimum bit length required?
            // key.Modulus.BitLength;

            if (!key.Exponent.IsProbablePrime(2))
                throw new TlsFatalAlert(AlertDescription.illegal_parameter);

            return key;
        }
    }
}
