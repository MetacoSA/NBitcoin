using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class BitcoinAddressTest
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ShouldThrowBase58Exception()
		{
			String key = "";
			Assert.Throws<FormatException>(() => BitcoinAddress.Create(key, Network.Main));

			key = null;
			Assert.Throws<ArgumentNullException>(() => BitcoinAddress.Create(key, Network.Main));
		}
	}
}
