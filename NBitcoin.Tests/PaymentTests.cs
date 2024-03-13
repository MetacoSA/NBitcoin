#if !NOFILEIO
using NBitcoin.DataEncoders;
using NBitcoin.Payment;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
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
			Assert.Equal("bitcoin:", new BitcoinUrlBuilder(Network.Main).Uri.ToString());

			var url = CreateBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha");
			Assert.Equal("129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha", url.Address.ToString());

			url = CreateBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=0.06");
			Assert.Equal("129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha", url.Address.ToString());
			Assert.Equal(Money.Parse("0.06"), url.Amount);

			url = new BitcoinUrlBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=0.06&label=Tom%20%26%20Jerry", Network.Main);
			Assert.Equal("129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha", url.Address.ToString());
			Assert.Equal(Money.Parse("0.06"), url.Amount);
			Assert.Equal("Tom & Jerry", url.Label);
			Assert.Equal(url.ToString(), new BitcoinUrlBuilder(url.ToString(), Network.Main).ToString());

			//Request 50 BTC with message: 
			url = new BitcoinUrlBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=50&label=Luke-Jr&message=Donation%20for%20project%20xyz", Network.Main);
			Assert.Equal(Money.Parse("50"), url.Amount);
			Assert.Equal("Luke-Jr", url.Label);
			Assert.Equal("Donation for project xyz", url.Message);
			Assert.Equal(url.ToString(), new BitcoinUrlBuilder(url.ToString(), Network.Main).ToString());

			//Some future version that has variables which are (currently) not understood and required and thus invalid: 
			url = new BitcoinUrlBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=50&label=Luke-Jr&message=Donation%20for%20project%20xyz&unknownparam=lol", Network.Main);

			//Some future version that has variables which are (currently) not understood but not required and thus valid: 
			Assert.Throws<FormatException>(() => new BitcoinUrlBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=50&label=Luke-Jr&message=Donation%20for%20project%20xyz&req-unknownparam=lol", Network.Main));
			Assert.Throws<FormatException>(() => new BitcoinUrlBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=50&amount=50", Network.Main));

			url = new BitcoinUrlBuilder("bitcoin:mq7se9wy2egettFxPbmn99cK8v5AFq55Lx?amount=0.11&r=https://merchant.com/pay.php?h%3D2a8628fc2fbe", Network.TestNet);
			Assert.Equal("bitcoin:mq7se9wy2egettFxPbmn99cK8v5AFq55Lx?amount=0.11&r=https://merchant.com/pay.php?h%3d2a8628fc2fbe", url.ToString());
			Assert.Equal(url.ToString(), new BitcoinUrlBuilder(url.ToString(), Network.TestNet).ToString());

			//Support no address
			url = new BitcoinUrlBuilder("bitcoin:?r=https://merchant.com/pay.php?h%3D2a8628fc2fbe", Network.Main);
			Assert.Equal(url.ToString(), new BitcoinUrlBuilder(url.ToString(), Network.Main).ToString());

			//Support shitcoins
			url = new BitcoinUrlBuilder("litecoin:LeLAhU5S7vbVxL4rsT69eMoMrpgV9SNbns", Altcoins.Litecoin.Instance.Mainnet);
			Assert.Equal(url.ToString(), new BitcoinUrlBuilder(url.ToString(), Altcoins.Litecoin.Instance.Mainnet).ToString());
			Assert.Equal("litecoin:LeLAhU5S7vbVxL4rsT69eMoMrpgV9SNbns", url.ToString());

			// Old verison of BitcoinUrl was only supporting bitcoin: to not break existing code, we should support this
			url = new BitcoinUrlBuilder("bitcoin:LeLAhU5S7vbVxL4rsT69eMoMrpgV9SNbns", Altcoins.Litecoin.Instance.Mainnet);
			Assert.Equal(url.ToString(), new BitcoinUrlBuilder(url.ToString(), Altcoins.Litecoin.Instance.Mainnet).ToString());
			Assert.Equal("bitcoin:LeLAhU5S7vbVxL4rsT69eMoMrpgV9SNbns", url.ToString());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void BitcoinUrlKeepUnknownParameter()
		{
			BitcoinUrlBuilder url = new BitcoinUrlBuilder("bitcoin:?r=https://merchant.com/pay.php?h%3D2a8628fc2fbe&idontknow=test", Network.Main);

			Assert.Equal("test", url.UnknownParameters["idontknow"]);
			Assert.Equal("https://merchant.com/pay.php?h=2a8628fc2fbe", url.UnknownParameters["r"]);
		}

		private BitcoinUrlBuilder CreateBuilder(string uri)
		{
			var builder = new BitcoinUrlBuilder(uri, Network.Main);
			Assert.Equal(builder.Uri.ToString(), uri);
			builder = new BitcoinUrlBuilder(new Uri(uri, UriKind.Absolute), Network.Main);
			Assert.Equal(builder.ToString(), uri);
			return builder;
		}


	}
}
#endif
