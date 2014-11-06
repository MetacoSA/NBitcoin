using System;
using System.Collections;

namespace Org.BouncyCastle.Crypto.Tls
{
    public abstract class PskTlsClient
        :   AbstractTlsClient
    {
        protected TlsPskIdentity mPskIdentity;

        public PskTlsClient(TlsPskIdentity pskIdentity)
            :   this(new DefaultTlsCipherFactory(), pskIdentity)
        {
        }

        public PskTlsClient(TlsCipherFactory cipherFactory, TlsPskIdentity pskIdentity)
            :   base(cipherFactory)
        {
            this.mPskIdentity = pskIdentity;
        }

        public override int[] GetCipherSuites()
        {
            return new int[]
            {
                CipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA256,
                CipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA,
                CipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA256,
                CipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA
            };
        }

        public override TlsKeyExchange GetKeyExchange()
        {
            switch (mSelectedCipherSuite)
            {
            case CipherSuite.TLS_DHE_PSK_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_DHE_PSK_WITH_AES_128_CCM:
            case CipherSuite.TLS_DHE_PSK_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_DHE_PSK_WITH_AES_256_CCM:
            case CipherSuite.TLS_DHE_PSK_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_DHE_PSK_WITH_ESTREAM_SALSA20_SHA1:
            case CipherSuite.TLS_DHE_PSK_WITH_NULL_SHA:
            case CipherSuite.TLS_DHE_PSK_WITH_NULL_SHA256:
            case CipherSuite.TLS_DHE_PSK_WITH_NULL_SHA384:
            case CipherSuite.TLS_DHE_PSK_WITH_RC4_128_SHA:
            case CipherSuite.TLS_DHE_PSK_WITH_SALSA20_SHA1:
            case CipherSuite.TLS_PSK_DHE_WITH_AES_128_CCM_8:
            case CipherSuite.TLS_PSK_DHE_WITH_AES_256_CCM_8:
                return CreatePskKeyExchange(KeyExchangeAlgorithm.DHE_PSK);

            case CipherSuite.TLS_ECDHE_PSK_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_ECDHE_PSK_WITH_ESTREAM_SALSA20_SHA1:
            case CipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA:
            case CipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA256:
            case CipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA384:
            case CipherSuite.TLS_ECDHE_PSK_WITH_RC4_128_SHA:
            case CipherSuite.TLS_ECDHE_PSK_WITH_SALSA20_SHA1:
                return CreatePskKeyExchange(KeyExchangeAlgorithm.ECDHE_PSK);

            case CipherSuite.TLS_PSK_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_PSK_WITH_AES_128_CCM:
            case CipherSuite.TLS_PSK_WITH_AES_128_CCM_8:
            case CipherSuite.TLS_PSK_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_PSK_WITH_AES_256_CCM:
            case CipherSuite.TLS_PSK_WITH_AES_256_CCM_8:
            case CipherSuite.TLS_PSK_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_PSK_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_PSK_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_PSK_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_PSK_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_PSK_WITH_ESTREAM_SALSA20_SHA1:
            case CipherSuite.TLS_PSK_WITH_NULL_SHA:
            case CipherSuite.TLS_PSK_WITH_NULL_SHA256:
            case CipherSuite.TLS_PSK_WITH_NULL_SHA384:
            case CipherSuite.TLS_PSK_WITH_RC4_128_SHA:
            case CipherSuite.TLS_PSK_WITH_SALSA20_SHA1:
                return CreatePskKeyExchange(KeyExchangeAlgorithm.PSK);

            case CipherSuite.TLS_RSA_PSK_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_RSA_PSK_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_RSA_PSK_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_RSA_PSK_WITH_ESTREAM_SALSA20_SHA1:
            case CipherSuite.TLS_RSA_PSK_WITH_NULL_SHA:
            case CipherSuite.TLS_RSA_PSK_WITH_NULL_SHA256:
            case CipherSuite.TLS_RSA_PSK_WITH_NULL_SHA384:
            case CipherSuite.TLS_RSA_PSK_WITH_RC4_128_SHA:
            case CipherSuite.TLS_RSA_PSK_WITH_SALSA20_SHA1:
                return CreatePskKeyExchange(KeyExchangeAlgorithm.RSA_PSK);

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
            case CipherSuite.TLS_DHE_PSK_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_ECDHE_PSK_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_PSK_WITH_3DES_EDE_CBC_SHA:
            case CipherSuite.TLS_RSA_PSK_WITH_3DES_EDE_CBC_SHA:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.cls_3DES_EDE_CBC, MacAlgorithm.hmac_sha1);

            case CipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA:
            case CipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_128_CBC, MacAlgorithm.hmac_sha1);

            case CipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA256:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_128_CBC, MacAlgorithm.hmac_sha256);

            case CipherSuite.TLS_DHE_PSK_WITH_AES_128_CCM:
            case CipherSuite.TLS_PSK_WITH_AES_128_CCM:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_128_CCM, MacAlgorithm.cls_null);

            case CipherSuite.TLS_PSK_DHE_WITH_AES_128_CCM_8:
            case CipherSuite.TLS_PSK_WITH_AES_128_CCM_8:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_128_CCM_8, MacAlgorithm.cls_null);

            case CipherSuite.TLS_DHE_PSK_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_PSK_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_RSA_PSK_WITH_AES_128_GCM_SHA256:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_128_GCM, MacAlgorithm.cls_null);

            case CipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA:
            case CipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_256_CBC, MacAlgorithm.hmac_sha1);

            case CipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA384:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_256_CBC, MacAlgorithm.hmac_sha384);

            case CipherSuite.TLS_DHE_PSK_WITH_AES_256_CCM:
            case CipherSuite.TLS_PSK_WITH_AES_256_CCM:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_256_CCM, MacAlgorithm.cls_null);

            case CipherSuite.TLS_PSK_DHE_WITH_AES_256_CCM_8:
            case CipherSuite.TLS_PSK_WITH_AES_256_CCM_8:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_256_CCM_8, MacAlgorithm.cls_null);

            case CipherSuite.TLS_DHE_PSK_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_PSK_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_RSA_PSK_WITH_AES_256_GCM_SHA384:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.AES_256_GCM, MacAlgorithm.cls_null);

            case CipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_PSK_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_CBC_SHA256:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.CAMELLIA_128_CBC, MacAlgorithm.hmac_sha256);

            case CipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_PSK_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_GCM_SHA256:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.CAMELLIA_128_GCM, MacAlgorithm.cls_null);

            case CipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_PSK_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_CBC_SHA384:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.CAMELLIA_256_CBC, MacAlgorithm.hmac_sha384);

            case CipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_PSK_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_GCM_SHA384:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.CAMELLIA_256_GCM, MacAlgorithm.cls_null);

            case CipherSuite.TLS_DHE_PSK_WITH_ESTREAM_SALSA20_SHA1:
            case CipherSuite.TLS_ECDHE_PSK_WITH_ESTREAM_SALSA20_SHA1:
            case CipherSuite.TLS_PSK_WITH_ESTREAM_SALSA20_SHA1:
            case CipherSuite.TLS_RSA_PSK_WITH_ESTREAM_SALSA20_SHA1:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.ESTREAM_SALSA20, MacAlgorithm.hmac_sha1);

            case CipherSuite.TLS_DHE_PSK_WITH_NULL_SHA:
            case CipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA:
            case CipherSuite.TLS_PSK_WITH_NULL_SHA:
            case CipherSuite.TLS_RSA_PSK_WITH_NULL_SHA:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.NULL, MacAlgorithm.hmac_sha1);

            case CipherSuite.TLS_DHE_PSK_WITH_NULL_SHA256:
            case CipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA256:
            case CipherSuite.TLS_PSK_WITH_NULL_SHA256:
            case CipherSuite.TLS_RSA_PSK_WITH_NULL_SHA256:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.NULL, MacAlgorithm.hmac_sha256);

            case CipherSuite.TLS_DHE_PSK_WITH_NULL_SHA384:
            case CipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA384:
            case CipherSuite.TLS_PSK_WITH_NULL_SHA384:
            case CipherSuite.TLS_RSA_PSK_WITH_NULL_SHA384:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.NULL, MacAlgorithm.hmac_sha384);

            case CipherSuite.TLS_DHE_PSK_WITH_RC4_128_SHA:
            case CipherSuite.TLS_ECDHE_PSK_WITH_RC4_128_SHA:
            case CipherSuite.TLS_PSK_WITH_RC4_128_SHA:
            case CipherSuite.TLS_RSA_PSK_WITH_RC4_128_SHA:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.RC4_128, MacAlgorithm.hmac_sha1);

            case CipherSuite.TLS_DHE_PSK_WITH_SALSA20_SHA1:
            case CipherSuite.TLS_ECDHE_PSK_WITH_SALSA20_SHA1:
            case CipherSuite.TLS_PSK_WITH_SALSA20_SHA1:
            case CipherSuite.TLS_RSA_PSK_WITH_SALSA20_SHA1:
                return mCipherFactory.CreateCipher(mContext, EncryptionAlgorithm.SALSA20, MacAlgorithm.hmac_sha1);

            default:
                /*
                    * Note: internal error here; the TlsProtocol implementation verifies that the
                    * server-selected cipher suite was in the list of client-offered cipher suites, so if
                    * we now can't produce an implementation, we shouldn't have offered it!
                    */
                throw new TlsFatalAlert(AlertDescription.internal_error);
            }
        }

        protected virtual TlsKeyExchange CreatePskKeyExchange(int keyExchange)
        {
            return new TlsPskKeyExchange(keyExchange, mSupportedSignatureAlgorithms, mPskIdentity, null, mNamedCurves,
                mClientECPointFormats, mServerECPointFormats);
        }
    }
}
