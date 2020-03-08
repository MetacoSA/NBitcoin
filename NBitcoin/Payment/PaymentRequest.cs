using NBitcoin.Crypto;
using NBitcoin.Protobuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
#if !NOHTTPCLIENT
using System.Net.Http;
#endif
using System.Text;
using System.Text.RegularExpressions;
#if CLASSICDOTNET
using System.Security.Cryptography.X509Certificates;
#endif
using System.Threading.Tasks;

namespace NBitcoin.Payment
{
	[Obsolete("BIP70 is obsolete")]
	public enum PKIType
	{
		None,
		X509SHA256,
		X509SHA1,
	}
	[Obsolete("BIP70 is obsolete")]
	public class PaymentOutput
	{
		public PaymentOutput()
		{

		}
		public PaymentOutput(Money amount, Script script)
		{
			Amount = amount;
			Script = script;
		}
		public PaymentOutput(Money amount, IDestination destination)
		{
			Amount = amount;
			if (destination != null)
				Script = destination.ScriptPubKey;
		}
		public PaymentOutput(TxOut txOut)
		{
			Amount = txOut.Value;
			Script = txOut.ScriptPubKey;
		}
		Money _Amount;
		public Money Amount
		{
			get
			{
				return _Amount ?? Money.Zero;
			}
			set
			{
				_Amount = value;
			}
		}
		public Script Script
		{
			get;
			set;
		}

		internal static PaymentOutput Load(byte[] bytes)
		{
			var reader = new ProtobufReaderWriter(new MemoryStream(bytes));
			PaymentOutput output = new PaymentOutput();
			int key;
			var start = reader.Position;
			while (reader.TryReadKey(out key))
			{
				switch (key)
				{
					case 1:
						output.Amount = Money.Satoshis(reader.ReadULong());
						break;
					case 2:
						output.Script = Script.FromBytesUnsafe(reader.ReadBytes());
						break;
					default:
						break;
				}
			}
			return output;
		}

		public byte[] ToBytes()
		{
			MemoryStream ms = new MemoryStream();
			Write(ms);
			return ms.ToArrayEfficient();
		}
		internal void Write(Stream output)
		{
			var writer = new ProtobufReaderWriter(output);
			if (_Amount != null)
			{
				writer.WriteKey(1, ProtobufReaderWriter.PROTOBUF_VARINT);
				writer.WriteULong((ulong)_Amount.Satoshi);
			}
			writer.WriteKey(2, ProtobufReaderWriter.PROTOBUF_LENDELIM);
			writer.WriteBytes(Script == null ? new byte[0] : Script.ToBytes(true));
		}
	}
	[Obsolete("BIP70 is obsolete")]
	public class PaymentDetails
	{
		public PaymentDetails()
		{
			Time = Utils.UnixTimeToDateTime(0);
		}
		public static PaymentDetails Load(byte[] details)
		{
			return Load(new MemoryStream(details));
		}

		private static PaymentDetails Load(Stream source)
		{
			var reader = new Protobuf.ProtobufReaderWriter(source);
			var result = new PaymentDetails();
			int key;
			while (reader.TryReadKey(out key))
			{
				switch (key)
				{
					case 1:
						var network = reader.ReadString();
						result.Network = network.Equals("main", StringComparison.OrdinalIgnoreCase) ? Network.Main :
										 network.Equals("test", StringComparison.OrdinalIgnoreCase) ? Network.TestNet :
										 network.Equals("regtest", StringComparison.OrdinalIgnoreCase) ? Network.RegTest : null;
						if (result.Network == null)
							throw new NotSupportedException("Invalid network");
						break;
					case 2:
						result.Outputs.Add(PaymentOutput.Load(reader.ReadBytes()));
						break;
					case 3:
						result.Time = Utils.UnixTimeToDateTime(reader.ReadULong());
						break;
					case 4:
						result.Expires = Utils.UnixTimeToDateTime(reader.ReadULong());
						break;
					case 5:
						result.Memo = reader.ReadString();
						break;
					case 6:
						result.PaymentUrl = new Uri(reader.ReadString());
						break;
					case 7:
						result.MerchantData = reader.ReadBytes();
						break;
					default:
						break;
				}
			}
			return result;
		}

		public void WriteTo(Stream stream)
		{
			var writer = new ProtobufReaderWriter(stream);
			if (_Network != null)
			{
				var str = _Network == Network.Main ? "main" :
					_Network == Network.TestNet ? "test" :
					_Network == Network.RegTest ? "regtest" : null;
				if (str == null)
					throw new InvalidOperationException("Impossible to serialize a payment request on network " + _Network);
				writer.WriteKey(1, ProtobufReaderWriter.PROTOBUF_LENDELIM);
				writer.WriteString(str);
			}

			foreach (var txout in Outputs)
			{
				writer.WriteKey(2, ProtobufReaderWriter.PROTOBUF_LENDELIM);
				writer.WriteBytes(txout.ToBytes());
			}

			writer.WriteKey(3, ProtobufReaderWriter.PROTOBUF_VARINT);
			writer.WriteULong(Utils.DateTimeToUnixTime(Time));

			if (Expires != null)
			{
				writer.WriteKey(4, ProtobufReaderWriter.PROTOBUF_VARINT);
				writer.WriteULong(Utils.DateTimeToUnixTime(Expires.Value));
			}

			if (Memo != null)
			{
				writer.WriteKey(5, ProtobufReaderWriter.PROTOBUF_LENDELIM);
				writer.WriteString(Memo);
			}

			if (PaymentUrl != null)
			{
				writer.WriteKey(6, ProtobufReaderWriter.PROTOBUF_LENDELIM);
				writer.WriteString(PaymentUrl.AbsoluteUri);
			}

			if (MerchantData != null)
			{
				writer.WriteKey(7, ProtobufReaderWriter.PROTOBUF_LENDELIM);
				writer.WriteBytes(MerchantData);
			}
		}

		Network _Network;
		public Network Network
		{
			get
			{
				return _Network ?? Network.Main;
			}
			set
			{
				_Network = value;
			}
		}

		/// <summary>
		/// timestamp (seconds since 1-Jan-1970 UTC) when the PaymentRequest was created.
		/// </summary>
		public DateTimeOffset Time
		{
			get;
			set;
		}
		/// <summary>
		/// timestamp (UTC) after which the PaymentRequest should be considered invalid. 
		/// </summary>
		public DateTimeOffset? Expires
		{
			get;
			set;
		}
		/// <summary>
		/// plain-text (no formatting) note that should be displayed to the customer, explaining what this PaymentRequest is for. 
		/// </summary>
		public string Memo
		{
			get;
			set;
		}

		public byte[] MerchantData
		{
			get;
			set;
		}

		/// <summary>
		/// Secure (usually https) location where a Payment message (see below) may be sent to obtain a PaymentACK. 
		/// </summary>
		public Uri PaymentUrl
		{
			get;
			set;
		}
		private readonly List<PaymentOutput> _Outputs = new List<PaymentOutput>();
		public List<PaymentOutput> Outputs
		{
			get
			{
				return _Outputs;
			}
		}
	}
	[Obsolete("BIP70 is obsolete")]
	public class PaymentRequest
	{
		class RecorderStream : Stream
		{
			private Stream _Inner;
			public RecorderStream(Stream inner)
			{
				_Inner = inner;
				Activated = true;
			}
			public override bool CanRead
			{
				get
				{
					return _Inner.CanRead;
				}
			}

			public override bool CanSeek
			{
				get
				{
					return _Inner.CanSeek;
				}
			}

			public override bool CanWrite
			{
				get
				{
					return _Inner.CanWrite;
				}
			}

			public override void Flush()
			{
				_Inner.Flush();
			}

			public override long Length
			{
				get
				{
					return _Inner.Length;
				}
			}

			public override long Position
			{
				get
				{
					return _Inner.Position;
				}
				set
				{
					_Inner.Position = value;
				}
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				var read = _Inner.Read(buffer, offset, count);
				if (read == -1)
					return -1;
				if (Activated)
				{
					byte[] copy = new byte[count];
					Array.Copy(buffer, offset, copy, 0, read);
					buffers.Add(buffer);
				}
				return read;
			}

			List<byte[]> buffers = new List<byte[]>();
			public byte[] ToBytes()
			{
				byte[] result = new byte[buffers.Select(b => b.Length).Sum()];
				int offset = 0;
				foreach (var b in buffers)
				{
					Array.Copy(b, 0, result, offset, b.Length);
					offset += b.Length;
				}
				return result;
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				return _Inner.Seek(offset, origin);
			}

			public override void SetLength(long value)
			{
				_Inner.SetLength(value);
			}

			public void RecordBytes(byte[] bytes)
			{
				buffers.Add(bytes);
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				throw new NotImplementedException();
			}

			public bool Activated
			{
				get;
				set;
			}
		}

#if !NOX509
		private static ICertificateServiceProvider _DefaultCertificateServiceProvider = new WindowsCertificateServiceProvider();
#else
		private static ICertificateServiceProvider _DefaultCertificateServiceProvider;		
#endif
		/// <summary>
		/// Default application wide certificate service provider
		/// </summary>
		public static ICertificateServiceProvider DefaultCertificateServiceProvider
		{
			get
			{
				return _DefaultCertificateServiceProvider;
			}
			set
			{
				_DefaultCertificateServiceProvider = value;
			}
		}

		/// <summary>
		/// Instance specific certificate service provider
		/// </summary>
		public ICertificateServiceProvider CertificateServiceProvider
		{
			get;
			set;
		}
#if !NOFILEIO
		public static PaymentRequest Load(string file)
		{
			using (var fs = File.OpenRead(file))
			{
				return Load(fs);
			}
		}
#endif
		public static PaymentRequest Load(byte[] request)
		{
			return Load(new MemoryStream(request));
		}
		public static PaymentRequest Load(Stream source)
		{
			byte[] signed;
			return Load(source, out signed);
		}
		public static PaymentRequest Load(Stream source, out byte[] signed)
		{
			RecorderStream record = new RecorderStream(source);
			PaymentRequest req = new PaymentRequest();
			var reader = new ProtobufReaderWriter(record);
			bool signatureLoaded = false;
			int key;
			bool firstCert = true;
			while (reader.TryReadKey(out key))
			{
				switch (key)
				{
					case 1:
						req.DetailsVersion = (uint)reader.ReadULong();
						break;
					case 2:
						req.PKIType = ToPKIType(reader.ReadString());
						break;
					case 3:
						var bytes = reader.ReadBytes();
						ProtobufReaderWriter certs = new ProtobufReaderWriter(new MemoryStream(bytes));
						int k;
						while (certs.TryReadKey(out k))
						{
							if (firstCert)
							{
								req.MerchantCertificate = certs.ReadBytes();
								firstCert = false;
							}
							else
								req.AdditionalCertificates.Add(certs.ReadBytes());
						}
						break;
					case 4:
						req._PaymentDetails = PaymentDetails.Load(reader.ReadBytes());
						break;
					case 5:
						record.Activated = false;
						req.Signature = reader.ReadBytes();
						signatureLoaded = req.Signature.Length != 0;
						record.Activated = true;
						record.RecordBytes(new byte[0]);
						break;
					default:
						break;
				}
			}
			signed = record.ToBytes();
			return req;
		}

		public PaymentMessage CreatePayment()
		{
			return new PaymentMessage(this)
			{
				ImplicitPaymentUrl = Details.PaymentUrl
			};
		}

		uint? _DetailsVersion;
		public uint DetailsVersion
		{
			get
			{
				return _DetailsVersion ?? 1;
			}
			set
			{
				_DetailsVersion = value;
			}
		}
		public void WriteTo(Stream output)
		{
			WriteTo(new ProtobufReaderWriter(output));
		}

		void WriteTo(ProtobufReaderWriter writer)
		{
			if (_DetailsVersion != null)
			{
				writer.WriteKey(1, ProtobufReaderWriter.PROTOBUF_VARINT);
				writer.WriteULong((uint)_DetailsVersion);
			}

			writer.WriteKey(2, ProtobufReaderWriter.PROTOBUF_LENDELIM);
			writer.WriteString(ToPKITypeString(PKIType));

			writer.WriteKey(3, ProtobufReaderWriter.PROTOBUF_LENDELIM);
			MemoryStream ms = new MemoryStream();
			ProtobufReaderWriter certs = new ProtobufReaderWriter(ms);

			if (this.MerchantCertificate != null)
			{
				certs.WriteKey(1, ProtobufReaderWriter.PROTOBUF_LENDELIM);
				certs.WriteBytes(MerchantCertificate);
			}
			foreach (var cert in AdditionalCertificates)
			{
				certs.WriteKey(1, ProtobufReaderWriter.PROTOBUF_LENDELIM);
				certs.WriteBytes(cert);
			}
			writer.WriteBytes(ms.ToArrayEfficient());

			writer.WriteKey(4, ProtobufReaderWriter.PROTOBUF_LENDELIM);
			ms = new MemoryStream();
			Details.WriteTo(ms);
			writer.WriteBytes(ms.ToArray());

			writer.WriteKey(5, ProtobufReaderWriter.PROTOBUF_LENDELIM);
			writer.WriteBytes(Signature ?? new byte[0]);
		}

		private string ToPKITypeString(PKIType pkitype)
		{
			switch (pkitype)
			{
				case Payment.PKIType.None:
					return "none";
				case Payment.PKIType.X509SHA1:
					return "x509+sha1";
				case Payment.PKIType.X509SHA256:
					return "x509+sha256";
				default:
					throw new NotSupportedException(pkitype.ToString());
			}
		}

		private static PKIType ToPKIType(string str)
		{
			switch (str)
			{
				case "none":
					return PKIType.None;
				case "x509+sha256":
					return PKIType.X509SHA256;
				case "x509+sha1":
					return PKIType.X509SHA1;
				default:
					throw new NotSupportedException(str);
			}
		}

		public PKIType PKIType
		{
			get;
			set;
		}

		public byte[] MerchantCertificate
		{
			get;
			set;
		}


		private readonly List<byte[]> _AdditionalCertificates = new List<byte[]>();
		public List<byte[]> AdditionalCertificates
		{
			get
			{
				return _AdditionalCertificates;
			}
		}

		private PaymentDetails _PaymentDetails = new PaymentDetails();
		public PaymentDetails Details
		{
			get
			{
				return _PaymentDetails;
			}
		}

		public byte[] ToBytes()
		{
			MemoryStream ms = new MemoryStream();
			WriteTo(new ProtobufReaderWriter(ms));
			return ms.ToArrayEfficient();
		}

		public byte[] Signature
		{
			get;
			set;
		}

		/// <summary>
		/// Verify that the certificate chain is trusted and signature correct.
		/// </summary>
		/// <returns>true if the certificate chain and the signature is trusted or if PKIType == None</returns>
		public bool Verify()
		{
			bool valid = true;
			if (this.PKIType != Payment.PKIType.None)
				valid = this.VerifyChain() && VerifySignature();
			if (!valid)
				return false;

			return Details.Expires < DateTimeOffset.UtcNow;
		}

		private ICertificateServiceProvider GetCertificateProvider()
		{
			var provider = CertificateServiceProvider = DefaultCertificateServiceProvider;
			if (provider == null)
				throw new InvalidOperationException("DefaultCertificateServiceProvider or CertificateServiceProvider must be set before calling this method.");
			return provider;
		}

		public bool VerifyChain()
		{
			if (MerchantCertificate == null || PKIType == Payment.PKIType.None)
				return false;
			return GetCertificateProvider().GetChainChecker().VerifyChain(MerchantCertificate, AdditionalCertificates.ToArray());
		}
		public bool VerifySignature()
		{
			if (MerchantCertificate == null || PKIType == Payment.PKIType.None)
				return false;
			var data = GetSignedData();

			byte[] hash = null;
			string hashName = null;
			if (PKIType == Payment.PKIType.X509SHA256)
			{
				hash = Hashes.SHA256(data);
				hashName = "sha256";
			}
			else
				throw new NotSupportedException(PKIType.ToString());

			return GetCertificateProvider().GetSignatureChecker().VerifySignature(MerchantCertificate, hash, hashName, Signature);
		}

		private byte[] GetSignedData()
		{
			MemoryStream ms = new MemoryStream(this.ToBytes());
			byte[] signed = null;
			Load(ms, out signed);
			return signed;
		}

#if CLASSICDOTNET
		public void Sign(X509Certificate2 certificate, Payment.PKIType type)
		{
			Sign((object)certificate, type);
		}
#endif
		public void Sign(byte[] certificate, Payment.PKIType type)
		{
			Sign((object)certificate, type);
		}
		public void Sign(object certificate, Payment.PKIType type)
		{
			if (certificate == null)
				throw new ArgumentNullException(nameof(certificate));
			if (type == Payment.PKIType.None)
				throw new ArgumentException("PKIType can't be none if signing");
			var signer = GetCertificateProvider().GetSigner();
			MerchantCertificate = signer.StripPrivateKey(certificate);
			PKIType = type;
			var data = GetSignedData();
			byte[] hash = null;
			string hashName = null;
			if (type == Payment.PKIType.X509SHA256)
			{
				hash = Hashes.SHA256(data);
				hashName = "sha256";
			}
			else if (type == Payment.PKIType.X509SHA1)
			{
				hash = Hashes.SHA1(data, 0, data.Length);
				hashName = "sha1";
			}
			else
				throw new NotSupportedException(PKIType.ToString());

			Signature = signer.Sign(certificate, hash, hashName);
		}

		public readonly static string MediaType = "application/bitcoin-paymentrequest";

	}
}
