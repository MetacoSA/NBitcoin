using System;
using System.Collections;
using System.IO;

using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.IO;

namespace Org.BouncyCastle.Crypto.Tls
{
    public class TlsDheKeyExchange
        :   TlsDHKeyExchange
    {
        protected TlsSignerCredentials mServerCredentials = null;

        public TlsDheKeyExchange(int keyExchange, IList supportedSignatureAlgorithms, DHParameters dhParameters)
            :   base(keyExchange, supportedSignatureAlgorithms, dhParameters)
        {
        }

        public override void ProcessServerCredentials(TlsCredentials serverCredentials)
        {
            if (!(serverCredentials is TlsSignerCredentials))
                throw new TlsFatalAlert(AlertDescription.internal_error);

            ProcessServerCertificate(serverCredentials.Certificate);

            this.mServerCredentials = (TlsSignerCredentials)serverCredentials;
        }

        public override byte[] GenerateServerKeyExchange()
        {
            if (this.mDHParameters == null)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            DigestInputBuffer buf = new DigestInputBuffer();

            this.mDHAgreeServerPrivateKey = TlsDHUtilities.GenerateEphemeralServerKeyExchange(context.SecureRandom,
                this.mDHParameters, buf);

            /*
             * RFC 5246 4.7. digitally-signed element needs SignatureAndHashAlgorithm from TLS 1.2
             */
            SignatureAndHashAlgorithm signatureAndHashAlgorithm;
            IDigest d;

            if (TlsUtilities.IsTlsV12(context))
            {
                signatureAndHashAlgorithm = mServerCredentials.SignatureAndHashAlgorithm;
                if (signatureAndHashAlgorithm == null)
                    throw new TlsFatalAlert(AlertDescription.internal_error);

                d = TlsUtilities.CreateHash(signatureAndHashAlgorithm.Hash);
            }
            else
            {
                signatureAndHashAlgorithm = null;
                d = new CombinedHash();
            }

            SecurityParameters securityParameters = context.SecurityParameters;
            d.BlockUpdate(securityParameters.clientRandom, 0, securityParameters.clientRandom.Length);
            d.BlockUpdate(securityParameters.serverRandom, 0, securityParameters.serverRandom.Length);
            buf.UpdateDigest(d);

            byte[] hash = DigestUtilities.DoFinal(d);

            byte[] signature = mServerCredentials.GenerateCertificateSignature(hash);

            DigitallySigned signed_params = new DigitallySigned(signatureAndHashAlgorithm, signature);
            signed_params.Encode(buf);

            return buf.ToArray();
        }

        public override void ProcessServerKeyExchange(Stream input)
        {
            SecurityParameters securityParameters = context.SecurityParameters;

            SignerInputBuffer buf = new SignerInputBuffer();
            Stream teeIn = new TeeInputStream(input, buf);

            ServerDHParams dhParams = ServerDHParams.Parse(teeIn);

            DigitallySigned signed_params = DigitallySigned.Parse(context, input);

            ISigner signer = InitVerifyer(mTlsSigner, signed_params.Algorithm, securityParameters);
            buf.UpdateSigner(signer);
            if (!signer.VerifySignature(signed_params.Signature))
                throw new TlsFatalAlert(AlertDescription.decrypt_error);

            this.mDHAgreeServerPublicKey = TlsDHUtilities.ValidateDHPublicKey(dhParams.PublicKey);
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
