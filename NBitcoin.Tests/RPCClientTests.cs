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
		[Trait("UnitTest", "UnitTest")]
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
			var tx = Network.TestNet.GetGenesis().Vtx[0];

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
			var client = new RPCClient(new NetworkCredential("NBitcoin", "NBitcoinPassword"), "127.0.0.1", Network.TestNet);
			return client;
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
