using System;
using System.IO;

using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Utilities;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;

namespace Org.BouncyCastle.Crypto.Tls
{
    public class Chacha20Poly1305
        :   TlsCipher
    {
        protected readonly TlsContext context;

        protected readonly ChaChaEngine encryptCipher;
        protected readonly ChaChaEngine decryptCipher;

        /// <exception cref="IOException"></exception>
        public Chacha20Poly1305(TlsContext context)
        {
            if (!TlsUtilities.IsTlsV12(context))
                throw new TlsFatalAlert(AlertDescription.internal_error);

            this.context = context;

            byte[] key_block = TlsUtilities.CalculateKeyBlock(context, 64);

            KeyParameter client_write_key = new KeyParameter(key_block, 0, 32);
            KeyParameter server_write_key = new KeyParameter(key_block, 32, 32);

            this.encryptCipher = new ChaChaEngine(20);
            this.decryptCipher = new ChaChaEngine(20);

            KeyParameter encryptKey, decryptKey;
            if (context.IsServer)
            {
                encryptKey = server_write_key;
                decryptKey = client_write_key;
            }
            else
            {
                encryptKey = client_write_key;
                decryptKey = server_write_key;
            }

            byte[] dummyNonce = new byte[8];

            this.encryptCipher.Init(true, new ParametersWithIV(encryptKey, dummyNonce));
            this.decryptCipher.Init(false, new ParametersWithIV(decryptKey, dummyNonce));
        }

        public virtual int GetPlaintextLimit(int ciphertextLimit)
        {
            return ciphertextLimit - 16;
        }

        /// <exception cref="IOException"></exception>
        public virtual byte[] EncodePlaintext(long seqNo, byte type, byte[] plaintext, int offset, int len)
        {
            int ciphertextLength = len + 16;

            KeyParameter macKey = InitRecordMac(encryptCipher, true, seqNo);

            byte[] output = new byte[ciphertextLength];
            encryptCipher.ProcessBytes(plaintext, offset, len, output, 0);

            byte[] additionalData = GetAdditionalData(seqNo, type, len);
            byte[] mac = CalculateRecordMac(macKey, additionalData, output, 0, len);
            Array.Copy(mac, 0, output, len, mac.Length);

            return output;
        }

        /// <exception cref="IOException"></exception>
        public virtual byte[] DecodeCiphertext(long seqNo, byte type, byte[] ciphertext, int offset, int len)
        {
            if (GetPlaintextLimit(len) < 0)
                throw new TlsFatalAlert(AlertDescription.decode_error);

            int plaintextLength = len - 16;

            byte[] receivedMAC = Arrays.CopyOfRange(ciphertext, offset + plaintextLength, offset + len);

            KeyParameter macKey = InitRecordMac(decryptCipher, false, seqNo);

            byte[] additionalData = GetAdditionalData(seqNo, type, plaintextLength);
            byte[] calculatedMAC = CalculateRecordMac(macKey, additionalData, ciphertext, offset, plaintextLength);

            if (!Arrays.ConstantTimeAreEqual(calculatedMAC, receivedMAC))
                throw new TlsFatalAlert(AlertDescription.bad_record_mac);

            byte[] output = new byte[plaintextLength];
            decryptCipher.ProcessBytes(ciphertext, offset, plaintextLength, output, 0);

            return output;
        }

        protected virtual KeyParameter InitRecordMac(ChaChaEngine cipher, bool forEncryption, long seqNo)
        {
            byte[] nonce = new byte[8];
            TlsUtilities.WriteUint64(seqNo, nonce, 0);

            cipher.Init(forEncryption, new ParametersWithIV(null, nonce));

            byte[] firstBlock = new byte[64];
            cipher.ProcessBytes(firstBlock, 0, firstBlock.Length, firstBlock, 0);

            // NOTE: The BC implementation puts 'r' after 'k'
            Array.Copy(firstBlock, 0, firstBlock, 32, 16);
            KeyParameter macKey = new KeyParameter(firstBlock, 16, 32);
            Poly1305KeyGenerator.Clamp(macKey.GetKey());
            return macKey;
        }

        protected virtual byte[] CalculateRecordMac(KeyParameter macKey, byte[] additionalData, byte[] buf, int off, int len)
        {
            IMac mac = new Poly1305();
            mac.Init(macKey);

            UpdateRecordMac(mac, additionalData, 0, additionalData.Length);
            UpdateRecordMac(mac, buf, off, len);
            return MacUtilities.DoFinal(mac);
        }

        protected virtual void UpdateRecordMac(IMac mac, byte[] buf, int off, int len)
        {
            mac.BlockUpdate(buf, off, len);

            byte[] longLen = Pack.UInt64_To_LE((ulong)len);
            mac.BlockUpdate(longLen, 0, longLen.Length);
        }

        /// <exception cref="IOException"></exception>
        protected virtual byte[] GetAdditionalData(long seqNo, byte type, int len)
        {
            /*
             * additional_data = seq_num + TLSCompressed.type + TLSCompressed.version +
             * TLSCompressed.length
             */
            byte[] additional_data = new byte[13];
            TlsUtilities.WriteUint64(seqNo, additional_data, 0);
            TlsUtilities.WriteUint8(type, additional_data, 8);
            TlsUtilities.WriteVersion(context.ServerVersion, additional_data, 9);
            TlsUtilities.WriteUint16(len, additional_data, 11);

            return additional_data;
        }
    }
}
