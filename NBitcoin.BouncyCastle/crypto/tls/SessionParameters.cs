using System;
using System.Collections;
using System.IO;

using Org.BouncyCastle.Utilities;

namespace Org.BouncyCastle.Crypto.Tls
{
    public sealed class SessionParameters
    {
        public sealed class Builder
        {
            private int mCipherSuite = -1;
            private short mCompressionAlgorithm = -1;
            private byte[] mMasterSecret = null;
            private Certificate mPeerCertificate = null;
            private byte[] mEncodedServerExtensions = null;

            public Builder()
            {
            }

            public SessionParameters Build()
            {
                Validate(this.mCipherSuite >= 0, "cipherSuite");
                Validate(this.mCompressionAlgorithm >= 0, "compressionAlgorithm");
                Validate(this.mMasterSecret != null, "masterSecret");
                return new SessionParameters(mCipherSuite, (byte)mCompressionAlgorithm, mMasterSecret, mPeerCertificate,
                    mEncodedServerExtensions);
            }

            public Builder SetCipherSuite(int cipherSuite)
            {
                this.mCipherSuite = cipherSuite;
                return this;
            }

            public Builder SetCompressionAlgorithm(byte compressionAlgorithm)
            {
                this.mCompressionAlgorithm = compressionAlgorithm;
                return this;
            }

            public Builder SetMasterSecret(byte[] masterSecret)
            {
                this.mMasterSecret = masterSecret;
                return this;
            }

            public Builder SetPeerCertificate(Certificate peerCertificate)
            {
                this.mPeerCertificate = peerCertificate;
                return this;
            }

            public Builder SetServerExtensions(IDictionary serverExtensions)
            {
                if (serverExtensions == null)
                {
                    mEncodedServerExtensions = null;
                }
                else
                {
                    MemoryStream buf = new MemoryStream();
                    TlsProtocol.WriteExtensions(buf, serverExtensions);
                    mEncodedServerExtensions = buf.ToArray();
                }
                return this;
            }

            private void Validate(bool condition, string parameter)
            {
                if (!condition)
                    throw new InvalidOperationException("Required session parameter '" + parameter + "' not configured");
            }
        }

        private int mCipherSuite;
        private byte mCompressionAlgorithm;
        private byte[] mMasterSecret;
        private Certificate mPeerCertificate;
        private byte[] mEncodedServerExtensions;

        private SessionParameters(int cipherSuite, byte compressionAlgorithm, byte[] masterSecret,
            Certificate peerCertificate, byte[] encodedServerExtensions)
        {
            this.mCipherSuite = cipherSuite;
            this.mCompressionAlgorithm = compressionAlgorithm;
            this.mMasterSecret = Arrays.Clone(masterSecret);
            this.mPeerCertificate = peerCertificate;
            this.mEncodedServerExtensions = encodedServerExtensions;
        }

        public void Clear()
        {
            if (this.mMasterSecret != null)
            {
                Arrays.Fill(this.mMasterSecret, (byte)0);
            }
        }

        public SessionParameters Copy()
        {
            return new SessionParameters(mCipherSuite, mCompressionAlgorithm, mMasterSecret, mPeerCertificate,
                mEncodedServerExtensions);
        }

        public int CipherSuite
        {
            get { return mCipherSuite; }
        }

        public byte CompressionAlgorithm
        {
            get { return mCompressionAlgorithm; }
        }

        public byte[] MasterSecret
        {
            get { return mMasterSecret; }
        }

        public Certificate PeerCertificate
        {
            get { return mPeerCertificate; }
        }

        public IDictionary ReadServerExtensions()
        {
            if (mEncodedServerExtensions == null)
                return null;

            MemoryStream buf = new MemoryStream(mEncodedServerExtensions, false);
            return TlsProtocol.ReadExtensions(buf);
        }
    }
}
