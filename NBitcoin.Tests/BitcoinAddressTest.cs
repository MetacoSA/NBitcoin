using System;
using Xunit;

namespace nStratis.Tests
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
