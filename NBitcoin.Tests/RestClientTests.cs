using NBitcoin.DataEncoders;
using NBitcoin.REST;
using NBitcoin.RPC;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using Xunit;

namespace NBitcoin.Tests
{
	//Require a rpc server on test network running on default port with -rest -rpcuser=NBitcoin -rpcpassword=NBitcoinPassword
	//For me : 
	//"bitcoin-qt.exe" -testnet -rest 
	[Trait("RestClient", "RestClient")]
	public class RestClientTests
	{
		private static Block TestNetGenesisBlock = Network.TestNet.GetGenesis();

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
		public void CanUnspentOutputs()
		{
			var client = CreateRestClient();
			var txId = uint256.Parse("7669ce92f93f9afd51ffae243e04076be4e5088cf69501aab6de9ede5c331402");
			var outPoint = new OutPoint(uint256.Parse("b2cdfd7b89def827ff8af7cd9bff7627ff72e5e8b0f71210f92ea7a4000c5d75"), 0);
			var tx = client.GetUnspectOutputsAsync(new []{ outPoint }, false).Result;
		}

		/// <summary>
		/// "bitcoin-qt.exe" -testnet -rest  
		/// </summary>
		/// <returns></returns>
		public static RestClient CreateRestClient()
		{
#if !NOSOCKET
			var process = Watcher.BitcoinQProcess.List()
				.FirstOrDefault(p => p.Testnet && p.Parameters.ContainsKey("rest"));
			if (process == null)
				throw new InvalidOperationException("No bitcoin-qt or bitcoinq process running with rpc server on test net (\"bitcoin-qt.exe\" -testnet -rest)");
			return process.CreateRestClient();
#else
			return new RestClient(new Uri("127.0.0.1:18332"));
#endif
		}
	}
}
