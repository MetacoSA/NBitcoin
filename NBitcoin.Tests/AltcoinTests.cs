using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	[Trait("Altcoins", "Altcoins")]
	public class AltcoinTests
	{
		[Fact]
		public void NoCrashQuickTest()
		{
			foreach(var network in NBitcoin.Altcoins.AltNetworkSets.GetAll().ToList())
			{
				Assert.NotEqual(network.Mainnet, network.Regtest);
				Assert.NotEqual(network.Regtest, network.Testnet);
				Assert.Equal(NetworkType.Main, network.Mainnet.NetworkType);
				Assert.Equal(NetworkType.Testnet, network.Testnet.NetworkType);
				Assert.Equal(NetworkType.Regtest, network.Regtest.NetworkType);
			}
		}


		[Fact]
		public void HasCorrectGenesisBlock()
		{
			using(var builder = NodeBuilderEx.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				var actual = (rpc.GetBlock(0)).GetHash();
				Assert.Equal(builder.Network.GetGenesis().GetHash(), actual);
			}
		}

		[Fact]
		public void CanParseBlock()
		{
			using(var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				builder.StartAll();
				var rpc = node.CreateRPCClient();
				rpc.Generate(100);
				var hash = rpc.GetBestBlockHash();
				Assert.NotNull(rpc.GetBlock(hash));
			}
		}

		[Fact]
		public void CanParseAddress()
		{
			using(var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				builder.StartAll();
				var addr = node.CreateRPCClient().SendCommand(RPC.RPCOperations.getnewaddress).Result.ToString();
				var addr2 = BitcoinAddress.Create(addr, builder.Network).ToString();
				Assert.Equal(addr, addr2);

				var address = new Key().PubKey.GetAddress(builder.Network);
				var isValid = ((JObject)node.CreateRPCClient().SendCommand("validateaddress", address.ToString()).Result)["isvalid"].Value<bool>();
				Assert.True(isValid);
			}
		}

		[Fact]
		public void CanSyncWithPoW()
		{
			using(var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				builder.StartAll();
				node.Generate(100);

				var nodeClient = node.CreateNodeClient();
				nodeClient.VersionHandshake();
				ConcurrentChain chain = new ConcurrentChain(builder.Network);
				nodeClient.SynchronizeChain(chain, new Protocol.SynchronizeChainOptions() { SkipPoWCheck = false });
				Assert.Equal(100, chain.Height);
			}
		}

		[Fact]
		public void CanSyncWithoutPoW()
		{
			using(var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				builder.StartAll();
				node.Generate(100);
				var nodeClient = node.CreateNodeClient();
				nodeClient.VersionHandshake();
				ConcurrentChain chain = new ConcurrentChain(builder.Network);
				nodeClient.SynchronizeChain(chain, new Protocol.SynchronizeChainOptions() { SkipPoWCheck = true });
				Assert.Equal(100, chain.Height);

				// If it fails, override Block.GetConsensusFactory()
				var b = node.CreateRPCClient().GetBlock(50);
				Assert.Equal(b.WithOptions(TransactionOptions.Witness).Header.GetType(), chain.GetBlock(50).Header.GetType());

				var b2 = nodeClient.GetBlocks().ToArray()[50];
				Assert.Equal(b2.Header.GetType(), chain.GetBlock(50).Header.GetType());
			}
		}
	}
}