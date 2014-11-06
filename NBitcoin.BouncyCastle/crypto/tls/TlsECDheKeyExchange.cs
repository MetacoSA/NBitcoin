using System;
using System.Collections;
using System.IO;

using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.IO;

namespace Org.BouncyCastle.Crypto.Tls
{
    /// <summary>(D)TLS ECDHE key exchange (see RFC 4492).</summary>
    public class TlsECDheKeyExchange
        :   TlsECDHKeyExchange
    {
        protected TlsSignerCredentials mServerCredentials = null;

        public TlsECDheKeyExchange(int keyExchange, IList supportedSignatureAlgorithms, int[] namedCurves,
            byte[] clientECPointFormats, byte[] serverECPointFormats)
            :   base(keyExchange, supportedSignatureAlgorithms, namedCurves, clientECPointFormats, serverECPointFormats)
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
            /*
             * First we try to find a supported named curve from the client's list.
             */
            int namedCurve = -1;
            if (mNamedCurves == null)
            {
                // TODO Let the peer choose the default named curve
                namedCurve = NamedCurve.secp256r1;
            }
            else
            {
                for (int i = 0; i < mNamedCurves.Length; ++i)
                {
                    int entry = mNamedCurves[i];
                    if (NamedCurve.IsValid(entry) && TlsEccUtilities.IsSupportedNamedCurve(entry))
                    {
                        namedCurve = entry;
                        break;
                    }
                }
            }

            ECDomainParameters curve_params = null;
            if (namedCurve >= 0)
            {
                curve_params = TlsEccUtilities.GetParametersForNamedCurve(namedCurve);
            }
            else
            {
                /*
                 * If no named curves are suitable, check if the client supports explicit curves.
                 */
                if (Arrays.Contains(mNamedCurves, NamedCurve.arbitrary_explicit_prime_curves))
                {
                    curve_params = TlsEccUtilities.GetParametersForNamedCurve(NamedCurve.secp256r1);
                }
                else if (Arrays.Contains(mNamedCurves, NamedCurve.arbitrary_explicit_char2_curves))
                {
                    curve_params = TlsEccUtilities.GetParametersForNamedCurve(NamedCurve.sect283r1);
                }
            }

            if (curve_params == null)
            {
                /*
                 * NOTE: We shouldn't have negotiated ECDHE key exchange since we apparently can't find
                 * a suitable curve.
                 */
                throw new TlsFatalAlert(AlertDescription.internal_error);
            }

            AsymmetricCipherKeyPair kp = TlsEccUtilities.GenerateECKeyPair(context.SecureRandom, curve_params);
            this.mECAgreePrivateKey = (ECPrivateKeyParameters)kp.Private;

            DigestInputBuffer buf = new DigestInputBuffer();

            if (namedCurve < 0)
            {
                TlsEccUtilities.WriteExplicitECParameters(mClientECPointFormats, curve_params, buf);
            }
            else
            {
                TlsEccUtilities.WriteNamedECParameters(namedCurve, buf);
            }

            ECPublicKeyParameters ecPublicKey = (ECPublicKeyParameters)kp.Public;
            TlsEccUtilities.WriteECPoint(mClientECPointFormats, ecPublicKey.Q, buf);

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

            ECDomainParameters curve_params = TlsEccUtilities.ReadECParameters(mNamedCurves, mClientECPointFormats, teeIn);

            byte[] point = TlsUtilities.ReadOpaque8(teeIn);

            DigitallySigned signed_params = DigitallySigned.Parse(context, input);

            ISigner signer = InitVerifyer(mTlsSigner, signed_params.Algorithm, securityParameters);
            buf.UpdateSigner(signer);
            if (!signer.VerifySignature(signed_params.Signature))
                throw new TlsFatalAlert(AlertDescription.decrypt_error);

            this.mECAgreePublicKey = TlsEccUtilities.ValidateECPublicKey(TlsEccUtilities.DeserializeECPublicKey(
                mClientECPointFormats, curve_params, point));
        }

        public override void ValidateCertificateRequest(CertificateRequest certificateRequest)
        {
            /*
             * RFC 4492 3. [...] The ECDSA_fixed_ECDH and RSA_fixed_ECDH mechanisms are usable with
             * ECDH_ECDSA and ECDH_RSA. Their use with ECDHE_ECDSA and ECDHE_RSA is prohibited because
             * the use of a long-term ECDH client key would jeopardize the forward secrecy property of
             * these algorithms.
             */
            byte[] types = certificateRequest.CertificateTypes;
            for (int i = 0; i < types.Length; ++i)
            {
                switch (types[i])
                {
                case ClientCertificateType.rsa_sign:
                case ClientCertificateType.dss_sign:
                case ClientCertificateType.ecdsa_sign:
                    break;
                default:
                    throw new TlsFatalAlert(AlertDescription.illegal_parameter);
                }
            }
        }

        public override void ProcessClientCredentials(TlsCredentials clientCredentials)
        {
            if (clientCredentials is TlsSignerCredentials)
            {
                // OK
            }
            else
            {
                throw new TlsFatalAlert(AlertDescription.internal_error);
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
