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
using NBitcoin.Logging;
using NBitcoin.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using FsCheck.Xunit;
using FsCheck;
using NBitcoin.Tests.Generators;
using static NBitcoin.Tests.Comparer;

namespace NBitcoin.Tests
{
	//Require a rpc server on test network running on default port with -rpcuser=NBitcoin -rpcpassword=NBitcoinPassword
	//For me : 
	//"bitcoin-qt.exe" -testnet -server -rpcuser=NBitcoin -rpcpassword=NBitcoinPassword 
	[Trait("RPCClient", "RPCClient")]
	public class RPCClientTests
	{
		const string TestAccount = "NBitcoin.RPCClientTests";

		public PSBTComparer PSBTComparerInstance { get; }
		public ITestOutputHelper Output { get; }

		public RPCClientTests(ITestOutputHelper output)
		{
			Arb.Register<PSBTGenerator>();
			Arb.Register<SegwitTransactionGenerators>();
			PSBTComparerInstance = new PSBTComparer();
			Output = output;
		}

		[Fact]
		public void InvalidCommandSendRPCException()
		{
			using (var builder = NodeBuilderEx.Create())
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
			using (var builder = NodeBuilderEx.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				var response = rpc.SendCommand(RPCOperations.getblockchaininfo);
				Assert.NotNull(response.Result);
				var copy = RPCCredentialString.Parse(rpc.CredentialString.ToString());
				copy.Server = rpc.Address.AbsoluteUri;
				rpc = new RPCClient(copy, null as string, builder.Network);
				response = rpc.SendCommand(RPCOperations.getblockchaininfo);
				Assert.NotNull(response.Result);
			}
		}

		[Fact]
		public void CanGetNewAddress()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				var address = rpc.GetNewAddress(new GetNewAddressRequest()
				{
					AddressType = AddressType.Bech32
				});
				Assert.IsType<BitcoinWitPubKeyAddress>(address);

				address = rpc.GetNewAddress(new GetNewAddressRequest()
				{
					AddressType = AddressType.P2SHSegwit
				});

				Assert.IsType<BitcoinScriptAddress>(address);

				address = rpc.GetNewAddress(new GetNewAddressRequest()
				{
					AddressType = AddressType.Legacy
				});

				Assert.IsType<BitcoinPubKeyAddress>(address);
			}
		}

		[Fact]
		public void CanUseMultipleWallets()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				node.ConfigParameters.Add("wallet", "w1");
				//node.ConfigParameters.Add("wallet", "w2");
				node.Start();
				var rpc = node.CreateRPCClient();
				var creds = RPCCredentialString.Parse(rpc.CredentialString.ToString());
				creds.Server = rpc.Address.AbsoluteUri;
				creds.WalletName = "w1";
				rpc = new RPCClient(creds, Network.RegTest);
				rpc.SendCommandAsync(RPCOperations.getwalletinfo).GetAwaiter().GetResult().ThrowIfError();
				Assert.NotNull(rpc.GetBalance());
				Assert.NotNull(rpc.GetBestBlockHash());
				var block = rpc.GetBlock(rpc.Generate(1)[0]);

				rpc = rpc.PrepareBatch();
				var b = rpc.GetBalanceAsync();
				var b2 = rpc.GetBestBlockHashAsync();
				var a = rpc.SendCommandAsync(RPCOperations.gettransaction, block.Transactions.First().GetHash().ToString());
				rpc.SendBatch();
				b.GetAwaiter().GetResult();
				b2.GetAwaiter().GetResult();
				a.GetAwaiter().GetResult();
			}
		}

		[Fact]
		public void CanGetGenesisFromRPC()
		{
			using (var builder = NodeBuilderEx.Create())
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
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				var rpc = node.CreateRPCClient();
				builder.StartAll();
				node.Generate(101);
				var txid = rpc.SendToAddress(new Key().PubKey.GetAddress(ScriptPubKeyType.Legacy, rpc.Network), Money.Coins(1.0m), "hello", "world");
				var ids = rpc.GetRawMempool();
				Assert.Single(ids);
				Assert.Equal(txid, ids[0]);
			}
		}

		[Fact]
		public void CanUseRPCAuth()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				var creds = new NetworkCredential("lnd", "afixedpasswordbecauselndsuckswithcookiefile");
				var str = RPCClient.GetRPCAuth(creds);
				node.ConfigParameters.Add("rpcauth", str);
				node.Start();
				var rpc = new RPCClient(creds, node.RPCUri, node.Network);
				rpc.GetBlockCount();
			}
		}

		[Fact]
		public void CanGetMemPool()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				var rpc = node.CreateRPCClient();
				builder.StartAll();
				node.Generate(101);

				var txid = rpc.SendToAddress(new Key().PubKey.GetAddress(ScriptPubKeyType.Legacy, rpc.Network), Money.Coins(1.0m), "hello", "world");
				var memPoolInfo = rpc.GetMemPool();
				Assert.NotNull(memPoolInfo);
				Assert.Equal(1, memPoolInfo.Size);
			}
		}

		[Fact]
		public void CanUseAsyncRPC()
		{
			using (var builder = NodeBuilderEx.Create())
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
		public void CanSignWithKey()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				var rpc = node.CreateRPCClient();
				builder.StartAll();
				node.Generate(101);
				var key = new Key();
				var dest = key.PubKey.Hash.GetAddress(builder.Network);
				var txid = rpc.SendToAddress(dest, Money.Coins(1.0m));
				var funding = rpc.GetRawTransaction(txid);
				var coin = funding.Outputs.AsCoins().Single(o => o.ScriptPubKey == dest.ScriptPubKey);


				var spent = Transaction.Create(builder.Network);
				spent.Inputs.Add(new TxIn(coin.Outpoint));
				spent.Outputs.Add(new TxOut(Money.Coins(1.0m), new Key().PubKey.Hash.ScriptPubKey));

				var response = rpc.SignRawTransactionWithKey(new SignRawTransactionWithKeyRequest()
				{
					Transaction = spent
				});

				Assert.False(response.Complete);
				Assert.Single(response.Errors);

				response = rpc.SignRawTransactionWithKey(new SignRawTransactionWithKeyRequest()
				{
					Transaction = spent,
					PrivateKeys = new[] { key }
				});

				Assert.True(response.Complete);
				Assert.Empty(response.Errors);
			}
		}

		[Fact]
		public void CanScanTxoutSet()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				var rpc = node.CreateRPCClient();
				builder.StartAll();
				node.Generate(101);


				var key = new Key();
				var dest = key.PubKey.Hash.GetAddress(builder.Network);
				var txid = rpc.SendToAddress(dest, Money.Coins(1.0m));
				var funding = rpc.GetRawTransaction(txid);
				var coin = funding.Outputs.AsCoins().Single(o => o.ScriptPubKey == dest.ScriptPubKey);

				var result = rpc.StartScanTxoutSet(new ScanTxoutSetObject(ScanTxoutDescriptor.Addr(dest)));

				Assert.Equal(101, result.SearchedItems);
				Assert.True(result.Success);
				Assert.Empty(result.Outputs);
				Assert.Equal(Money.Zero, result.TotalAmount);

				Assert.False(rpc.AbortScanTxoutSet());
				Assert.Null(rpc.GetStatusScanTxoutSet());

				rpc.Generate(1);

				result = rpc.StartScanTxoutSet(new ScanTxoutSetObject(ScanTxoutDescriptor.Addr(dest)));

				Assert.True(result.SearchedItems > 100);
				Assert.True(result.Success);
				Assert.Single(result.Outputs);
				Assert.Equal(102, result.Outputs[0].Height);
				Assert.Equal(Money.Coins(1.0m), result.TotalAmount);

				Assert.False(rpc.AbortScanTxoutSet());
				Assert.Null(rpc.GetStatusScanTxoutSet());
			}
		}

		[Fact]
		public void CanSignWithWallet()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				var rpc = node.CreateRPCClient();
				builder.StartAll();
				node.Generate(101);
				var key = new Key();
				var dest = key.PubKey.Hash.GetAddress(builder.Network);
				var txid = rpc.SendToAddress(dest, Money.Coins(1.0m));
				var funding = rpc.GetRawTransaction(txid);
				var coin = funding.Outputs.AsCoins().Single(o => o.ScriptPubKey == dest.ScriptPubKey);


				var spent = Transaction.Create(builder.Network);
				spent.Inputs.Add(new TxIn(coin.Outpoint));
				spent.Outputs.Add(new TxOut(Money.Coins(1.0m), new Key().PubKey.Hash.ScriptPubKey));

				var response = rpc.SignRawTransactionWithWallet(new SignRawTransactionRequest()
				{
					Transaction = spent
				});

				Assert.False(response.Complete);
				Assert.Single(response.Errors);

				rpc.ImportPrivKey(key.GetBitcoinSecret(builder.Network), "*", false);
				response = rpc.SignRawTransactionWithWallet(new SignRawTransactionRequest()
				{
					Transaction = spent
				});

				Assert.True(response.Complete);
				Assert.Empty(response.Errors);
			}
		}

		[Fact]
		public void CanRBFTransaction()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				var rpc = node.CreateRPCClient();
				builder.StartAll();
				node.Generate(101);

				var key = new Key();
				var address = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, rpc.Network);

				var txid = rpc.SendToAddress(address, Money.Coins(2), null, null, false, true);
				var txbumpid = rpc.BumpFee(txid);
				var blocks = rpc.Generate(1);

				var block = rpc.GetBlock(blocks.First());
				Assert.DoesNotContain(block.Transactions, x => x.GetHash() == txid);
				Assert.Contains(block.Transactions, x => x.GetHash() == txbumpid.TransactionId);
			}
		}


		[Fact]
		public async Task CanGetBlockchainInfo()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				var response = await rpc.GetBlockchainInfoAsync();

				Assert.Equal(builder.Network, response.Chain);
				Assert.Equal(builder.Network.GetGenesis().GetHash(), response.BestBlockHash);
				Assert.Contains(response.Bip9SoftForks, x => x.Name == "segwit");
				Assert.Contains(response.Bip9SoftForks, x => x.Name == "csv");
				Assert.Contains(response.SoftForks, x => x.Bip == "bip34");
				Assert.Contains(response.SoftForks, x => x.Bip == "bip65");
				Assert.Contains(response.SoftForks, x => x.Bip == "bip66");
			}
		}

		[Fact]
		public void CanGetTransactionInfo()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				var rpc = node.CreateRPCClient();
				builder.StartAll();

				var blocks = node.Generate(101);
				var secondBlockHash = blocks.First();
				var secondBlock = rpc.GetBlock(secondBlockHash);
				var firstTx = secondBlock.Transactions.First();

				var txInfo = rpc.GetRawTransactionInfo(firstTx.GetHash());

				Assert.Equal(101U, txInfo.Confirmations);
				Assert.Equal(secondBlockHash, txInfo.BlockHash);
				Assert.Equal(firstTx.GetHash(), txInfo.TransactionId);
				Assert.Equal(secondBlock.Header.BlockTime, txInfo.BlockTime);
				Assert.Equal(firstTx.Version, txInfo.Version);
				Assert.Equal(firstTx.LockTime, txInfo.LockTime);
				Assert.Equal(firstTx.GetWitHash(), txInfo.Hash);
				Assert.Equal((uint)firstTx.GetSerializedSize(), txInfo.Size);
				Assert.Equal((uint)firstTx.GetVirtualSize(), txInfo.VirtualSize);

				// unconfirmed tx doesn't have blockhash, blocktime nor transactiontime.
				var mempoolTxId = rpc.SendToAddress(new Key().PubKey.GetAddress(ScriptPubKeyType.Legacy, builder.Network), Money.Coins(1));
				txInfo = rpc.GetRawTransactionInfo(mempoolTxId);
				Assert.Null(txInfo.TransactionTime);
				Assert.Null(txInfo.BlockHash);
				Assert.Null(txInfo.BlockTime);
				Assert.Equal(0U, txInfo.Confirmations);
			}
		}

		[Fact]
		public void CanGetBlockFromRPC()
		{
			using (var builder = NodeBuilderEx.Create())
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
		public async Task CanGetTxoutSetInfoAsync()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				var response = rpc.GetTxoutSetInfo();

				Assert.Equal("0f9188f13cb7b2c71f2a335e3a4fc328bf5beb436012afca590b1a11466e2206", response.Bestblock.ToString());
				Assert.Equal("b32ec1dda5a53cd025b95387aad344a801825fe46a60ff952ce26528f01d3be8", response.HashSerialized2);

				const int BLOCKS_TO_GENERATE = 10;
				uint256[] blockHashes = await rpc.GenerateAsync(BLOCKS_TO_GENERATE);

				response = rpc.GetTxoutSetInfo();
				Assert.Equal(BLOCKS_TO_GENERATE, response.Height);
				Assert.Equal(BLOCKS_TO_GENERATE, response.Transactions);
				Assert.Equal(BLOCKS_TO_GENERATE, response.Txouts);
				Assert.Equal(BLOCKS_TO_GENERATE * 50M, response.TotalAmount.ToDecimal(MoneyUnit.BTC));
			}
		}

		[Fact]
		public async Task CanGetTxOutFromRPCAsync()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();

				// 1. Generate some blocks and check if gettxout gives the right outputs for the first coin
				var blocksToGenerate = 101;
				uint256[] blockHashes = await rpc.GenerateAsync(blocksToGenerate);
				var txId = rpc.GetTransactions(blockHashes.First()).First().GetHash();
				GetTxOutResponse getTxOutResponse = await rpc.GetTxOutAsync(txId, 0);
				Assert.NotNull(getTxOutResponse); // null if spent
				Assert.Equal(blockHashes.Last(), getTxOutResponse.BestBlock);
				Assert.Equal(getTxOutResponse.Confirmations, blocksToGenerate);
				Assert.Equal(Money.Coins(50), getTxOutResponse.TxOut.Value);
				Assert.NotNull(getTxOutResponse.TxOut.ScriptPubKey);
				Assert.True(getTxOutResponse.ScriptPubKeyType == "pubkey" || getTxOutResponse.ScriptPubKeyType == "scripthash");
				Assert.True(getTxOutResponse.IsCoinBase);

				// 2. Spend the first coin
				var address = new Key().PubKey.GetAddress(ScriptPubKeyType.Legacy, rpc.Network);
				Money sendAmount = Money.Parse("49");
				txId = await rpc.SendToAddressAsync(address, sendAmount);

				// 3. Make sure if we don't include the mempool into the database the txo will not be considered utxo
				getTxOutResponse = await rpc.GetTxOutAsync(txId, 0, false);
				Assert.Null(getTxOutResponse);

				// 4. Find the output index we want to check
				var tx = rpc.GetRawTransaction(txId);
				int index = -1;
				for (int i = 0; i < tx.Outputs.Count; i++)
				{
					if (tx.Outputs[i].Value == sendAmount)
					{
						index = i;
					}
				}
				Assert.NotEqual(index, -1);

				// 5. Make sure the expected amounts are received for unconfirmed transactions
				getTxOutResponse = await rpc.GetTxOutAsync(txId, index, true);
				Assert.NotNull(getTxOutResponse); // null if spent
				Assert.Equal(blockHashes.Last(), getTxOutResponse.BestBlock);
				Assert.Equal(0, getTxOutResponse.Confirmations);
				Assert.Equal(Money.Coins(49), getTxOutResponse.TxOut.Value);
				Assert.NotNull(getTxOutResponse.TxOut.ScriptPubKey);
				Assert.Equal("pubkeyhash", getTxOutResponse.ScriptPubKeyType);
				Assert.False(getTxOutResponse.IsCoinBase);
			}
		}


		[Fact]
		public void EstimateSmartFee()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				node.Start();
				node.Generate(101);
				var rpc = node.CreateRPCClient();
				Assert.Throws<NoEstimationException>(() => rpc.EstimateSmartFee(1));
				Assert.Equal(Money.Coins(50m), rpc.GetBalance(1, false));
				Assert.Equal(Money.Coins(50m), rpc.GetBalance());
			}
		}

		[Fact]
		public void TryEstimateSmartFee()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				node.Start();
				node.Generate(101);
				var rpc = node.CreateRPCClient();
				Assert.Null(rpc.TryEstimateSmartFee(1));
			}
		}

		[Fact]
		public void TestFundRawTransaction()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				var rpc = node.CreateRPCClient();
				node.Start();
				rpc.Generate(101);

				var k = new Key();
				var tx = builder.Network.CreateTransaction();
				tx.Outputs.Add(new TxOut(Money.Coins(1), k));
				var result = rpc.FundRawTransaction(tx);
				TestFundRawTransactionResult(tx, result);

				result = rpc.FundRawTransaction(tx, new FundRawTransactionOptions());
				TestFundRawTransactionResult(tx, result);
				var result1 = result;

				var change = rpc.GetNewAddress();
				var change2 = rpc.GetRawChangeAddress();
				result = rpc.FundRawTransaction(tx, new FundRawTransactionOptions()
				{
					FeeRate = new FeeRate(Money.Satoshis(50), 1),
					IncludeWatching = true,
					ChangeAddress = change,
				});
				TestFundRawTransactionResult(tx, result);
				Assert.True(result1.Fee < result.Fee);
				Assert.Contains(result.Transaction.Outputs, o => o.ScriptPubKey == change.ScriptPubKey);
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
			using (var builder = NodeBuilderEx.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				var blockId = rpc.GetBestBlockHash();
				var block = rpc.GetBlock(blockId);
				Assert.True(block.CheckMerkleRoot());
			}
		}

		[Fact]
		public void CanImportMultiAddresses()
		{
			// Test cases borrowed from: https://github.com/bitcoin/bitcoin/blob/master/test/functional/wallet_importmulti.py
			// TODO: Those tests need to be rewritten to test warnings
			using (var builder = NodeBuilderEx.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();

				Key key;
				RPCException response;
				List<ImportMultiAddress> multiAddresses;
				Network network = Network.RegTest;

				// 20 total test cases

				#region Bitcoin Address
				key = new Key();
				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { Address = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network) },
						Timestamp = Utils.UnixTimeToDateTime(0)
					}
				};

				rpc.ImportMulti(multiAddresses.ToArray(), false);

				#endregion

				#region ScriptPubKey + internal
				key = new Key();
				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject (key.ScriptPubKey),
						Internal = true
					}
				};

				rpc.ImportMulti(multiAddresses.ToArray(), false);
				#endregion

				#region ScriptPubKey + !internal
				key = new Key();
				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { ScriptPubKey = key.ScriptPubKey },
					}
				};

				rpc.ImportMulti(multiAddresses.ToArray(), false);
				#endregion

				#region Address + Public key + !internal
				key = new Key();
				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject(key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network)),
						PubKeys = new [] { key.PubKey }
					}
				};

				rpc.ImportMulti(multiAddresses.ToArray(), false);
				#endregion

				#region ScriptPubKey + Public key + internal
				key = new Key();
				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { ScriptPubKey = key.ScriptPubKey },
						PubKeys = new[] { key.PubKey },
						Internal = true
					}
				};

				rpc.ImportMulti(multiAddresses.ToArray(), false);
				#endregion

				#region ScriptPubKey + Public key + !internal
				key = new Key();
				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { ScriptPubKey = key.ScriptPubKey },
						PubKeys = new [] { key.PubKey }
					}
				};

				rpc.ImportMulti(multiAddresses.ToArray(), false);
				#endregion

				#region Address + Private key + !watchonly
				key = new Key();
				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { Address = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network) },
						Keys = new [] { key.GetWif(network) }
					}
				};

				rpc.ImportMulti(multiAddresses.ToArray(), false);

				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { Address = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network) },
						Keys = new [] { key.GetWif(network) }
					}
				};

				response = Assert.Throws<RPCException>(() => rpc.ImportMulti(multiAddresses.ToArray(), false));

				//Assert.False(response.Result[0].Value<bool>());

				#endregion

				#region Address + Private key + watchonly
				key = new Key();
				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { Address = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network) },
						Keys = new [] { key.GetWif(network) },
						WatchOnly = true
					}
				};

				rpc.ImportMulti(multiAddresses.ToArray(), false);
				#endregion

				#region ScriptPubKey + Private key + internal
				key = new Key();
				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { ScriptPubKey = key.ScriptPubKey },
						Keys = new [] { key.GetWif(network) },
						Internal = true
					}
				};

				rpc.ImportMulti(multiAddresses.ToArray(), false);
				#endregion

				#region ScriptPubKey + Private key + !internal
				key = new Key();
				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { ScriptPubKey = key.ScriptPubKey },
						Keys = new [] { key.GetWif(network) }
					}
				};

				rpc.ImportMulti(multiAddresses.ToArray(), false);
				#endregion

				#region P2SH address
				//Blocked : Dependent on implementation of rpc.CreateMultiSig()
				#endregion

				#region P2SH + Redeem script
				//Blocked : Dependent on implementation of rpc.CreateMultiSig()
				#endregion

				#region P2SH + Redeem script + Private Keys + !Watchonly
				//Blocked : Dependent on implementation of rpc.CreateMultiSig()
				#endregion

				#region P2SH + Redeem script + Private Keys + Watchonly
				//Blocked : Dependent on implementation of rpc.CreateMultiSig()
				#endregion

				#region Address + Public key + !Internal + Wrong pubkey
				key = new Key();
				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { Address = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network) },
						PubKeys = new [] { new Key().PubKey }
					}
				};

				rpc.ImportMulti(multiAddresses.ToArray(), false);
				#endregion

				#region ScriptPubKey + Public key + internal + Wrong pubkey
				key = new Key();
				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { ScriptPubKey = key.ScriptPubKey },
						PubKeys = new [] { new Key().PubKey },
						Internal = true
					}
				};

				rpc.ImportMulti(multiAddresses.ToArray(), false);
				#endregion

				#region Address + Private key + !watchonly + Wrong private key
				key = new Key();
				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { Address = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network) },
						Keys = new [] { new Key().GetWif(network) }
					}
				};

				rpc.ImportMulti(multiAddresses.ToArray(), false);
				#endregion

				#region ScriptPubKey + Private key + internal + Wrong private key
				key = new Key();
				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { ScriptPubKey = key.ScriptPubKey },
						Keys = new [] { new Key().GetWif(network) },
						Internal = true
					}
				};

				rpc.ImportMulti(multiAddresses.ToArray(), false);
				#endregion

				#region Importing existing watch only address with new timestamp should replace saved timestamp.
				//TODO
				#endregion

				#region restart nodes to check for proper serialization/deserialization of watch only address
				//TODO
				#endregion
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
			var unspentCoin = new UnspentCoin(testData, Network.TestNet);

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
			var unspentCoin = new UnspentCoin(testData, Network.TestNet);

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
			var unspentCoin = new UnspentCoin(testData, Network.TestNet);

			Assert.NotNull(unspentCoin.RedeemScript);
		}

		[Fact]
		public void RawTransactionIsConformsToRPC()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				var tx = Network.TestNet.GetGenesis().Transactions[0];

				var tx2 = rpc.DecodeRawTransaction(tx.ToBytes());
				AssertJsonEquals(tx.ToString(RawFormat.Satoshi), tx2.ToString(RawFormat.Satoshi));
			}
		}

		[Fact]
		public void InvalidateBlockToRPC()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				var generatedBlockHashes = rpc.Generate(2);
				var tip = rpc.GetBestBlockHash();

				var bestBlockHash = generatedBlockHashes.Last();
				Assert.Equal(tip, bestBlockHash);

				rpc.InvalidateBlock(bestBlockHash);
				tip = rpc.GetBestBlockHash();
				Assert.NotEqual(tip, bestBlockHash);

				bestBlockHash = generatedBlockHashes.First();
				Assert.Equal(tip, bestBlockHash);
			}
		}


		[Fact]
		public void CanUseBatchedRequests()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var nodeA = builder.CreateNode();
				builder.StartAll();
				var rpc = nodeA.CreateRPCClient();
				var blocks = rpc.Generate(10);
				Assert.Throws<InvalidOperationException>(() => rpc.SendBatch());
				rpc = rpc.PrepareBatch();
				List<Task<uint256>> requests = new List<Task<uint256>>();
				for (int i = 1; i < 11; i++)
				{
					requests.Add(rpc.GetBlockHashAsync(i));
				}
				Thread.Sleep(1000);
				foreach (var req in requests)
				{
					Assert.Equal(TaskStatus.WaitingForActivation, req.Status);
				}
				rpc.SendBatch();
				rpc = rpc.PrepareBatch();
				int blockIndex = 0;
				foreach (var req in requests)
				{
					Assert.Equal(blocks[blockIndex], req.Result);
					Assert.Equal(TaskStatus.RanToCompletion, req.Status);
					blockIndex++;
				}
				requests.Clear();

				requests.Add(rpc.GetBlockHashAsync(10));
				requests.Add(rpc.GetBlockHashAsync(11));
				requests.Add(rpc.GetBlockHashAsync(9));
				requests.Add(rpc.GetBlockHashAsync(8));
				rpc.SendBatch();
				rpc = rpc.PrepareBatch();
				Assert.Equal(TaskStatus.RanToCompletion, requests[0].Status);
				Assert.Equal(TaskStatus.Faulted, requests[1].Status);
				Assert.Equal(TaskStatus.RanToCompletion, requests[2].Status);
				Assert.Equal(TaskStatus.RanToCompletion, requests[3].Status);
				requests.Clear();

				requests.Add(rpc.GetBlockHashAsync(10));
				requests.Add(rpc.GetBlockHashAsync(11));
				rpc.CancelBatch();
				rpc = rpc.PrepareBatch();
				Thread.Sleep(100);
				Assert.Equal(TaskStatus.Canceled, requests[0].Status);
				Assert.Equal(TaskStatus.Canceled, requests[1].Status);
			}
		}

		[Fact]
		public void CanGetPeersInfo()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var nodeA = builder.CreateNode();
				builder.StartAll();
				var rpc = nodeA.CreateRPCClient();
				using (var node = nodeA.CreateNodeClient())
				{
					node.VersionHandshake();
					var peers = rpc.GetPeersInfo();
					Assert.NotEmpty(peers);
				}
			}
		}

		[Fact]
		public void CanGetMemPoolEntry()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				var rpc = node.CreateRPCClient();
				builder.StartAll();
				node.Generate(101);

				var amount = Money.Coins(40.0m);
				var fee = Money.Coins(0.0001m);
				var txs = new List<uint256>();
				for (var i = 0; i < 10; i++)
				{
					amount = amount / 2 - fee;
					var address = rpc.GetNewAddress();
					var txid = rpc.SendToAddress(address, amount, "");
					txs.Add(txid);
				}
				var mempoolEntry = rpc.GetMempoolEntry(txs[3]);
				Assert.Equal(4, mempoolEntry.AncestorCount);
				Assert.Equal(7, mempoolEntry.DescendantCount);
				Assert.Equal(1, (int)mempoolEntry.SpentBy.Length);
				Assert.Equal(1, (int)mempoolEntry.Depends.Length);

				// Here we spend the change of the second transaction
				var funding = rpc.GetRawTransaction(txs[1]);
				var funding_spent = rpc.GetRawTransaction(txs[2]);
				var spent_idx = funding_spent.Inputs.First().PrevOut.N;
				var coins = funding.Outputs.AsCoins().ToList();
				var coin = spent_idx == 0 ? coins.Skip(1).First() : coins.First();

				var spent = Transaction.Create(builder.Network);
				spent.Inputs.Add(new TxIn(coin.Outpoint));
				spent.Outputs.Add(new TxOut(coin.Amount - fee, new Key().PubKey.Hash.ScriptPubKey));

				var signedTx = rpc.SignRawTransactionWithWallet(new SignRawTransactionRequest()
				{
					Transaction = spent
				});

				var txx = rpc.SendRawTransaction(signedTx.SignedTransaction);

				mempoolEntry = rpc.GetMempoolEntry(txs[1]);
				Assert.Equal(2, mempoolEntry.AncestorCount);
				Assert.Equal(10, mempoolEntry.DescendantCount);
				Assert.Equal(2, (int)mempoolEntry.SpentBy.Length);
				Assert.Equal(1, (int)mempoolEntry.Depends.Length);

				mempoolEntry = rpc.GetMempoolEntry(txx);
				Assert.Equal(3, mempoolEntry.AncestorCount);
				Assert.Equal(1, mempoolEntry.DescendantCount);
				Assert.Equal(0, (int)mempoolEntry.SpentBy.Length);
				Assert.Equal(1, (int)mempoolEntry.Depends.Length);

				mempoolEntry = rpc.GetMempoolEntry(txs[3]);
				Assert.Equal(4, mempoolEntry.AncestorCount);
				Assert.Equal(7, mempoolEntry.DescendantCount);
				Assert.Equal(1, (int)mempoolEntry.SpentBy.Length);
				Assert.Equal(1, (int)mempoolEntry.Depends.Length);
			}
		}

		[Fact]
		public void CanTestMempoolAccept()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				var rpc = node.CreateRPCClient();
				builder.StartAll();
				node.Generate(101);

				var coins = rpc.ListUnspent();
				var coin = coins[0];
				var fee = Money.Coins(0.0001m);
				var tx = Transaction.Create(node.Network);
				tx.Inputs.Add(coin.OutPoint);
				tx.Outputs.Add(tx.Outputs.CreateNewTxOut(coin.Amount - fee, new Key().PubKey.Hash.ScriptPubKey));

				var result = rpc.TestMempoolAccept(tx);
				Assert.False(result.IsAllowed);
				Assert.Equal(Protocol.RejectCode.INVALID, result.RejectCode);
				Assert.Equal("mandatory-script-verify-flag-failed (Operation not valid with the current stack size)", result.RejectReason);

				var signedTx = rpc.SignRawTransactionWithWallet(new SignRawTransactionRequest()
				{
					Transaction = tx
				});

				result = rpc.TestMempoolAccept(signedTx.SignedTransaction);
				Assert.True(result.IsAllowed);
				Assert.Equal((Protocol.RejectCode)0, result.RejectCode);
				Assert.Equal(string.Empty, result.RejectReason);
			}
		}

#if !NOSOCKET

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void onioncat_test()
		{
			var ip1 = Utils.ParseEndpoint("FD87:D87E:EB43:edb1:8e4:3588:e546:35ca", 10);
			var ip2 = Utils.ParseEndpoint("5wyqrzbvrdsumnok.onion", 10);
			Assert.True(ip1.IsTor());
			Assert.True(ip2.IsTor());
			Assert.IsType<IPEndPoint>(ip1);
			Assert.IsType<DnsEndPoint>(ip2);
			var torv3 = Utils.ParseEndpoint("explorerzydxu5ecjrkwceayqybizmpjjznk5izmitf2modhcusuqlid.onion", 10);
			Assert.True(torv3.IsTor());
			Assert.Null(torv3.AsOnionCatIPEndpoint());
			ip2 = ip2.AsOnionCatIPEndpoint();
			Assert.True(ip2.IsTor());
			Assert.Equal(ip1, ip2);
			ip1 = ip1.AsOnionCatIPEndpoint();
			Assert.Equal(ip1, ip2);
			Assert.True(((IPEndPoint)ip1).Address.IsRoutable(false));

			ip2 = Utils.ParseEndpoint("5wyqrzbvrdsumnok.onion", 10);
			ip1 = ip1.AsOnionDNSEndpoint();
			Assert.Equal(ip1, ip2);
			ip2 = ip2.AsOnionDNSEndpoint();
			Assert.Equal(ip1, ip2);
			Assert.Equal("5wyqrzbvrdsumnok.onion:10", ip2.ToEndpointString());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseEndpoint()
		{
			var endpoint = Utils.ParseEndpoint("google.com:94", 90);
			Assert.Equal(94, Assert.IsType<DnsEndPoint>(endpoint).Port);
			endpoint = Utils.ParseEndpoint("google.com", 90);
			Assert.Equal(90, Assert.IsType<DnsEndPoint>(endpoint).Port);
			endpoint = Utils.ParseEndpoint("10.10.1.3", 90);
			Assert.Equal("10.10.1.3", Assert.IsType<IPEndPoint>(endpoint).Address.ToString());
			Assert.Equal(90, Assert.IsType<IPEndPoint>(endpoint).Port);
			endpoint = Utils.ParseEndpoint("10.10.1.3:94", 90);
			Assert.Equal("10.10.1.3", Assert.IsType<IPEndPoint>(endpoint).Address.ToString());
			Assert.Equal(94, Assert.IsType<IPEndPoint>(endpoint).Port);

			endpoint = Utils.ParseEndpoint("::1", 90);
			Assert.Equal("[::1]:90", Assert.IsType<IPEndPoint>(endpoint).ToString());
			Assert.Equal(90, Assert.IsType<IPEndPoint>(endpoint).Port);

			endpoint = Utils.ParseEndpoint("[2001:db8:1f70::999:de8:7648:6e8]:100", 90);
			Assert.Equal("2001:db8:1f70:0:999:de8:7648:6e8", Assert.IsType<IPEndPoint>(endpoint).Address.ToString());
			Assert.Equal(100, Assert.IsType<IPEndPoint>(endpoint).Port);

			endpoint = Utils.ParseEndpoint("2001:db8:1f70::999:de8:7648:6e8", 90);
			Assert.Equal("2001:db8:1f70:0:999:de8:7648:6e8", Assert.IsType<IPEndPoint>(endpoint).Address.ToString());
			Assert.Equal(90, Assert.IsType<IPEndPoint>(endpoint).Port);
			endpoint = Utils.ParseEndpoint("[2001:db8:1f70::999:de8:7648:6e8]:94", 90);
			Assert.Equal("2001:db8:1f70:0:999:de8:7648:6e8", Assert.IsType<IPEndPoint>(endpoint).Address.ToString());
			Assert.Equal(94, Assert.IsType<IPEndPoint>(endpoint).Port);
			Assert.Throws<FormatException>(() => Utils.ParseEndpoint("inv LiewoN(#)9 hostname:94", 90));
			Assert.Throws<FormatException>(() => Utils.ParseEndpoint("inv LiewoN(#)9 hostname", 90));
			Assert.Throws<FormatException>(() => Utils.ParseEndpoint("", 90));
		}

		[Fact]
		public void CanAuthWithCookieFile()
		{
#if NOFILEIO
			Assert.Throws<NotSupportedException>(() => new RPCClient(Network.Main));
#else
			using (var builder = NodeBuilderEx.Create())
			{
				//Sanity check that it does not throw
#pragma warning disable CS0618
				new RPCClient(new NetworkCredential("toto", "tata:blah"), "localhost:10393", Network.Main);

				var node = builder.CreateNode();
				node.CookieAuth = true;
				node.Start();
				var rpc = node.CreateRPCClient();
				rpc.GetBlockCount();
				node.Restart();
				rpc.GetBlockCount();
				new RPCClient("cookiefile=data/tx_valid.json", new Uri("http://localhost/"), Network.RegTest);
				new RPCClient("cookiefile=data/efpwwie.json", new Uri("http://localhost/"), Network.RegTest);

				rpc = new RPCClient("bla:bla", null as Uri, Network.RegTest);
				Assert.Equal("http://127.0.0.1:" + Network.RegTest.RPCPort + "/", rpc.Address.AbsoluteUri);

				rpc = node.CreateRPCClient();
				rpc = rpc.PrepareBatch();
				var blockCountAsync = rpc.GetBlockCountAsync();
				rpc.SendBatch();
				var blockCount = blockCountAsync.GetAwaiter().GetResult();

				node.Restart();

				rpc = rpc.PrepareBatch();
				blockCountAsync = rpc.GetBlockCountAsync();
				rpc.SendBatch();
				blockCount = blockCountAsync.GetAwaiter().GetResult();

				rpc = new RPCClient("bla:bla", "http://toto/", Network.RegTest);
			}
#endif
		}



		[Fact]
		public void RPCSendRPCException()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				builder.StartAll();
				var rpcClient = node.CreateRPCClient();
				try
				{
					rpcClient.SendCommand("whatever");
					Assert.False(true, "Should have thrown");
				}
				catch (RPCException ex)
				{
					if (ex.RPCCode != RPCErrorCode.RPC_METHOD_NOT_FOUND)
					{
						Assert.False(true, "Should have thrown RPC_METHOD_NOT_FOUND");
					}
				}
			}
		}

		void WaitAssert(Action act)
		{
			int totalTry = 30;
			while (totalTry > 0)
			{
				try
				{
					act();
					return;
				}
				catch (AssertActualExpectedException)
				{
					Thread.Sleep(100);
					totalTry--;
				}
			}
		}
#endif
		[Fact]
		public void CanBackupWallet()
		{
			using (var builder = NodeBuilderEx.Create())
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
					if (File.Exists(filePath))
						File.Delete(filePath);
				}
			}
		}

		[Fact]
		public async Task CanGenerateBlocks()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				node.CookieAuth = true;
				node.Start();
				var rpc = node.CreateRPCClient();
				var capabilities = await rpc.ScanRPCCapabilitiesAsync();

				var address = new Key().PubKey.GetSegwitAddress(Network.RegTest);
				var blockHash1 = rpc.GenerateToAddress(1, address);
				var block = rpc.GetBlock(blockHash1[0]);

				var coinbaseScriptPubKey = block.Transactions[0].Outputs[0].ScriptPubKey;
				Assert.Equal(address, coinbaseScriptPubKey.GetDestinationAddress(Network.RegTest));

				rpc.Capabilities.SupportGenerateToAddress = true;
				var blockHash2 = rpc.Generate(1);

				rpc.Capabilities.SupportGenerateToAddress = false;
				var blockHash3 = rpc.Generate(1);

				var heigh = rpc.GetBlockCount();
				Assert.Equal(3, heigh);
			}
		}

		[Fact]
		public void ShouldCreatePSBTAcceptableByRPCAsExpected()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				node.Start();
				var client = node.CreateRPCClient();

				var keys = new Key[] { new Key(), new Key(), new Key() }.Select(k => k.GetWif(Network.RegTest)).ToArray();
				var redeem = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(3, keys.Select(ki => ki.PubKey).ToArray());
				var funds = PSBTTests.CreateDummyFunds(Network.TestNet, keys, redeem);

				// case1: PSBT from already fully signed tx
				var tx = PSBTTests.CreateTxToSpendFunds(funds, keys, redeem, true, true);
				var psbt = PSBT.FromTransaction(tx, builder.Network);
				psbt.AddCoins(funds);
				CheckPSBTIsAcceptableByRealRPC(psbt.ToBase64(), client);

				// but if we use rpc to convert tx to psbt, it will discard input scriptSig and ScriptWitness.
				// So it will be acceptable by any other rpc.
				psbt = PSBT.FromTransaction(tx.Clone(), builder.Network);
				CheckPSBTIsAcceptableByRealRPC(psbt.ToBase64(), client);

				// case2: PSBT from tx with script (but without signatures)
				tx = PSBTTests.CreateTxToSpendFunds(funds, keys, redeem, true, false);
				psbt = PSBT.FromTransaction(tx, builder.Network);
				psbt.AddCoins(funds);
				CheckPSBTIsAcceptableByRealRPC(psbt.ToBase64(), client);

				// case3: PSBT from tx without script nor signatures.
				tx = PSBTTests.CreateTxToSpendFunds(funds, keys, redeem, false, false);
				psbt = PSBT.FromTransaction(tx, builder.Network);
				// This time, it will not throw an error at the first place.
				// Since sanity check for witness input will not complain about witness-script-without-witnessUtxo
				CheckPSBTIsAcceptableByRealRPC(psbt.ToBase64(), client);

				var dummyKey = new Key();
				var dummyScript = new Script("OP_DUP " + "OP_HASH160 " + Op.GetPushOp(dummyKey.PubKey.Hash.ToBytes()) + " OP_EQUALVERIFY");

				// even after adding coins and scripts ...
				var psbtWithCoins = psbt.Clone().AddCoins(funds);
				CheckPSBTIsAcceptableByRealRPC(psbtWithCoins.ToBase64(), client);
				psbtWithCoins.AddScripts(redeem);
				CheckPSBTIsAcceptableByRealRPC(psbtWithCoins.ToBase64(), client);
				var tmp = psbtWithCoins.Clone().AddScripts(dummyScript); // should not change with dummyScript
				Assert.Equal(psbtWithCoins, tmp, PSBTComparerInstance);
				// or txs and scripts.
				var psbtWithTXs = psbt.Clone().AddTransactions(funds);
				CheckPSBTIsAcceptableByRealRPC(psbtWithTXs.ToBase64(), client);
				psbtWithTXs.AddScripts(redeem);
				CheckPSBTIsAcceptableByRealRPC(psbtWithTXs.ToBase64(), client);
				tmp = psbtWithTXs.Clone().AddScripts(dummyScript);
				Assert.Equal(psbtWithTXs, tmp, PSBTComparerInstance);

				// Let's not forget about hd KeyPath
				psbtWithTXs.AddKeyPath(keys[0].PubKey, new RootedKeyPath(default(HDFingerprint), KeyPath.Parse("m/1'/2/3")));
				psbtWithTXs.AddKeyPath(keys[1].PubKey, new RootedKeyPath(default(HDFingerprint), KeyPath.Parse("m/3'/2/1")));
				psbtWithTXs.AddKeyPath(keys[1].PubKey, new RootedKeyPath(default(HDFingerprint), KeyPath.Parse("m/3'/2/1")));
				CheckPSBTIsAcceptableByRealRPC(psbtWithTXs.ToBase64(), client);

				// What about after adding some signatures?
				psbtWithTXs.SignWithKeys(keys);
				CheckPSBTIsAcceptableByRealRPC(psbtWithTXs.ToBase64(), client);
				tmp = psbtWithTXs.Clone().SignWithKeys(dummyKey); // Try signing with unrelated key should not change anything
				Assert.Equal(psbtWithTXs, tmp, PSBTComparerInstance);
				// And finalization?
				psbtWithTXs.Finalize();
				CheckPSBTIsAcceptableByRealRPC(psbtWithTXs.ToBase64(), client);
			}
			return;
		}

		/// <summary>
		/// Just Check if the psbt is acceptable by bitcoin core rpc.
		/// </summary>
		/// <param name="base64"></param>
		/// <returns></returns>
		private void CheckPSBTIsAcceptableByRealRPC(string base64, RPCClient client)
			=> client.SendCommand(RPCOperations.decodepsbt, base64);

		[Fact]
		public void ShouldWalletProcessPSBTAndExtractMempoolAcceptableTX()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				node.Start();

				var client = node.CreateRPCClient();

				// ensure the wallet has whole kinds of coins ... 
				var addr = client.GetNewAddress();
				client.GenerateToAddress(101, addr);
				addr = client.GetNewAddress(new GetNewAddressRequest() { AddressType = AddressType.Bech32 });
				client.SendToAddress(addr, Money.Coins(15));
				addr = client.GetNewAddress(new GetNewAddressRequest() { AddressType = AddressType.P2SHSegwit });
				client.SendToAddress(addr, Money.Coins(15));
				var tmpaddr = new Key();
				client.GenerateToAddress(1, tmpaddr.PubKey.GetAddress(node.Network));

				// case 1: irrelevant psbt.
				var keys = new Key[] { new Key(), new Key(), new Key() }.Select(k => k.GetWif(Network.RegTest)).ToArray();
				var redeem = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(3, keys.Select(ki => ki.PubKey).ToArray());
				var funds = PSBTTests.CreateDummyFunds(Network.TestNet, keys, redeem);
				var tx = PSBTTests.CreateTxToSpendFunds(funds, keys, redeem, true, true);
				var psbt = PSBT.FromTransaction(tx, builder.Network)
					.AddTransactions(funds)
					.AddScripts(redeem);
				var case1Result = client.WalletProcessPSBT(psbt);
				// nothing must change for the psbt unrelated to the wallet.
				Assert.Equal(psbt, case1Result.PSBT, PSBTComparerInstance);

				// case 2: psbt relevant to the wallet. (but already finalized)
				var kOut = new Key();
				tx = builder.Network.CreateTransaction();
				tx.Outputs.Add(new TxOut(Money.Coins(45), kOut)); // This has to be big enough since the wallet must use whole kinds of address.
				var fundTxResult = client.FundRawTransaction(tx);
				Assert.Equal(3, fundTxResult.Transaction.Inputs.Count);
				var psbtFinalized = PSBT.FromTransaction(fundTxResult.Transaction, builder.Network);
				var result = client.WalletProcessPSBT(psbtFinalized, false);
				Assert.False(result.PSBT.CanExtractTransaction());
				result = client.WalletProcessPSBT(psbtFinalized, true);
				Assert.True(result.PSBT.CanExtractTransaction());

				// case 3a: psbt relevant to the wallet (and not finalized)
				var spendableCoins = client.ListUnspent().Where(c => c.IsSpendable).Select(c => c.AsCoin());
				tx = builder.Network.CreateTransaction();
				foreach (var coin in spendableCoins)
					tx.Inputs.Add(coin.Outpoint);
				tx.Outputs.Add(new TxOut(Money.Coins(45), kOut));
				var psbtUnFinalized = PSBT.FromTransaction(tx, builder.Network);

				var type = SigHash.All;
				// unsigned
				result = client.WalletProcessPSBT(psbtUnFinalized, false, type, bip32derivs: true);
				Assert.False(result.Complete);
				Assert.False(result.PSBT.CanExtractTransaction());
				var ex2 = Assert.Throws<PSBTException>(
					() => result.PSBT.Finalize()
				);
				Assert.NotEmpty(ex2.Errors);
				foreach (var psbtin in result.PSBT.Inputs)
				{
					Assert.Null(psbtin.SighashType);
					Assert.NotEmpty(psbtin.HDKeyPaths);
				}

				// signed
				result = client.WalletProcessPSBT(psbtUnFinalized, true, type);
				// does not throw
				result.PSBT.Finalize();

				var txResult = result.PSBT.ExtractTransaction();
				var acceptResult = client.TestMempoolAccept(txResult, true);
				Assert.True(acceptResult.IsAllowed, acceptResult.RejectReason);
			}
		}

		// refs: https://github.com/bitcoin/bitcoin/blob/df73c23f5fac031cc9b2ec06a74275db5ea322e3/doc/psbt.md#workflows
		// with 2 difference.
		// 1. one user (David) do not use bitcoin core (only NBitcoin)
		// 2. 4-of-4 instead of 2-of-3
		// 3. In version 0.17, `importmulti` can not handle witness script so only p2sh are considered here. TODO: fix
		[Fact]
		public void ShouldPerformMultisigProcessingWithCore()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var nodeAlice = builder.CreateNode();
				var nodeBob = builder.CreateNode();
				var nodeCarol = builder.CreateNode();
				var nodeFunder = builder.CreateNode();
				var david = new Key();
				builder.StartAll();

				// prepare multisig script and watch with node.
				var nodes = new CoreNode[] { nodeAlice, nodeBob, nodeCarol };
				var clients = nodes.Select(n => n.CreateRPCClient()).ToArray();
				var addresses = clients.Select(c => c.GetNewAddress());
				var addrInfos = addresses.Select((a, i) => clients[i].GetAddressInfo(a));
				var pubkeys = new List<PubKey> { david.PubKey };
				pubkeys.AddRange(addrInfos.Select(i => i.PubKey).ToArray());
				var script = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(4, pubkeys.ToArray());
				var aMultiP2SH = script.Hash.ScriptPubKey;
				// var aMultiP2WSH = script.WitHash.ScriptPubKey;
				// var aMultiP2SH_P2WSH = script.WitHash.ScriptPubKey.Hash.ScriptPubKey;
				var multiAddresses = new BitcoinAddress[] { aMultiP2SH.GetDestinationAddress(builder.Network) };
				var importMultiObject = new ImportMultiAddress[] {
						new ImportMultiAddress()
						{
							ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject(multiAddresses[0]),
							RedeemScript = script,
							Internal = true,
						},
						/*
						new ImportMultiAddress()
						{
							ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject(aMultiP2WSH),
							RedeemScript = script.ToHex(),
							Internal = true,
						},
						new ImportMultiAddress()
						{
							ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject(aMultiP2SH_P2WSH),
							RedeemScript = script.WitHash.ScriptPubKey.ToHex(),
							Internal = true,
						},
						new ImportMultiAddress()
						{
							ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject(aMultiP2SH_P2WSH),
							RedeemScript = script.ToHex(),
							Internal = true,
						}
						*/
					};

				for (var i = 0; i < clients.Length; i++)
				{
					var c = clients[i];
					Output.WriteLine($"Importing for {i}");
					c.ImportMulti(importMultiObject, false);
				}

				// pay from funder
				nodeFunder.Generate(103);
				var funderClient = nodeFunder.CreateRPCClient();
				funderClient.SendToAddress(aMultiP2SH, Money.Coins(40));
				// funderClient.SendToAddress(aMultiP2WSH, Money.Coins(40));
				// funderClient.SendToAddress(aMultiP2SH_P2WSH, Money.Coins(40));
				nodeFunder.Generate(1);
				foreach (var n in nodes)
				{
					nodeFunder.Sync(n, true);
				}

				// pay from multisig address
				// first carol creates psbt
				var carol = clients[2];
				// check if we have enough balance
				var info = carol.GetBlockchainInfoAsync().Result;
				Assert.Equal((ulong)104, info.Blocks);
				var balance = carol.GetBalance(0, true);
				// Assert.Equal(Money.Coins(120), balance);
				Assert.Equal(Money.Coins(40), balance);

				var aSend = new Key().PubKey.GetAddress(nodeAlice.Network);
				var outputs = new Dictionary<BitcoinAddress, Money>();
				outputs.Add(aSend, Money.Coins(10));
				var fundOptions = new FundRawTransactionOptions() { SubtractFeeFromOutputs = new int[] { 0 }, IncludeWatching = true };
				PSBT psbt = carol.WalletCreateFundedPSBT(null, outputs, 0, fundOptions).PSBT;
				psbt = carol.WalletProcessPSBT(psbt).PSBT;

				// second, Bob checks and process psbt.
				var bob = clients[1];
				Assert.Contains(multiAddresses, a =>
					psbt.Inputs.Any(psbtin => psbtin.WitnessUtxo?.ScriptPubKey == a.ScriptPubKey) ||
					psbt.Inputs.Any(psbtin => (bool)psbtin.NonWitnessUtxo?.Outputs.Any(o => a.ScriptPubKey == o.ScriptPubKey))
					);
				var psbt1 = bob.WalletProcessPSBT(psbt.Clone()).PSBT;

				// at the same time, David may do the ;
				psbt.SignWithKeys(david);
				var alice = clients[0];
				var psbt2 = alice.WalletProcessPSBT(psbt).PSBT;

				// not enough signatures
				Assert.Throws<PSBTException>(() => psbt.Finalize());

				// So let's combine.
				var psbtCombined = psbt1.Combine(psbt2);

				// Finally, anyone can finalize and broadcast the psbt.
				var tx = psbtCombined.Finalize().ExtractTransaction();
				var result = alice.TestMempoolAccept(tx);
				Assert.True(result.IsAllowed, result.RejectReason);
			}
		}


		[Fact]
		/// <summary>
		/// For p2sh, p2wsh, p2sh-p2wsh, we must also test the case for `solvable` to the wallet.
		/// For that, both script and the address must be imported by `importmulti`.
		/// but importmulti can not handle witness script(in v0.17).
		/// TODO: add test for solvable scripts.
		/// </summary>
		public void ShouldGetAddressInfo()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var client = builder.CreateNode(true).CreateRPCClient();
				var addrLegacy = client.GetNewAddress(new GetNewAddressRequest() { AddressType = AddressType.Legacy });
				var addrBech32 = client.GetNewAddress(new GetNewAddressRequest() { AddressType = AddressType.Bech32 });
				var addrP2SHSegwit = client.GetNewAddress(new GetNewAddressRequest() { AddressType = AddressType.P2SHSegwit });
				var pubkeys = new PubKey[] { new Key().PubKey, new Key().PubKey, new Key().PubKey };
				var redeem = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, pubkeys);
				client.ImportAddress(redeem.Hash);
				client.ImportAddress(redeem.WitHash);
				client.ImportAddress(redeem.WitHash.ScriptPubKey.Hash);

				Assert.NotNull(client.GetAddressInfo(addrLegacy));
				Assert.NotNull(client.GetAddressInfo(addrBech32));
				Assert.NotNull(client.GetAddressInfo(addrP2SHSegwit));
				Assert.NotNull(client.GetAddressInfo(redeem.Hash));
				Assert.NotNull(client.GetAddressInfo(redeem.WitHash));
				Assert.NotNull(client.GetAddressInfo(redeem.WitHash.ScriptPubKey.Hash));
			}
		}


		private void AssertJsonEquals(string json1, string json2)
		{
			foreach (var c in new[] { "\r\n", " ", "\t" })
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
			catch (T ex)
			{
				assert(ex);
			}
		}
	}
}
