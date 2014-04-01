using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bitcoin.Private.Bitcoin.Tests
{
	public class key_tests
	{
		const string strSecret1 = ("5HxWvvfubhXpYYpS3tJkw6fq9jE9j18THftkZjHHfmFiWtmAbrj");
		const string strSecret2 = ("5KC4ejrDjv152FGwP386VD1i2NYc5KkfSMyv1nGy1VGDxGHqVY3");
		const string strSecret1C = ("Kwr371tjA9u2rFSMZjTNun2PXXP3WPZu2afRHTcta6KxEUdm1vEw");
		const string strSecret2C = ("L3Hq7a8FEQwJkW1M2GNKDW28546Vp5miewcCzSqUD9kCAXrJdS3g");
		const string strAddressBad = ("1HV9Lc3sNHZxwj4Zk6fB38tEmBryq2cBiF");
		[Fact]
		public void key_test1()
		{
			BitcoinSecret bsecret1 = new BitcoinSecret();
			BitcoinSecret bsecret2 = new BitcoinSecret();
			BitcoinSecret bsecret1C = new BitcoinSecret();
			BitcoinSecret bsecret2C = new BitcoinSecret();
			BitcoinSecret baddress1 = new BitcoinSecret();

			Assert.True(bsecret1.SetString(strSecret1));
			Assert.True(bsecret2.SetString(strSecret2));
			Assert.True(bsecret1C.SetString(strSecret1C));
			Assert.True(bsecret2C.SetString(strSecret2C));
			Assert.True(!baddress1.SetString(strAddressBad));

			Key key1 = bsecret1.GetKey();
			Assert.True(key1.IsCompressed == false);
			Key key2 = bsecret2.GetKey();
			Assert.True(key2.IsCompressed == false);
			Key key1C = bsecret1C.GetKey();
			Assert.True(key1C.IsCompressed == true);
			Key key2C = bsecret2C.GetKey();
			Assert.True(key1C.IsCompressed == true);
		}
	}
}
