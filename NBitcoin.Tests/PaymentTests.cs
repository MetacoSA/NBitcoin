using NBitcoin.DataEncoders;
using NBitcoin.Payment;
using System;
using System.Collections.Generic;
using System.Linq;
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
		public void CanParsePaymentUrl()
		{
			var url = CreateBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha");
			Assert.Equal("129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha", url.Address.ToString());

			url = CreateBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=0.06");
			Assert.Equal("129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha", url.Address.ToString());
			Assert.Equal(Money.Parse("0.06"), url.Amount);

			url = CreateBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=0.06&label=Tom%20%26%20Jerry");
			Assert.Equal("129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha", url.Address.ToString());
			Assert.Equal(Money.Parse("0.06"), url.Amount);
			Assert.Equal("Tom & Jerry", url.Label);

			url = new BitcoinUrlBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=50&label=Luke-Jr&message=Donation%20for%20project%20xyz");
			Assert.Equal(Money.Parse("50"), url.Amount);
			Assert.Equal("Luke-Jr", url.Label);
			Assert.Equal("Donation for project xyz", url.Message);
			Assert.Equal(url.Uri.ToString(), new BitcoinUrlBuilder(url.Uri.ToString()).ToString());

			url = new BitcoinUrlBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=50&label=Luke-Jr&message=Donation%20for%20project%20xyz&unknownparam=lol");

			Assert.Throws<FormatException>(() => new BitcoinUrlBuilder("bitcoin:129mVqKUmJ9uwPxKJBnNdABbuaaNfho4Ha?amount=50&label=Luke-Jr&message=Donation%20for%20project%20xyz&req-unknownparam=lol"));
		}

		private BitcoinUrlBuilder CreateBuilder(string uri)
		{
			var builder = new BitcoinUrlBuilder(uri);
			Assert.Equal(builder.Uri, uri);
			return builder;
		}
	}
}
