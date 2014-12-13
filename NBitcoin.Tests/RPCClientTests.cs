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
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	//Require a rpc server on test network running on default port with -rpcuser=NBitcoin -rpcpassword=NBitcoinPassword
	//For me : 
	//"bitcoin-qt.exe" -testnet -server -rpcuser=NBitcoin -rpcpassword=NBitcoinPassword 
	public class RPCClientTests
	{
		const string TestAccount = "NBitcoin.RPCClientTests";
		[Fact]
		[Trait("RPCClient", "RPCClient")]
		public void InvalidCommandSendRPCException()
		{
			var rpc = CreateRPCClient();
			AssertException<RPCException>(() => rpc.SendCommand("donotexist"), (ex) =>
			{
				Assert.True(ex.RPCCode == RPCErrorCode.RPC_METHOD_NOT_FOUND);
			});
		}


		[Fact]
		[Trait("RPCClient", "RPCClient")]
		public void CanSendCommand()
		{
			var rpc = CreateRPCClient();
			var response = rpc.SendCommand(RPCOperations.getinfo);
			Assert.NotNull(response.Result);
		}

		[Fact]
		[Trait("RPCClient", "RPCClient")]
		public void CanGetGenesisFromRPC()
		{
			var rpc = CreateRPCClient();
			var response = rpc.SendCommand(RPCOperations.getblockhash, 0);
			var actualGenesis = (string)response.Result;
			Assert.Equal(Network.TestNet.GetGenesis().GetHash().ToString(), actualGenesis);
		}

		[Fact]
		[Trait("RPCClient", "RPCClient")]
		public void CanGetRawMemPool()
		{
			var rpc = CreateRPCClient();
			var ids = rpc.GetRawMempool();
		}

		[Fact]
		[Trait("RPCClient", "RPCClient")]
		public void CanUseAsyncRPC()
		{
			var rpc = CreateRPCClient();
			var blkCount = rpc.GetBlockCountAsync().Result;
			Assert.True(blkCount != 0);
		}

		[Fact]
		[Trait("RPCClient", "RPCClient")]
		public void CanGetBestBlockHash()
		{
			var rpc = CreateRPCClient();
			var hash = rpc.GetBestBlockHash();
			Assert.NotNull(hash);
		}

		[Fact]
		[Trait("RPCClient", "RPCClient")]
		public void CanGetBlockFromRPC()
		{
			var rpc = CreateRPCClient();
			var response = rpc.GetBlockHeader(0);
			AssertEx.CollectionEquals(Network.TestNet.GetGenesis().Header.ToBytes(), response.ToBytes());

			response = rpc.GetBlockHeader(260583);
			Assert.Equal("00000000bcd68bd3d66ae5a198bb21133e44d9fc13c0688c846037658d95b87c", response.GetHash().ToString());
		}

		[Fact]
		[Trait("RPCClient", "RPCClient")]
		public void CanGetTransactionBlockFromRPC()
		{
			var rpc = CreateRPCClient();
			var blockId = rpc.GetBestBlockHash();
			var result = rpc.GetTransactions(blockId).ToList();
			var block = rpc.GetBlock(blockId);
			//Block can be null if not all transactions are present (txindex=0)
			if(block != null)
			{
				Assert.True(block.CheckMerkleRoot());
				Assert.True(block.Transactions.Count == result.Count);
			}
		}

		[Fact]
		[Trait("RPCClient", "RPCClient")]
		public void CanGetPrivateKeysFromAccount()
		{
			var rpc = CreateRPCClient();
			BitcoinAddress address = rpc.GetAccountAddress(TestAccount);
			BitcoinSecret secret = rpc.DumpPrivKey(address);
			BitcoinSecret secret2 = rpc.GetAccountSecret(TestAccount);

			Assert.Equal(secret.ToString(), secret2.ToString());
			Assert.Equal(address.ToString(), secret.GetAddress().ToString());
		}

		[Fact]
		[Trait("RPCClient", "RPCClient")]
		public void CanDecodeAndEncodeRawTransaction()
		{
			var tests = TestCase.read_json("data/tx_raw.json");
			var rpc = CreateRPCClient();

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
		[Trait("RPCClient", "RPCClient")]
		public void RawTransactionIsConformsToRPC()
		{
			var rpc = CreateRPCClient();
			var tx = Network.TestNet.GetGenesis().Transactions[0];

			var tx2 = rpc.DecodeRawTransaction(tx.ToBytes());
			AssertJsonEquals(tx.ToString(RawFormat.Satoshi), tx2.ToString(RawFormat.Satoshi));
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

		/// <summary>
		/// "bitcoin-qt.exe" -testnet -server -rpcuser=NBitcoin -rpcpassword=NBitcoinPassword 
		/// </summary>
		/// <returns></returns>
		public static RPCClient CreateRPCClient()
		{
#if !NOSOCKET
			var process = NBitcoin.Watcher.BitcoinQProcess.List()
				.FirstOrDefault(p => p.Server && p.Testnet);
			if(process == null)
				throw new InvalidOperationException("No bitcoin-qt or bitcoinq process running with rpc server on test net (\"bitcoin-qt.exe\" -testnet -server -rpcuser=NBitcoin -rpcpassword=NBitcoinPassword )");
			return process.CreateClient();
#else
			return new RPCClient(new NetworkCredential("NBitcoin", "NBitcoinPassword"), "127.0.0.1", Network.TestNet);
#endif
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
