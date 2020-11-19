using System.IO;
using System.Linq;
using System.Threading;
using NBitcoin;
using Xunit;
using System;
using NBitcoin.Protocol;

namespace NBitcoin.Tests
{
	public class NetworkAddressTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseSpecialAddresses()
		{
			var addr = new NetworkAddress();
						
			// TORv2
			Assert.True(addr.SetSpecial("6hzph5hv6337r6p2.onion"));
			Assert.True(addr.IsOnion);

			Assert.True(addr.IsAddrV1Compatible);
			Assert.Equal(addr.ToString(), "6hzph5hv6337r6p2.onion");

			// TORv3
			var torv3_addr = "pg6mmjiyjmcrsslvykfwnntlaru7p5svn6y2ymmju6nubxndf4pscryd.onion";
			Assert.True(addr.SetSpecial(torv3_addr));
			Assert.True(addr.IsOnion);

			Assert.False(addr.IsAddrV1Compatible);
			Assert.Equal(addr.ToString(), torv3_addr);

			// TORv3, broken, with wrong checksum
			Assert.False(addr.SetSpecial("pg6mmjiyjmcrsslvykfwnntlaru7p5svn6y2ymmju6nubxndf4pscsad.onion"));

			// TORv3, broken, with wrong version
			Assert.False(addr.SetSpecial("pg6mmjiyjmcrsslvykfwnntlaru7p5svn6y2ymmju6nubxndf4pscrye.onion"));

			// TORv3, malicious
			Assert.False(addr.SetSpecial("pg6mmjiyjmcrsslvykfwnntlaru7p5svn6y2ymmju6nubxndf4pscryd\0wtf.onion"));

			// TOR, bogus length
			Assert.False(addr.SetSpecial("mfrggzak.onion"));

			// TOR, invalid base32
			Assert.False(addr.SetSpecial("mf*g zak.onion"));
		}
	}
}