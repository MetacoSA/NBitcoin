using System.IO;
using System.Linq;
using System.Threading;
using NBitcoin;
using Xunit;

namespace NBitcoin.Tests
{
	public class NetworkTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanGetNetworkFromName()
		{
            Assert.Equal(Network.StratisMain, Network.StratisMain);

            Assert.Equal(Network.GetNetwork("main"), Network.Main);
			Assert.Equal(Network.GetNetwork("reg"), Network.RegTest);
			Assert.Equal(Network.GetNetwork("regtest"), Network.RegTest);
			Assert.Equal(Network.GetNetwork("testnet"), Network.TestNet);
			Assert.Equal(Network.GetNetwork("testnet3"), Network.TestNet);
			Assert.Null(Network.GetNetwork("invalid"));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ReadMagicByteWithFirstByteDuplicated()
		{
			var bytes = Network.Main.MagicBytes.ToList();
			bytes.Insert(0, bytes.First());

			using (var memstrema = new MemoryStream(bytes.ToArray()))
			{
				var found = Network.Main.ReadMagic(memstrema, new CancellationToken());
				Assert.True(found);
			}
		}
	}
}
