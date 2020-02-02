using NBitcoin.Altcoins.Elements;
using NBitcoin.RPC;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin.JsonConverters;
using Newtonsoft.Json;
using Xunit;
using Encoders = NBitcoin.DataEncoders.Encoders;

namespace NBitcoin.Tests
{
	[Trait("Altcoins", "Altcoins")]
	public class AltcoinTests
	{
		[Fact]
		public void NoCrashQuickTest()
		{
			HashSet<string> coins = new HashSet<string>();
			foreach (var network in NBitcoin.Altcoins.AltNetworkSets.GetAll().ToList())
			{
				if (network == Altcoins.AltNetworkSets.Liquid) // No testnet
					continue;
				Assert.True(coins.Add(network.CryptoCode.ToLowerInvariant()));
				Assert.NotEqual(network.Mainnet, network.Regtest);
				Assert.NotEqual(network.Regtest, network.Testnet);
				Assert.Equal(network.Regtest.NetworkSet, network.Testnet.NetworkSet);
				Assert.Equal(network.Mainnet.NetworkSet, network.Testnet.NetworkSet);
				Assert.Equal(network, network.Testnet.NetworkSet);
				Assert.Equal(NetworkType.Mainnet, network.Mainnet.NetworkType);
				Assert.Equal(NetworkType.Testnet, network.Testnet.NetworkType);
				Assert.Equal(NetworkType.Regtest, network.Regtest.NetworkType);
				Assert.Equal(network.CryptoCode, network.CryptoCode.ToUpperInvariant());
				Assert.Equal(network.Mainnet, Network.GetNetwork(network.CryptoCode.ToLowerInvariant() + "-mainnet"));
				Assert.Equal(network.Testnet, Network.GetNetwork(network.CryptoCode.ToLowerInvariant() + "-testnet"));
				Assert.Equal(network.Regtest, Network.GetNetwork(network.CryptoCode.ToLowerInvariant() + "-regtest"));

				foreach (var n in new[] { network.Mainnet, network.Testnet, network.Regtest })
				{
					n.Parse(new Key().PubKey.GetAddress(ScriptPubKeyType.Legacy, n).ToString());
				}
			}
		}


		[Fact]
		public async Task CanCalculateTransactionHash()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				var blockHash = (await rpc.GenerateAsync(10))[0];
				var block = rpc.GetBlock(blockHash);

				Transaction walletTx = null;
				try
				{
					walletTx = rpc.GetRawTransaction(block.Transactions[0].GetHash(), block.GetHash());
				}
				// Some nodes does not support the blockid
				catch
				{
					walletTx = rpc.GetRawTransaction(block.Transactions[0].GetHash());
				}
				Assert.Equal(walletTx.ToHex(), block.Transactions[0].ToHex());
			}
		}

		[Fact]
		public void HasCorrectGenesisBlock()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				var genesis = rpc.GetBlock(0);
				if (IsElements(builder.Network))
				{
					Assert.Contains(genesis.Transactions.SelectMany(t => t.Outputs).OfType<ElementsTxOut>(), o => o.IsPeggedAsset == true && o.ConfidentialValue.Amount != null && o.ConfidentialValue.Amount != Money.Zero);
				}
				var actual = genesis.GetHash();
				var calculatedGenesis = builder.Network.GetGenesis().GetHash();
				Assert.Equal(calculatedGenesis, actual);
				Assert.Equal(rpc.GetBlockHash(0), calculatedGenesis);
			}
		}

		[Fact]
		public async Task CanParseBlock()
		{
			using (var builder = NodeBuilderEx.Create())
			{

				var node = builder.CreateNode();
				builder.StartAll();
				var rpc = node.CreateRPCClient();
				await rpc.GenerateAsync(10);
				var hash = await rpc.GetBestBlockHashAsync();
				var b = await rpc.GetBlockAsync(hash);
				Assert.NotNull(b);
				Assert.Equal(hash, b.GetHash());

				new ConcurrentChain(builder.Network);
			}
		}
		[Fact]
		public void ElementsAddressSerializationTest()
		{

			var network = Altcoins.Liquid.Instance.Regtest;
			var address =
				"el1qqvx2mprx8re8pd7xjeg9tu8w3jllhcty05l0hlyvlsaj0rce90nk97ze47dv3sy356nuxhjlpms73ztf8lalkerz9ndvg0rva";
			var  bitcoinBlindedAddress=new BitcoinBlindedAddress(address, network);
			var seria = new JsonSerializerSettings();
			Serializer.RegisterFrontConverters(seria, network);
			var serializer = JsonSerializer.Create(seria);
			using (var textWriter = new StringWriter())
			{
				 serializer.Serialize(textWriter, bitcoinBlindedAddress);

				 Assert.Equal(address,textWriter.ToString().Trim('"'));

				 using (var textReader = new JsonTextReader(new StringReader(textWriter.ToString())))
				 {

					 Assert.Equal(bitcoinBlindedAddress, serializer.Deserialize<BitcoinAddress>(textReader));
					 Assert.Equal(bitcoinBlindedAddress, serializer.Deserialize<BitcoinBlindedAddress>(textReader));
					 Assert.Throws<JsonObjectException>(() =>
					 {
						 Assert.Equal(bitcoinBlindedAddress, serializer.Deserialize<IDestination>(textReader));
					 });
					 Assert.Throws<ArgumentNullException>(() =>
					 {
						 Assert.Equal(bitcoinBlindedAddress, serializer.Deserialize<IBitcoinString>(textReader));
					 });
				 }
			}

		}

		[Fact]
		public void ElementsAddressTests()
		{

			var network = Altcoins.Liquid.Instance.Mainnet;
			//p2sh-segwit blidned addresses mainnet
			var key = Key.Parse("L22adb3BwuUxLoE8jDhNS7y9e92AYaHpXH5HSXZtFUKdJddEuFgm",network );
			var blindingKey =
				new Key(Encoders.Hex.DecodeData("bb1cbb24decbf8510c0db6ced89a0fca20382ef0aba1fcffc0f70b4310320892"));
			var pubBlindingKey = new PubKey("0287bad01b847963609da945cd5a08a1937649f7adbfdba5dc7e9ff46a44e54bed");
			Assert.Equal(blindingKey.PubKey, pubBlindingKey);
			var p2sh = key.PubKey.GetAddress(ScriptPubKeyType.SegwitP2SH, network);
			Assert.Equal("GtqMkbR82hDis4EPKAaBNSKHY2ZR3ue4Ef", p2sh.ToString());
			var blinded = new BitcoinBlindedAddress("VJL9DzChzwuw7Amnb1SL7M5WEq4TXmzZeAWzNFM5ULcr84gUEpu46Hbs1hZoYJXVkaqM5E3YxAyHy18N", network);
			Assert.Equal(blinded.ToString(), new BitcoinBlindedAddress(pubBlindingKey, p2sh ).ToString());

			//legacy blinded addresses mainnet
			key = Key.Parse("L2EApGmxCemfhVHRmgXa1TWRoEaoiKfJHfvfmcPpRzF2v6neHqZv",network );
			var legacy = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network);
			Assert.Equal("QCAGkwismL6CZ8LR1Bvbzx1z3S7dfNsPwv", legacy.ToString());
			blinded = new BitcoinBlindedAddress("VTpxFwLujc7Z8ufVaVxz1JJq7wVcmq6dxc4dDVBa9jK1zyHfuTUXQpZAhG4JJjv2DYGTKRW5r39RuHXF", network);
			Assert.Equal(blinded.ToString(), new BitcoinBlindedAddress(new PubKey("029c293fbb855b709d7af1b696f26b16de06de6746616ebee32aee07be9aadc5f0"), legacy ).ToString());


			//segwit blinded addresses mainnet
			key = Key.Parse("KxYLpF8yCrphfji3AFzDFurjFfZum9wFhqzpQVeGAFRi4Gtewu6z",network );
			var segwit = key.PubKey.GetAddress(ScriptPubKeyType.Segwit, network);
			Assert.Equal("ex1qrqm9fah7lu9vf6t8v8tsjg0ul9gdtd89gwqcfz", segwit.ToString());
			blinded = new BitcoinBlindedAddress("lq1qqds20c9qasz0y9fup3wc8xca6ceeyf7e4w6wd9l4qd4vwh887l76gxpk2nm0alc2cn5kwcwhpysle72s6k6w2uhhtgwsltf66", network);
			var computed =
				new BitcoinBlindedAddress(
					new PubKey("0360a7e0a0ec04f2153c0c5d839b1dd6339227d9abb4e697f5036ac75ce7f7fda4"), segwit);

			Assert.Equal(blinded.ToString(), computed.ToString());
		}

		[Fact]
		public void CanSignTransactions()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				builder.StartAll();
				node.Generate(builder.Network.Consensus.CoinbaseMaturity + 1);
				var rpc = node.CreateRPCClient();

				var alice = new Key().GetBitcoinSecret(builder.Network);
				BitcoinAddress aliceAddress = alice.GetAddress(ScriptPubKeyType.Legacy);
				var txid = rpc.SendToAddress(aliceAddress, Money.Coins(1.0m));
				var tx = rpc.GetRawTransaction(txid);
				var coin = tx.Outputs.AsCoins().First(c => c.ScriptPubKey == aliceAddress.ScriptPubKey);

				// Check the hash calculated correctly
				Assert.Equal(txid, tx.GetHash());
				TransactionBuilder txbuilder = builder.Network.CreateTransactionBuilder();
				txbuilder.AddCoins(coin);
				txbuilder.AddKeys(alice);
				txbuilder.Send(new Key().ScriptPubKey, Money.Coins(0.4m));
				txbuilder.SendFees(Money.Coins(0.001m));
				txbuilder.SetChange(aliceAddress);
				var signed = txbuilder.BuildTransaction(false);
				txbuilder.SignTransactionInPlace(signed);
				txbuilder.Verify(signed, out var err);
				Assert.True(txbuilder.Verify(signed));
				rpc.SendRawTransaction(signed);

				// Let's try P2SH with 2 coins
				aliceAddress = alice.PubKey.ScriptPubKey.GetScriptAddress(builder.Network);
				txid = rpc.SendToAddress(aliceAddress, Money.Coins(1.0m));
				tx = rpc.GetRawTransaction(txid);
				coin = tx.Outputs.AsCoins().First(c => c.ScriptPubKey == aliceAddress.ScriptPubKey);

				txid = rpc.SendToAddress(aliceAddress, Money.Coins(1.0m));
				tx = rpc.GetRawTransaction(txid);
				var coin2 = tx.Outputs.AsCoins().First(c => c.ScriptPubKey == aliceAddress.ScriptPubKey);

				txbuilder = builder.Network.CreateTransactionBuilder()
								.AddCoins(new[] { coin.ToScriptCoin(alice.PubKey.ScriptPubKey), coin2.ToScriptCoin(alice.PubKey.ScriptPubKey) })
								.AddKeys(alice)
								.SendAll(new Key().ScriptPubKey)
								.SendFees(Money.Coins(0.00001m))
								.SubtractFees()
								.SetChange(aliceAddress);

				signed = txbuilder.BuildTransaction(false);
				txbuilder.SignTransactionInPlace(signed);
				txbuilder.Verify(signed, out err);
				Assert.True(txbuilder.Verify(signed));
				rpc.SendRawTransaction(signed);
			}
		}

		[Fact]
		public void CanParseAddress()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				builder.StartAll();
				var addr = node.CreateRPCClient().SendCommand(RPC.RPCOperations.getnewaddress).Result.ToString();
				var addr2 = BitcoinAddress.Create(addr, builder.Network).ToString();
				Assert.Equal(addr, addr2);

				var address = (BitcoinAddress)new Key().PubKey.GetAddress(ScriptPubKeyType.Legacy, builder.Network);

				// Test normal address
				var isValid = ((JObject)node.CreateRPCClient().SendCommand("validateaddress", address.ToString()).Result)["isvalid"].Value<bool>();
				Assert.True(isValid);

				// Test p2sh
				address = new Key().PubKey.ScriptPubKey.Hash.ScriptPubKey.GetDestinationAddress(builder.Network);
				isValid = ((JObject)node.CreateRPCClient().SendCommand("validateaddress", address.ToString()).Result)["isvalid"].Value<bool>();
				Assert.True(isValid);
			}
		}

		/// <summary>
		/// This test check if we can scan RPC capabilities
		/// </summary>
		[Fact]
		public void DoesRPCCapabilitiesWellAdvertised()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				builder.StartAll();
				var rpc = node.CreateRPCClient();
				rpc.ScanRPCCapabilities();
				Assert.NotNull(rpc.Capabilities);

				CheckCapabilities(rpc, "getnetworkinfo", rpc.Capabilities.SupportGetNetworkInfo);
				CheckCapabilities(rpc, "scantxoutset", rpc.Capabilities.SupportScanUTXOSet);
				CheckCapabilities(rpc, "signrawtransactionwithkey", rpc.Capabilities.SupportSignRawTransactionWith);
				CheckCapabilities(rpc, "estimatesmartfee", rpc.Capabilities.SupportEstimateSmartFee);

				try
				{
					var address = rpc.GetNewAddress(new GetNewAddressRequest()
					{
						AddressType = AddressType.Bech32
					});
					// If this fail, rpc support segwit but you said it does not
					Assert.Equal(rpc.Capabilities.SupportSegwit, address.ScriptPubKey.IsScriptType(ScriptType.Witness));
					if (rpc.Capabilities.SupportSegwit)
					{
						Assert.True(builder.Network.Consensus.SupportSegwit, "The node RPC support segwit, but Network.Consensus.SupportSegwit is set to false");
						rpc.SendToAddress(address, Money.Coins(1.0m));
					}
					else
					{
						Assert.False(builder.Network.Consensus.SupportSegwit, "The node RPC does not support segwit, but Network.Consensus.SupportSegwit is set to true (This error can be normal if you are using a old node version)");
					}
				}
				catch (RPCException) when (!rpc.Capabilities.SupportSegwit)
				{
				}
			}
		}
		private void CheckCapabilities(Action command, bool supported)
		{
			if (!supported)
			{
				var ex = Assert.Throws<RPCException>(command);
				Assert.True(ex.RPCCode == RPCErrorCode.RPC_METHOD_NOT_FOUND || ex.RPCCode == RPCErrorCode.RPC_METHOD_DEPRECATED);
			}
			else
			{
				try
				{
					command();
				}
				catch (RPCException ex) when (ex.RPCCode != RPCErrorCode.RPC_METHOD_NOT_FOUND && ex.RPCCode != RPCErrorCode.RPC_METHOD_DEPRECATED)
				{
					// Method exists
				}
			}
		}
		private void CheckCapabilities(RPCClient rpc, string command, bool supported)
		{
			CheckCapabilities(() => rpc.SendCommand(command, "random"), supported);
		}

		[Fact]
		public void CanSyncWithPoW()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				if (IsElements(builder.Network))
				{
					//no pow in liquid
					return;
				}
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
		public async Task CorrectCoinMaturity()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				builder.StartAll();
				node.Generate(builder.Network.Consensus.CoinbaseMaturity);
				var rpc = node.CreateRPCClient();
				if (IsElements(node.Network))
				{
					Assert.Contains((await rpc.GetBalancesAsync()),
						pair => pair.Value == Money.FromUnit(2100000, MoneyUnit.BTC));
					node.Generate(1);
					Assert.Contains((await rpc.GetBalancesAsync()),
						pair => pair.Value == Money.FromUnit(2100000, MoneyUnit.BTC));
				}
				else
				{
					Assert.Equal(Money.Zero, await rpc.GetBalanceAsync());
					node.Generate(1);
					Assert.NotEqual(Money.Zero,await rpc.GetBalanceAsync());
				}
			}
		}

		[Fact]
		public void CanSyncWithoutPoW()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				builder.StartAll();
				node.Generate(100);
				var nodeClient = node.CreateNodeClient();
				nodeClient.VersionHandshake();
				ConcurrentChain chain = new ConcurrentChain(builder.Network);
				nodeClient.SynchronizeChain(chain, new Protocol.SynchronizeChainOptions() { SkipPoWCheck = true });
				Assert.Equal(node.CreateRPCClient().GetBestBlockHash(), chain.Tip.HashBlock);
				Assert.Equal(100, chain.Height);

				// If it fails, override Block.GetConsensusFactory()
				var b = node.CreateRPCClient().GetBlock(50);
				Assert.Equal(b.WithOptions(TransactionOptions.Witness).Header.GetType(), chain.GetBlock(50).Header.GetType());

				var b2 = nodeClient.GetBlocks(new Protocol.SynchronizeChainOptions() { SkipPoWCheck = true }).ToArray()[50];
				Assert.Equal(b2.Header.GetType(), chain.GetBlock(50).Header.GetType());
			}
		}

		private bool IsElements(Network nodeNetwork)
		{
			return nodeNetwork.NetworkSet == Altcoins.Liquid.Instance;
		}
	}
}
