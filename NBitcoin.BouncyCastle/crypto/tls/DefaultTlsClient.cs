using System;
using System.Collections;
using System.IO;

using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Org.BouncyCastle.Crypto.Tls
{
    public abstract class DefaultTlsClient
        :   AbstractTlsClient
    {
        public DefaultTlsClient()
            :   base()
        {
        }

        public DefaultTlsClient(TlsCipherFactory cipherFactory)
            :   base(cipherFactory)
        {
        }

        public override int[] GetCipherSuites()
        {
            return new int[]
            {
                CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
                CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256,
                CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA,
                CipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256,
                CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256,
                CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA,
            };
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
                return CreateRsaKeyExchange();

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
                    * Note: internal error here; the TlsProtocol implementation verifies that the
                    * server-selected cipher suite was in the list of client-offered cipher suites, so if
                    * we now can't produce an implementation, we shouldn't have offered it!
                    */
                throw new TlsFatalAlert(AlertDescription.internal_error);
            }
        }

        protected virtual TlsKeyExchange CreateDHKeyExchange(int keyExchange)
        {
            return new TlsDHKeyExchange(keyExchange, mSupportedSignatureAlgorithms, null);
        }

        protected virtual TlsKeyExchange CreateDheKeyExchange(int keyExchange)
        {
            return new TlsDheKeyExchange(keyExchange, mSupportedSignatureAlgorithms, null);
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

        protected virtual TlsKeyExchange CreateRsaKeyExchange()
        {
            return new TlsRsaKeyExchange(mSupportedSignatureAlgorithms);
        }
    }
}
