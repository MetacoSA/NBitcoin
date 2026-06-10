using NBitcoin.Payment;
using System;
using Xunit;

namespace NBitcoin.Tests;

//https://github.com/bitcoin/bips/blob/master/bip-0021.mediawiki
//Their examples are broken
public class PaymentTests
{
	[Fact]
	[Trait("UnitTest", "UnitTest")]
	public void CanParsePaymentUri()
	{
		Assert.Equal("bitcoin:", new BitcoinUriBuilder(Network.Main).Uri.ToString());

		var url = CreateBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha");
		Assert.Equal("129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha", url.Address.ToString());

		url = CreateBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=0.06");
		Assert.Equal("129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha", url.Address.ToString());
		Assert.Equal(Money.Parse("0.06"), url.Amount);

		url = new BitcoinUriBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=0.06&label=Tom%20%26%20Jerry", Network.Main);
		Assert.Equal("129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha", url.Address.ToString());
		Assert.Equal(Money.Parse("0.06"), url.Amount);
		Assert.Equal("Tom & Jerry", url.Label);
		Assert.Equal(url.ToString(), new BitcoinUriBuilder(url.ToString(), Network.Main).ToString());

		//Request 50 BTC with message: 
		url = new BitcoinUriBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=50&label=Luke-Jr&message=Donation%20for%20project%20xyz", Network.Main);
		Assert.Equal(Money.Parse("50"), url.Amount);
		Assert.Equal("Luke-Jr", url.Label);
		Assert.Equal("Donation for project xyz", url.Message);
		Assert.Equal(url.ToString(), new BitcoinUriBuilder(url.ToString(), Network.Main).ToString());

		//Some future version that has variables which are (currently) not understood and required and thus invalid: 
		url = new BitcoinUriBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=50&label=Luke-Jr&message=Donation%20for%20project%20xyz&unknownparam=lol", Network.Main);

		//Some future version that has variables which are (currently) not understood but not required and thus valid: 
		Assert.Throws<FormatException>(() => new BitcoinUriBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=50&label=Luke-Jr&message=Donation%20for%20project%20xyz&req-unknownparam=lol", Network.Main));
		Assert.Throws<FormatException>(() => new BitcoinUriBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=50&amount=50", Network.Main));

		url = new BitcoinUriBuilder("bitcoin:mq7se9wy2egettFxPbmn99cK8v5AFq55Lx?amount=0.11&r=https://merchant.com/pay.php?h%3D2a8628fc2fbe", Network.TestNet);
		Assert.Equal("bitcoin:mq7se9wy2egettFxPbmn99cK8v5AFq55Lx?amount=0.11&r=https://merchant.com/pay.php?h%3d2a8628fc2fbe", url.ToString());
		Assert.Equal(url.ToString(), new BitcoinUriBuilder(url.ToString(), Network.TestNet).ToString());

		//Support no address
		url = new BitcoinUriBuilder("bitcoin:?r=https://merchant.com/pay.php?h%3D2a8628fc2fbe", Network.Main);
		Assert.Equal(url.ToString(), new BitcoinUriBuilder(url.ToString(), Network.Main).ToString());

		//Support shitcoins
		url = new BitcoinUriBuilder("litecoin:LeLAhU5S7vbVxL4rsT69eMoMrpgV9SNbns", Altcoins.Litecoin.Instance.Mainnet);
		Assert.Equal(url.ToString(), new BitcoinUriBuilder(url.ToString(), Altcoins.Litecoin.Instance.Mainnet).ToString());
		Assert.Equal("litecoin:LeLAhU5S7vbVxL4rsT69eMoMrpgV9SNbns", url.ToString());

		// Old version of BitcoinUri was only supporting bitcoin: to not break existing code, we should support this
		url = new BitcoinUriBuilder("bitcoin:LeLAhU5S7vbVxL4rsT69eMoMrpgV9SNbns", Altcoins.Litecoin.Instance.Mainnet);
		Assert.Equal(url.ToString(), new BitcoinUriBuilder(url.ToString(), Altcoins.Litecoin.Instance.Mainnet).ToString());
		Assert.Equal("bitcoin:LeLAhU5S7vbVxL4rsT69eMoMrpgV9SNbns", url.ToString());
	}

	[Fact]
	[Trait("UnitTest", "UnitTest")]
	public void BitcoinUriKeepUnknownParameter()
	{
		var url = new BitcoinUriBuilder("bitcoin:?r=https://merchant.com/pay.php?h%3D2a8628fc2fbe&idontknow=test", Network.Main);

		Assert.Equal("test", url.UnknownParameters["idontknow"]);
		Assert.Equal("https://merchant.com/pay.php?h=2a8628fc2fbe", url.UnknownParameters["r"]);
	}

	private static BitcoinUriBuilder CreateBuilder(string uri)
	{
		var builder = new BitcoinUriBuilder(uri, Network.Main);
		Assert.Equal(builder.Uri.ToString(), uri);
		builder = new BitcoinUriBuilder(new Uri(uri, UriKind.Absolute), Network.Main);
		Assert.Equal(builder.ToString(), uri);
		return builder;
	}
}
