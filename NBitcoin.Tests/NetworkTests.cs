using System.IO;
using System.Linq;
using System.Threading;
using NBitcoin.Altcoins;
using Xunit;
using System;

namespace NBitcoin.Tests
{
	public class NetworkTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanGetNetworkFromName()
		{
			Assert.Equal(Network.GetNetwork("main"), Network.Main);
			Assert.Equal(Network.GetNetwork("reg"), Network.RegTest);
			Assert.Equal(Network.GetNetwork("regtest"), Network.RegTest);
			Assert.Equal(Network.GetNetwork("testnet"), Network.TestNet);
			Assert.Equal(Network.GetNetwork("testnet3"), Network.TestNet);
			Assert.Null(Network.GetNetwork("invalid"));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanGetAllShitcoins()
		{
			var altNetworkSets = new AltNetworkSets()
				.NetworkSets;

			altNetworkSets.Select(c => c.Regtest).ToList();
			altNetworkSets.Select(c => c.Testnet).ToList();
			altNetworkSets.Select(c => c.Mainnet).ToList();
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanGetRandomAltcoins()
		{
			var altNetworkSets = new AltNetworkSets()
				.NetworkSets;

			Assert.Contains<INetworkSet>(altNetworkSets, ns => ns.GetType().Equals(typeof(Litecoin)));
			Assert.Contains<INetworkSet>(altNetworkSets, ns => ns.GetType().Equals(typeof(Dogecoin)));
			Assert.Contains<INetworkSet>(altNetworkSets, ns => ns.GetType().Equals(typeof(Dash)));
			Assert.Contains<INetworkSet>(altNetworkSets, ns => ns.GetType().Equals(typeof(LBRYCredits)));
			Assert.Contains<INetworkSet>(altNetworkSets, ns => ns.GetType().Equals(typeof(Monacoin)));
			Assert.Contains<INetworkSet>(altNetworkSets, ns => ns.GetType().Equals(typeof(BCash)));
			Assert.Contains<INetworkSet>(altNetworkSets, ns => ns.GetType().Equals(typeof(ECash)));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCreateNetwork()
		{
			NetworkBuilder builder = new NetworkBuilder();
			builder.CopyFrom(Network.Main);
			builder.SetName(null);
			Assert.Throws<InvalidOperationException>(() => builder.BuildAndRegister());
			builder.SetName("new");
			builder.AddAlias("newalias");
			var network = builder.BuildAndRegister();
			Assert.Throws<InvalidOperationException>(() => builder.BuildAndRegister());

			Assert.Equal(network, Network.GetNetwork("new"));
			Assert.Equal(network, Network.GetNetwork("newalias"));

			CanGetNetworkFromName();

			Assert.Contains(network, Network.GetNetworks());
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
