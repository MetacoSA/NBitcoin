using NBitcoin.Altcoins.Elements;
using NBitcoin.RPC;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin.Altcoins;
using NBitcoin.JsonConverters;
using Newtonsoft.Json;
using Xunit;
using Encoders = NBitcoin.DataEncoders.Encoders;
using System.Threading;

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
				Assert.Equal(ChainName.Mainnet, network.Mainnet.ChainName);
				Assert.Equal(ChainName.Testnet, network.Testnet.ChainName);
				Assert.Equal(ChainName.Regtest, network.Regtest.ChainName);
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
		[Trait("UnitTest", "UnitTest")]
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
					 Assert.Throws<JsonObjectException>(() =>
					 {
						 Assert.Equal(bitcoinBlindedAddress, serializer.Deserialize<IBitcoinString>(textReader));
					 });
				 }
			}

		}

		[Fact]
		public void Slip21Tests()
		{
			var allMnemonic = new Mnemonic("all all all all all all all all all all all all");
			var master = Slip21Node.FromSeed(allMnemonic.DeriveSeed());
			Assert.Equal("dbf12b44133eaab506a740f6565cc117228cbf1dd70635cfa8ddfdc9af734756", master.Key.ToHex());

			var child1 = master.DeriveChild("SLIP-0021");
			Assert.Equal("1d065e3ac1bbe5c7fad32cf2305f7d709dc070d672044a19e610c77cdf33de0d", child1.Key.ToHex());

			var child2 = child1.DeriveChild("Master encryption key");
			Assert.Equal("ea163130e35bbafdf5ddee97a17b39cef2be4b4f390180d65b54cf05c6a82fde", child2.Key.ToHex());

			var child3 = child1.DeriveChild("Authentication key");
			Assert.Equal("47194e938ab24cc82bfa25f6486ed54bebe79c40ae2a5a32ea6db294d81861a6", child3.Key.ToHex());
		}

		[Fact]
		public void Slip77Tests()
		{
			//test vector: https://github.com/vulpemventures/slip77/commit/bc0fa0c712512d27cf1e2d6e1aaee5c36a3d38fa
			var allMnemonic = new Mnemonic("all all all all all all all all all all all all");
			var master = Slip21Node.FromSeed(allMnemonic.DeriveSeed());
			Assert.Equal("dbf12b44133eaab506a740f6565cc117228cbf1dd70635cfa8ddfdc9af734756", master.Key.ToHex());
			var slip77 = Slip21Node.FromSeed(allMnemonic.DeriveSeed()).GetSlip77Node();
			var script = Script.FromHex("76a914a579388225827d9f2fe9014add644487808c695d88ac");
			var privateBlindingKey = slip77.DeriveSlip77BlindingKey(script);
			var unconfidentialAddress =
				BitcoinAddress.Create("2dpWh6jbhAowNsQ5agtFzi7j6nKscj6UnEr", Altcoins.Liquid.Instance.Regtest);
			var publicBlindingKey = privateBlindingKey.PubKey;
			Assert.Equal("4e6e94df28448c7bb159271fe546da464ea863b3887d2eec6afd841184b70592",
				privateBlindingKey.ToHex());
			Assert.Equal("0223ef5cf5d1185f86204b9386c8541061a24b6f72fa4a29e3a0b60e1c20ffaf5b",
				publicBlindingKey.ToHex());
			Assert.Equal("CTEkf75DFff5ReB7juTg2oehrj41aMj21kvvJaQdWsEAQohz1EDhu7Ayh6goxpz3GZRVKidTtaXaXYEJ",
				new BitcoinBlindedAddress(publicBlindingKey, unconfidentialAddress).ToString());


			//test vector: https://github.com/trezor/trezor-firmware/pull/398/files#diff-afc9a622fb2281269983493a9da47a364e94f8bd338e5322f4a7cef99f98ee69R119
			var abusiveWords =
				new Mnemonic("alcohol woman abuse must during monitor noble actual mixed trade anger aisle");
			var derived = abusiveWords.DeriveExtKey().Derive(new KeyPath("44'/1'/0'/0/0"));
			var addr = derived.PrivateKey.GetAddress(ScriptPubKeyType.Legacy, Liquid.Instance.Regtest);
			var abusiveslip77 = Slip21Node.FromSeed(abusiveWords.DeriveSeed()).GetSlip77Node();
			Assert.Equal("26f1dc2c52222394236d76e0809516255cfcca94069fd5187c0f090d18f42ad6",
				abusiveslip77.DeriveSlip77BlindingKey(addr.ScriptPubKey).ToHex());
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
				txbuilder.Send(new Key(), Money.Coins(0.4m));
				txbuilder.SendFees(Money.Coins(0.001m));
				txbuilder.SetChange(aliceAddress);
				var signed = txbuilder.BuildTransaction(false);
				txbuilder.SignTransactionInPlace(signed);
				txbuilder.Verify(signed, out var err);
				Assert.True(txbuilder.Verify(signed));
				rpc.SendRawTransaction(signed);

				// Let's try P2SH with 2 coins
				aliceAddress = alice.PubKey.ScriptPubKey.Hash.GetAddress(builder.Network);
				txid = rpc.SendToAddress(aliceAddress, Money.Coins(1.0m));
				tx = rpc.GetRawTransaction(txid);
				coin = tx.Outputs.AsCoins().First(c => c.ScriptPubKey == aliceAddress.ScriptPubKey);

				txid = rpc.SendToAddress(aliceAddress, Money.Coins(1.0m));
				tx = rpc.GetRawTransaction(txid);
				var coin2 = tx.Outputs.AsCoins().First(c => c.ScriptPubKey == aliceAddress.ScriptPubKey);

				txbuilder = builder.Network.CreateTransactionBuilder()
								.AddCoins(new[] { coin.ToScriptCoin(alice.PubKey.ScriptPubKey), coin2.ToScriptCoin(alice.PubKey.ScriptPubKey) })
								.AddKeys(alice)
								.SendAll(new Key())
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

				var address = new Key().PubKey.GetAddress(ScriptPubKeyType.Legacy, builder.Network);

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
				node.Generate(node.Network.Consensus.CoinbaseMaturity + 1);
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
					if (rpc.Capabilities.SupportTaproot)
					{
						Assert.True(builder.Network.Consensus.SupportTaproot, "The node RPC support segwit, but Network.Consensus.SupportSegwit is set to false");
						rpc.SendToAddress(new Key().GetAddress(ScriptPubKeyType.TaprootBIP86, rpc.Network), Money.Coins(1.0m));
					}
					else
					{
						Assert.False(builder.Network.Consensus.SupportTaproot, "The node RPC does not support segwit, but Network.Consensus.SupportSegwit is set to true (This error can be normal if you are using a old node version)");
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
		public async Task CanSignAltcoinTransaction()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				builder.StartAll();
				var rpc = node.CreateRPCClient();
				rpc.Generate(builder.Network.Consensus.CoinbaseMaturity + 1);
				var key = new Key();
				var addr = key.GetAddress(ScriptPubKeyType.Legacy, builder.Network);
				var txid = await rpc.SendToAddressAsync(addr, Money.Coins(1.0m));
				var tx = await rpc.GetRawTransactionAsync(txid);
				var dest = await rpc.GetNewAddressAsync();
				var txbuilder = builder.Network.CreateTransactionBuilder();
				txbuilder.AddCoins(tx.Outputs.AsCoins());
				txbuilder.AddKeys(key);
				txbuilder.Send(dest, Money.Coins(1.0m));
				txbuilder.SendFees(Money.Coins(0.00004m));
				txbuilder.SubtractFees();
				var signed = txbuilder.BuildTransaction(true);
				Assert.True(txbuilder.Verify(signed));
				await rpc.SendRawTransactionAsync(signed);
			}
		}

		[Fact]
		public void CheckForkIdIsUsedDuringSigning()
		{
			var n = Altcoins.AltNetworkSets.BCash.Regtest;
			var key = new Key(Encoders.Hex.DecodeData("1718bce503a08a80ab698ad6e9b5211ed7a232958532877dd1cc67213fa5c4d9"));
			var dest = BitcoinAddress.Create("bchreg:qz8d85evkscf7qx3y7pycllwwpyjy8dj7uwryky89l", n);
			var tx = Transaction.Parse("0200000001766dc30996037844f838a4fc2f214e1fc13c3d65d46221e4d915c8b9b1c002bc0000000048473044022076c1ff7362f4cb50d4a36fb9a72db9ac8c8de4f7de1d9145e8bf8fb667578f4b022051a774e17cd8a0e3d3862985bc860c8fd49202b424d78a3989fae2a3848ba06441feffffff0241101024010000001976a9141686731726f06127a4e0d33a90d7912055582eb188ac00e1f505000000001976a9147bf316ee14ba66ba07bbcaa9ad5d94515acf35fc88ac65000000", n);

			var txbuilder = n.CreateTransactionBuilder();
			txbuilder.AddCoins(tx.Outputs.AsCoins());
			txbuilder.AddKeys(key);
			txbuilder.Send(dest, Money.Coins(1.0m));
			txbuilder.SendFees(Money.Coins(0.00004m));
			txbuilder.SubtractFees();
			var signed = txbuilder.BuildTransaction(true);
			var expected = Transaction.Parse("0100000001557fec4d75208907d177542573a31aad7d5e825514c54d5e97ddb159a9621477010000006a473044022053ae899e9927f3f77c9aff605bd88b6b84205c290f8498bdba73ad063107a5e7022006357498990b46475f65045a74eb25645d4dee0835e108b11a87ce77ffd3348341210309046b4af074cfb8138abd6d87003398d497336d2aca5102393aff1f47c6c009ffffffff0160d1f505000000001976a9148ed3d32cb4309f00d127824c7fee7049221db2f788ac00000000", n);
			Assert.True(txbuilder.Verify(signed));
			Assert.Equal(expected.ToString(), signed.ToString());
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
