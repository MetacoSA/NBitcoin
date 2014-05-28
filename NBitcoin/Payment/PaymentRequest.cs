using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
		internal PaymentOutput(Proto.Output output)
		{
			Amount = new Money(output.amount);
			Script = output.script == null ? null : new Script(output.script);
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
	}
	public class PaymentDetails
	{
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
			result.Memo = details.memo;
			result.MerchantData = details.merchant_data;
			result.PaymentUrl = details.payment_url == null ? null : new Uri(details.payment_url, UriKind.Absolute);
			foreach(var output in details.outputs)
			{
				result.Outputs.Add(new PaymentOutput(output));
			}
			return result;
		}

		public Network Network
		{
			get;
			set;
		}

		public DateTimeOffset Time
		{
			get;
			set;
		}

		public DateTimeOffset Expires
		{
			get;
			set;
		}

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

		public void WriteTo(Stream output)
		{
			var details = new Proto.PaymentDetails();
			details.memo = Memo;
			details.merchant_data = MerchantData;
			details.network = Network == Network.Main ? "main" :
							  Network == Network.TestNet ? "test" : null;
			details.time = Utils.DateTimeToUnixTimeLong(Time);
			details.expires = Utils.DateTimeToUnixTimeLong(Expires);
			details.payment_url = PaymentUrl == null ? null : PaymentUrl.AbsoluteUri;
			foreach(var o in Outputs)
			{
				details.outputs.Add(new Proto.Output()
				{
					amount = (ulong)o.Amount.Satoshi,
					script = o.Script.ToRawScript()
				});
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
				foreach(var cert in certs.certificate)
				{
					result.Certificates.Add(new X509Certificate2(cert));
				}
			}
			result._PaymentDetails = PaymentDetails.Load(req.serialized_payment_details);
			result.Signature = req.signature;
			return result;
		}

		public void WriteTo(Stream output)
		{
			var req = new Proto.PaymentRequest();
			req.pki_type = ToPKITypeString(PKIType);

			var certs = new Proto.X509Certificates();
			foreach(var cert in Certificates)
			{
				certs.certificate.Add(cert.Export(X509ContentType.Cert));
			}
			MemoryStream ms = new MemoryStream();
			Serializer.Serialize(ms, certs);
			req.pki_data = ms.ToArray();
			req.serialized_payment_details = PaymentDetails.ToBytes();
			req.signature = Signature;
			req.payment_details_version = PaymentDetails.Version;
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

		private readonly List<X509Certificate2> _Certificates = new List<X509Certificate2>();
		public List<X509Certificate2> Certificates
		{
			get
			{
				return _Certificates;
			}
		}

		private PaymentDetails _PaymentDetails = new PaymentDetails();
		public PaymentDetails PaymentDetails
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


	}
}
