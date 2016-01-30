using NBitcoin;
using Xunit;

namespace NBitcoin.Tests
{
    public class NetworkTests
    {
        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void CanGetMainNetwork()
        {
            Assert.Equal(Network.GetNetwork("main"), Network.Main);
        }

        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void CanGetRegTestNetwork()
        {
            Assert.Equal(Network.GetNetwork("reg"), Network.RegTest);
            Assert.Equal(Network.GetNetwork("regtest"), Network.RegTest);
        }

        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void CanGetSegNetNetwork()
        {
            Assert.Equal(Network.GetNetwork("segnet"), Network.SegNet);
        }

        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void CanGetTestNetwork()
        {
            Assert.Equal(Network.GetNetwork("testnet"), Network.TestNet);
            Assert.Equal(Network.GetNetwork("testnet3"), Network.TestNet);
        }
    }
}
