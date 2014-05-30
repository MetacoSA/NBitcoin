using NBitcoin.DataEncoders;
using NBitcoin.Payment;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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

			url = new BitcoinUrlBuilder("bitcoin:mq7se9wy2egettFxPbmn99cK8v5AFq55Lx?amount=0.11&r=https://merchant.com/pay.php?h%3D2a8628fc2fbe");
			Assert.Equal("https://merchant.com/pay.php?h=2a8628fc2fbe", url.PaymentRequestUrl.ToString());
			Assert.Equal(url.ToString(), new BitcoinUrlBuilder(url.ToString()).ToString());

			//Support no address
			url = new BitcoinUrlBuilder("bitcoin:?r=https://merchant.com/pay.php?h%3D2a8628fc2fbe");
			Assert.Equal("https://merchant.com/pay.php?h=2a8628fc2fbe", url.PaymentRequestUrl.ToString());
			Assert.Equal(url.ToString(), new BitcoinUrlBuilder(url.ToString()).ToString());
		}

		private BitcoinUrlBuilder CreateBuilder(string uri)
		{
			var builder = new BitcoinUrlBuilder(uri);
			Assert.Equal(builder.Uri.ToString(), uri);
			builder = new BitcoinUrlBuilder(new Uri(uri, UriKind.Absolute));
			Assert.Equal(builder.ToString(), uri);
			return builder;
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanReadPaymentRequest()
		{
			var request = PaymentRequest.Load("data/payreq.dat");
			AssertEx.CollectionEquals(request.ToBytes(), File.ReadAllBytes("data/payreq.dat"));
			Assert.True(request.VerifySignature());
			request.Details.Memo = "lol";
			Assert.False(request.VerifySignature());
			request.Details.Memo = "this is a memo";
			Assert.True(request.VerifySignature());
			Assert.True(request.VerifyCertificate(X509VerificationFlags.IgnoreNotTimeValid));
			request = PaymentRequest.Load("data/payreq2.dat");
			AssertEx.CollectionEquals(request.ToBytes(), File.ReadAllBytes("data/payreq2.dat"));
			Assert.True(request.VerifySignature());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCreatePaymentRequest()
		{
			var cert = new X509Certificate2("Data/nicolasdorier.pfx", (string)null, X509KeyStorageFlags.Exportable);
			var request = new PaymentRequest();
			request.Details.Memo = "hello";
			request.Sign(cert, PKIType.X509SHA256);

			Assert.NotNull(request.MerchantCertificate);
			Assert.True(request.VerifySignature());
			Assert.False(request.VerifyCertificate(X509VerificationFlags.IgnoreNotTimeValid));
			Assert.True(PaymentRequest.Load(request.ToBytes(true)).VerifySignature());
			Assert.True(PaymentRequest.Load(request.ToBytes(false)).VerifySignature());
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCreatePaymentMessageAndACK()
		{
			var request = PaymentRequest.Load("data/payreq.dat");
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
		private T Reserialize<T>(T data)
		{
			MemoryStream ms = new MemoryStream();
			PaymentRequest.Serializer.Serialize(ms, data);
			ms.Position = 0;
			return (T)PaymentRequest.Serializer.Deserialize(ms, null, typeof(T));
		}
	}
}
