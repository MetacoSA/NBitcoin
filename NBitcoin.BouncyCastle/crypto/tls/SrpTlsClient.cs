using System;
using System.Collections;
using System.IO;

using Org.BouncyCastle.Utilities;

namespace Org.BouncyCastle.Crypto.Tls
{
    public abstract class SrpTlsClient
        :   AbstractTlsClient
    {
        protected byte[] mIdentity;
        protected byte[] mPassword;

        public SrpTlsClient(byte[] identity, byte[] password)
            : this(new DefaultTlsCipherFactory(), identity, password)
        {
        }

        public SrpTlsClient(TlsCipherFactory cipherFactory, byte[] identity, byte[] password)
            :   base(cipherFactory)
        {
            this.mIdentity = Arrays.Clone(identity);
            this.mPassword = Arrays.Clone(password);
        }

        protected virtual bool RequireSrpServerExtension
        {
            // No explicit guidance in RFC 5054; by default an (empty) extension from server is optional
            get { return false; }
        }

        public override int[] GetCipherSuites()
        {
            return new int[]
            {
                CipherSuite.TLS_SRP_SHA_RSA_WITH_AES_128_CBC_SHA
            };
        }

        public override IDictionary GetClientExtensions()
        {
            IDictionary clientExtensions = TlsExtensionsUtilities.EnsureExtensionsInitialised(base.GetClientExtensions());
            TlsSrpUtilities.AddSrpExtension(clientExtensions, this.mIdentity);
            return clientExtensions;
        }

        public override void ProcessServerExtensions(IDictionary serverExtensions)
        {
            if (!TlsUtilities.HasExpectedEmptyExtensionData(serverExtensions, ExtensionType.srp,
                AlertDescription.illegal_parameter))
            {
                if (RequireSrpServerExtension)
                    throw new TlsFatalAlert(AlertDescription.illegal_parameter);
            }

            base.ProcessServerExtensions(serverExtensions);
        }

        public override TlsKeyExchange GetKeyExchange()
        {
            switch (mSelectedCipherSuite)
            {
            case CipherSuite.TLS_SRP_SHA_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_SRP_SHA_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_SRP_SHA_WITH_AES_256_CBC_SHA:
                return CreateSrpKeyExchange(KeyExchangeAlgorithm.SRP);

            case CipherSuite.TLS_SRP_SHA_RSA_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_SRP_SHA_RSA_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_SRP_SHA_RSA_WITH_AES_256_CBC_SHA:
                return CreateSrpKeyExchange(KeyExchangeAlgorithm.SRP_RSA);

            case CipherSuite.TLS_SRP_SHA_DSS_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_SRP_SHA_DSS_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_SRP_SHA_DSS_WITH_AES_256_CBC_SHA:
                return CreateSrpKeyExchange(KeyExchangeAlgorithm.SRP_DSS);

            default:
                /*
                 * Note: internal error here; the TlsProtocol implementation verifies that the
                 * server-selected cipher suite was in the list of client-offered cipher suites, so if
                 * we now can't produce an implementation, we shouldn't have offered it!
                 */
                throw new TlsFatalAlert(AlertDescription.internal_error);
            }
        }

        public override TlsCipher GetCipher()
        {
            switch (mSelectedCipherSuite)
            {
            case CipherSuite.TLS_SRP_SHA_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_SRP_SHA_RSA_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_SRP_SHA_DSS_WITH_3DES_EDE_CBC_SHA:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.cls_3DES_EDE_CBC, MacAlgorithm.hmac_sha1);

            case CipherSuite.TLS_SRP_SHA_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_SRP_SHA_RSA_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_SRP_SHA_DSS_WITH_AES_128_CBC_SHA:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_128_CBC, MacAlgorithm.hmac_sha1);

            case CipherSuite.TLS_SRP_SHA_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_SRP_SHA_RSA_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_SRP_SHA_DSS_WITH_AES_256_CBC_SHA:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_256_CBC, MacAlgorithm.hmac_sha1);

            default:
                /*
                 * Note: internal error here; the TlsProtocol implementation verifies that the
                 * server-selected cipher suite was in the list of client-offered cipher suites, so if
                 * we now can't produce an implementation, we shouldn't have offered it!
                 */
                throw new TlsFatalAlert(AlertDescription.internal_error);
            }
        }

        protected virtual TlsKeyExchange CreateSrpKeyExchange(int keyExchange)
        {
            return new TlsSrpKeyExchange(keyExchange, mSupportedSignatureAlgorithms, mIdentity, mPassword);
        }
    }
}
