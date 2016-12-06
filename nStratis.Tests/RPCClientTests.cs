using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using nStratis.RPC;
using Newtonsoft.Json.Linq;
using Xunit;

namespace nStratis.Tests
{
	using System.Diagnostics;

	using Xunit.Sdk;

	// This test requires a main net node running locally in server mode 
	// on default port with -rpcuser=rpcuser -rpcpassword=rpcpassword
	// if a node is not running locally all this tests will pass
	// todo: implement the xuint skip framework https://github.com/xunit/samples.xunit/tree/master/DynamicSkipExample

	[Trait("RPCClient", "RPCClient")]
	public class RPCClientTests 
	{
		public static bool noClient = !Process.GetProcesses().Any(p => p.ProcessName.Contains("stratis"));

		const string TestAccount = "nStratis.RPCClientTests";
		[Fact]
		public void InvalidCommandSendRPCException()
		{
			if (RPCClientTests.noClient) return;

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
			if (RPCClientTests.noClient) return;

			using (var builder = NodeBuilder.Create())
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
			if (RPCClientTests.noClient) return;

			using (var builder = NodeBuilder.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				var response = rpc.SendCommand(RPCOperations.getblockhash, 0);
				var actualGenesis = (string)response.Result;
				Assert.Equal(Network.Main.GetGenesis().GetHash().ToString(), actualGenesis);
				//Assert.Equal(Network.Main.GetGenesis().GetHash(), rpc.GetBestBlockHash());
			}
		}

		[Fact]
		public void CanGetRawMemPool()
		{
			if (RPCClientTests.noClient) return;

			using (var builder = NodeBuilder.Create())
			{
				var node = builder.CreateNode();
				var rpc = node.CreateRPCClient();
				builder.StartAll();
				//node.Generate(101);
				//var txid = rpc.SendToAddress(new Key().PubKey.GetAddress(rpc.Network), Money.Coins(1.0m), "hello", "world");
				var ids = rpc.GetRawMempool();
				Assert.NotNull(ids);
			}
		}

		[Fact]
		public void CanUseAsyncRPC()
		{
			if (RPCClientTests.noClient) return;

			using (var builder = NodeBuilder.Create())
			{
				var node = builder.CreateNode();
				var rpc = node.CreateRPCClient();
				builder.StartAll();
				//node.Generate(10);
				var blkCount = rpc.GetBlockCountAsync().Result;
				Assert.True(blkCount > 0);
			}
		}

		[Fact]
		public void CanGetBlockFromRPC()
		{
			if (RPCClientTests.noClient) return;

			using (var builder = NodeBuilder.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				var response = rpc.GetBlockHeader(0);
				AssertEx.CollectionEquals(Network.Main.GetGenesis().Header.ToBytes(), response.ToBytes());

				response = rpc.GetBlockHeader(0);
				Assert.Equal(Network.Main.GenesisHash, response.GetHash());
			}
		}

		// this method does not exist in stratis
		//[Fact]
		//public void CanEstimateFees()
		//{
		//	using(var builder = NodeBuilder.Create())
		//	{
		//		var node = builder.CreateNode();
		//		builder.StartAll();
		//		//node.Generate(101);
		//		var rpc = node.CreateRPCClient();
		//		var result = rpc.EstimateFee(1);
		//		Assert.Equal(Money.Zero, result.FeePerK);
		//	}
		//}

		[Fact]
		public void CanGetBlockWithSignature()
		{
			if (RPCClientTests.noClient) return;

			using (var builder = NodeBuilder.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				var blockId = uint256.Parse("ae36de379a543378e13b0ce70275f21487f613c216d76c7ffb36c685c8992a74");
				var block = rpc.GetBlock(blockId);
				Assert.NotNull(block);
			}
		}

		[Fact]
		public void CanGetBlockWithoutSignature()
		{
			if (RPCClientTests.noClient) return;

			using (var builder = NodeBuilder.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				var blockId = uint256.Parse("c9920baf967a314bd123efa184d54d4b9e7460301e3f2e059bafc77c45d03017");
				var block = rpc.GetBlock(blockId);
				Assert.NotNull(block);
			}
		}

		[Fact]
		public void CanGetPrivateKeysFromAccount()
		{
			if (RPCClientTests.noClient) return;

			using (var builder = NodeBuilder.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				Key key = new Key();
				rpc.ImportPrivKey(key.GetBitcoinSecret(Network.Main));
				BitcoinSecret secret = rpc.DumpPrivKey(key.PubKey.GetAddress(Network.Main));

				Assert.Equal(secret.ToString(), key.GetBitcoinSecret(Network.Main).ToString());
			}
		}

		[Fact]
		public void CanDecodeAndEncodeRawTransaction()
		{
			if (RPCClientTests.noClient) return;

			var tests = TestCase.read_json(TestDataLocations.DataFolder(@"tx_raw.json"));
			using(var builder = NodeBuilder.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				var index = 0;
				foreach(var test in tests)
				{
					var format = (RawFormat)Enum.Parse(typeof(RawFormat), (string)test[0], true);
					var network = ((string)test[1]) == "Main" ? Network.Main : Network.TestNet;
					var testData = ((JObject)test[2]).ToString();

					Transaction raw = Transaction.Parse(testData, format, network);

					AssertJsonEquals(raw.ToString(format, network), testData);

					var raw3 = Transaction.Parse(raw.ToString(format, network), format);
					Assert.Equal(raw.ToString(format, network), raw3.ToString(format, network));
					index++;
				}
			}
		}

		[Fact]
		public void RawTransactionIsConformsToRPC()
		{
			if (RPCClientTests.noClient) return;

			using (var builder = NodeBuilder.Create())
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
			if (RPCClientTests.noClient) return;

			using (var builder = NodeBuilder.Create())
			{
				var nodeA = builder.CreateNode();
				builder.StartAll();
				var rpc = nodeA.CreateRPCClient();
				//using(var node = nodeA.CreateNodeClient())
				//{
					//node.VersionHandshake();
					var peers = rpc.GetPeersInfo();
					Assert.NotEmpty(peers);
				//}
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
		public void CanAddNodes()
		{
			if (RPCClientTests.noClient) return;

			using (var builder = NodeBuilder.Create())
			{
				var nodeA = builder.CreateNode();
				//var nodeB = builder.CreateNode();
				builder.StartAll();
				var rpc = nodeA.CreateRPCClient();
				//rpc.RemoveNode(nodeA.Endpoint);
				var ep = new IPEndPoint(IPAddress.Parse("50.50.50.50"), 50505);
				rpc.RemoveNode(ep);
				Thread.Sleep(500);
				var info = rpc.GetAddedNodeInfo(true);
				Assert.False(info.Any(a => a.AddedNode.ToString() ==  ep.ToString()));

				rpc.AddNode(ep);
				Thread.Sleep(500);
				info = rpc.GetAddedNodeInfo(true);
				Assert.NotNull(info);
				Assert.NotEmpty(info);
				Assert.True(info.Any(a => a.AddedNode.ToString() == ep.ToString()));
			}
		}
#endif
		//[Fact]
		//public void CanBackupWallet()
		//{
		//	using(var builder = NodeBuilder.Create())
		//	{
		//		var node = builder.CreateNode();
		//		builder.StartAll();
		//		var buildOutputDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		//		var filePath = Path.Combine(buildOutputDir, "wallet_backup.dat");
		//		try
		//		{
		//			var rpc = node.CreateRPCClient();
		//			rpc.BackupWallet(filePath);
		//			Assert.True(File.Exists(filePath));
		//		}
		//		finally
		//		{
		//			if(File.Exists(filePath))
		//				File.Delete(filePath);
		//		}
		//	}
		//}

		//[Fact]
		//public void CanEstimatePriority()
		//{
		//	using(var builder = NodeBuilder.Create())
		//	{
		//		var node = builder.CreateNode();
		//		node.Start();
		//		var rpc = node.CreateRPCClient();
		//		node.Generate(101);
		//		var priority = rpc.EstimatePriority(10);
		//		Assert.True(priority > 0 || priority == -1);
		//	}
		//}


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
