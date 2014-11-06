using System;
using System.Collections;
using System.IO;

using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;

namespace Org.BouncyCastle.Crypto.Tls
{
    public abstract class TlsProtocol
    {
        private static readonly string TLS_ERROR_MESSAGE = "Internal TLS error, this could be an attack";

        /*
         * Our Connection states
         */
        protected const short CS_START = 0;
        protected const short CS_CLIENT_HELLO = 1;
        protected const short CS_SERVER_HELLO = 2;
        protected const short CS_SERVER_SUPPLEMENTAL_DATA = 3;
        protected const short CS_SERVER_CERTIFICATE = 4;
        protected const short CS_CERTIFICATE_STATUS = 5;
        protected const short CS_SERVER_KEY_EXCHANGE = 6;
        protected const short CS_CERTIFICATE_REQUEST = 7;
        protected const short CS_SERVER_HELLO_DONE = 8;
        protected const short CS_CLIENT_SUPPLEMENTAL_DATA = 9;
        protected const short CS_CLIENT_CERTIFICATE = 10;
        protected const short CS_CLIENT_KEY_EXCHANGE = 11;
        protected const short CS_CERTIFICATE_VERIFY = 12;
        protected const short CS_CLIENT_FINISHED = 13;
        protected const short CS_SERVER_SESSION_TICKET = 14;
        protected const short CS_SERVER_FINISHED = 15;
        protected const short CS_END = 16;

        /*
         * Queues for data from some protocols.
         */
        private ByteQueue mApplicationDataQueue = new ByteQueue();
        private ByteQueue mAlertQueue = new ByteQueue(2);
        private ByteQueue mHandshakeQueue = new ByteQueue();
    //    private ByteQueue mHeartbeatQueue = new ByteQueue();

        /*
         * The Record Stream we use
         */
        internal RecordStream mRecordStream;
        protected SecureRandom mSecureRandom;

        private TlsStream mTlsStream = null;

        private volatile bool mClosed = false;
        private volatile bool mFailedWithError = false;
        private volatile bool mAppDataReady = false;
        private volatile bool mSplitApplicationDataRecords = true;
        private byte[] mExpectedVerifyData = null;

        protected TlsSession mTlsSession = null;
        protected SessionParameters mSessionParameters = null;
        protected SecurityParameters mSecurityParameters = null;
        protected Certificate mPeerCertificate = null;

        protected int[] mOfferedCipherSuites = null;
        protected byte[] mOfferedCompressionMethods = null;
        protected IDictionary mClientExtensions = null;
        protected IDictionary mServerExtensions = null;

        protected short mConnectionState = CS_START;
        protected bool mResumedSession = false;
        protected bool mReceivedChangeCipherSpec = false;
        protected bool mSecureRenegotiation = false;
        protected bool mAllowCertificateStatus = false;
        protected bool mExpectSessionTicket = false;

        public TlsProtocol(Stream stream, SecureRandom secureRandom)
            :   this(stream, stream, secureRandom)
        {
        }

        public TlsProtocol(Stream input, Stream output, SecureRandom secureRandom)
        {
            this.mRecordStream = new RecordStream(this, input, output);
            this.mSecureRandom = secureRandom;
        }

        protected abstract TlsContext Context { get; }

        internal abstract AbstractTlsContext ContextAdmin { get; }

        protected abstract TlsPeer Peer { get; }

        protected virtual void HandleChangeCipherSpecMessage()
        {
        }

        protected abstract void HandleHandshakeMessage(byte type, byte[] buf);

        protected virtual void HandleWarningMessage(byte description)
        {
        }

        protected virtual void CleanupHandshake()
        {
            if (this.mExpectedVerifyData != null)
            {
                Arrays.Fill(this.mExpectedVerifyData, (byte)0);
                this.mExpectedVerifyData = null;
            }

            this.mSecurityParameters.Clear();
            this.mPeerCertificate = null;

            this.mOfferedCipherSuites = null;
            this.mOfferedCompressionMethods = null;
            this.mClientExtensions = null;
            this.mServerExtensions = null;

            this.mResumedSession = false;
            this.mReceivedChangeCipherSpec = false;
            this.mSecureRenegotiation = false;
            this.mAllowCertificateStatus = false;
            this.mExpectSessionTicket = false;
        }

        protected virtual void CompleteHandshake()
        {
            try
            {
                /*
                 * We will now read data, until we have completed the handshake.
                 */
                while (this.mConnectionState != CS_END)
                {
                    if (this.mClosed)
                    {
                        // TODO What kind of exception/alert?
                    }

                    SafeReadRecord();
                }

                this.mRecordStream.FinaliseHandshake();

                this.mSplitApplicationDataRecords = !TlsUtilities.IsTlsV11(Context);

                /*
                 * If this was an initial handshake, we are now ready to send and receive application data.
                 */
                if (!mAppDataReady)
                {
                    this.mAppDataReady = true;

                    this.mTlsStream = new TlsStream(this);
                }

                if (this.mTlsSession != null)
                {
                    if (this.mSessionParameters == null)
                    {
                        this.mSessionParameters = new SessionParameters.Builder()
                            .SetCipherSuite(this.mSecurityParameters.cipherSuite)
                            .SetCompressionAlgorithm(this.mSecurityParameters.compressionAlgorithm)
                            .SetMasterSecret(this.mSecurityParameters.masterSecret)
                            .SetPeerCertificate(this.mPeerCertificate)
                            // TODO Consider filtering extensions that aren't relevant to resumed sessions
                            .SetServerExtensions(this.mServerExtensions)
                            .Build();

                        this.mTlsSession = new TlsSessionImpl(this.mTlsSession.SessionID, this.mSessionParameters);
                    }

                    ContextAdmin.SetResumableSession(this.mTlsSession);
                }

                Peer.NotifyHandshakeComplete();
            }
            finally
            {
                CleanupHandshake();
            }
        }

        protected internal void ProcessRecord(byte protocol, byte[] buf, int offset, int len)
        {
            /*
             * Have a look at the protocol type, and add it to the correct queue.
             */
            switch (protocol)
            {
            case ContentType.alert:
            {
                mAlertQueue.AddData(buf, offset, len);
                ProcessAlert();
                break;
            }
            case ContentType.application_data:
            {
                if (!mAppDataReady)
                    throw new TlsFatalAlert(AlertDescription.unexpected_message);

                mApplicationDataQueue.AddData(buf, offset, len);
                ProcessApplicationData();
                break;
            }
            case ContentType.change_cipher_spec:
            {
                ProcessChangeCipherSpec(buf, offset, len);
                break;
            }
            case ContentType.handshake:
            {
                mHandshakeQueue.AddData(buf, offset, len);
                ProcessHandshake();
                break;
            }
            case ContentType.heartbeat:
            {
                if (!mAppDataReady)
                    throw new TlsFatalAlert(AlertDescription.unexpected_message);

                // TODO[RFC 6520]
    //            mHeartbeatQueue.AddData(buf, offset, len);
    //            ProcessHeartbeat();
                break;
            }
            default:
                /*
                 * Uh, we don't know this protocol.
                 * 
                 * RFC2246 defines on page 13, that we should ignore this.
                 */
                break;
            }
        }

        private void ProcessHandshake()
        {
            bool read;
            do
            {
                read = false;
                /*
                 * We need the first 4 bytes, they contain type and length of the message.
                 */
                if (mHandshakeQueue.Available >= 4)
                {
                    byte[] beginning = new byte[4];
                    mHandshakeQueue.Read(beginning, 0, 4, 0);
                    byte type = TlsUtilities.ReadUint8(beginning, 0);
                    int len = TlsUtilities.ReadUint24(beginning, 1);

                    /*
                     * Check if we have enough bytes in the buffer to read the full message.
                     */
                    if (mHandshakeQueue.Available >= (len + 4))
                    {
                        /*
                         * Read the message.
                         */
                        byte[] buf = mHandshakeQueue.RemoveData(len, 4);

                        /*
                         * RFC 2246 7.4.9. The value handshake_messages includes all handshake messages
                         * starting at client hello up to, but not including, this finished message.
                         * [..] Note: [Also,] Hello Request messages are omitted from handshake hashes.
                         */
                        switch (type)
                        {
                        case HandshakeType.hello_request:
                            break;
                        case HandshakeType.finished:
                        default:
                            if (type == HandshakeType.finished && this.mExpectedVerifyData == null)
                            {
                                this.mExpectedVerifyData = CreateVerifyData(!Context.IsServer);
                            }

                            mRecordStream.UpdateHandshakeData(beginning, 0, 4);
                            mRecordStream.UpdateHandshakeData(buf, 0, len);
                            break;
                        }

                        /*
                         * Now, parse the message.
                         */
                        HandleHandshakeMessage(type, buf);
                        read = true;
                    }
                }
            }
            while (read);
        }

        private void ProcessApplicationData()
        {
            /*
             * There is nothing we need to do here.
             * 
             * This function could be used for callbacks when application data arrives in the future.
             */
        }

        private void ProcessAlert()
        {
            while (mAlertQueue.Available >= 2)
            {
                /*
                 * An alert is always 2 bytes. Read the alert.
                 */
                byte[] tmp = mAlertQueue.RemoveData(2, 0);
                byte level = tmp[0];
                byte description = tmp[1];

                Peer.NotifyAlertReceived(level, description);

                if (level == AlertLevel.fatal)
                {
                    /*
                     * RFC 2246 7.2.1. The session becomes unresumable if any connection is terminated
                     * without proper close_notify messages with level equal to warning.
                     */
                    InvalidateSession();

                    this.mFailedWithError = true;
                    this.mClosed = true;

                    mRecordStream.SafeClose();

                    throw new IOException(TLS_ERROR_MESSAGE);
                }
                else
                {

                    /*
                     * RFC 5246 7.2.1. The other party MUST respond with a close_notify alert of its own
                     * and close down the connection immediately, discarding any pending writes.
                     */
                    // TODO Can close_notify be a fatal alert?
                    if (description == AlertDescription.close_notify)
                    {
                        HandleClose(false);
                    }

                    /*
                     * If it is just a warning, we continue.
                     */
                    HandleWarningMessage(description);
                }
            }
        }

        /**
         * This method is called, when a change cipher spec message is received.
         *
         * @throws IOException If the message has an invalid content or the handshake is not in the correct
         * state.
         */
        private void ProcessChangeCipherSpec(byte[] buf, int off, int len)
        {
            for (int i = 0; i < len; ++i)
            {
                byte message = TlsUtilities.ReadUint8(buf, off + i);

                if (message != ChangeCipherSpec.change_cipher_spec)
                    throw new TlsFatalAlert(AlertDescription.decode_error);

                if (this.mReceivedChangeCipherSpec
                    || mAlertQueue.Available > 0
                    || mHandshakeQueue.Available > 0)
                {
                    throw new TlsFatalAlert(AlertDescription.unexpected_message);
                }

                mRecordStream.ReceivedReadCipherSpec();

                this.mReceivedChangeCipherSpec = true;

                HandleChangeCipherSpecMessage();
            }
        }

        protected internal virtual int ApplicationDataAvailable()
        {
            return mApplicationDataQueue.Available;
        }

        /**
         * Read data from the network. The method will return immediately, if there is still some data
         * left in the buffer, or block until some application data has been read from the network.
         *
         * @param buf    The buffer where the data will be copied to.
         * @param offset The position where the data will be placed in the buffer.
         * @param len    The maximum number of bytes to read.
         * @return The number of bytes read.
         * @throws IOException If something goes wrong during reading data.
         */
        protected internal virtual int ReadApplicationData(byte[] buf, int offset, int len)
        {
            if (len < 1)
                return 0;

            while (mApplicationDataQueue.Available == 0)
            {
                /*
                 * We need to read some data.
                 */
                if (this.mClosed)
                {
                    if (this.mFailedWithError)
                    {
                        /*
                         * Something went terribly wrong, we should throw an IOException
                         */
                        throw new IOException(TLS_ERROR_MESSAGE);
                    }

                    /*
                     * Connection has been closed, there is no more data to read.
                     */
                    return 0;
                }

                SafeReadRecord();
            }

            len = System.Math.Min(len, mApplicationDataQueue.Available);
            mApplicationDataQueue.RemoveData(buf, offset, len, 0);
            return len;
        }

        protected virtual void SafeReadRecord()
        {
            try
            {
                if (!mRecordStream.ReadRecord())
                {
                    // TODO It would be nicer to allow graceful connection close if between records
    //                this.FailWithError(AlertLevel.warning, AlertDescription.close_notify);
                    throw new EndOfStreamException();
                }
            }
            catch (TlsFatalAlert e)
            {
                if (!mClosed)
                {
                    this.FailWithError(AlertLevel.fatal, e.AlertDescription, "Failed to read record", e);
                }
                throw e;
            }
            catch (Exception e)
            {
                if (!mClosed)
                {
                    this.FailWithError(AlertLevel.fatal, AlertDescription.internal_error, "Failed to read record", e);
                }
                throw e;
            }
        }

        protected virtual void SafeWriteRecord(byte type, byte[] buf, int offset, int len)
        {
            try
            {
                mRecordStream.WriteRecord(type, buf, offset, len);
            }
            catch (TlsFatalAlert e)
            {
                if (!mClosed)
                {
                    this.FailWithError(AlertLevel.fatal, e.AlertDescription, "Failed to write record", e);
                }
                throw e;
            }
            catch (Exception e)
            {
                if (!mClosed)
                {
                    this.FailWithError(AlertLevel.fatal, AlertDescription.internal_error, "Failed to write record", e);
                }
                throw e;
            }
        }

        /**
         * Send some application data to the remote system.
         * <p/>
         * The method will handle fragmentation internally.
         *
         * @param buf    The buffer with the data.
         * @param offset The position in the buffer where the data is placed.
         * @param len    The length of the data.
         * @throws IOException If something goes wrong during sending.
         */
        protected internal virtual void WriteData(byte[] buf, int offset, int len)
        {
            if (this.mClosed)
            {
                if (this.mFailedWithError)
                    throw new IOException(TLS_ERROR_MESSAGE);

                throw new IOException("Sorry, connection has been closed, you cannot write more data");
            }

            while (len > 0)
            {
                /*
                 * RFC 5246 6.2.1. Zero-length fragments of Application data MAY be sent as they are
                 * potentially useful as a traffic analysis countermeasure.
                 * 
                 * NOTE: Actually, implementations appear to have settled on 1/n-1 record splitting.
                 */

                if (this.mSplitApplicationDataRecords)
                {
                    /*
                     * Protect against known IV attack!
                     * 
                     * DO NOT REMOVE THIS CODE, EXCEPT YOU KNOW EXACTLY WHAT YOU ARE DOING HERE.
                     */
                    SafeWriteRecord(ContentType.application_data, buf, offset, 1);
                    ++offset;
                    --len;
                }

                if (len > 0)
                {
                    // Fragment data according to the current fragment limit.
                    int toWrite = System.Math.Min(len, mRecordStream.GetPlaintextLimit());
                    SafeWriteRecord(ContentType.application_data, buf, offset, toWrite);
                    offset += toWrite;
                    len -= toWrite;
                }
            }
        }

        protected virtual void WriteHandshakeMessage(byte[] buf, int off, int len)
        {
            while (len > 0)
            {
                // Fragment data according to the current fragment limit.
                int toWrite = System.Math.Min(len, mRecordStream.GetPlaintextLimit());
                SafeWriteRecord(ContentType.handshake, buf, off, toWrite);
                off += toWrite;
                len -= toWrite;
            }
        }

        /// <summary>The secure bidirectional stream for this connection</summary>
        public virtual Stream Stream
        {
            get { return this.mTlsStream; }
        }

        /**
         * Terminate this connection with an alert. Can be used for normal closure too.
         * 
         * @param alertLevel
         *            See {@link AlertLevel} for values.
         * @param alertDescription
         *            See {@link AlertDescription} for values.
         * @throws IOException
         *             If alert was fatal.
         */
        protected virtual void FailWithError(byte alertLevel, byte alertDescription, string message, Exception cause)
        {
            /*
             * Check if the connection is still open.
             */
            if (!mClosed)
            {
                /*
                 * Prepare the message
                 */
                this.mClosed = true;

                if (alertLevel == AlertLevel.fatal)
                {
                    /*
                     * RFC 2246 7.2.1. The session becomes unresumable if any connection is terminated
                     * without proper close_notify messages with level equal to warning.
                     */
                    // TODO This isn't quite in the right place. Also, as of TLS 1.1 the above is obsolete.
                    InvalidateSession();

                    this.mFailedWithError = true;
                }
                RaiseAlert(alertLevel, alertDescription, message, cause);
                mRecordStream.SafeClose();
                if (alertLevel != AlertLevel.fatal)
                {
                    return;
                }
            }

            throw new IOException(TLS_ERROR_MESSAGE);
        }

        protected virtual void InvalidateSession()
        {
            if (this.mSessionParameters != null)
            {
                this.mSessionParameters.Clear();
                this.mSessionParameters = null;
            }

            if (this.mTlsSession != null)
            {
                this.mTlsSession.Invalidate();
                this.mTlsSession = null;
            }
        }

        protected virtual void ProcessFinishedMessage(MemoryStream buf)
        {
            byte[] verify_data = TlsUtilities.ReadFully(mExpectedVerifyData.Length, buf);

            AssertEmpty(buf);

            /*
             * Compare both checksums.
             */
            if (!Arrays.ConstantTimeAreEqual(mExpectedVerifyData, verify_data))
            {
                /*
                 * Wrong checksum in the finished message.
                 */
                throw new TlsFatalAlert(AlertDescription.decrypt_error);
            }
        }

        protected virtual void RaiseAlert(byte alertLevel, byte alertDescription, string message, Exception cause)
        {
            Peer.NotifyAlertRaised(alertLevel, alertDescription, message, cause);

            byte[] error = new byte[]{ alertLevel, alertDescription };

            SafeWriteRecord(ContentType.alert, error, 0, 2);
        }

        protected virtual void RaiseWarning(byte alertDescription, string message)
        {
            RaiseAlert(AlertLevel.warning, alertDescription, message, null);
        }

        protected virtual void SendCertificateMessage(Certificate certificate)
        {
            if (certificate == null)
            {
                certificate = Certificate.EmptyChain;
            }

            if (certificate.IsEmpty)
            {
                TlsContext context = Context;
                if (!context.IsServer)
                {
                    ProtocolVersion serverVersion = Context.ServerVersion;
                    if (serverVersion.IsSsl)
                    {
                        string errorMessage = serverVersion.ToString() + " client didn't provide credentials";
                        RaiseWarning(AlertDescription.no_certificate, errorMessage);
                        return;
                    }
                }
            }

            HandshakeMessage message = new HandshakeMessage(HandshakeType.certificate);

            certificate.Encode(message);

            message.WriteToRecordStream(this);
        }

        protected virtual void SendChangeCipherSpecMessage()
        {
            byte[] message = new byte[]{ 1 };
            SafeWriteRecord(ContentType.change_cipher_spec, message, 0, message.Length);
            mRecordStream.SentWriteCipherSpec();
        }

        protected virtual void SendFinishedMessage()
        {
            byte[] verify_data = CreateVerifyData(Context.IsServer);

            HandshakeMessage message = new HandshakeMessage(HandshakeType.finished, verify_data.Length);

            message.Write(verify_data, 0, verify_data.Length);

            message.WriteToRecordStream(this);
        }

        protected virtual void SendSupplementalDataMessage(IList supplementalData)
        {
            HandshakeMessage message = new HandshakeMessage(HandshakeType.supplemental_data);

            WriteSupplementalData(message, supplementalData);

            message.WriteToRecordStream(this);
        }

        protected virtual byte[] CreateVerifyData(bool isServer)
        {
            TlsContext context = Context;
            string asciiLabel = isServer ? ExporterLabel.server_finished : ExporterLabel.client_finished;
            byte[] sslSender = isServer ? TlsUtilities.SSL_SERVER : TlsUtilities.SSL_CLIENT;
            byte[] hash = GetCurrentPrfHash(context, mRecordStream.HandshakeHash, sslSender);
            return TlsUtilities.CalculateVerifyData(context, asciiLabel, hash);
        }

        /**
         * Closes this connection.
         *
         * @throws IOException If something goes wrong during closing.
         */
        public virtual void Close()
        {
            HandleClose(true);
        }

        protected virtual void HandleClose(bool user_canceled)
        {
            if (!mClosed)
            {
                if (user_canceled && !mAppDataReady)
                {
                    RaiseWarning(AlertDescription.user_canceled, "User canceled handshake");
                }
                this.FailWithError(AlertLevel.warning, AlertDescription.close_notify, "Connection closed", null);
            }
        }

        protected internal virtual void Flush()
        {
            mRecordStream.Flush();
        }

        protected internal virtual bool IsClosed
        {
            get { return mClosed; }
        }

        protected virtual short ProcessMaxFragmentLengthExtension(IDictionary clientExtensions, IDictionary serverExtensions,
            byte alertDescription)
        {
            short maxFragmentLength = TlsExtensionsUtilities.GetMaxFragmentLengthExtension(serverExtensions);
            if (maxFragmentLength >= 0 && !this.mResumedSession)
            {
                if (maxFragmentLength != TlsExtensionsUtilities.GetMaxFragmentLengthExtension(clientExtensions))
                    throw new TlsFatalAlert(alertDescription);
            }
            return maxFragmentLength;
        }

        /**
         * Make sure the InputStream 'buf' now empty. Fail otherwise.
         *
         * @param buf The InputStream to check.
         * @throws IOException If 'buf' is not empty.
         */
        protected internal static void AssertEmpty(MemoryStream buf)
        {
            if (buf.Position < buf.Length)
                throw new TlsFatalAlert(AlertDescription.decode_error);
        }

        protected internal static byte[] CreateRandomBlock(bool useGmtUnixTime, IRandomGenerator randomGenerator)
        {
            byte[] result = new byte[32];
            randomGenerator.NextBytes(result);

            if (useGmtUnixTime)
            {
                TlsUtilities.WriteGmtUnixTime(result, 0);
            }

            return result;
        }

        protected internal static byte[] CreateRenegotiationInfo(byte[] renegotiated_connection)
        {
            return TlsUtilities.EncodeOpaque8(renegotiated_connection);
        }

        protected internal static void EstablishMasterSecret(TlsContext context, TlsKeyExchange keyExchange)
        {
            byte[] pre_master_secret = keyExchange.GeneratePremasterSecret();

            try
            {
                context.SecurityParameters.masterSecret = TlsUtilities.CalculateMasterSecret(context, pre_master_secret);
            }
            finally
            {
                // TODO Is there a way to ensure the data is really overwritten?
                /*
                 * RFC 2246 8.1. The pre_master_secret should be deleted from memory once the
                 * master_secret has been computed.
                 */
                if (pre_master_secret != null)
                {
                    Arrays.Fill(pre_master_secret, (byte)0);
                }
            }
        }

        /**
         * 'sender' only relevant to SSLv3
         */
        protected internal static byte[] GetCurrentPrfHash(TlsContext context, TlsHandshakeHash handshakeHash, byte[] sslSender)
        {
            IDigest d = handshakeHash.ForkPrfHash();

            if (sslSender != null && TlsUtilities.IsSsl(context))
            {
                d.BlockUpdate(sslSender, 0, sslSender.Length);
            }

            return DigestUtilities.DoFinal(d);
        }

        protected internal static IDictionary ReadExtensions(MemoryStream input)
        {
            if (input.Position >= input.Length)
                return null;

            byte[] extBytes = TlsUtilities.ReadOpaque16(input);

            AssertEmpty(input);

            MemoryStream buf = new MemoryStream(extBytes, false);

            // Integer -> byte[]
            IDictionary extensions = Platform.CreateHashtable();

            while (buf.Position < buf.Length)
            {
                int extension_type = TlsUtilities.ReadUint16(buf);
                byte[] extension_data = TlsUtilities.ReadOpaque16(buf);

                /*
                 * RFC 3546 2.3 There MUST NOT be more than one extension of the same type.
                 */
                if (extensions.Contains(extension_type))
                    throw new TlsFatalAlert(AlertDescription.illegal_parameter);

                extensions.Add(extension_type, extension_data);
            }

            return extensions;
        }

        protected internal static IList ReadSupplementalDataMessage(MemoryStream input)
        {
            byte[] supp_data = TlsUtilities.ReadOpaque24(input);

            AssertEmpty(input);

            MemoryStream buf = new MemoryStream(supp_data, false);

            IList supplementalData = Platform.CreateArrayList();

            while (buf.Position < buf.Length)
            {
                int supp_data_type = TlsUtilities.ReadUint16(buf);
                byte[] data = TlsUtilities.ReadOpaque16(buf);

                supplementalData.Add(new SupplementalDataEntry(supp_data_type, data));
            }

            return supplementalData;
        }

        protected internal static void WriteExtensions(Stream output, IDictionary extensions)
        {
            MemoryStream buf = new MemoryStream();

            foreach (int extension_type in extensions.Keys)
            {
                byte[] extension_data = (byte[])extensions[extension_type];

                TlsUtilities.CheckUint16(extension_type);
                TlsUtilities.WriteUint16(extension_type, buf);
                TlsUtilities.WriteOpaque16(extension_data, buf);
            }

            byte[] extBytes = buf.ToArray();

            TlsUtilities.WriteOpaque16(extBytes, output);
        }

        protected internal static void WriteSupplementalData(Stream output, IList supplementalData)
        {
            MemoryStream buf = new MemoryStream();

            foreach (SupplementalDataEntry entry in supplementalData)
            {
                int supp_data_type = entry.DataType;
                TlsUtilities.CheckUint16(supp_data_type);
                TlsUtilities.WriteUint16(supp_data_type, buf);
                TlsUtilities.WriteOpaque16(entry.Data, buf);
            }

            byte[] supp_data = buf.ToArray();

            TlsUtilities.WriteOpaque24(supp_data, output);
        }

        protected internal static int GetPrfAlgorithm(TlsContext context, int ciphersuite)
        {
            bool isTLSv12 = TlsUtilities.IsTlsV12(context);

            switch (ciphersuite)
            {
            case CipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_CBC_SHA256:
            case CipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_DH_DSS_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA256:
            case CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA256:
            case CipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_DH_RSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA256:
            case CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA256:
            case CipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_DHE_DSS_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA256:
            case CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA256:
            case CipherSuite.TLS_DHE_PSK_WITH_AES_128_CCM:
            case CipherSuite.TLS_DHE_PSK_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_DHE_PSK_WITH_AES_256_CCM:
            case CipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM_8:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM_8:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA256:
            case CipherSuite.TLS_DHE_RSA_WITH_CHACHA20_POLY1305_SHA256:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_ECDH_RSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256:
            case CipherSuite.TLS_PSK_DHE_WITH_AES_128_CCM_8:
            case CipherSuite.TLS_PSK_DHE_WITH_AES_256_CCM_8:
            case CipherSuite.TLS_PSK_WITH_AES_128_CCM:
            case CipherSuite.TLS_PSK_WITH_AES_128_CCM_8:
            case CipherSuite.TLS_PSK_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_PSK_WITH_AES_256_CCM:
            case CipherSuite.TLS_PSK_WITH_AES_256_CCM_8:
            case CipherSuite.TLS_PSK_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_RSA_PSK_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256:
            case CipherSuite.TLS_RSA_WITH_AES_128_CCM:
            case CipherSuite.TLS_RSA_WITH_AES_128_CCM_8:
            case CipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256:
            case CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256:
            case CipherSuite.TLS_RSA_WITH_AES_256_CCM:
            case CipherSuite.TLS_RSA_WITH_AES_256_CCM_8:
            case CipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA256:
            case CipherSuite.TLS_RSA_WITH_CAMELLIA_128_GCM_SHA256:
            case CipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA256:
            case CipherSuite.TLS_RSA_WITH_NULL_SHA256:
            {
                if (isTLSv12)
                {
                    return PrfAlgorithm.tls_prf_sha256;
                }
                throw new TlsFatalAlert(AlertDescription.illegal_parameter);
            }

            case CipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_DH_DSS_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_DH_RSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_DHE_DSS_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_DHE_PSK_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_ECDH_RSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_PSK_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_PSK_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_RSA_PSK_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_GCM_SHA384:
            case CipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384:
            case CipherSuite.TLS_RSA_WITH_CAMELLIA_256_GCM_SHA384:
            {
                if (isTLSv12)
                {
                    return PrfAlgorithm.tls_prf_sha384;
                }
                throw new TlsFatalAlert(AlertDescription.illegal_parameter);
            }

            case CipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_DHE_PSK_WITH_NULL_SHA384:
            case CipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA384:
            case CipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_PSK_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_PSK_WITH_NULL_SHA384:
            case CipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA384:
            case CipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_CBC_SHA384:
            case CipherSuite.TLS_RSA_PSK_WITH_NULL_SHA384:
            {
                if (isTLSv12)
                {
                    return PrfAlgorithm.tls_prf_sha384;
                }
                return PrfAlgorithm.tls_prf_legacy;
            }

            default:
            {
                if (isTLSv12)
                {
                    return PrfAlgorithm.tls_prf_sha256;
                }
                return PrfAlgorithm.tls_prf_legacy;
            }
            }
        }

        internal class HandshakeMessage
            :   MemoryStream
        {
            internal HandshakeMessage(byte handshakeType)
                :   this(handshakeType, 60)
            {
            }

            internal HandshakeMessage(byte handshakeType, int length)
                :   base(length + 4)
            {
                TlsUtilities.WriteUint8(handshakeType, this);
                // Reserve space for length
                TlsUtilities.WriteUint24(0, this);
            }

            internal void Write(byte[] data)
            {
                Write(data, 0, data.Length);
            }

            internal void WriteToRecordStream(TlsProtocol protocol)
            {
                // Patch actual length back in
                long length = Length - 4;
                TlsUtilities.CheckUint24(length);
                this.Position = 1;
                TlsUtilities.WriteUint24((int)length, this);
                protocol.WriteHandshakeMessage(GetBuffer(), 0, (int)Length);
                this.Close();
            }
        }
    }
}
