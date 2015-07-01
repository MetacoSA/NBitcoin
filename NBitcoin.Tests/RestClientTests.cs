using NBitcoin.RPC;
using System;
using System.Linq;
using Xunit;

namespace NBitcoin.Tests
{
	//Require a rpc server on test network running on default port with -rest -rpcuser=NBitcoin -rpcpassword=NBitcoinPassword
	//For me : 
	//"bitcoin-qt.exe" -testnet -server -rest 
	[Trait("RestClient", "RestClient")]
	public class RestClientTests
	{
		private static readonly Block TestNetGenesisBlock = Network.TestNet.GetGenesis();

		[Fact]
		public void CanGetChainInfo()
		{
			var client = CreateRestClient();
			var info = client.GetChainInfoAsync().Result;
			Assert.Equal("test", info.Chain);
		}

		[Fact]
		public void CanGetBlock()
		{
			var client = CreateRestClient();
			var block = client.GetBlockAsync(TestNetGenesisBlock.GetHash()).Result;
			Assert.Equal(block.GetHash(), TestNetGenesisBlock.GetHash());
		}

		[Fact]
		public void CanGetBlockHeader()
		{
			var client = CreateRestClient();
			var result = client.GetBlockHeadersAsync(TestNetGenesisBlock.GetHash(), 3).Result;
			var headers = result.ToArray();
			var last = headers.Last();
			Assert.Equal(3, headers.Length );
			Assert.Equal(uint256.Parse("000000006c02c8ea6e4ff69651f7fcde348fb9d557a06e6957b65552002a7820"), last.GetHash());
			Assert.Equal(headers[1].GetHash(), last.HashPrevBlock);
			Assert.Equal(TestNetGenesisBlock.GetHash(), headers[1].HashPrevBlock);
		}

		[Fact]
		public void CanGetTransaction()
		{
			var client = CreateRestClient();
			var txId = uint256.Parse("7669ce92f93f9afd51ffae243e04076be4e5088cf69501aab6de9ede5c331402");
			var tx = client.GetTransactionAsync(txId).Result;
			Assert.True(tx.IsCoinBase);
			Assert.Equal(Money.Coins(50), tx.TotalOut);
			Assert.True(tx.Outputs[0].IsTo(new PubKey("023044e5692e0ceb61bf88018abb99bcf5d0063e2f082a4e45879a1640728db5bc")));
			//new BitcoinScriptAddress("mpUjdth9vH3RiRjrqmmWtHTUotWZjZHuCY", Network.TestNet)));
		}

		[Fact]
		public void CanGetUTXOsMempool()
		{
			var client = CreateRestClient();
			var txId = uint256.Parse("3a3422dfd155f1d2ffc3e46cf978a9c5698c17c187f04cfa1b93358699c4ed3f");
			var outPoint = new OutPoint(txId, 0);
			var utxos = client.GetUnspentOutputsAsync(new []{ outPoint }, true).Result;
			Assert.Equal(1, utxos.Outputs.Length);
			Assert.Equal(1, (int)utxos.Outputs[0].Version);
			Assert.Equal(Money.Parse("0.1"), (int)utxos.Outputs[0].Output.Value);
		}

		[Fact]
		public void CanGetUTXOs()
		{
			var client = CreateRestClient();
			var txId = uint256.Parse("3a3422dfd155f1d2ffc3e46cf978a9c5698c17c187f04cfa1b93358699c4ed3f");
			var outPoint = new OutPoint(txId, 0);
			var utxos = client.GetUnspentOutputsAsync(new[] { outPoint }, false).Result;
			Assert.Equal(true, utxos.Bitmap[0]);
			Assert.Equal(false, utxos.Bitmap[1]);
			Assert.Equal(0, utxos.Outputs.Length);
		}

		[Fact]
		public void ThrowsRestApiClientException()
		{
			var client = CreateRestClient();

			var unexistingBlockId = uint256.Parse("100000006c02c8ea6e4ff69651f7fcde348fb9d557a06e6957b65552002a7820");
			Assert.Throws<RestApiException>(() => client.GetBlock(unexistingBlockId));

			var txId = uint256.Parse("7669ce92f93f9afd51ffae243e04076be4e5088cf69501aab6de9ede5c331402");
			Assert.Throws<RestApiException>(() => client.GetTransaction(txId));

			var result = client.GetBlockHeaders(unexistingBlockId, 3);
			var headers = result.ToArray();
			Assert.Empty(headers);
		}

		/// <summary>
		/// "bitcoin-qt.exe" -testnet -server -rest  
		/// </summary>
		/// <returns></returns>
		public static RestClient CreateRestClient()
		{
#if !NOSOCKET
			var process = Watcher.BitcoinQProcess.List()
				.FirstOrDefault(p => p.Testnet && p.Rest && p.Server && p.TxIndex);
			if (process == null)
				throw new InvalidOperationException("No bitcoin-qt or bitcoinq process running with rpc server on test net (\"bitcoin-qt.exe\" -testnet -server -rest -txindex)");
			return process.CreateRestClient();
#else
			return new RestClient(new Uri("http://127.0.0.1:18332"));
#endif
		}
	}
}
