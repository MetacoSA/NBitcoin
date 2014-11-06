using System;
using System.Collections;
using System.IO;

using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;

namespace Org.BouncyCastle.Crypto.Tls
{
    public abstract class DtlsProtocol
    {
        protected readonly SecureRandom mSecureRandom;

        protected DtlsProtocol(SecureRandom secureRandom)
        {
            if (secureRandom == null)
                throw new ArgumentNullException("secureRandom");

            this.mSecureRandom = secureRandom;
        }

        /// <exception cref="IOException"/>
        protected virtual void ProcessFinished(byte[] body, byte[] expected_verify_data)
        {
            MemoryStream buf = new MemoryStream(body, false);

            byte[] verify_data = TlsUtilities.ReadFully(expected_verify_data.Length, buf);

            TlsProtocol.AssertEmpty(buf);

            if (!Arrays.ConstantTimeAreEqual(expected_verify_data, verify_data))
                throw new TlsFatalAlert(AlertDescription.handshake_failure);
        }

        /// <exception cref="IOException"/>
        protected static short EvaluateMaxFragmentLengthExtension(IDictionary clientExtensions, IDictionary serverExtensions,
            byte alertDescription)
        {
            short maxFragmentLength = TlsExtensionsUtilities.GetMaxFragmentLengthExtension(serverExtensions);
            if (maxFragmentLength >= 0 && maxFragmentLength != TlsExtensionsUtilities.GetMaxFragmentLengthExtension(clientExtensions))
                throw new TlsFatalAlert(alertDescription);
            return maxFragmentLength;
        }

        /// <exception cref="IOException"/>
        protected static byte[] GenerateCertificate(Certificate certificate)
        {
            MemoryStream buf = new MemoryStream();
            certificate.Encode(buf);
            return buf.ToArray();
        }

        /// <exception cref="IOException"/>
        protected static byte[] GenerateSupplementalData(IList supplementalData)
        {
            MemoryStream buf = new MemoryStream();
            TlsProtocol.WriteSupplementalData(buf, supplementalData);
            return buf.ToArray();
        }

        /// <exception cref="IOException"/>
        protected static void ValidateSelectedCipherSuite(int selectedCipherSuite, byte alertDescription)
        {
            switch (TlsUtilities.GetEncryptionAlgorithm(selectedCipherSuite))
            {
            case EncryptionAlgorithm.RC4_40:
            case EncryptionAlgorithm.RC4_128:
                throw new TlsFatalAlert(alertDescription);
            }
        }
    }
}
