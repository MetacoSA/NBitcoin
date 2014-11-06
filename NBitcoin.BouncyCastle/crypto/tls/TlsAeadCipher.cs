using System;
using System.IO;

using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Utilities;

namespace Org.BouncyCastle.Crypto.Tls
{
    public class TlsAeadCipher
        :   TlsCipher
    {
        protected readonly TlsContext context;
        protected readonly int macSize;
        protected readonly int nonce_explicit_length;

        protected readonly IAeadBlockCipher encryptCipher;
        protected readonly IAeadBlockCipher decryptCipher;

        protected readonly byte[] encryptImplicitNonce, decryptImplicitNonce;

        /// <exception cref="IOException"></exception>
        public TlsAeadCipher(TlsContext context, IAeadBlockCipher clientWriteCipher, IAeadBlockCipher serverWriteCipher,
            int cipherKeySize, int macSize)
        {
            if (!TlsUtilities.IsTlsV12(context))
                throw new TlsFatalAlert(AlertDescription.internal_error);

            this.context = context;
            this.macSize = macSize;

            // NOTE: Valid for RFC 5288/6655 ciphers but may need review for other AEAD ciphers
            this.nonce_explicit_length = 8;

            // TODO SecurityParameters.fixed_iv_length
            int fixed_iv_length = 4;

            int key_block_size = (2 * cipherKeySize) + (2 * fixed_iv_length);

            byte[] key_block = TlsUtilities.CalculateKeyBlock(context, key_block_size);

            int offset = 0;

            KeyParameter client_write_key = new KeyParameter(key_block, offset, cipherKeySize);
            offset += cipherKeySize;
            KeyParameter server_write_key = new KeyParameter(key_block, offset, cipherKeySize);
            offset += cipherKeySize;
            byte[] client_write_IV = Arrays.CopyOfRange(key_block, offset, offset + fixed_iv_length);
            offset += fixed_iv_length;
            byte[] server_write_IV = Arrays.CopyOfRange(key_block, offset, offset + fixed_iv_length);
            offset += fixed_iv_length;

            if (offset != key_block_size)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            KeyParameter encryptKey, decryptKey;
            if (context.IsServer)
            {
                this.encryptCipher = serverWriteCipher;
                this.decryptCipher = clientWriteCipher;
                this.encryptImplicitNonce = server_write_IV;
                this.decryptImplicitNonce = client_write_IV;
                encryptKey = server_write_key;
                decryptKey = client_write_key;
            }
            else
            {
                this.encryptCipher = clientWriteCipher;
                this.decryptCipher = serverWriteCipher;
                this.encryptImplicitNonce = client_write_IV;
                this.decryptImplicitNonce = server_write_IV;
                encryptKey = client_write_key;
                decryptKey = server_write_key;
            }

            byte[] dummyNonce = new byte[fixed_iv_length + nonce_explicit_length];

            this.encryptCipher.Init(true, new AeadParameters(encryptKey, 8 * macSize, dummyNonce));
            this.decryptCipher.Init(false, new AeadParameters(decryptKey, 8 * macSize, dummyNonce));
        }

        public virtual int GetPlaintextLimit(int ciphertextLimit)
        {
            // TODO We ought to be able to ask the decryptCipher (independently of it's current state!)
            return ciphertextLimit - macSize - nonce_explicit_length;
        }

        /// <exception cref="IOException"></exception>
        public virtual byte[] EncodePlaintext(long seqNo, byte type, byte[] plaintext, int offset, int len)
        {
            byte[] nonce = new byte[this.encryptImplicitNonce.Length + nonce_explicit_length];
            Array.Copy(encryptImplicitNonce, 0, nonce, 0, encryptImplicitNonce.Length);

            /*
             * RFC 5288/6655 The nonce_explicit MAY be the 64-bit sequence number.
             * 
             * (May need review for other AEAD ciphers).
             */
            TlsUtilities.WriteUint64(seqNo, nonce, encryptImplicitNonce.Length);

            int plaintextOffset = offset;
            int plaintextLength = len;
            int ciphertextLength = encryptCipher.GetOutputSize(plaintextLength);

            byte[] output = new byte[nonce_explicit_length + ciphertextLength];
            Array.Copy(nonce, encryptImplicitNonce.Length, output, 0, nonce_explicit_length);
            int outputPos = nonce_explicit_length;

            byte[] additionalData = GetAdditionalData(seqNo, type, plaintextLength);
            AeadParameters parameters = new AeadParameters(null, 8 * macSize, nonce, additionalData);

            try
            {
                encryptCipher.Init(true, parameters);
                outputPos += encryptCipher.ProcessBytes(plaintext, plaintextOffset, plaintextLength, output, outputPos);
                outputPos += encryptCipher.DoFinal(output, outputPos);
            }
            catch (Exception e)
            {
                throw new TlsFatalAlert(AlertDescription.internal_error, e);
            }

            if (outputPos != output.Length)
            {
                // NOTE: Existing AEAD cipher implementations all give exact output lengths
                throw new TlsFatalAlert(AlertDescription.internal_error);
            }

            return output;
        }

        /// <exception cref="IOException"></exception>
        public virtual byte[] DecodeCiphertext(long seqNo, byte type, byte[] ciphertext, int offset, int len)
        {
            if (GetPlaintextLimit(len) < 0)
                throw new TlsFatalAlert(AlertDescription.decode_error);

            byte[] nonce = new byte[this.decryptImplicitNonce.Length + nonce_explicit_length];
            Array.Copy(decryptImplicitNonce, 0, nonce, 0, decryptImplicitNonce.Length);
            Array.Copy(ciphertext, offset, nonce, decryptImplicitNonce.Length, nonce_explicit_length);

            int ciphertextOffset = offset + nonce_explicit_length;
            int ciphertextLength = len - nonce_explicit_length;
            int plaintextLength = decryptCipher.GetOutputSize(ciphertextLength);

            byte[] output = new byte[plaintextLength];
            int outputPos = 0;

            byte[] additionalData = GetAdditionalData(seqNo, type, plaintextLength);
            AeadParameters parameters = new AeadParameters(null, 8 * macSize, nonce, additionalData);

            try
            {
                decryptCipher.Init(false, parameters);
                outputPos += decryptCipher.ProcessBytes(ciphertext, ciphertextOffset, ciphertextLength, output, outputPos);
                outputPos += decryptCipher.DoFinal(output, outputPos);
            }
            catch (Exception e)
            {
                throw new TlsFatalAlert(AlertDescription.bad_record_mac, e);
            }

            if (outputPos != output.Length)
            {
                // NOTE: Existing AEAD cipher implementations all give exact output lengths
                throw new TlsFatalAlert(AlertDescription.internal_error);
            }

            return output;
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
