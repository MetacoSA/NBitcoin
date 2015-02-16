#if !USEBC
using NBitcoin.Crypto;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NBitcoin.Payment
{
	public enum PKIType
	{
		None,
		X509SHA256,
		X509SHA1,
	}

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
			if(destination != null)
				Script = destination.ScriptPubKey;
		}
		internal PaymentOutput(Proto.Output output)
		{
			Amount = new Money(output.amount);
			Script = output.script == null ? null : new Script(output.script);
			OriginalData = output;
		}
		public Money Amount
		{
			get;
			set;
		}
		public Script Script
		{
			get;
			set;
		}
		internal Proto.Output OriginalData
		{
			get;
			set;
		}

		internal Proto.Output ToData()
		{
			var data = OriginalData == null ? new Proto.Output() : (Proto.Output)PaymentRequest.Serializer.DeepClone(OriginalData);
			data.amount = (ulong)Amount.Satoshi;
			data.script = Script.ToBytes();
			return data;
		}
	}
	public class PaymentDetails
	{
		public PaymentDetails()
		{
			Time = Utils.UnixTimeToDateTime(0);
			Expires = Utils.UnixTimeToDateTime(0);
		}
		public static PaymentDetails Load(byte[] details)
		{
			return Load(new MemoryStream(details));
		}

		private static PaymentDetails Load(Stream source)
		{
			var result = new PaymentDetails();
			var details = PaymentRequest.Serializer.Deserialize<Proto.PaymentDetails>(source);
			result.Network = details.network == "main" ? Network.Main :
							 details.network == "test" ? Network.TestNet : null;
			if(result.Network == null)
				throw new NotSupportedException("Invalid network");
			result.Time = Utils.UnixTimeToDateTime(details.time);
			result.Expires = Utils.UnixTimeToDateTime(details.expires);
			result.Memo = details.memoSpecified ? details.memo : null;
			result.MerchantData = details.merchant_dataSpecified ? details.merchant_data : null;
			result.PaymentUrl = details.payment_urlSpecified ? new Uri(details.payment_url, UriKind.Absolute) : null;
			foreach(var output in details.outputs)
			{
				result.Outputs.Add(new PaymentOutput(output));
			}
			result.OriginalData = details;
			return result;
		}

		public Network Network
		{
			get;
			set;
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
		public DateTimeOffset Expires
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

		public byte[] ToBytes()
		{
			MemoryStream ms = new MemoryStream();
			WriteTo(ms);
			return ms.ToArray();
		}

		static byte[] GetByte<T>(T obj)
		{
			MemoryStream ms = new MemoryStream();
			PaymentRequest.Serializer.Serialize(ms, obj);
			return ms.ToArray();
		}
		public void WriteTo(Stream output)
		{
			var details = OriginalData == null ? new Proto.PaymentDetails() : (Proto.PaymentDetails)PaymentRequest.Serializer.DeepClone(OriginalData);
			details.memo = Memo;


			details.merchant_data = MerchantData;

			var network = Network == Network.Main ? "main" :
							  Network == Network.TestNet ? "test" : null;
			if(details.network != network)
				details.network = network;

			var time = Utils.DateTimeToUnixTimeLong(Time);
			if(time != details.time)
				details.time = time;
			var expires = Utils.DateTimeToUnixTimeLong(Expires);
			if(expires != details.expires)
				details.expires = expires;

			details.payment_url = PaymentUrl == null ? null : PaymentUrl.AbsoluteUri;
			details.outputs.Clear();
			foreach(var o in Outputs)
			{
				details.outputs.Add(o.ToData());
			}
			PaymentRequest.Serializer.Serialize(output, details);
		}

		public uint Version
		{
			get
			{
				return 1;
			}
		}

		internal Proto.PaymentDetails OriginalData
		{
			get;
			set;
		}
	}

	internal static class RuntimeTypeModelExtensions
	{
		public static T Deserialize<T>(this RuntimeTypeModel seria, Stream source)
		{
			return (T)seria.Deserialize(source, null, typeof(T));
		}
	}
	public class PaymentRequest
	{
		internal static RuntimeTypeModel Serializer;
		static PaymentRequest()
		{
			Serializer = TypeModel.Create();
			Serializer.UseImplicitZeroDefaults = false;
		}
		public static PaymentRequest Load(string file)
		{
			using(var fs = File.OpenRead(file))
			{
				return Load(fs);
			}
		}
		public static PaymentRequest Load(byte[] request)
		{
			return Load(new MemoryStream(request));
		}
		public static PaymentRequest Load(Stream source)
		{
			var result = new PaymentRequest();
			var req = Serializer.Deserialize<Proto.PaymentRequest>(source);
			result.PKIType = ToPKIType(req.pki_type);
			if(req.pki_data != null && req.pki_data.Length != 0)
			{
				var certs = Serializer.Deserialize<Proto.X509Certificates>(new MemoryStream(req.pki_data));
				bool first = true;
				foreach(var cert in certs.certificate)
				{
					if(first)
					{
						first = false;
						result.MerchantCertificate = new X509Certificate2(cert);
					}
					else
					{
						result.AdditionalCertificates.Add(new X509Certificate2(cert));
					}
				}
			}
			result._PaymentDetails = PaymentDetails.Load(req.serialized_payment_details);
			result.Signature = req.signature;
			result.OriginalData = req;
			return result;
		}

		public PaymentMessage CreatePayment()
		{
			return new PaymentMessage(this)
			{
				ImplicitPaymentUrl = Details.PaymentUrl
			};
		}
		public void WriteTo(Stream output)
		{
			var req = OriginalData == null ? new Proto.PaymentRequest() : (Proto.PaymentRequest)Serializer.DeepClone(OriginalData);
			req.pki_type = ToPKITypeString(PKIType);

			var certs = new Proto.X509Certificates();
			if(this.MerchantCertificate != null)
			{
				certs.certificate.Add(MerchantCertificate.Export(X509ContentType.Cert));
			}
			foreach(var cert in AdditionalCertificates)
			{
				certs.certificate.Add(cert.Export(X509ContentType.Cert));
			}
			MemoryStream ms = new MemoryStream();
			Serializer.Serialize(ms, certs);
			req.pki_data = ms.ToArray();
			req.serialized_payment_details = Details.ToBytes();
			req.signature = Signature;
			if(Details.Version != 1)
			{
				req.payment_details_version = Details.Version;
			}
			Serializer.Serialize(output, req);
		}

		private string ToPKITypeString(PKIType pkitype)
		{
			switch(pkitype)
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
			switch(str)
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

		/// <summary>
		/// Get the merchant name from the certificate subject
		/// </summary>
		public string MerchantName
		{
			get
			{
				if(MerchantCertificate == null)
					return null;
				if(!string.IsNullOrEmpty(MerchantCertificate.FriendlyName))
					return MerchantCertificate.FriendlyName;
				else
				{
					var match = Regex.Match(MerchantCertificate.Subject, "^(CN=)?(?<Name>[^,]*)", RegexOptions.IgnoreCase);
					if(!match.Success)
						return MerchantCertificate.Subject;
					return match.Groups["Name"].Value.Trim();
				}
			}
		}

		public X509Certificate2 MerchantCertificate
		{
			get;
			set;
		}


		private readonly List<X509Certificate2> _AdditionalCertificates = new List<X509Certificate2>();
		public List<X509Certificate2> AdditionalCertificates
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
			var ms = new MemoryStream();
			WriteTo(ms);
			return ms.ToArray();
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
			if(this.PKIType != Payment.PKIType.None)
				valid = this.VerifyChain() && VerifySignature();
			if(!valid)
				return valid;

			return Details.Expires < DateTimeOffset.UtcNow;
		}

		public bool VerifyChain(X509VerificationFlags flags = X509VerificationFlags.NoFlag)
		{
			X509Chain chain;
			return VerifyChain(out chain, flags);
		}

		public bool VerifyChain(out X509Chain chain, X509VerificationFlags flags = X509VerificationFlags.NoFlag)
		{
			chain = null;
			if(MerchantCertificate == null || PKIType == Payment.PKIType.None)
				return false;
			chain = new X509Chain();
			chain.ChainPolicy.VerificationFlags = flags;
			foreach(var additional in AdditionalCertificates)
				chain.ChainPolicy.ExtraStore.Add(additional);
			return chain.Build(MerchantCertificate);
		}

		public bool VerifySignature()
		{
			if(MerchantCertificate == null || PKIType == Payment.PKIType.None)
				return false;

			var key = (RSACryptoServiceProvider)MerchantCertificate.PublicKey.Key;
			var sig = Signature;
			Signature = new byte[0];
			byte[] data = null;
			try
			{
				data = this.ToBytes();
			}
			finally
			{
				Signature = sig;
			}

			byte[] hash = null;
			string hashName = null;
			if(PKIType == Payment.PKIType.X509SHA256)
			{
				hash = Hashes.SHA256(data);
				hashName = "sha256";
			}
			else if(PKIType == Payment.PKIType.X509SHA1)
			{
				hash = Hashes.SHA1(data, data.Length);
				hashName = "sha1";
			}
			else
				throw new NotSupportedException(PKIType.ToString());

			return key.VerifyHash(hash, hashName, Signature);
		}

		internal Proto.PaymentRequest OriginalData
		{
			get;
			set;
		}


		public void Sign(X509Certificate2 certificate, Payment.PKIType type)
		{
			if(type == Payment.PKIType.None)
				throw new ArgumentException("PKIType can't be none if signing");
			var privateKey = certificate.PrivateKey as RSACryptoServiceProvider;
			if(privateKey == null)
				throw new ArgumentException("Private key not present in the certificate, impossible to sign");
			MerchantCertificate = new X509Certificate2(certificate.Export(X509ContentType.Cert));
			PKIType = type;
			Signature = new byte[0];
			var data = this.ToBytes();
			byte[] hash = null;
			string hashName = null;
			if(type == Payment.PKIType.X509SHA256)
			{
				hash = Hashes.SHA256(data);
				hashName = "sha256";
			}
			else if(type == Payment.PKIType.X509SHA1)
			{
				hash = Hashes.SHA1(data, data.Length);
				hashName = "sha1";
			}
			else
				throw new NotSupportedException(PKIType.ToString());

			Signature = privateKey.SignHash(hash, hashName);
		}

		public readonly static string MediaType = "application/bitcoin-paymentrequest";
	}
}
#endif