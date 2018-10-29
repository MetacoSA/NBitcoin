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
				var txid = rpc.SendToAddress(new Key().PubKey.GetAddress(rpc.Network), Money.Coins(1.0m), "hello", "world");
				var ids = rpc.GetRawMempool();
				Assert.Single(ids);
				Assert.Equal(txid, ids[0]);
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

				var txid = rpc.SendToAddress(new Key().PubKey.GetAddress(rpc.Network), Money.Coins(1.0m), "hello", "world");
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
				var address = key.PubKey.GetAddress(rpc.Network);

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
				var mempoolTxId = rpc.SendToAddress(new Key().PubKey.GetAddress(builder.Network), Money.Coins(1));
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
				Assert.Equal("pubkey", getTxOutResponse.ScriptPubKeyType);
				Assert.True(getTxOutResponse.IsCoinBase);

				// 2. Spend the first coin
				var address = new Key().PubKey.GetAddress(rpc.Network);
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
			// Test cases borrowed from: https://github.com/bitcoin/bitcoin/blob/master/test/functional/importmulti.py
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
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { Address = key.PubKey.GetAddress(network) },
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

				response = Assert.Throws<RPCException>(() => rpc.ImportMulti(multiAddresses.ToArray(), false));
				Assert.Equal(RPCErrorCode.RPC_INVALID_PARAMETER, response.RPCCode);
				Assert.Equal("Internal must be set for hex scriptPubKey", response.Message);
				#endregion

				#region Address + Public key + !internal
				key = new Key();
				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject(key.PubKey.GetAddress(network)),
						PubKeys = new string[] { key.PubKey.ToString() }
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
						PubKeys = new string[] { key.PubKey.ToString() },
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
						PubKeys = new string[] { key.PubKey.ToString() }
					}
				};

				response = Assert.Throws<RPCException>(() => rpc.ImportMulti(multiAddresses.ToArray(), false));
				Assert.Equal(RPCErrorCode.RPC_INVALID_PARAMETER, response.RPCCode);
				Assert.Equal("Internal must be set for hex scriptPubKey", response.Message);
				#endregion

				#region Address + Private key + !watchonly
				key = new Key();
				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { Address = key.PubKey.GetAddress(network) },
						Keys = new string[] { key.GetWif(network).ToString() }
					}
				};

				rpc.ImportMulti(multiAddresses.ToArray(), false);

				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { Address = key.PubKey.GetAddress(network) },
						Keys = new string[] { key.GetWif(network).ToString() }
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
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { Address = key.PubKey.GetAddress(network) },
						Keys = new string[] { key.GetWif(network).ToString() },
						WatchOnly = true
					}
				};

				response = Assert.Throws<RPCException>(() => rpc.ImportMulti(multiAddresses.ToArray(), false));
				Assert.Equal(RPCErrorCode.RPC_INVALID_PARAMETER, response.RPCCode);
				Assert.Equal("Incompatibility found between watchonly and keys", response.Message);
				#endregion

				#region ScriptPubKey + Private key + internal
				key = new Key();
				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { ScriptPubKey = key.ScriptPubKey },
						Keys = new string[] { key.GetWif(network).ToString() },
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
						Keys = new string[] { key.GetWif(network).ToString() }
					}
				};

				response = Assert.Throws<RPCException>(() => rpc.ImportMulti(multiAddresses.ToArray(), false));
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
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { Address = key.PubKey.GetAddress(network) },
						PubKeys = new string[] { new Key().PubKey.ToString() }
					}
				};

				response = Assert.Throws<RPCException>(() => rpc.ImportMulti(multiAddresses.ToArray(), false));
				Assert.Equal(RPCErrorCode.RPC_INVALID_ADDRESS_OR_KEY, response.RPCCode);
				Assert.Equal("Consistency check failed", response.Message);
				#endregion

				#region ScriptPubKey + Public key + internal + Wrong pubkey
				key = new Key();
				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { ScriptPubKey = key.ScriptPubKey },
						PubKeys = new string[] { new Key().PubKey.ToString() },
						Internal = true
					}
				};

				response = Assert.Throws<RPCException>(() => rpc.ImportMulti(multiAddresses.ToArray(), false));
				Assert.Equal(RPCErrorCode.RPC_INVALID_ADDRESS_OR_KEY, response.RPCCode);
				Assert.Equal("Consistency check failed", response.Message);
				#endregion

				#region Address + Private key + !watchonly + Wrong private key
				key = new Key();
				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { Address = key.PubKey.GetAddress(network) },
						Keys = new string[] { new Key().GetWif(network).ToString() }
					}
				};

				response = Assert.Throws<RPCException>(() => rpc.ImportMulti(multiAddresses.ToArray(), false));
				Assert.Equal(RPCErrorCode.RPC_INVALID_ADDRESS_OR_KEY, response.RPCCode);
				Assert.Equal("Consistency check failed", response.Message);
				#endregion

				#region ScriptPubKey + Private key + internal + Wrong private key
				key = new Key();
				multiAddresses = new List<ImportMultiAddress>
				{
					new ImportMultiAddress
					{
						ScriptPubKey = new ImportMultiAddress.ScriptPubKeyObject { ScriptPubKey = key.ScriptPubKey },
						Keys = new string[] { new Key().GetWif(network).ToString() },
						Internal = true
					}
				};

				response = Assert.Throws<RPCException>(() => rpc.ImportMulti(multiAddresses.ToArray(), false));
				Assert.Equal(RPCErrorCode.RPC_INVALID_ADDRESS_OR_KEY, response.RPCCode);
				Assert.Equal("Consistency check failed", response.Message);
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

			Exception exception = null;
			try
			{
				Utils.ParseIpEndpoint("2001:db8:1f70::999:de8:7648:6e8:100", 90);
			}
			catch (Exception ex)
			{
				exception = ex;
			}
			Assert.NotNull(exception);
			Assert.Contains("SocketException", exception.GetType().FullName);

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
