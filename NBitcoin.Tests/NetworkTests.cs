using System.IO;
using System.Linq;
using System.Threading;
using NBitcoin;
using Xunit;
using System;

namespace NBitcoin.Tests
{
	public class NetworkTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void NetworkListIsInitialized()
		{
			Assert.NotEmpty(Network.GetNetworks());
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void NetworkListHasNoDuplicates()
		{
			var uniqueNetworkCount = Network.GetNetworks().Select(n => n.Name + n.ChainName).ToHashSet().Count();
			Assert.Equal(Network.GetNetworks().Count(), uniqueNetworkCount);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanGetNetworkFromName()
		{
			Assert.Equal(Network.GetNetwork("main"), Network.Main);
			Assert.Equal(Network.GetNetwork("reg"), Network.RegTest);
			Assert.Equal(Network.GetNetwork("regtest"), Network.RegTest);
			Assert.Equal(Network.GetNetwork("testnet"), Network.TestNet);
			Assert.Equal(Network.GetNetwork("testnet3"), Network.TestNet);
			Assert.Equal(Network.GetNetwork("testnet4"), Bitcoin.Instance.Testnet4);
			Assert.Equal(Network.GetNetwork("signet"), Bitcoin.Instance.Signet);
			Assert.Equal(Network.GetNetwork("mutinynet"), Bitcoin.Instance.Mutinynet);
			Assert.Null(Network.GetNetwork("invalid"));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanGetAllShitcoins()
		{
			NBitcoin.Altcoins.AltNetworkSets.GetAll().Select(c => c.Regtest).ToList();
			NBitcoin.Altcoins.AltNetworkSets.GetAll().Select(c => c.Testnet).ToList();
			NBitcoin.Altcoins.AltNetworkSets.GetAll().Select(c => c.Mainnet).ToList();
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

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanGetDefaultSignet()
		{
			var signet = Network.GetNetwork("signet");

			Assert.NotNull(signet);

			var consensusFactory = new ConsensusFactory();
			var block = consensusFactory.CreateBlock();
			block.ReadWrite(DataEncoders.Encoders.Hex.DecodeData(SignetSettings.DEFAULT_SIGNET_GENESIS_BLOCK), consensusFactory);

			Assert.Equal(block.GetHash(), signet.GenesisHash);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanGetCustomSignet()
		{
			var customSignetName = "signet-custom";
			var customSignetChallenge = "5121033da06bd7068e9859ee902a0608df9b948829718c60c587f2e497ad4d7420e43151AE";
			var customSignetGenesisBlock = "0100000000000000000000000000000000000000000000000000000000000000000000003ba3edfd7a7b12b27ac72c3e67768f617fc81bc3888a51323a9fb8aa4b1e5e4a008f4d5fae77031e8ad222030101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4d04ffff001d0104455468652054696d65732030332f4a616e2f32303039204368616e63656c6c6f72206f6e206272696e6b206f66207365636f6e64206261696c6f757420666f722062616e6b73ffffffff0100f2052a01000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000";
			var customSignetSeeds = new[]
			{
				"127.0.0.1",
				"[::1]"
			};

			var signetSettings = new SignetSettings(customSignetName, customSignetChallenge, customSignetGenesisBlock, customSignetSeeds);

			var initSignet = Bitcoin.Instance.InitCustomSignet(signetSettings);

			Assert.NotNull(initSignet);

			var customSignet = Network.GetNetwork(customSignetName);

			var consensusFactory = new ConsensusFactory();
			var block = consensusFactory.CreateBlock();
			block.ReadWrite(DataEncoders.Encoders.Hex.DecodeData(customSignetGenesisBlock), consensusFactory);

			Assert.Equal(block.GetHash(), customSignet.GenesisHash);
		}
	}
}
