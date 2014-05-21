using NBitcoin.RPC;
using System;
using System.Collections.Generic;
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
		[Trait("RPCClient","RPCClient")]
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


		/// <summary>
		/// "bitcoin-qt.exe" -testnet -server -rpcuser=NBitcoin -rpcpassword=NBitcoinPassword 
		/// </summary>
		/// <returns></returns>
		private RPCClient CreateRPCClient()
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
