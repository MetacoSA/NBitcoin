using System;
using System.Collections;
using System.IO;

using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Agreement.Srp;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.IO;

namespace Org.BouncyCastle.Crypto.Tls
{
    /// <summary>(D)TLS SRP key exchange (RFC 5054).</summary>
    public class TlsSrpKeyExchange
        :   AbstractTlsKeyExchange
    {
        protected TlsSigner mTlsSigner;
        protected byte[] mIdentity;
        protected byte[] mPassword;

        protected AsymmetricKeyParameter mServerPublicKey = null;

        protected byte[] mS = null;
        protected BigInteger mB = null;
        protected Srp6Client mSrpClient = new Srp6Client();

        public TlsSrpKeyExchange(int keyExchange, IList supportedSignatureAlgorithms, byte[] identity, byte[] password)
            :   base(keyExchange, supportedSignatureAlgorithms)
        {
            switch (keyExchange)
            {
            case KeyExchangeAlgorithm.SRP:
                this.mTlsSigner = null;
                break;
            case KeyExchangeAlgorithm.SRP_RSA:
                this.mTlsSigner = new TlsRsaSigner();
                break;
            case KeyExchangeAlgorithm.SRP_DSS:
                this.mTlsSigner = new TlsDssSigner();
                break;
            default:
                throw new InvalidOperationException("unsupported key exchange algorithm");
            }

            this.mIdentity = identity;
            this.mPassword = password;
        }

        public override void Init(TlsContext context)
        {
            base.Init(context);

            if (this.mTlsSigner != null)
            {
                this.mTlsSigner.Init(context);
            }
        }

        public override void SkipServerCredentials()
        {
            if (mTlsSigner != null)
                throw new TlsFatalAlert(AlertDescription.unexpected_message);
        }

        public override void ProcessServerCertificate(Certificate serverCertificate)
        {
            if (mTlsSigner == null)
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

            if (!mTlsSigner.IsValidPublicKey(this.mServerPublicKey))
                throw new TlsFatalAlert(AlertDescription.certificate_unknown);

            TlsUtilities.ValidateKeyUsage(x509Cert, KeyUsage.DigitalSignature);

            base.ProcessServerCertificate(serverCertificate);
        }

        public override bool RequiresServerKeyExchange
        {
            get { return true; }
        }

        public override void ProcessServerKeyExchange(Stream input)
        {
            SecurityParameters securityParameters = context.SecurityParameters;

            SignerInputBuffer buf = null;
            Stream teeIn = input;

            if (mTlsSigner != null)
            {
                buf = new SignerInputBuffer();
                teeIn = new TeeInputStream(input, buf);
            }

            byte[] NBytes = TlsUtilities.ReadOpaque16(teeIn);
            byte[] gBytes = TlsUtilities.ReadOpaque16(teeIn);
            byte[] sBytes = TlsUtilities.ReadOpaque8(teeIn);
            byte[] BBytes = TlsUtilities.ReadOpaque16(teeIn);

            if (buf != null)
            {
                DigitallySigned signed_params = DigitallySigned.Parse(context, input);

                ISigner signer = InitVerifyer(mTlsSigner, signed_params.Algorithm, securityParameters);
                buf.UpdateSigner(signer);
                if (!signer.VerifySignature(signed_params.Signature))
                    throw new TlsFatalAlert(AlertDescription.decrypt_error);
            }

            BigInteger N = new BigInteger(1, NBytes);
            BigInteger g = new BigInteger(1, gBytes);

            // TODO Validate group parameters (see RFC 5054)
    //        throw new TlsFatalAlert(AlertDescription.insufficient_security);

            this.mS = sBytes;

            /*
             * RFC 5054 2.5.3: The client MUST abort the handshake with an "illegal_parameter" alert if
             * B % N = 0.
             */
            try
            {
                this.mB = Srp6Utilities.ValidatePublicValue(N, new BigInteger(1, BBytes));
            }
            catch (CryptoException e)
            {
                throw new TlsFatalAlert(AlertDescription.illegal_parameter, e);
            }

            this.mSrpClient.Init(N, g, TlsUtilities.CreateHash(HashAlgorithm.sha1), context.SecureRandom);
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
            BigInteger A = mSrpClient.GenerateClientCredentials(mS, this.mIdentity, this.mPassword);
            TlsUtilities.WriteOpaque16(BigIntegers.AsUnsignedByteArray(A), output);
        }

        public override byte[] GeneratePremasterSecret()
        {
            try
            {
                // TODO Check if this needs to be a fixed size
                return BigIntegers.AsUnsignedByteArray(mSrpClient.CalculateSecret(mB));
            }
            catch (CryptoException e)
            {
                throw new TlsFatalAlert(AlertDescription.illegal_parameter, e);
            }
        }

        protected virtual ISigner InitVerifyer(TlsSigner tlsSigner, SignatureAndHashAlgorithm algorithm,
            SecurityParameters securityParameters)
        {
            ISigner signer = tlsSigner.CreateVerifyer(algorithm, this.mServerPublicKey);
            signer.BlockUpdate(securityParameters.clientRandom, 0, securityParameters.clientRandom.Length);
            signer.BlockUpdate(securityParameters.serverRandom, 0, securityParameters.serverRandom.Length);
            return signer;
        }
    }
}
