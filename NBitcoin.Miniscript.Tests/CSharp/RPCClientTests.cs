using Xunit;
using NBitcoin;
using NBitcoin.Tests;
using NBitcoin.RPC;
using NBitcoin.BIP174;
using System;
using System.Linq;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace NBitcoin.Miniscript.Tests.CSharp
{
	public class RPCClientTests
	{
		internal PSBTComparer PSBTComparerInstance { get; }
		public ITestOutputHelper Output { get; }

		public RPCClientTests(ITestOutputHelper output)
		{
			PSBTComparerInstance = new PSBTComparer();

			Output = output;
		}
		[Fact]
		public void ShouldCreatePSBTAcceptableByRPCAsExpected()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				node.Start();
				var client = node.CreateRPCClient();

				var keys = new Key[] { new Key(), new Key(), new Key() };
				var redeem = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(3, keys.Select(ki => ki.PubKey).ToArray());
				var funds = PSBTTests.CreateDummyFunds(Network.TestNet, keys, redeem);

				// case1: PSBT from already fully signed tx
				var tx = PSBTTests.CreateTxToSpendFunds(funds, keys, redeem, true, true);
				// PSBT without previous outputs but with finalized_script_witness will throw an error.
				var psbt = PSBT.FromTransaction(tx.Clone(), true);
				Assert.Throws<FormatException>(() => psbt.ToBase64());

				// after adding coins, will not throw an error.
				psbt.AddCoins(funds.SelectMany(f => f.Outputs.AsCoins()).ToArray());
				CheckPSBTIsAcceptableByRealRPC(psbt.ToBase64(), client);

				// but if we use rpc to convert tx to psbt, it will discard input scriptSig and ScriptWitness.
				// So it will be acceptable by any other rpc.
				psbt = PSBT.FromTransaction(tx.Clone());
				CheckPSBTIsAcceptableByRealRPC(psbt.ToBase64(), client);

				// case2: PSBT from tx with script (but without signatures)
				tx = PSBTTests.CreateTxToSpendFunds(funds, keys, redeem, true, false);
				psbt = PSBT.FromTransaction(tx, true);
				// it has witness_script but has no prevout so it will throw an error.
				Assert.Throws<FormatException>(() => psbt.ToBase64());
				// after adding coins, will not throw error.
				psbt.AddCoins(funds.SelectMany(f => f.Outputs.AsCoins()).ToArray());
				CheckPSBTIsAcceptableByRealRPC(psbt.ToBase64(), client);

				// case3: PSBT from tx without script nor signatures.
				tx = PSBTTests.CreateTxToSpendFunds(funds, keys, redeem, false, false);
				psbt = PSBT.FromTransaction(tx, true);
				// This time, it will not throw an error at the first place.
				// Since sanity check for witness input will not complain about witness-script-without-witnessUtxo
				CheckPSBTIsAcceptableByRealRPC(psbt.ToBase64(), client);

				var dummyKey = new Key();
				var dummyScript = new Script("OP_DUP " + "OP_HASH160 " + Op.GetPushOp(dummyKey.PubKey.Hash.ToBytes()) + " OP_EQUALVERIFY");

				// even after adding coins and scripts ...
				var psbtWithCoins = psbt.Clone().AddCoins(funds.SelectMany(f => f.Outputs.AsCoins()).ToArray());
				CheckPSBTIsAcceptableByRealRPC(psbtWithCoins.ToBase64(), client);
				psbtWithCoins.AddScript(redeem);
				CheckPSBTIsAcceptableByRealRPC(psbtWithCoins.ToBase64(), client);
				var tmp = psbtWithCoins.Clone().AddScript(dummyScript); // should not change with dummyScript
				Assert.Equal(psbtWithCoins, tmp, PSBTComparerInstance);
				// or txs and scripts.
				var psbtWithTXs = psbt.Clone().AddTransactions(funds);
				CheckPSBTIsAcceptableByRealRPC(psbtWithTXs.ToBase64(), client);
				psbtWithTXs.AddScript(redeem);
				CheckPSBTIsAcceptableByRealRPC(psbtWithTXs.ToBase64(), client);
				tmp = psbtWithTXs.Clone().AddScript(dummyScript);
				Assert.Equal(psbtWithTXs, tmp, PSBTComparerInstance);

				// Let's don't forget about hd KeyPath
				psbtWithTXs.AddKeyPath(keys[0].PubKey, Tuple.Create((uint)1234, KeyPath.Parse("m/1'/2/3")));
				psbtWithTXs.AddPathTo(3, keys[1].PubKey, 4321, KeyPath.Parse("m/3'/2/1"));
				psbtWithTXs.AddPathTo(0, keys[1].PubKey, 4321, KeyPath.Parse("m/3'/2/1"), false);
				CheckPSBTIsAcceptableByRealRPC(psbtWithTXs.ToBase64(), client);

				// What about after adding some signatures?
				psbtWithTXs.SignAll(keys);
				CheckPSBTIsAcceptableByRealRPC(psbtWithTXs.ToBase64(), client);
				tmp = psbtWithTXs.Clone().SignAll(dummyKey); // Try signing with unrelated key should not change anything
				Assert.Equal(psbtWithTXs, tmp, PSBTComparerInstance);
				// And finalization?
				psbtWithTXs.FinalizeUnsafe();
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
				var keys = new Key[] { new Key(), new Key(), new Key() };
				var redeem = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(3, keys.Select(ki => ki.PubKey).ToArray());
				var funds = PSBTTests.CreateDummyFunds(Network.TestNet, keys, redeem);
				var tx = PSBTTests.CreateTxToSpendFunds(funds, keys, redeem, true, true);
				var psbt = PSBT.FromTransaction(tx, true)
					.AddTransactions(funds)
					.AddScript(redeem);
				var case1Result = client.WalletProcessPSBT(psbt);
				// nothing must change for the psbt unrelated to the wallet.
				Assert.Equal(psbt, case1Result.PSBT, PSBTComparerInstance);

				// case 2: psbt relevant to the wallet. (but already finalized)
				var kOut = new Key();
				tx = builder.Network.CreateTransaction();
				tx.Outputs.Add(new TxOut(Money.Coins(45), kOut)); // This has to be big enough since the wallet must use whole kinds of address.
				var fundTxResult = client.FundRawTransaction(tx);
				Assert.Equal(3, fundTxResult.Transaction.Inputs.Count);
				var psbtFinalized = PSBT.FromTransaction(fundTxResult.Transaction, true);
				var result = client.WalletProcessPSBT(psbtFinalized, false);
				Assert.False(result.PSBT.CanExtractTX());
				result = client.WalletProcessPSBT(psbtFinalized, true);
				Assert.True(result.PSBT.CanExtractTX());

				// case 3a: psbt relevant to the wallet (and not finalized)
				var spendableCoins = client.ListUnspent().Where(c => c.IsSpendable).Select(c => c.AsCoin());
				tx = builder.Network.CreateTransaction();
				foreach (var coin in spendableCoins)
					tx.Inputs.Add(coin.Outpoint);
				tx.Outputs.Add(new TxOut(Money.Coins(45), kOut));
				var psbtUnFinalized = PSBT.FromTransaction(tx, true);

				var type = SigHash.All;
				// unsigned
				result = client.WalletProcessPSBT(psbtUnFinalized, false, type, bip32derivs: true);
				Assert.False(result.Complete);
				Assert.False(result.PSBT.CanExtractTX());
				var ex2 = Assert.Throws<AggregateException>(
					() => result.PSBT.FinalizeUnsafe()
				);
				var errors2 = ex2.InnerExceptions;
				Assert.NotEmpty(errors2);
				foreach (var psbtin in result.PSBT.Inputs)
				{
					Assert.Equal(SigHash.Undefined, psbtin.SighashType);
					Assert.NotEmpty(psbtin.HDKeyPaths);
				}

				// signed
				result = client.WalletProcessPSBT(psbtUnFinalized, true, type);
				// does not throw
				result.PSBT.FinalizeUnsafe();

				var txResult = result.PSBT.ExtractTX();
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
				if (!builder.NodeImplementation.Version.Contains("0.17"))
					throw new Exception("Test must be updated!");
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
							RedeemScript = script.ToHex(),
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
				psbt.SignAll(david);
				var alice = clients[0];
				var psbt2 = alice.WalletProcessPSBT(psbt).PSBT;

				// not enough signatures
				Assert.Throws<AggregateException>(() => psbt.FinalizeUnsafe());

				// So let's combine.
				var psbtCombined = psbt1.Combine(psbt2);

				// Finally, anyone can finalize and broadcast the psbt.
				var tx = psbtCombined.FinalizeUnsafe().ExtractTX();
				var result = alice.TestMempoolAccept(tx);
				Assert.True(result.IsAllowed, result.RejectReason);
			}
		}
	}
 }