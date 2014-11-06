using System;
using System.Collections;
using System.IO;

namespace Org.BouncyCastle.Crypto.Tls
{
    public abstract class TlsSrpUtilities
    {
        public static void AddSrpExtension(IDictionary extensions, byte[] identity)
        {
            extensions[ExtensionType.srp] = CreateSrpExtension(identity);
        }

        public static byte[] GetSrpExtension(IDictionary extensions)
        {
            byte[] extensionData = TlsUtilities.GetExtensionData(extensions, ExtensionType.srp);
            return extensionData == null ? null : ReadSrpExtension(extensionData);
        }

        public static byte[] CreateSrpExtension(byte[] identity)
        {
            if (identity == null)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            return TlsUtilities.EncodeOpaque8(identity);
        }

        public static byte[] ReadSrpExtension(byte[] extensionData)
        {
            if (extensionData == null)
                throw new ArgumentNullException("extensionData");

            MemoryStream buf = new MemoryStream(extensionData, false);
            byte[] identity = TlsUtilities.ReadOpaque8(buf);

            TlsProtocol.AssertEmpty(buf);

            return identity;
        }
    }
}
