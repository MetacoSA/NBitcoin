using NBitcoin.DataEncoders;
using NBitcoin.Payment;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
#if !PORTABLE
using System.Net.Http;
#endif
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	//https://github.com/bitcoin/bips/blob/master/bip-0021.mediawiki
	//Their examples are broken
	public class PaymentTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParsePaymentUrl()
		{
			Assert.Equal("bitcoin:", new BitcoinUrlBuilder().Uri.ToString());

			var url = CreateBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha");
			Assert.Equal("129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha", url.Address.ToString());

			url = CreateBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=0.06");
			Assert.Equal("129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha", url.Address.ToString());
			Assert.Equal(Money.Parse("0.06"), url.Amount);

			url = new BitcoinUrlBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=0.06&label=Tom%20%26%20Jerry");
			Assert.Equal("129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha", url.Address.ToString());
			Assert.Equal(Money.Parse("0.06"), url.Amount);
			Assert.Equal("Tom & Jerry", url.Label);
			Assert.Equal(url.ToString(), new BitcoinUrlBuilder(url.ToString()).ToString());

			//Request 50 BTC with message: 
			url = new BitcoinUrlBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=50&label=Luke-Jr&message=Donation%20for%20project%20xyz");
			Assert.Equal(Money.Parse("50"), url.Amount);
			Assert.Equal("Luke-Jr", url.Label);
			Assert.Equal("Donation for project xyz", url.Message);
			Assert.Equal(url.ToString(), new BitcoinUrlBuilder(url.ToString()).ToString());

			//Some future version that has variables which are (currently) not understood and required and thus invalid: 
			url = new BitcoinUrlBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=50&label=Luke-Jr&message=Donation%20for%20project%20xyz&unknownparam=lol");

			//Some future version that has variables which are (currently) not understood but not required and thus valid: 
			Assert.Throws<FormatException>(() => new BitcoinUrlBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=50&label=Luke-Jr&message=Donation%20for%20project%20xyz&req-unknownparam=lol"));
			Assert.Throws<FormatException>(() => new BitcoinUrlBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=50&amount=50"));

			url = new BitcoinUrlBuilder("bitcoin:mq7se9wy2egettFxPbmn99cK8v5AFq55Lx?amount=0.11&r=https://merchant.com/pay.php?h%3D2a8628fc2fbe");
			Assert.Equal("bitcoin:mq7se9wy2egettFxPbmn99cK8v5AFq55Lx?amount=0.11&r=https://merchant.com/pay.php?h%3d2a8628fc2fbe", url.ToString());
			Assert.Equal("https://merchant.com/pay.php?h=2a8628fc2fbe", url.PaymentRequestUrl.ToString());
			Assert.Equal(url.ToString(), new BitcoinUrlBuilder(url.ToString()).ToString());

			//Support no address
			url = new BitcoinUrlBuilder("bitcoin:?r=https://merchant.com/pay.php?h%3D2a8628fc2fbe");
			Assert.Equal("https://merchant.com/pay.php?h=2a8628fc2fbe", url.PaymentRequestUrl.ToString());
			Assert.Equal(url.ToString(), new BitcoinUrlBuilder(url.ToString()).ToString());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void BitcoinUrlKeepUnknowParameter()
		{
			BitcoinUrlBuilder url = new BitcoinUrlBuilder("bitcoin:?r=https://merchant.com/pay.php?h%3D2a8628fc2fbe&idontknow=test");

			Assert.Equal("test", url.UnknowParameters["idontknow"]);
			Assert.Equal(1, url.UnknowParameters.Count);
		}

		private BitcoinUrlBuilder CreateBuilder(string uri)
		{
			var builder = new BitcoinUrlBuilder(uri);
			Assert.Equal(builder.Uri.ToString(), uri);
			builder = new BitcoinUrlBuilder(new Uri(uri, UriKind.Absolute));
			Assert.Equal(builder.ToString(), uri);
			return builder;
		}

		public PaymentRequest LoadPaymentRequest(string path)
		{
			using(var fs = File.OpenRead(path))
			{
				return PaymentRequest.Load(fs);
			}
		}
		public PaymentACK LoadPaymentACK(string path)
		{
			using(var fs = File.OpenRead(path))
			{
				return PaymentACK.Load(fs);
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanReadPaymentRequest()
		{
			foreach(var provider in new ICertificateServiceProvider[]
			{ 
#if WIN
				new WindowsCertificateServiceProvider(X509VerificationFlags.IgnoreNotTimeValid |
						X509VerificationFlags.AllowUnknownCertificateAuthority |
						X509VerificationFlags.IgnoreRootRevocationUnknown |
						X509VerificationFlags.IgnoreCertificateAuthorityRevocationUnknown |
						X509VerificationFlags.IgnoreEndRevocationUnknown)
#endif
			})
			{
				PaymentRequest.DefaultCertificateServiceProvider = provider;
				var request = LoadPaymentRequest("data/payreq1_sha1.paymentrequest");
				AssertEx.CollectionEquals(request.ToBytes(), File.ReadAllBytes("data/payreq1_sha1.paymentrequest"));
				Assert.True(request.VerifySignature());
				request.Details.Memo = "lol";
				Assert.False(request.VerifySignature());
				request.Details.Memo = "this is a memo";
				Assert.True(request.VerifySignature());
				Assert.True(request.VerifyChain());
				request = LoadPaymentRequest("data/payreq2_sha1.paymentrequest");
				AssertEx.CollectionEquals(request.ToBytes(), File.ReadAllBytes("data/payreq2_sha1.paymentrequest"));
				Assert.True(request.VerifySignature());
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanVerifyValidChain()
		{
			foreach(var provider in new ICertificateServiceProvider[]
			{ 
#if WIN
				new WindowsCertificateServiceProvider(X509VerificationFlags.IgnoreNotTimeValid, X509RevocationMode.NoCheck)
#endif
			})
			{
				PaymentRequest.DefaultCertificateServiceProvider = provider;
				var req = LoadPaymentRequest("data/payreq3_validchain.paymentrequest");
				Assert.True(req.VerifyChain());
				Assert.True(req.VerifySignature());
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanReadTestVectorPayments()
		{
			var tests = new[]
			{
				"data/payreq1_sha256_omitteddefault.paymentrequest",
				"data/payreq1_sha256.paymentrequest",
				"data/payreq2_sha256_omitteddefault.paymentrequest",
				"data/payreq2_sha256.paymentrequest",
				"data/payreq1_sha1_omitteddefault.paymentrequest",
				"data/payreq1_sha1.paymentrequest",
				"data/payreq2_sha1_omitteddefault.paymentrequest",
				"data/payreq2_sha1.paymentrequest",
			};

			foreach(var provider in new ICertificateServiceProvider[]
			{ 
#if WIN
				new WindowsCertificateServiceProvider(X509VerificationFlags.IgnoreNotTimeValid |
						X509VerificationFlags.AllowUnknownCertificateAuthority |
						X509VerificationFlags.IgnoreRootRevocationUnknown |
						X509VerificationFlags.IgnoreCertificateAuthorityRevocationUnknown |
						X509VerificationFlags.IgnoreEndRevocationUnknown)
#endif
			})
			{
				PaymentRequest.DefaultCertificateServiceProvider = provider;
				foreach(var test in tests)
				{
					var bytes = File.ReadAllBytes(test);
					var request = PaymentRequest.Load(bytes);
					AssertEx.Equal(request.ToBytes(), bytes);

					Assert.True(request.VerifySignature());
					request = PaymentRequest.Load(PaymentRequest.Load(bytes).ToBytes());
					Assert.True(request.VerifySignature());
					Assert.True(request.VerifyChain());
				}
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCreatePaymentRequest()
		{
			foreach(var provider in new ICertificateServiceProvider[]
			{ 
#if WIN
				new WindowsCertificateServiceProvider(X509VerificationFlags.IgnoreNotTimeValid)
#endif
			})
			{
				PaymentRequest.DefaultCertificateServiceProvider = provider;
				var cert = File.ReadAllBytes("Data/NicolasDorierMerchant.pfx");
				CanCreatePaymentRequestCore(cert);
#if WIN
				if(provider is WindowsCertificateServiceProvider)
				{
					CanCreatePaymentRequestCore(new X509Certificate2(cert, "", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet));
				}
#endif
			}
		}

		private static void CanCreatePaymentRequestCore(object cert)
		{
			var request = new PaymentRequest();
			request.Details.Memo = "hello";
			request.Sign(cert, PKIType.X509SHA256);

			Assert.NotNull(request.MerchantCertificate);
#if WIN
			Assert.False(new X509Certificate2(request.MerchantCertificate, "", X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable).HasPrivateKey);
#endif
			Assert.True(request.VerifySignature());
			Assert.False(request.VerifyChain());
			AssertEx.CollectionEquals(request.ToBytes(), PaymentRequest.Load(request.ToBytes()).ToBytes());
			Assert.True(PaymentRequest.Load(request.ToBytes()).VerifySignature());
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParsePaymentACK()
		{
			var ack = LoadPaymentACK("data/paymentack.data");
			Assert.Equal("thanks customer !", ack.Memo);
			Assert.Equal("thanks merchant !", ack.Payment.Memo);
			Assert.Equal(2, ack.Payment.Transactions.Count);
			Assert.Equal(2, ack.Payment.RefundTo.Count);
			AssertEx.CollectionEquals(ack.ToBytes(), PaymentACK.Load(ack.ToBytes()).ToBytes());
			AssertEx.CollectionEquals(ack.ToBytes(), File.ReadAllBytes("data/paymentack.data"));
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCreatePaymentMessageAndACK()
		{
			var request = LoadPaymentRequest("data/payreq1_sha1.paymentrequest");
			var payment = request.CreatePayment();
			AssertEx.CollectionEquals(request.Details.MerchantData, payment.MerchantData);
			AssertEx.CollectionEquals(payment.ToBytes(), PaymentMessage.Load(payment.ToBytes()).ToBytes());
			payment.Memo = "thanks merchant !";
			AssertEx.CollectionEquals(payment.ToBytes(), PaymentMessage.Load(payment.ToBytes()).ToBytes());
			var ack = payment.CreateACK();
			AssertEx.CollectionEquals(ack.Payment.ToBytes(), PaymentMessage.Load(payment.ToBytes()).ToBytes());
			AssertEx.CollectionEquals(ack.ToBytes(), PaymentACK.Load(ack.ToBytes()).ToBytes());
			ack.Memo = "thanks customer !";
			AssertEx.CollectionEquals(ack.ToBytes(), PaymentACK.Load(ack.ToBytes()).ToBytes());
		}
#if !NOHTTPCLIENT && !NOHTTPSERVER
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanTalkToPaymentServer()
		{
			using(var server = new PaymentServerTester())
			{
				var uri = server.GetPaymentRequestUri(2);
				BitcoinUrlBuilder btcUri = new BitcoinUrlBuilder(uri);
				var request = btcUri.GetPaymentRequest();
				Assert.True(request.VerifySignature());
				Assert.Equal(2, BitConverter.ToInt32(request.Details.MerchantData, 0));
				var ack = request.CreatePayment().SubmitPayment();
				Assert.NotNull(ack);
			}
		}
#endif

	}
#if !NOHTTPSERVER
	public class PaymentServerTester : IDisposable
	{
		HttpListener _Listener;
		Random rand = new Random();
		string _Prefix;
		public PaymentServerTester()
		{
			while(true)
			{
				try
				{
					_Prefix = "http://127.0.0.1:" + rand.Next(2000, 50000) + "/";
					_Listener = new HttpListener();
					_Listener.Prefixes.Add(_Prefix);
					_Listener.Start();
					_Listener.BeginGetContext(ListenerCallback, null);
					break;
				}
				catch(HttpListenerException)
				{
				}
			}
		}

		void ListenerCallback(IAsyncResult ar)
		{
			try
			{
				var context = _Listener.EndGetContext(ar);
				var type = context.Request.QueryString.Get("type");
				var businessId = int.Parse(context.Request.QueryString.Get("id"));
				if(type == "Request")
				{
					Assert.Equal(PaymentRequest.MediaType, context.Request.AcceptTypes[0]);
					context.Response.ContentType = PaymentRequest.MediaType;
					PaymentRequest request = new PaymentRequest();
					request.Details.MerchantData = BitConverter.GetBytes(businessId);
					request.Details.PaymentUrl = new Uri(_Prefix + "?id=" + businessId + "&type=Payment");
					request.Sign(File.ReadAllBytes("data/NicolasDorierMerchant.pfx"), PKIType.X509SHA256);
					request.WriteTo(context.Response.OutputStream);
				}
				else if(type == "Payment")
				{
					Assert.Equal(PaymentMessage.MediaType, context.Request.ContentType);
					Assert.Equal(PaymentACK.MediaType, context.Request.AcceptTypes[0]);

					var payment = PaymentMessage.Load(context.Request.InputStream);
					Assert.Equal(businessId, BitConverter.ToInt32(payment.MerchantData, 0));

					context.Response.ContentType = PaymentACK.MediaType;
					var ack = payment.CreateACK();
					ack.WriteTo(context.Response.OutputStream);
				}
				else
					Assert.False(true, "Impossible");

				context.Response.Close();
				_Listener.BeginGetContext(ListenerCallback, null);
			}
			catch(Exception)
			{
				if(!_Stopped)
					throw;
			}
		}
		public Uri GetPaymentRequestUri(int businessId)
		{
			BitcoinUrlBuilder builder = new BitcoinUrlBuilder()
			{
				PaymentRequestUrl = new Uri(_Prefix + "?id=" + businessId + "&type=Request")
			};
			return builder.Uri;
		}

		volatile bool _Stopped;

		#region IDisposable Members

		public void Dispose()
		{
			_Stopped = true;
			_Listener.Close();
		}

		#endregion
	}
#endif
}