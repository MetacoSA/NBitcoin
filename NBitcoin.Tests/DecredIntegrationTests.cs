using NBitcoin.Altcoins;
using NBitcoin.RPC;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	// These tests require a running Decred simnet harness.
	// Start with: cd ~/git/dcrdex/dex/testing/dcr && ./harness.sh
	// The harness creates alpha/beta nodes and wallets on simnet.
	[Trait("Decred", "Integration")]
	public class DecredIntegrationTests
	{
		// Alpha wallet RPC endpoint from the harness.
		const string RpcUser = "user";
		const string RpcPass = "pass";
		const string WalletRpcHost = "https://127.0.0.1:19562";
		static readonly string TlsCertPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			"dextest", "dcr", "alpha", "rpc.cert");

		private RPCClient CreateClient()
		{
			var network = Decred.Instance.Regtest;
			var creds = new RPCCredentialString { UserPassword = new NetworkCredential(RpcUser, RpcPass) };
			var client = new RPCClient(creds, WalletRpcHost, network);
			client.UseCustomTLSCert(TlsCertPath);
			client.AllowBatchFallback = true;
			return client;
		}

		private static bool? _harnessRunning;
		private static string _harnessError;
		private bool HarnessRunning()
		{
			if (_harnessRunning.HasValue)
				return _harnessRunning.Value;
			_harnessRunning = false;
			if (!File.Exists(TlsCertPath))
			{
				_harnessError = $"TLS cert not found at {TlsCertPath}";
				return false;
			}
			try
			{
				var client = CreateClient();
				client.GetBlockCount();
				_harnessRunning = true;
			}
			catch (Exception ex)
			{
				_harnessError = ex.ToString();
			}
			return _harnessRunning.Value;
		}

		[Fact]
		public void CanConnectToDecredWallet()
		{
			if (!HarnessRunning()) Assert.Fail($"Decred simnet harness not running: {_harnessError}");
			var client = CreateClient();
			var blockCount = client.GetBlockCount();
			Assert.True(blockCount > 0);
		}

		[Fact]
		public async Task CanGetBlockchainInfo()
		{
			if (!HarnessRunning()) Assert.Fail($"Decred simnet harness not running: {_harnessError}");
			var client = CreateClient();
			var info = await client.GetBlockchainInfoAsync();
			Assert.NotNull(info);
			Assert.NotNull(info.Chain);
			Assert.True(info.Blocks > 0);
		}

		[Fact]
		public async Task CanGetBalance()
		{
			if (!HarnessRunning()) Assert.Fail($"Decred simnet harness not running: {_harnessError}");
			var client = CreateClient();
			var balance = await client.GetBalanceAsync();
			Assert.True(balance > Money.Zero);
		}

		[Fact]
		public async Task CanGetBalanceWithMinConf()
		{
			if (!HarnessRunning()) Assert.Fail($"Decred simnet harness not running: {_harnessError}");
			var client = CreateClient();
			var balance = await client.GetBalanceAsync(1, false);
			Assert.True(balance > Money.Zero);
		}

		[Fact]
		public async Task CanGetNewAddress()
		{
			if (!HarnessRunning()) Assert.Fail($"Decred simnet harness not running: {_harnessError}");
			var client = CreateClient();
			var address = await client.GetNewAddressAsync(null);
			Assert.NotNull(address);
			// Simnet addresses start with "Ss"
			Assert.StartsWith("Ss", address.ToString());
		}

		[Fact]
		public async Task CanGetBlock()
		{
			if (!HarnessRunning()) Assert.Fail($"Decred simnet harness not running: {_harnessError}");
			var client = CreateClient();
			var bestHash = (await client.GetBlockchainInfoAsync()).BestBlockHash;
			var block = await client.GetBlockAsync(bestHash);
			Assert.NotNull(block);
			Assert.NotNull(block.Header);
			Assert.IsType<Decred.DecredBlockHeader>(block.Header);
		}

		[Fact]
		public async Task CanGetBlockWithFullTx()
		{
			if (!HarnessRunning()) Assert.Fail($"Decred simnet harness not running: {_harnessError}");
			var client = CreateClient();
			var bestHash = (await client.GetBlockchainInfoAsync()).BestBlockHash;
			var block = await client.GetBlockAsync(bestHash, GetBlockVerbosity.WithFullTx);
			Assert.NotNull(block);
			Assert.NotNull(block.Block);
			Assert.NotEmpty(block.Block.Transactions);
		}

		[Fact]
		public async Task CanSendToAddress()
		{
			if (!HarnessRunning()) Assert.Fail($"Decred simnet harness not running: {_harnessError}");
			var client = CreateClient();
			var address = await client.GetNewAddressAsync(null);
			var txid = await client.SendToAddressAsync(address, Money.Coins(1.0m));
			Assert.NotNull(txid);
		}

		[Fact]
		public async Task CanGetRawTransaction()
		{
			if (!HarnessRunning()) Assert.Fail($"Decred simnet harness not running: {_harnessError}");
			var client = CreateClient();

			// Send a tx first
			var address = await client.GetNewAddressAsync(null);
			var txid = await client.SendToAddressAsync(address, Money.Coins(0.5m));

			// Fetch it back
			var tx = await client.GetRawTransactionAsync(txid);
			Assert.NotNull(tx);
			Assert.IsType<Decred.DecredTransaction>(tx);
		}

		[Fact]
		public async Task CanGetRawTransactionInfo()
		{
			if (!HarnessRunning()) Assert.Fail($"Decred simnet harness not running: {_harnessError}");
			var client = CreateClient();

			var address = await client.GetNewAddressAsync(null);
			var txid = await client.SendToAddressAsync(address, Money.Coins(0.5m));

			var info = await client.GetRawTransactionInfoAsync(txid);
			Assert.NotNull(info);
			Assert.Equal(txid, info.TransactionId);
		}

		[Fact]
		public async Task CanGetPeersInfo()
		{
			if (!HarnessRunning()) Assert.Fail($"Decred simnet harness not running: {_harnessError}");
			var client = CreateClient();
			var peers = await client.GetPeersInfoAsync();
			Assert.NotNull(peers);
			// Alpha is connected to beta
			Assert.NotEmpty(peers);
		}

		[Fact]
		public async Task CanGetTxOutSetInfo()
		{
			if (!HarnessRunning()) Assert.Fail($"Decred simnet harness not running: {_harnessError}");
			var client = CreateClient();
			// Note: GetTxoutSetInfoAsync may throw on simnet due to
			// totalamount overflowing Money (long satoshis) when a
			// large number of blocks have been mined. Test the raw
			// RPC call instead.
			var response = await client.SendCommandAsync("gettxoutsetinfo");
			Assert.NotNull(response.Result);
			Assert.True(response.Result.Value<int>("height") > 0);
			Assert.True(response.Result.Value<long>("txouts") > 0);
		}

		[Fact]
		public async Task BatchFallbackWorks()
		{
			if (!HarnessRunning()) Assert.Fail($"Decred simnet harness not running: {_harnessError}");
			var client = CreateClient();

			// PrepareBatch should work via fallback since Decred
			// doesn't support batch RPC.
			var batch = client.PrepareBatch();
			var countTask = batch.GetBlockCountAsync();
			var balanceTask = batch.GetBalanceAsync();
			await batch.SendBatchAsync();

			Assert.True(await countTask > 0);
			Assert.True(await balanceTask > Money.Zero);
		}

		[Fact]
		public async Task CreateWalletReturnsError()
		{
			if (!HarnessRunning()) Assert.Fail($"Decred simnet harness not running: {_harnessError}");
			var client = CreateClient();

			// createwallet is intercepted by RPCRequestHook and should
			// return a method-not-found error.
			var ex = await Assert.ThrowsAsync<RPCException>(async () =>
			{
				await client.CreateWalletAsync("test");
			});
			Assert.Equal(RPCErrorCode.RPC_METHOD_NOT_FOUND, ex.RPCCode);
		}

		[Fact]
		public async Task CanFundRawTransaction()
		{
			if (!HarnessRunning()) Assert.Fail($"Decred simnet harness not running: {_harnessError}");
			var client = CreateClient();
			var network = Decred.Instance.Regtest;

			// Create a simple raw transaction
			var address = await client.GetNewAddressAsync(null);
			var tx = network.Consensus.ConsensusFactory.CreateTransaction();
			var txOut = network.Consensus.ConsensusFactory.CreateTxOut();
			txOut.Value = Money.Coins(1.0m);
			txOut.ScriptPubKey = address.ScriptPubKey;
			tx.Outputs.Add(txOut);

			var funded = await client.FundRawTransactionAsync(tx);
			Assert.NotNull(funded);
			Assert.NotNull(funded.Transaction);
			Assert.True(funded.Fee > Money.Zero);
		}
	}
}
