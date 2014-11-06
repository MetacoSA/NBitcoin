using System;
using System.Collections;
using System.IO;

using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Parameters;

namespace Org.BouncyCastle.Crypto.Tls
{
    public abstract class DefaultTlsServer
        :   AbstractTlsServer
    {
        public DefaultTlsServer()
            :   base()
        {
        }

        public DefaultTlsServer(TlsCipherFactory cipherFactory)
            :   base(cipherFactory)
        {
        }

        protected virtual TlsEncryptionCredentials GetRsaEncryptionCredentials()
        {
            throw new TlsFatalAlert(AlertDescription.internal_error);
        }

        protected virtual TlsSignerCredentials GetRsaSignerCredentials()
        {
            throw new TlsFatalAlert(AlertDescription.internal_error);
        }

        protected virtual DHParameters GetDHParameters()
        {
            return DHStandardGroups.rfc5114_1024_160;
        }

        protected override int[] GetCipherSuites()
        {
            return new int[]
            {
                CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
                CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
                CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384,
                CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256,
                CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA,
                CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA,
                CipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384,
                CipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256,
                CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256,
                CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256,
                CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA,
                CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA,
            };
        }

        public override TlsCredentials GetCredentials()
        {
            switch (mSelectedCipherSuite)
            {
            case CipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_RSA_WITH_AES_128_CCM:
            case CipherSuite.TLS_RSA_WITH_AES_128_CCM_8:
            case CipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256:
            case CipherSuite.TLS_RSA_WITH_AES_256_CCM:
            case CipherSuite.TLS_RSA_WITH_AES_256_CCM_8:
            case CipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA:
            case CipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_RSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA:
            case CipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA256:
            case CipherSuite.TLS_RSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_RSA_WITH_NULL_MD5:
            case CipherSuite.TLS_RSA_WITH_NULL_SHA:
            case CipherSuite.TLS_RSA_WITH_NULL_SHA256:
            case CipherSuite.TLS_RSA_WITH_RC4_128_MD5:
            case CipherSuite.TLS_RSA_WITH_RC4_128_SHA:
            case CipherSuite.TLS_RSA_WITH_SEED_CBC_SHA:
                return GetRsaEncryptionCredentials();

            case CipherSuite.TLS_DHE_RSA_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM_8:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM_8:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_DHE_RSA_WITH_CHACHA20_POLY1305_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_SEED_CBC_SHA:
            case CipherSuite.TLS_ECDHE_RSA_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256:
            case CipherSuite.TLS_ECDHE_RSA_WITH_ESTREAM_SALSA20_SHA1:
            case CipherSuite.TLS_ECDHE_RSA_WITH_NULL_SHA:
            case CipherSuite.TLS_ECDHE_RSA_WITH_RC4_128_SHA:
            case CipherSuite.TLS_ECDHE_RSA_WITH_SALSA20_SHA1:
                return GetRsaSignerCredentials();

            default:
                /*
                 * Note: internal error here; selected a key exchange we don't implement!
                 */
                throw new TlsFatalAlert(AlertDescription.internal_error);
            }
        }

        public override TlsKeyExchange GetKeyExchange()
        {
            switch (mSelectedCipherSuite)
            {
            case CipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_DH_DSS_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA256:
            case CipherSuite.TLS_DH_DSS_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA:
            case CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA:
            case CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA256:
            case CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_DH_DSS_WITH_SEED_CBC_SHA:
                return CreateDHKeyExchange(KeyExchangeAlgorithm.DH_DSS);

            case CipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_DH_RSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA256:
            case CipherSuite.TLS_DH_RSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA:
            case CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA:
            case CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA256:
            case CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_DH_RSA_WITH_SEED_CBC_SHA:
                return CreateDHKeyExchange(KeyExchangeAlgorithm.DH_RSA);

            case CipherSuite.TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_DHE_DSS_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA256:
            case CipherSuite.TLS_DHE_DSS_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA:
            case CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA:
            case CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA256:
            case CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_DHE_DSS_WITH_SEED_CBC_SHA:
                return CreateDheKeyExchange(KeyExchangeAlgorithm.DHE_DSS);

            case CipherSuite.TLS_DHE_RSA_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM_8:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM_8:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_DHE_RSA_WITH_CHACHA20_POLY1305_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_ESTREAM_SALSA20_SHA1:
            case CipherSuite.TLS_DHE_RSA_WITH_SALSA20_SHA1:
            case CipherSuite.TLS_DHE_RSA_WITH_SEED_CBC_SHA:
                return CreateDheKeyExchange(KeyExchangeAlgorithm.DHE_RSA);

            case CipherSuite.TLS_ECDH_ECDSA_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_NULL_SHA:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_RC4_128_SHA:
                return CreateECDHKeyExchange(KeyExchangeAlgorithm.ECDH_ECDSA);

            case CipherSuite.TLS_ECDH_RSA_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_ECDH_RSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_ECDH_RSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_ECDH_RSA_WITH_NULL_SHA:
            case CipherSuite.TLS_ECDH_RSA_WITH_RC4_128_SHA:
                return CreateECDHKeyExchange(KeyExchangeAlgorithm.ECDH_RSA);

            case CipherSuite.TLS_ECDHE_ECDSA_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_ESTREAM_SALSA20_SHA1:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_NULL_SHA:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_RC4_128_SHA:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_SALSA20_SHA1:
                return CreateECDheKeyExchange(KeyExchangeAlgorithm.ECDHE_ECDSA);

            case CipherSuite.TLS_ECDHE_RSA_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256:
            case CipherSuite.TLS_ECDHE_RSA_WITH_ESTREAM_SALSA20_SHA1:
            case CipherSuite.TLS_ECDHE_RSA_WITH_NULL_SHA:
            case CipherSuite.TLS_ECDHE_RSA_WITH_RC4_128_SHA:
            case CipherSuite.TLS_ECDHE_RSA_WITH_SALSA20_SHA1:
                return CreateECDheKeyExchange(KeyExchangeAlgorithm.ECDHE_RSA);

            case CipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_RSA_WITH_AES_128_CCM:
            case CipherSuite.TLS_RSA_WITH_AES_128_CCM_8:
            case CipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256:
            case CipherSuite.TLS_RSA_WITH_AES_256_CCM:
            case CipherSuite.TLS_RSA_WITH_AES_256_CCM_8:
            case CipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA:
            case CipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_RSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA:
            case CipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA256:
            case CipherSuite.TLS_RSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_RSA_WITH_ESTREAM_SALSA20_SHA1:
            case CipherSuite.TLS_RSA_WITH_NULL_MD5:
            case CipherSuite.TLS_RSA_WITH_NULL_SHA:
            case CipherSuite.TLS_RSA_WITH_NULL_SHA256:
            case CipherSuite.TLS_RSA_WITH_RC4_128_MD5:
            case CipherSuite.TLS_RSA_WITH_RC4_128_SHA:
            case CipherSuite.TLS_RSA_WITH_SALSA20_SHA1:
            case CipherSuite.TLS_RSA_WITH_SEED_CBC_SHA:
                return createRSAKeyExchange();

            default:
                /*
                 * Note: internal error here; selected a key exchange we don't implement!
                 */
                throw new TlsFatalAlert(AlertDescription.internal_error);
            }
        }

        public override TlsCipher GetCipher()
        {
            switch (mSelectedCipherSuite)
            {
            case CipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_DHE_RSA_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_ECDH_RSA_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_ECDHE_RSA_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.cls_3DES_EDE_CBC, MacAlgorithm.hmac_sha1);

            case CipherSuite.TLS_DHE_RSA_WITH_CHACHA20_POLY1305_SHA256:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256:
            case CipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AEAD_CHACHA20_POLY1305, MacAlgorithm.cls_null);

            case CipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_128_CBC, MacAlgorithm.hmac_sha1);

            case CipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_128_CBC, MacAlgorithm.hmac_sha256);

            case CipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM:
            case CipherSuite.TLS_RSA_WITH_AES_128_CCM:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_128_CCM, MacAlgorithm.cls_null);

            case CipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM_8:
            case CipherSuite.TLS_RSA_WITH_AES_128_CCM_8:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_128_CCM_8, MacAlgorithm.cls_null);

            case CipherSuite.TLS_DH_DSS_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_DH_RSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_DHE_DSS_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_ECDH_RSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_128_GCM, MacAlgorithm.cls_null);

            case CipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_256_CBC, MacAlgorithm.hmac_sha1);

            case CipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA256:
            case CipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA256:
            case CipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA256:
            case CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_256_CBC, MacAlgorithm.hmac_sha256);

            case CipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_256_CBC, MacAlgorithm.hmac_sha384);

            case CipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM:
            case CipherSuite.TLS_RSA_WITH_AES_256_CCM:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_256_CCM, MacAlgorithm.cls_null);

            case CipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM_8:
            case CipherSuite.TLS_RSA_WITH_AES_256_CCM_8:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_256_CCM_8, MacAlgorithm.cls_null);

            case CipherSuite.TLS_DH_DSS_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_DH_RSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_DHE_DSS_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_ECDH_RSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_256_GCM, MacAlgorithm.cls_null);

            case CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA:
            case CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA:
            case CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA:
            case CipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.CAMELLIA_128_CBC, MacAlgorithm.hmac_sha1);

            case CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA256:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.CAMELLIA_128_CBC, MacAlgorithm.hmac_sha256);

            case CipherSuite.TLS_RSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_GCM_SHA256:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.CAMELLIA_128_GCM, MacAlgorithm.cls_null);

            case CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA:
            case CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA:
            case CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA:
            case CipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.CAMELLIA_256_CBC, MacAlgorithm.hmac_sha1);

            case CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA256:
            case CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA256:
            case CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA256:
            case CipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA256:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.CAMELLIA_256_CBC, MacAlgorithm.hmac_sha256);

            case CipherSuite.TLS_RSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_GCM_SHA384:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.CAMELLIA_256_GCM, MacAlgorithm.cls_null);

            case CipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_CBC_SHA384:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.CAMELLIA_256_CBC, MacAlgorithm.hmac_sha384);

            case CipherSuite.TLS_DHE_RSA_WITH_ESTREAM_SALSA20_SHA1:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_ESTREAM_SALSA20_SHA1:
            case CipherSuite.TLS_ECDHE_RSA_WITH_ESTREAM_SALSA20_SHA1:
            case CipherSuite.TLS_RSA_WITH_ESTREAM_SALSA20_SHA1:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.ESTREAM_SALSA20, MacAlgorithm.hmac_sha1);

            case CipherSuite.TLS_RSA_WITH_NULL_MD5:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.NULL, MacAlgorithm.hmac_md5);

            case CipherSuite.TLS_ECDH_ECDSA_WITH_NULL_SHA:
            case CipherSuite.TLS_ECDH_RSA_WITH_NULL_SHA:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_NULL_SHA:
            case CipherSuite.TLS_ECDHE_RSA_WITH_NULL_SHA:
            case CipherSuite.TLS_RSA_WITH_NULL_SHA:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.NULL, MacAlgorithm.hmac_sha1);

            case CipherSuite.TLS_RSA_WITH_NULL_SHA256:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.NULL, MacAlgorithm.hmac_sha256);

            case CipherSuite.TLS_RSA_WITH_RC4_128_MD5:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.RC4_128, MacAlgorithm.hmac_md5);

            case CipherSuite.TLS_ECDH_ECDSA_WITH_RC4_128_SHA:
            case CipherSuite.TLS_ECDH_RSA_WITH_RC4_128_SHA:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_RC4_128_SHA:
            case CipherSuite.TLS_ECDHE_RSA_WITH_RC4_128_SHA:
            case CipherSuite.TLS_RSA_WITH_RC4_128_SHA:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.RC4_128, MacAlgorithm.hmac_sha1);

            case CipherSuite.TLS_DHE_RSA_WITH_SALSA20_SHA1:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_SALSA20_SHA1:
            case CipherSuite.TLS_ECDHE_RSA_WITH_SALSA20_SHA1:
            case CipherSuite.TLS_RSA_WITH_SALSA20_SHA1:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.SALSA20, MacAlgorithm.hmac_sha1);

            case CipherSuite.TLS_DH_DSS_WITH_SEED_CBC_SHA:
            case CipherSuite.TLS_DH_RSA_WITH_SEED_CBC_SHA:
            case CipherSuite.TLS_DHE_DSS_WITH_SEED_CBC_SHA:
            case CipherSuite.TLS_DHE_RSA_WITH_SEED_CBC_SHA:
            case CipherSuite.TLS_RSA_WITH_SEED_CBC_SHA:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.SEED_CBC, MacAlgorithm.hmac_sha1);

            default:
                /*
                 * Note: internal error here; selected a cipher suite we don't implement!
                 */
                throw new TlsFatalAlert(AlertDescription.internal_error);
            }
        }

        protected virtual TlsKeyExchange CreateDHKeyExchange(int keyExchange)
        {
            return new TlsDHKeyExchange(keyExchange, mSupportedSignatureAlgorithms, GetDHParameters());
        }

        protected virtual TlsKeyExchange CreateDheKeyExchange(int keyExchange)
        {
            return new TlsDheKeyExchange(keyExchange, mSupportedSignatureAlgorithms, GetDHParameters());
        }

        protected virtual TlsKeyExchange CreateECDHKeyExchange(int keyExchange)
        {
            return new TlsECDHKeyExchange(keyExchange, mSupportedSignatureAlgorithms, mNamedCurves, mClientECPointFormats,
                mServerECPointFormats);
        }

        protected virtual TlsKeyExchange CreateECDheKeyExchange(int keyExchange)
        {
            return new TlsECDheKeyExchange(keyExchange, mSupportedSignatureAlgorithms, mNamedCurves, mClientECPointFormats,
                mServerECPointFormats);
        }

        protected virtual TlsKeyExchange createRSAKeyExchange()
        {
            return new TlsRsaKeyExchange(mSupportedSignatureAlgorithms);
        }
    }
}
