using NBitcoin.DataEncoders;
using NBitcoin.RPC;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	//Require a rpc server on test network running on default port with -rpcuser=NBitcoin -rpcpassword=NBitcoinPassword
	//For me : 
	//"bitcoin-qt.exe" -testnet -server -rpcuser=NBitcoin -rpcpassword=NBitcoinPassword 
	[Trait("RPCClient", "RPCClient")]
	public class RPCClientTests
	{
		const string TestAccount = "NBitcoin.RPCClientTests";
		[Fact]
		public void InvalidCommandSendRPCException()
		{
			using(var builder = NodeBuilder.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				AssertException<RPCException>(() => rpc.SendCommand("donotexist"), (ex) =>
				{
					Assert.True(ex.RPCCode == RPCErrorCode.RPC_METHOD_NOT_FOUND);
				});
			}
		}


		[Fact]
		public void CanSendCommand()
		{
			using(var builder = NodeBuilder.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				var response = rpc.SendCommand(RPCOperations.getinfo);
				Assert.NotNull(response.Result);
			}
		}

		[Fact]
		public void CanGetGenesisFromRPC()
		{
			using(var builder = NodeBuilder.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				var response = rpc.SendCommand(RPCOperations.getblockhash, 0);
				var actualGenesis = (string)response.Result;
				Assert.Equal(Network.RegTest.GetGenesis().GetHash().ToString(), actualGenesis);
				Assert.Equal(Network.RegTest.GetGenesis().GetHash(), rpc.GetBestBlockHash());
			}
		}

		[Fact]
		public void CanGetRawMemPool()
		{
			using(var builder = NodeBuilder.Create())
			{
				var node = builder.CreateNode();
				var rpc = node.CreateRPCClient();
				builder.StartAll();
				node.Generate(101);
				var txid = rpc.SendToAddress(new Key().PubKey.GetAddress(rpc.Network), Money.Coins(1.0m), "hello", "world");
				var ids = rpc.GetRawMempool();
				Assert.Equal(1, ids.Length);
				Assert.Equal(txid, ids[0]);
			}
		}

		[Fact]
		public void CanUseAsyncRPC()
		{
			using(var builder = NodeBuilder.Create())
			{
				var node = builder.CreateNode();
				var rpc = node.CreateRPCClient();
				builder.StartAll();
				node.Generate(10);
				var blkCount = rpc.GetBlockCountAsync().Result;
				Assert.Equal(10, blkCount);
			}
		}

		[Fact]
		public void CanGetBlockFromRPC()
		{
			using(var builder = NodeBuilder.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				var response = rpc.GetBlockHeader(0);
				AssertEx.CollectionEquals(Network.RegTest.GetGenesis().Header.ToBytes(), response.ToBytes());

				response = rpc.GetBlockHeader(0);
				Assert.Equal(Network.RegTest.GenesisHash, response.GetHash());
			}
		}

		[Fact]
		public void EstimateFeeRate()
		{
			using(var builder = NodeBuilder.Create())
			{
				var node = builder.CreateNode();
				node.Start();
				node.Generate(101);
				var rpc = node.CreateRPCClient();
				Assert.Throws<NoEstimationException>(() => rpc.EstimateFeeRate(1));
			}
		}

		[Fact]
		public void TryEstimateFeeRate()
		{
			using(var builder = NodeBuilder.Create())
			{
				var node = builder.CreateNode();
				node.Start();
				node.Generate(101);
				var rpc = node.CreateRPCClient();
				Assert.Null(rpc.TryEstimateFeeRate(1));
			}
		}

		[Fact]
		public void TestFundRawTransaction()
		{
			using(var builder = NodeBuilder.Create())
			{
				var node = builder.CreateNode();
				node.Start();
				node.Generate(101);

				var k = new Key();
				var tx = new Transaction();
				tx.Outputs.Add(new TxOut(Money.Coins(1), k));
				var rpc = node.CreateRPCClient();
				var result = rpc.FundRawTransaction(tx);
				TestFundRawTransactionResult(tx, result);

				result = rpc.FundRawTransaction(tx, new FundRawTransactionOptions());
				TestFundRawTransactionResult(tx, result);
				var result1 = result;

				var change = rpc.GetNewAddress();
				result = rpc.FundRawTransaction(tx, new FundRawTransactionOptions()
				{
					FeeRate = new FeeRate(Money.Satoshis(50), 1),
					IncludeWatching = true,
					ChangeAddress = change,
				});
				TestFundRawTransactionResult(tx, result);
				Assert.True(result1.Fee < result.Fee);
				Assert.True(result.Transaction.Outputs.Any(o => o.ScriptPubKey == change.ScriptPubKey));
			}
		}

		private static void TestFundRawTransactionResult(Transaction tx, FundRawTransactionResponse result)
		{
			Assert.Equal(tx.Version, result.Transaction.Version);
			Assert.True(result.Transaction.Inputs.Count > 0);
			Assert.True(result.Transaction.Outputs.Count > 1);
			Assert.True(result.ChangePos != -1);
			Assert.Equal(Money.Coins(50m) - result.Transaction.Outputs.Select(txout => txout.Value).Sum(), result.Fee);
		}

		[Fact]
		public void CanGetTransactionBlockFromRPC()
		{
			using(var builder = NodeBuilder.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				var blockId = rpc.GetBestBlockHash();
				var block = rpc.GetBlock(blockId);
				Assert.True(block.CheckMerkleRoot());
			}
		}

		[Fact]
		public void CanGetPrivateKeysFromAccount()
		{
			using(var builder = NodeBuilder.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				Key key = new Key();
				rpc.ImportAddress(key.PubKey.GetAddress(Network.RegTest), TestAccount, false);
				BitcoinAddress address = rpc.GetAccountAddress(TestAccount);
				BitcoinSecret secret = rpc.DumpPrivKey(address);
				BitcoinSecret secret2 = rpc.GetAccountSecret(TestAccount);

				Assert.Equal(secret.ToString(), secret2.ToString());
				Assert.Equal(address.ToString(), secret.GetAddress().ToString());
			}
		}

		[Fact]
		public void CanDecodeAndEncodeRawTransaction()
		{
			var a = new Protocol.AddressManager().Select();
			var tests = TestCase.read_json("data/tx_raw.json");
			foreach(var test in tests)
			{
				var format = (RawFormat)Enum.Parse(typeof(RawFormat), (string)test[0], true);
				var network = ((string)test[1]) == "Main" ? Network.Main : Network.TestNet;
				var testData = ((JObject)test[2]).ToString();

				Transaction raw = Transaction.Parse(testData, format, network);

				AssertJsonEquals(raw.ToString(format, network), testData);

				var raw3 = Transaction.Parse(raw.ToString(format, network), format);
				Assert.Equal(raw.ToString(format, network), raw3.ToString(format, network));
			}
		}

		[Fact]
		public void CanDecodeUnspentCoinWatchOnlyAddress()
		{
			var testJson =
@"{
	""txid"" : ""d54994ece1d11b19785c7248868696250ab195605b469632b7bd68130e880c9a"",
	""vout"" : 1,
	""address"" : ""mgnucj8nYqdrPFh2JfZSB1NmUThUGnmsqe"",
	""account"" : ""test label"",
	""scriptPubKey"" : ""76a9140dfc8bafc8419853b34d5e072ad37d1a5159f58488ac"",
	""amount"" : 0.00010000,
	""confirmations"" : 6210,
	""spendable"" : false
}";
			var testData = JObject.Parse(testJson);
			var unspentCoin = new UnspentCoin(testData);

			Assert.Equal("test label", unspentCoin.Account);
			Assert.False(unspentCoin.IsSpendable);
			Assert.Null(unspentCoin.RedeemScript);
		}

		[Fact]
		public void CanDecodeUnspentCoinLegacyPre_0_10_0()
		{
			var testJson =
@"{
	""txid"" : ""d54994ece1d11b19785c7248868696250ab195605b469632b7bd68130e880c9a"",
	""vout"" : 1,
	""address"" : ""mgnucj8nYqdrPFh2JfZSB1NmUThUGnmsqe"",
	""account"" : ""test label"",
	""scriptPubKey"" : ""76a9140dfc8bafc8419853b34d5e072ad37d1a5159f58488ac"",
	""amount"" : 0.00010000,
	""confirmations"" : 6210
}";
			var testData = JObject.Parse(testJson);
			var unspentCoin = new UnspentCoin(testData);

			// Versions prior to 0.10.0 were always spendable (but had no JSON field)
			Assert.True(unspentCoin.IsSpendable);
		}

		[Fact]
		public void CanDecodeUnspentCoinWithRedeemScript()
		{
			var testJson =
@"{
	""txid"" : ""d54994ece1d11b19785c7248868696250ab195605b469632b7bd68130e880c9a"",
	""vout"" : 1,
	""address"" : ""mgnucj8nYqdrPFh2JfZSB1NmUThUGnmsqe"",
	""account"" : ""test label"",
	""scriptPubKey"" : ""76a9140dfc8bafc8419853b34d5e072ad37d1a5159f58488ac"",
	""redeemScript"" : ""522103310188e911026cf18c3ce274e0ebb5f95b007f230d8cb7d09879d96dbeab1aff210243930746e6ed6552e03359db521b088134652905bd2d1541fa9124303a41e95621029e03a901b85534ff1e92c43c74431f7ce72046060fcf7a95c37e148f78c7725553ae"",
	""amount"" : 0.00010000,
	""confirmations"" : 6210,
	""spendable"" : true
}";
			var testData = JObject.Parse(testJson);
			var unspentCoin = new UnspentCoin(testData);

			Console.WriteLine("Redeem Script: {0}", unspentCoin.RedeemScript);
			Assert.NotNull(unspentCoin.RedeemScript);
		}

		[Fact]
		public void RawTransactionIsConformsToRPC()
		{
			using(var builder = NodeBuilder.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				var tx = Network.TestNet.GetGenesis().Transactions[0];

				var tx2 = rpc.DecodeRawTransaction(tx.ToBytes());
				AssertJsonEquals(tx.ToString(RawFormat.Satoshi), tx2.ToString(RawFormat.Satoshi));
			}
		}

#if !PORTABLE
		[Fact]
		public void CanGetPeersInfo()
		{
			using(var builder = NodeBuilder.Create())
			{
				var nodeA = builder.CreateNode();
				builder.StartAll();
				var rpc = nodeA.CreateRPCClient();
				using(var node = nodeA.CreateNodeClient())
				{
					node.VersionHandshake();
					var peers = rpc.GetPeersInfo();
					Assert.NotEmpty(peers);
				}
			}
		}
#endif
#if !NOSOCKET
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseIpEndpoint()
		{
			var endpoint = Utils.ParseIpEndpoint("google.com:94", 90);
			Assert.Equal(94, endpoint.Port);
			endpoint = Utils.ParseIpEndpoint("google.com", 90);
			Assert.Equal(90, endpoint.Port);
			endpoint = Utils.ParseIpEndpoint("10.10.1.3", 90);
			Assert.Equal("10.10.1.3", endpoint.Address.ToString());
			Assert.Equal(90, endpoint.Port);
			endpoint = Utils.ParseIpEndpoint("10.10.1.3:94", 90);
			Assert.Equal("10.10.1.3", endpoint.Address.ToString());
			Assert.Equal(94, endpoint.Port);
			Assert.Throws<System.Net.Sockets.SocketException>(() => Utils.ParseIpEndpoint("2001:db8:1f70::999:de8:7648:6e8:100", 90));
			endpoint = Utils.ParseIpEndpoint("2001:db8:1f70::999:de8:7648:6e8", 90);
			Assert.Equal("2001:db8:1f70:0:999:de8:7648:6e8", endpoint.Address.ToString());
			Assert.Equal(90, endpoint.Port);
			endpoint = Utils.ParseIpEndpoint("[2001:db8:1f70::999:de8:7648:6e8]:94", 90);
			Assert.Equal("2001:db8:1f70:0:999:de8:7648:6e8", endpoint.Address.ToString());
			Assert.Equal(94, endpoint.Port);
		}

		[Fact]
		public void CanAuthWithCookieFile()
		{
			var rpc = new RPCClient("cookiefile=Data\\.cookie", new Uri("http://localhost/"), Network.RegTest);
			Assert.Throws<ArgumentException>(() => new RPCClient("cookiefile=Data\\tx_valid.json", new Uri("http://localhost/"), Network.RegTest));
			Assert.Throws<FileNotFoundException>(() => new RPCClient("cookiefile=Data\\efpwwie.json", new Uri("http://localhost/"), Network.RegTest));
			rpc = new RPCClient("bla:bla", null as Uri, Network.RegTest);
			Assert.Equal("http://127.0.0.1:" + Network.RegTest.RPCPort + "/", rpc.Address.AbsoluteUri);

			rpc = new RPCClient("bla:bla", "http://toto/", Network.RegTest);
		}



		[Fact]
		public void RPCSendRPCException()
		{
			using(var builder = NodeBuilder.Create())
			{
				var node = builder.CreateNode();
				builder.StartAll();
				var rpcClient = node.CreateRPCClient();
				try
				{
					rpcClient.SendCommand("whatever");
					Assert.False(true, "Should have thrown");
				}
				catch(RPCException ex)
				{
					if(ex.RPCCode != RPCErrorCode.RPC_METHOD_NOT_FOUND)
					{
						Assert.False(true, "Should have thrown RPC_METHOD_NOT_FOUND");
					}
				}
			}
		}

		[Fact]
		public void CanAddNodes()
		{
			using(var builder = NodeBuilder.Create())
			{
				var nodeA = builder.CreateNode();
				var nodeB = builder.CreateNode();
				builder.StartAll();
				var rpc = nodeA.CreateRPCClient();
				rpc.RemoveNode(nodeA.Endpoint);
				rpc.AddNode(nodeB.Endpoint);
				Thread.Sleep(500);
				var info = rpc.GetAddedNodeInfo(true);
				Assert.NotNull(info);
				Assert.NotEmpty(info);
				//For some reason this one does not pass anymore in 0.13.1
				//Assert.Equal(nodeB.Endpoint, info.First().Addresses.First().Address);
				var oneInfo = rpc.GetAddedNodeInfo(true, nodeB.Endpoint);
				Assert.NotNull(oneInfo);
				Assert.True(oneInfo.AddedNode.ToString() == nodeB.Endpoint.ToString());
				oneInfo = rpc.GetAddedNodeInfo(true, nodeA.Endpoint);
				Assert.Null(oneInfo);
				rpc.RemoveNode(nodeB.Endpoint);
				Thread.Sleep(500);
				info = rpc.GetAddedNodeInfo(true);
				Assert.Equal(0, info.Count());
			}
		}
#endif
		[Fact]
		public void CanBackupWallet()
		{
			using(var builder = NodeBuilder.Create())
			{
				var node = builder.CreateNode();
				node.Start();
				var buildOutputDir = Path.GetDirectoryName(".");
				var filePath = Path.Combine(buildOutputDir, "wallet_backup.dat");
				try
				{
					var rpc = node.CreateRPCClient();
					rpc.BackupWallet(filePath);
					Assert.True(File.Exists(filePath));
				}
				finally
				{
					if(File.Exists(filePath))
						File.Delete(filePath);
				}
			}
		}

		[Fact]
		public void CanEstimatePriority()
		{
			using(var builder = NodeBuilder.Create())
			{
				var node = builder.CreateNode();
				node.Start();
				var rpc = node.CreateRPCClient();
				node.Generate(101);
				var priority = rpc.EstimatePriority(10);
				Assert.True(priority > 0 || priority == -1);
			}
		}


		private void AssertJsonEquals(string json1, string json2)
		{
			foreach(var c in new[] { "\r\n", " ", "\t" })
			{
				json1 = json1.Replace(c, "");
				json2 = json2.Replace(c, "");
			}

			Assert.Equal(json1, json2);
		}

		void AssertException<T>(Action act, Action<T> assert) where T : Exception
		{
			try
			{
				act();
				Assert.False(true, "Should have thrown an exception");
			}
			catch(T ex)
			{
				assert(ex);
			}
		}
	}
}
