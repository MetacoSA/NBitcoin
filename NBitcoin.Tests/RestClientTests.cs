using NBitcoin.RPC;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	//Require a rpc server on test network running on default port with -rest -rpcuser=NBitcoin -rpcpassword=NBitcoinPassword
	//For me : 
	//"bitcoin-qt.exe" -testnet -server -rest 
	[Trait("RestClient", "RestClient")]
	public class RestClientTests
	{
		private static readonly Block RegNetGenesisBlock = Network.RegTest.GetGenesis();

		[Fact]
		public async Task CanGetChainInfo()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var client = builder.CreateNode().CreateRESTClient();
				builder.StartAll();
				var info = await client.GetChainInfoAsync();
				Assert.Equal("regtest", info.Chain);
			}
		}

		[Fact]
		public async Task CanCalculateChainWork()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				var client = node.CreateRESTClient();
				var rpc = node.CreateRPCClient();
				builder.StartAll();
				var info = await client.GetChainInfoAsync();
				Assert.Equal("regtest", info.Chain);
				Assert.Equal(new ChainedBlock(Network.RegTest.GetGenesis().Header, 0).GetChainWork(false), info.ChainWork);
				rpc.Generate(10);
				var chain = node.CreateNodeClient().GetChain();
				info = await client.GetChainInfoAsync();
				Assert.Equal(info.ChainWork, chain.Tip.GetChainWork(false));
			}
		}

		[Fact]
		public async Task CanGetBlock()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var client = builder.CreateNode().CreateRESTClient();
				builder.StartAll();
				var block = await client.GetBlockAsync(RegNetGenesisBlock.GetHash());
				Assert.Equal(block.GetHash(), RegNetGenesisBlock.GetHash());
			}
		}

		[Fact]
		public async Task CanGetBlockHeader()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var client = builder.CreateNode().CreateRESTClient();
				var rpc = builder.Nodes[0].CreateRPCClient();
				builder.StartAll();
				rpc.Generate(2);
				var result = await client.GetBlockHeadersAsync(RegNetGenesisBlock.GetHash(), 3);
				var headers = result.ToArray();
				var last = headers.Last();
				Assert.Equal(3, headers.Length);
				Assert.Equal(rpc.GetBestBlockHash(), last.GetHash());
				Assert.Equal(headers[1].GetHash(), last.HashPrevBlock);
				Assert.Equal(RegNetGenesisBlock.GetHash(), headers[1].HashPrevBlock);
			}
		}

		[Fact]
		public async Task CanGetTransaction()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var client = builder.CreateNode().CreateRESTClient();
				builder.StartAll();
				builder.Nodes[0].Generate(1);
				var block = builder.Nodes[0].CreateRPCClient().GetBestBlockHash();
				var txId = builder.Nodes[0].CreateRPCClient().GetBlock(block).Transactions[0].GetHash();
				var tx = await client.GetTransactionAsync(txId);
				Assert.True(tx.IsCoinBase);
				Assert.Equal(Money.Coins(50), tx.TotalOut);
			}
		}

		[Fact]
		public async Task CanGetUTXOsMempool()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var client = builder.CreateNode().CreateRESTClient();
				var rpc = builder.Nodes[0].CreateRPCClient();
				builder.StartAll();
				var k = new Key().GetBitcoinSecret(Network.RegTest);
				rpc.Generate(102);
				rpc.ImportPrivKey(k);
				rpc.SendToAddress(k.GetAddress(ScriptPubKeyType.Legacy), Money.Coins(50m));
				rpc.Generate(1);
				var c = rpc.ListUnspent().First();
				c = rpc.ListUnspent(0, 999999, k.GetAddress(ScriptPubKeyType.Legacy)).First();
				var outPoint = c.OutPoint;
				var utxos = await client.GetUnspentOutputsAsync(new[] { outPoint }, true);
				Assert.Single(utxos.Outputs);
				Assert.Equal(0, (int)utxos.Outputs[0].Version);
				Assert.Equal(Money.Coins(50m), utxos.Outputs[0].Output.Value);

				var countBefore = rpc.ListUnspent().Length;
				rpc.LockUnspent(outPoint);
				var countAfter = rpc.ListUnspent().Length;
				Assert.Equal(countBefore - 1, countAfter);
			}
		}

		[Fact]
		public async Task CanGetUTXOs()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var client = builder.CreateNode().CreateRESTClient();
				builder.StartAll();
				var txId = uint256.Parse("3a3422dfd155f1d2ffc3e46cf978a9c5698c17c187f04cfa1b93358699c4ed3f");
				var outPoint = new OutPoint(txId, 0);
				var utxos = await client.GetUnspentOutputsAsync(new[] { outPoint }, false);
				Assert.True(utxos.Bitmap[0]);
				Assert.False(utxos.Bitmap[1]);
				Assert.Empty(utxos.Outputs);
			}
		}

		[Fact]
		public void ThrowsRestApiClientException()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var client = builder.CreateNode().CreateRESTClient();
				builder.StartAll();
				var unexistingBlockId = uint256.Parse("100000006c02c8ea6e4ff69651f7fcde348fb9d557a06e6957b65552002a7820");
				var err = Assert.Throws<RestApiException>(() => client.GetBlock(unexistingBlockId));
				Assert.Equal(System.Net.HttpStatusCode.NotFound, err.HttpStatusCode);
				var txId = uint256.Parse("7569ce92f93f9afd51ffae243e04076be4e5088cf69501aab6de9ede5c331402");
				Assert.Throws<RestApiException>(() => client.GetTransaction(txId));

				var result = client.GetBlockHeaders(unexistingBlockId, 3);
				var headers = result.ToArray();
				Assert.Empty(headers);
			}
		}

		[Fact]
		public async Task CanGetBlockHashByHeight()
		{			
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				var client = node.CreateRESTClient();
				builder.StartAll();
								
				var genesisHash = await client.GetBlockHashByHeightAsync(0);
				Assert.Equal(RegNetGenesisBlock.GetHash(), genesisHash);
				
				var expectedHash = node.Generate(1).FirstOrDefault();
								
				var actualHash = await client.GetBlockHashByHeightAsync(1);
				Assert.Equal(expectedHash, actualHash);

				// Should throw exception for non-existent block height
				await Assert.ThrowsAsync<RestApiException>(async () => 
					await client.GetBlockHashByHeightAsync(2));
			}
		}
	}
}
