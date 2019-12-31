using Xunit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System;
using NBitcoin.DataEncoders;
using System.Collections.Generic;
using System.Linq;
using static NBitcoin.Tests.Comparer;
using Xunit.Abstractions;

namespace NBitcoin.Tests
{
	public class PSBTTests
	{
		private readonly ITestOutputHelper Output;

		private static JObject testdata { get; }
		private static PSBTComparer ComparerInstance { get; }

		static PSBTTests()
		{
			testdata = JObject.Parse(File.ReadAllText("data/psbt.json"));
			ComparerInstance = new PSBTComparer();
		}
		public PSBTTests(ITestOutputHelper output)
		{
			Output = output;
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public static void ShouldThrowExceptionForInvalidData()
		{
			JArray invalidTestCases = (JArray)testdata["invalid"];
			foreach (string i in invalidTestCases)
			{
				Assert.Throws<FormatException>(() => PSBT.Parse(i, Network.Main));
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public static void ShouldCalculateBalanceOfHDKey()
		{
			var aliceMaster = new ExtKey();
			var bobMaster = new ExtKey();

			var alice = aliceMaster.Derive(new KeyPath("1/2/3"));
			var bob = bobMaster.Derive(new KeyPath("4/5/6"));

			var funding = Network.Main.CreateTransaction();
			funding.Outputs.Add(Money.Coins(1.0m), alice);
			funding.Outputs.Add(Money.Coins(1.5m), bob);

			var coins = funding.Outputs.AsCoins().ToArray();
			var aliceCoin = coins[0];
			var bobCoin = coins[1];

			TransactionBuilder builder = Network.Main.CreateTransactionBuilder();
			builder.SetGroupName("Alice");
			builder.AddCoins(aliceCoin);
			builder.AddKeys(alice);
			builder.Send(new Key(), Money.Coins(0.2m));
			builder.Send(new Key(), Money.Coins(0.1m));
			builder.Send(bob, Money.Coins(0.123m));
			builder.SetChange(alice);

			builder.Then();
			builder.SetGroupName("Bob");
			builder.AddCoins(bobCoin);
			builder.AddKeys(bob);
			builder.Send(new Key(), Money.Coins(0.25m));
			builder.Send(new Key(), Money.Coins(0.01m));
			builder.SetChange(bob);
			builder.SendFees(Money.Coins(0.001m));

			var psbt = builder.BuildPSBT(false);
			psbt.AddKeyPath(aliceMaster, new KeyPath("1/2/3"));
			psbt.AddKeyPath(bobMaster, new KeyPath("4/5/6"));

			var actualBalance = psbt.GetBalance(ScriptPubKeyType.Legacy, aliceMaster);
			var expectedChange = aliceCoin.Amount - (Money.Coins(0.2m) + Money.Coins(0.1m) + Money.Coins(0.123m));
			var expectedBalance = -aliceCoin.Amount + expectedChange;
			Assert.Equal(expectedBalance, actualBalance);

			actualBalance = psbt.GetBalance(ScriptPubKeyType.Legacy, bobMaster);
			expectedChange = bobCoin.Amount - (Money.Coins(0.25m) + Money.Coins(0.01m) + Money.Coins(0.001m)) + Money.Coins(0.123m);
			expectedBalance = -bobCoin.Amount + expectedChange;
			Assert.Equal(expectedBalance, actualBalance);

			Assert.True(psbt.TryGetFee(out var fee));
			Assert.Equal(Money.Coins(0.001m), fee);

			Assert.True(psbt.TryGetEstimatedFeeRate(out var estimated));

			Assert.False(psbt.IsReadyToSign());
			psbt.AddTransactions(funding);
			Assert.True(psbt.IsReadyToSign());
			psbt.SignAll(ScriptPubKeyType.Legacy, bobMaster);
			psbt.SignAll(ScriptPubKeyType.Legacy, aliceMaster);

			psbt.Finalize();
			var result = psbt.ExtractTransaction();
			Assert.True(builder.Verify(result));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public static void ShouldParseValidDataDeterministically()
		{
			JArray validTestCases = (JArray)testdata["valid"];
			foreach (string i in validTestCases)
			{
				var psbt = PSBT.Parse(i, Network.Main);
				var psbtBase64 = Encoders.Base64.EncodeData(psbt.ToBytes());
				var psbt2 = PSBT.Parse(psbtBase64, Network.Main);
				Assert.Equal(psbt, psbt2, ComparerInstance);
			}
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ShouldPreserveOriginalTxPropertyAsPossible()
		{
			var keys = new Key[] { new Key(), new Key(), new Key() }.Select(k => k.GetWif(Network.RegTest)).ToArray();
			var redeem = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(3, keys.Select(k => k.PubKey).ToArray());
			var network = Network.Main;
			var funds = CreateDummyFunds(network, keys, redeem);

			// 1. without signature nor scripts.
			var tx = CreateTxToSpendFunds(funds, keys, redeem, false, false);

			// 2. with (unsigned) scriptSig and witness.
			tx = CreateTxToSpendFunds(funds, keys, redeem, true, false);
			var psbt = PSBT.FromTransaction(tx, Network.Main).AddCoins(funds);
			Assert.Null(psbt.Inputs[0].FinalScriptSig); // it is not finalized since it is not signed
			Assert.Null(psbt.Inputs[1].FinalScriptWitness); // This too
			Assert.NotNull(psbt.Inputs[2].RedeemScript); // But it holds redeem script.
			Assert.NotNull(psbt.Inputs[3].WitnessScript); // And witness script.
			Assert.NotNull(psbt.Inputs[5].WitnessScript); // even in p2sh-nested-p2wsh
			Assert.NotNull(psbt.Inputs[5].RedeemScript);

			// 3. with finalized scriptSig and witness
			tx = CreateTxToSpendFunds(funds, keys, redeem, true, true);
			psbt = PSBT.FromTransaction(tx, Network.Main)
				.AddTransactions(funds)
				.Finalize();

			Assert.Equal(tx.ToHex(), psbt.GetOriginalTransaction().ToHex()); // Check that we can still get the original tx

			Assert.NotNull(psbt.Inputs[0].FinalScriptSig); // it should be finalized
			Assert.NotNull(psbt.Inputs[0].NonWitnessUtxo);
			Assert.NotNull(psbt.Inputs[1].FinalScriptWitness); // p2wpkh too
			Assert.NotNull(psbt.Inputs[1].WitnessUtxo);
			Assert.Null(psbt.Inputs[2].RedeemScript);
			Assert.Null(psbt.Inputs[3].WitnessScript);

			Assert.NotNull(psbt.Inputs[4].FinalScriptSig); // Same principle holds for p2sh-nested version.
			Assert.NotNull(psbt.Inputs[4].FinalScriptWitness);
			Assert.Null(psbt.Inputs[5].WitnessScript);
			Assert.Null(psbt.Inputs[5].RedeemScript);

			Assert.Empty(psbt.Inputs[2].PartialSigs); // It can not hold partial_sigs
			Assert.Empty(psbt.Inputs[3].PartialSigs); // Even in p2wsh
			Assert.Empty(psbt.Inputs[5].PartialSigs); // And p2sh-p2wsh
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanUpdate()
		{
			var network = Network.Main;
			var alice = new BitcoinSecret("L23Ng7B8iXTcQ9emwDYpabUJVsxDQDxKwePrTwAZo1VT9xcDPfBF", network);
			var bob = new BitcoinSecret("L2nRrbzZytXSTjn95a4droGrAj5uwSEeG3JUHeNwdB9pUHu8Znjo", network);
			var carol = new BitcoinSecret("KzHNhJn4P3FML22cQ9yr6rc35hmTPwVmaCnXkc114fQ8ZKRR9hoK", network);
			var keys = new[] { alice, bob, carol };
			var redeem = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(3, keys.Select(k => k.PubKey).ToArray());
			var funds = CreateDummyFunds(network, keys, redeem);

			var tx = CreateTxToSpendFunds(funds, keys, redeem, true, false);
			var coins = DummyFundsToCoins(funds, redeem, alice);
			var psbtWithCoins = PSBT.FromTransaction(tx, Network.Main)
				.AddCoins(coins);

			Assert.Null(psbtWithCoins.Inputs[0].WitnessUtxo);
			Assert.NotNull(psbtWithCoins.Inputs[1].WitnessUtxo);
			Assert.Null(psbtWithCoins.Inputs[2].WitnessUtxo);
			Assert.NotNull(psbtWithCoins.Inputs[3].WitnessUtxo);
			Assert.NotNull(psbtWithCoins.Inputs[4].WitnessUtxo);
			Assert.NotNull(psbtWithCoins.Inputs[5].WitnessUtxo);

			// Check if it holds scripts as expected.
			Assert.Null(psbtWithCoins.Inputs[0].RedeemScript); // p2pkh
			Assert.Null(psbtWithCoins.Inputs[0].WitnessScript); // p2pkh
			Assert.Null(psbtWithCoins.Inputs[1].WitnessScript); // p2wpkh
			Assert.NotNull(psbtWithCoins.Inputs[2].RedeemScript); // p2sh
			Assert.NotNull(psbtWithCoins.Inputs[4].RedeemScript); // p2sh-p2wpkh
			Assert.NotNull(psbtWithCoins.Inputs[5].RedeemScript); // p2sh-p2wsh
			Assert.NotNull(psbtWithCoins.Inputs[3].WitnessScript); // p2wsh
			Assert.NotNull(psbtWithCoins.Inputs[5].WitnessScript); // p2sh-p2wsh

			// Operation must be idempotent.
			var tmp = psbtWithCoins.Clone().AddCoins(coins);
			Assert.Equal(tmp, psbtWithCoins, ComparerInstance);

			var signedPSBTWithCoins = psbtWithCoins
				.SignWithKeys(alice);
			Assert.Empty(signedPSBTWithCoins.Inputs[0].PartialSigs); // can not sign for non segwit input without non-witness UTXO
			Assert.Empty(signedPSBTWithCoins.Inputs[2].PartialSigs); // This too.
																	 // otherwise, It will increase Partial sigs count.
			Assert.Single(signedPSBTWithCoins.Inputs[1].PartialSigs);
			Assert.Single(signedPSBTWithCoins.Inputs[3].PartialSigs);
			Assert.Single(signedPSBTWithCoins.Inputs[4].PartialSigs);
			Assert.Single(signedPSBTWithCoins.Inputs[5].PartialSigs);
			var ex = Assert.Throws<PSBTException>(() =>
				signedPSBTWithCoins.Finalize()
			);
			// Only p2wpkh and p2sh-p2wpkh will succeed.
			Assert.Equal(4, ex.Errors.GroupBy(e => e.InputIndex).Count());

			var psbtWithTXs = PSBT.FromTransaction(tx, Network.Main)
				.AddCoins(coins)
				.AddTransactions(funds);
			Assert.Null(psbtWithTXs.Inputs[0].WitnessUtxo);
			Assert.NotNull(psbtWithTXs.Inputs[0].NonWitnessUtxo);
			Assert.NotNull(psbtWithTXs.Inputs[1].WitnessUtxo);
			Assert.Null(psbtWithTXs.Inputs[2].WitnessUtxo);
			Assert.NotNull(psbtWithTXs.Inputs[2].NonWitnessUtxo);
			Assert.NotNull(psbtWithTXs.Inputs[3].WitnessUtxo);
			Assert.NotNull(psbtWithTXs.Inputs[4].WitnessUtxo);
			Assert.NotNull(psbtWithTXs.Inputs[5].WitnessUtxo);

			// Operation must be idempotent.
			tmp = psbtWithTXs.Clone()
				.AddCoins(coins)
				.AddTransactions(funds);
			Assert.Equal(psbtWithTXs, tmp, ComparerInstance);

			var clonedPSBT = psbtWithTXs.Clone();

			clonedPSBT.SignWithKeys(keys[0]);
			psbtWithTXs.SignWithKeys(keys[1], keys[2]);

			var whollySignedPSBT = clonedPSBT.Combine(psbtWithTXs);

			// must sign only once for whole kinds of non-multisig tx.
			Assert.Single(whollySignedPSBT.Inputs[0].PartialSigs);
			Assert.Single(whollySignedPSBT.Inputs[1].PartialSigs);
			Assert.Single(whollySignedPSBT.Inputs[4].PartialSigs);

			// for multisig
			Assert.Equal(3, whollySignedPSBT.Inputs[2].PartialSigs.Count);
			Assert.Equal(3, whollySignedPSBT.Inputs[2].PartialSigs.Values.Distinct().Count());
			Assert.Equal(3, whollySignedPSBT.Inputs[3].PartialSigs.Count);
			Assert.Equal(3, whollySignedPSBT.Inputs[3].PartialSigs.Values.Distinct().Count());
			Assert.Equal(3, whollySignedPSBT.Inputs[5].PartialSigs.Count);
			Assert.Equal(3, whollySignedPSBT.Inputs[5].PartialSigs.Values.Distinct().Count());

			Assert.False(whollySignedPSBT.CanExtractTransaction());

			var finalizedPSBT = whollySignedPSBT.Finalize();
			Assert.True(finalizedPSBT.CanExtractTransaction());

			var finalTX = finalizedPSBT.ExtractTransaction();
			var result = finalTX.Check();
			Assert.Equal(TransactionCheckResult.Success, result);

			var builder = network.CreateTransactionBuilder();
			builder.AddCoins(coins).AddKeys(keys);
			if (!builder.Verify(finalTX, (Money)null, out var errors))
				throw new InvalidOperationException(errors.Aggregate(string.Empty, (a, b) => a + ";\n" + b));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ShouldFailToSignForTestcaseInvalidForSigner()
		{
			JArray testcases = (JArray)testdata["invalidForSigners"];
			foreach (string i in testcases)
			{
				var psbt = PSBT.Parse(i, Network.Main);
				Assert.Throws<PSBTException>(() => psbt.SignWithKeys(new Key()));
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ShouldCaptureExceptionInFinalization()
		{
			var keys = new Key[] { new Key(), new Key(), new Key() }.Select(k => k.GetWif(Network.RegTest)).ToArray();
			var redeem = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(3, keys.Select(k => k.PubKey).ToArray());
			var network = Network.Main;
			var funds = CreateDummyFunds(network, keys, redeem);

			var tx = CreateTxToSpendFunds(funds, keys, redeem, false, false);
			var psbt = PSBT.FromTransaction(tx, Network.Main);

			var ex = Assert.Throws<PSBTException>(() => psbt.Finalize());
			Assert.Equal(6, ex.Errors.GroupBy(e => e.InputIndex).Count());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void AddingScriptCoinShouldResultMoreInfoThanAddingSeparatelyInCaseOfP2SH()
		{
			var keys = new Key[] { new Key(), new Key(), new Key() }.Select(k => k.GetWif(Network.RegTest)).ToArray();
			var redeem = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(3, keys.Select(k => k.PubKey).ToArray());
			var network = Network.Main;
			var funds = CreateDummyFunds(network, keys, redeem);

			var tx = CreateTxToSpendFunds(funds, keys, redeem, false, false);
			var psbt = PSBT.FromTransaction(tx, Network.Main);

			// case 1: Check that it will result to more info by adding ScriptCoin in case of p2sh-p2wpkh
			var coins1 = DummyFundsToCoins(funds, null, null); // without script
			var scriptCoins2 = DummyFundsToCoins(funds, null, keys[0]); // only with p2sh-p2wpkh redeem.
			var psbt1 = psbt.Clone().AddCoins(coins1);
			var psbt2 = psbt.Clone().AddCoins(scriptCoins2);
			for (int i = 0; i < 6; i++)
			{
				Output.WriteLine($"Testing {i}");
				var a = psbt1.Inputs[i];
				var e = psbt2.Inputs[i];
				// Since there are no way psbt can know p2sh-p2wpkh is actually a witness input in case we add coins and scripts separately,
				// coin will not be added to the inputs[4].
				if (i == 4) // p2sh-p2wpkh
				{
					Assert.NotEqual(a.ToBytes(), e.ToBytes());
					Assert.Null(a.RedeemScript);
					Assert.Null(a.WitnessUtxo);
					Assert.NotNull(e.RedeemScript);
					Assert.NotNull(e.WitnessUtxo);
				}
				// but otherwise, it will be the same.
				else
				{
					Assert.Equal(a.PrevOut, e.PrevOut);
				}
			}

			// case 2: bare p2sh and p2sh-pw2sh
			var scriptCoins3 = DummyFundsToCoins(funds, redeem, keys[0]); // with full scripts.
			var psbt3 = psbt.Clone().AddCoins(scriptCoins3);
			for (int i = 0; i < 6; i++)
			{
				Output.WriteLine($"Testing {i}");
				var a = psbt2.Inputs[i];
				var e = psbt3.Inputs[i];
				if (i == 2 || i == 5) // p2sh, p2sh-p2wsh
				{
					Assert.NotEqual<byte[]>(a.ToBytes(), e.ToBytes());
					Assert.Null(a.WitnessUtxo);
					Assert.Null(a.RedeemScript);
					Assert.NotNull(e.RedeemScript);
					if (i == 5) // p2sh-p2wsh
					{
						Assert.NotNull(e.WitnessUtxo);
						Assert.NotNull(e.WitnessScript);
					}
				}
				else if (i == 3) // p2wsh
				{
					Assert.NotNull(a.WitnessUtxo);
					Assert.NotNull(e.WitnessUtxo);
					Assert.Null(a.RedeemScript);
					Assert.Null(e.RedeemScript);
					Assert.Null(a.WitnessScript);
					Assert.NotNull(e.WitnessScript);
				}
				else
				{
					Assert.Equal(a.PrevOut, e.PrevOut);
				}
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ShouldPassTheLongestTestInBIP174()
		{
			JObject testcase = (JObject)testdata["final"];
			var network = Network.TestNet;
			var master = ExtKey.Parse((string)testcase["master"], network);
			var masterFP = master.PrivateKey.PubKey.GetHDFingerPrint();
			var tx = network.CreateTransaction();
			tx.Version = 2;

			var scriptPubKey1 = Script.FromBytesUnsafe(Encoders.Hex.DecodeData((string)testcase["out1"]["script"]));
			var money1 = Money.Coins((decimal)testcase["out1"]["value"]);
			var scriptPubKey2 = Script.FromBytesUnsafe(Encoders.Hex.DecodeData((string)testcase["out2"]["script"]));
			var money2 = Money.Coins((decimal)testcase["out2"]["value"]);
			tx.Outputs.Add(new TxOut(value: money1, scriptPubKey: scriptPubKey1));
			tx.Outputs.Add(new TxOut(value: money2, scriptPubKey: scriptPubKey2));
			tx.Inputs.Add(new OutPoint(uint256.Parse((string)testcase["in1"]["txid"]), (uint)testcase["in1"]["index"]));
			tx.Inputs.Add(new OutPoint(uint256.Parse((string)testcase["in2"]["txid"]), (uint)testcase["in2"]["index"]));

			var expected = PSBT.Parse((string)testcase["psbt1"], Network.Main);

			var psbt = PSBT.FromTransaction(tx, Network.Main);
			Assert.Equal(expected, psbt, ComparerInstance);

			var prevtx1 = Transaction.Parse((string)testcase["prevtx1"], network);
			var prevtx2 = Transaction.Parse((string)testcase["prevtx2"], network);
			psbt.AddTransactions(prevtx1, prevtx2);
			var redeem1 = Script.FromBytesUnsafe(Encoders.Hex.DecodeData((string)testcase["redeem1"]));
			var redeem2 = Script.FromBytesUnsafe(Encoders.Hex.DecodeData((string)testcase["redeem2"]));
			var witness_script1 = Script.FromBytesUnsafe(Encoders.Hex.DecodeData((string)testcase["witness1"]));
			psbt.AddScripts(new[] { redeem1, redeem2, witness_script1 });

			for (int i = 0; i < 6; i++)
			{
				var pk = testcase[$"pubkey{i}"];
				var pubkey = new PubKey((string)pk["hex"]);
				var path = KeyPath.Parse((string)pk["path"]);
				psbt.AddKeyPath(pubkey, new RootedKeyPath(masterFP, path));
			}

			expected = PSBT.Parse((string)testcase["psbt2"], Network.Main);
			Assert.Equal(expected, psbt, ComparerInstance);

			foreach (var psbtin in psbt.Inputs)
				psbtin.SighashType = SigHash.All;
			expected = PSBT.Parse((string)testcase["psbt3"], Network.Main);
			Assert.Equal(expected, psbt, ComparerInstance);

			psbt.AssertSanity();
			var psbtForBob = psbt.Clone();

			// path 1 ... alice
			Assert.Equal(psbt, psbtForBob, ComparerInstance);
			var aliceKey1 = master.Derive(new KeyPath((string)testcase["key7"]["path"])).PrivateKey;
			var aliceKey2 = master.Derive(new KeyPath((string)testcase["key8"]["path"])).PrivateKey;
			psbt.SignWithKeys(aliceKey1, aliceKey2);
			expected = PSBT.Parse((string)testcase["psbt4"], Network.Main);
			Assert.Equal(expected, psbt);

			// path 2 ... bob.
			var bobKey1 = master.Derive(new KeyPath((string)testcase["key9"]["path"])).PrivateKey;
			var bobKey2 = master.Derive(new KeyPath((string)testcase["key10"]["path"])).PrivateKey;
			var bobKeyhex1 = (string)testcase["key9"]["wif"];
			var bobKeyhex2 = (string)testcase["key10"]["wif"];
			Assert.Equal(bobKey1, new BitcoinSecret(bobKeyhex1, network).PrivateKey);
			Assert.Equal(bobKey2, new BitcoinSecret(bobKeyhex2, network).PrivateKey);
			psbtForBob.Settings.UseLowR = false;
			psbtForBob.SignWithKeys(bobKey1, bobKey2);
			expected = PSBT.Parse((string)testcase["psbt5"], Network.Main);
			Assert.Equal(expected, psbtForBob);

			// merge above 2
			var combined = psbt.Combine(psbtForBob);
			expected = PSBT.Parse((string)testcase["psbtcombined"], Network.Main);
			Assert.Equal(expected, combined);

			Assert.True(combined.TryGetFee(out var fee));
			Assert.Equal(Money.Coins(0.00010000m), fee);
			Assert.True(combined.TryGetEstimatedFeeRate(out var feeRate));
			Assert.Equal(new FeeRate(21.6m).SatoshiPerByte, feeRate.SatoshiPerByte, 1);

			var finalized = psbt.Finalize();
			expected = PSBT.Parse((string)testcase["psbtfinalized"], Network.Main);
			Assert.Equal(expected, finalized);

			var finalTX = psbt.ExtractTransaction();
			var expectedTX = Transaction.Parse((string)testcase["txextracted"], network);
			AssertEx.CollectionEquals(expectedTX.ToBytes(), finalTX.ToBytes());

			Assert.True(psbt.TryGetFee(out fee));
			Assert.Equal(Money.Coins(0.00010000m), fee);
			Assert.True(psbt.TryGetEstimatedFeeRate(out feeRate));
			Assert.Equal(new FeeRate(21.6m).SatoshiPerByte, feeRate.SatoshiPerByte, 2);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanHandleUnKnown()
		{
			var data1 = PSBT.Parse((string)testdata["psbtUnknown0"], Network.Main);
			var data2 = PSBT.Parse((string)testdata["psbtUnknown1"], Network.Main);
			data1.Combine(data2);
			var expected = PSBT.Parse((string)testdata["psbtUnknown2"], Network.Main);
			Assert.Equal(data1, expected, ComparerInstance);
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanRebaseKeypathInPSBT()
		{
			var masterExtkey = new BitcoinExtKey("tprv8ZgxMBicQKsPd9TeAdPADNnSyH9SSUUbTVeFszDE23Ki6TBB5nCefAdHkK8Fm3qMQR6sHwA56zqRmKmxnHk37JkiFzvncDqoKmPWubu7hDF", Network.TestNet);
			var masterFP = masterExtkey.GetPublicKey().GetHDFingerPrint();
			var accountExtKey = masterExtkey.Derive(new KeyPath("0'/0'/0'"));
			var accountRootedKeyPath = new KeyPath("0'/0'/0'").ToRootedKeyPath(masterExtkey);
			uint hardenedFlag = 0x80000000U;
		retry:
			Transaction funding = masterExtkey.Network.CreateTransaction();
			funding.Outputs.Add(Money.Coins(2.0m), accountExtKey.Derive(0 | hardenedFlag).ScriptPubKey);
			funding.Outputs.Add(Money.Coins(2.0m), accountExtKey.Derive(1 | hardenedFlag).ScriptPubKey);

			Transaction tx = masterExtkey.Network.CreateTransaction();
			tx.Version = 2;
			tx.Outputs.Add(Money.Coins(1.49990000m), new Script(Encoders.Hex.DecodeData("0014d85c2b71d0060b09c9886aeb815e50991dda124d")));
			tx.Outputs.Add(Money.Coins(1.00000000m), new Script(Encoders.Hex.DecodeData("001400aea9a2e5f0f876a588df5546e8742d1d87008f")));
			tx.Inputs.Add(funding, 0);
			tx.Inputs.Add(funding, 1);

			var psbt = PSBT.FromTransaction(tx, Network.TestNet);
			psbt.AddTransactions(funding);
			psbt.AddKeyPath(accountExtKey, Tuple.Create(new KeyPath(0 | hardenedFlag), funding.Outputs[0].ScriptPubKey),
										   Tuple.Create(new KeyPath(1 | hardenedFlag), funding.Outputs[1].ScriptPubKey));
			Assert.Equal(new KeyPath(0 | hardenedFlag), psbt.Inputs[0].HDKeyPaths[accountExtKey.Derive(0 | hardenedFlag).GetPublicKey()].KeyPath);
			Assert.Equal(new KeyPath(1 | hardenedFlag), psbt.Inputs[1].HDKeyPaths[accountExtKey.Derive(1 | hardenedFlag).GetPublicKey()].KeyPath);
			Assert.Equal(accountExtKey.GetPublicKey().GetHDFingerPrint(), psbt.Inputs[0].HDKeyPaths[accountExtKey.Derive(0 | hardenedFlag).GetPublicKey()].MasterFingerprint);
			Assert.Equal(accountExtKey.GetPublicKey().GetHDFingerPrint(), psbt.Inputs[1].HDKeyPaths[accountExtKey.Derive(1 | hardenedFlag).GetPublicKey()].MasterFingerprint);

			var memento = psbt.Clone();
			psbt.GlobalXPubs.Add(accountExtKey.Neuter(), new RootedKeyPath(accountExtKey, new KeyPath()));
			var rebasingKeyPath = new RootedKeyPath(masterExtkey.GetPublicKey().GetHDFingerPrint(), new KeyPath("0'/0'/0'"));
			psbt.RebaseKeyPaths(accountExtKey, rebasingKeyPath);
			Assert.Equal(psbt.GlobalXPubs.Single().Value, rebasingKeyPath);
			psbt.ToString();
			psbt = PSBT.Parse(psbt.ToHex(), psbt.Network);
			Assert.Equal(psbt.GlobalXPubs.Single().Value, rebasingKeyPath);
			Assert.Equal(new KeyPath("0'/0'/0'").Derive(0 | hardenedFlag), psbt.Inputs[0].HDKeyPaths[accountExtKey.Derive(0 | hardenedFlag).GetPublicKey()].KeyPath);
			Assert.Equal(new KeyPath("0'/0'/0'").Derive(1 | hardenedFlag), psbt.Inputs[1].HDKeyPaths[accountExtKey.Derive(1 | hardenedFlag).GetPublicKey()].KeyPath);
			Assert.Equal(masterExtkey.GetPublicKey().GetHDFingerPrint(), psbt.Inputs[0].HDKeyPaths[accountExtKey.Derive(0 | hardenedFlag).GetPublicKey()].MasterFingerprint);
			Assert.Equal(masterExtkey.GetPublicKey().GetHDFingerPrint(), psbt.Inputs[1].HDKeyPaths[accountExtKey.Derive(1 | hardenedFlag).GetPublicKey()].MasterFingerprint);

			Assert.NotEqual(Money.Zero, psbt.GetBalance(ScriptPubKeyType.Legacy, masterExtkey));
			Assert.Equal(psbt.GetBalance(ScriptPubKeyType.Legacy, masterExtkey), psbt.GetBalance(ScriptPubKeyType.Legacy, accountExtKey, accountRootedKeyPath));
			if (hardenedFlag != 0) // If hardened, we can't get the balance from the account pubkey
			{
				Assert.Equal(Money.Zero, psbt.GetBalance(ScriptPubKeyType.Legacy, accountExtKey.Neuter(), accountRootedKeyPath));
			}
			else
			{
				Assert.Equal(psbt.GetBalance(ScriptPubKeyType.Legacy, masterExtkey), psbt.GetBalance(ScriptPubKeyType.Legacy, accountExtKey.Neuter(), accountRootedKeyPath));
			}
			Assert.Equal(Money.Zero, psbt.GetBalance(ScriptPubKeyType.Legacy, masterExtkey.Derive(new KeyPath("0'/0'/1'")), new KeyPath("0'/0'/1'").ToRootedKeyPath(masterFP)));
			Assert.Equal(Money.Zero, psbt.GetBalance(ScriptPubKeyType.Legacy, masterExtkey.Neuter())); // Can't derive!
			if (hardenedFlag != 0)
			{
				hardenedFlag = 0;
				goto retry;
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanFollowBIPExample()
		{
			var extkey = new BitcoinExtKey("tprv8ZgxMBicQKsPd9TeAdPADNnSyH9SSUUbTVeFszDE23Ki6TBB5nCefAdHkK8Fm3qMQR6sHwA56zqRmKmxnHk37JkiFzvncDqoKmPWubu7hDF", Network.TestNet);
			// A creator creating a PSBT for a transaction which creates the following outputs:
			Transaction tx = extkey.Network.CreateTransaction();
			tx.Version = 2;
			tx.Outputs.Add(Money.Coins(1.49990000m), new Script(Encoders.Hex.DecodeData("0014d85c2b71d0060b09c9886aeb815e50991dda124d")));
			tx.Outputs.Add(Money.Coins(1.00000000m), new Script(Encoders.Hex.DecodeData("001400aea9a2e5f0f876a588df5546e8742d1d87008f")));
			// and spends the following inputs:
			tx.Inputs.Add(OutPoint.Parse("75ddabb27b8845f5247975c8a5ba7c6f336c4570708ebe230caf6db5217ae858-0"));
			tx.Inputs.Add(OutPoint.Parse("1dea7cd05979072a3578cab271c02244ea8a090bbb46aa680a65ecd027048d83-1"));
			var actualPsbt = PSBT.FromTransaction(tx, Network.Main);
			// must create this PSBT:
			var expectedPsbt = PSBT.Parse("70736274ff01009a020000000258e87a21b56daf0c23be8e7070456c336f7cbaa5c8757924f545887bb2abdd750000000000ffffffff838d0427d0ec650a68aa46bb0b098aea4422c071b2ca78352a077959d07cea1d0100000000ffffffff0270aaf00800000000160014d85c2b71d0060b09c9886aeb815e50991dda124d00e1f5050000000016001400aea9a2e5f0f876a588df5546e8742d1d87008f000000000000000000", extkey.Network);
			Assert.Equal(expectedPsbt, actualPsbt);

			// Given the above PSBT, an updater with only the following:
			// Previous Transactions:
			actualPsbt.AddTransactions(
				Transaction.Parse("0200000000010158e87a21b56daf0c23be8e7070456c336f7cbaa5c8757924f545887bb2abdd7501000000171600145f275f436b09a8cc9a2eb2a2f528485c68a56323feffffff02d8231f1b0100000017a914aed962d6654f9a2b36608eb9d64d2b260db4f1118700c2eb0b0000000017a914b7f5faf40e3d40a5a459b1db3535f2b72fa921e88702483045022100a22edcc6e5bc511af4cc4ae0de0fcd75c7e04d8c1c3a8aa9d820ed4b967384ec02200642963597b9b1bc22c75e9f3e117284a962188bf5e8a74c895089046a20ad770121035509a48eb623e10aace8bfd0212fdb8a8e5af3c94b0b133b95e114cab89e4f7965000000", Network.Main),
				Transaction.Parse("0200000001aad73931018bd25f84ae400b68848be09db706eac2ac18298babee71ab656f8b0000000048473044022058f6fc7c6a33e1b31548d481c826c015bd30135aad42cd67790dab66d2ad243b02204a1ced2604c6735b6393e5b41691dd78b00f0c5942fb9f751856faa938157dba01feffffff0280f0fa020000000017a9140fb9463421696b82c833af241c78c17ddbde493487d0f20a270100000017a91429ca74f8a08f81999428185c97b5d852e4063f618765000000", Network.Main)
				);
			// Scripts
			actualPsbt.AddScripts(
				new Script(Encoders.Hex.DecodeData("5221029583bf39ae0a609747ad199addd634fa6108559d6c5cd39b4c2183f1ab96e07f2102dab61ff49a14db6a7d02b0cd1fbb78fc4b18312b5b4e54dae4dba2fbfef536d752ae")),
				new Script(Encoders.Hex.DecodeData("00208c2353173743b595dfb4a07b72ba8e42e3797da74e87fe7d9d7497e3b2028903")),
				new Script(Encoders.Hex.DecodeData("522103089dc10c7ac6db54f91329af617333db388cead0c231f723379d1b99030b02dc21023add904f3d6dcf59ddb906b0dee23529b7ffb9ed50e5e86151926860221f0e7352ae")));
			// Public Keys
			actualPsbt.AddKeyPath(extkey, new KeyPath("m/0'/0'/0'"),
										  new KeyPath("m/0'/0'/1'"),
										  new KeyPath("m/0'/0'/2'"),
										  new KeyPath("m/0'/0'/3'"),
										  new KeyPath("m/0'/0'/4'"),
										  new KeyPath("m/0'/0'/5'"));

			// Must create this PSBT:
			expectedPsbt = PSBT.Parse("70736274ff01009a020000000258e87a21b56daf0c23be8e7070456c336f7cbaa5c8757924f545887bb2abdd750000000000ffffffff838d0427d0ec650a68aa46bb0b098aea4422c071b2ca78352a077959d07cea1d0100000000ffffffff0270aaf00800000000160014d85c2b71d0060b09c9886aeb815e50991dda124d00e1f5050000000016001400aea9a2e5f0f876a588df5546e8742d1d87008f00000000000100bb0200000001aad73931018bd25f84ae400b68848be09db706eac2ac18298babee71ab656f8b0000000048473044022058f6fc7c6a33e1b31548d481c826c015bd30135aad42cd67790dab66d2ad243b02204a1ced2604c6735b6393e5b41691dd78b00f0c5942fb9f751856faa938157dba01feffffff0280f0fa020000000017a9140fb9463421696b82c833af241c78c17ddbde493487d0f20a270100000017a91429ca74f8a08f81999428185c97b5d852e4063f6187650000000104475221029583bf39ae0a609747ad199addd634fa6108559d6c5cd39b4c2183f1ab96e07f2102dab61ff49a14db6a7d02b0cd1fbb78fc4b18312b5b4e54dae4dba2fbfef536d752ae2206029583bf39ae0a609747ad199addd634fa6108559d6c5cd39b4c2183f1ab96e07f10d90c6a4f000000800000008000000080220602dab61ff49a14db6a7d02b0cd1fbb78fc4b18312b5b4e54dae4dba2fbfef536d710d90c6a4f0000008000000080010000800001012000c2eb0b0000000017a914b7f5faf40e3d40a5a459b1db3535f2b72fa921e88701042200208c2353173743b595dfb4a07b72ba8e42e3797da74e87fe7d9d7497e3b2028903010547522103089dc10c7ac6db54f91329af617333db388cead0c231f723379d1b99030b02dc21023add904f3d6dcf59ddb906b0dee23529b7ffb9ed50e5e86151926860221f0e7352ae2206023add904f3d6dcf59ddb906b0dee23529b7ffb9ed50e5e86151926860221f0e7310d90c6a4f000000800000008003000080220603089dc10c7ac6db54f91329af617333db388cead0c231f723379d1b99030b02dc10d90c6a4f00000080000000800200008000220203a9a4c37f5996d3aa25dbac6b570af0650394492942460b354753ed9eeca5877110d90c6a4f000000800000008004000080002202027f6399757d2eff55a136ad02c684b1838b6556e5f1b6b34282a94b6b5005109610d90c6a4f00000080000000800500008000", extkey.Network);
			Assert.Equal(expectedPsbt, actualPsbt);

			// An updater which adds SIGHASH_ALL to the above PSBT must create this PSBT:
			foreach (var input in actualPsbt.Inputs)
				input.SighashType = SigHash.All;
			expectedPsbt = PSBT.Parse("70736274ff01009a020000000258e87a21b56daf0c23be8e7070456c336f7cbaa5c8757924f545887bb2abdd750000000000ffffffff838d0427d0ec650a68aa46bb0b098aea4422c071b2ca78352a077959d07cea1d0100000000ffffffff0270aaf00800000000160014d85c2b71d0060b09c9886aeb815e50991dda124d00e1f5050000000016001400aea9a2e5f0f876a588df5546e8742d1d87008f00000000000100bb0200000001aad73931018bd25f84ae400b68848be09db706eac2ac18298babee71ab656f8b0000000048473044022058f6fc7c6a33e1b31548d481c826c015bd30135aad42cd67790dab66d2ad243b02204a1ced2604c6735b6393e5b41691dd78b00f0c5942fb9f751856faa938157dba01feffffff0280f0fa020000000017a9140fb9463421696b82c833af241c78c17ddbde493487d0f20a270100000017a91429ca74f8a08f81999428185c97b5d852e4063f618765000000010304010000000104475221029583bf39ae0a609747ad199addd634fa6108559d6c5cd39b4c2183f1ab96e07f2102dab61ff49a14db6a7d02b0cd1fbb78fc4b18312b5b4e54dae4dba2fbfef536d752ae2206029583bf39ae0a609747ad199addd634fa6108559d6c5cd39b4c2183f1ab96e07f10d90c6a4f000000800000008000000080220602dab61ff49a14db6a7d02b0cd1fbb78fc4b18312b5b4e54dae4dba2fbfef536d710d90c6a4f0000008000000080010000800001012000c2eb0b0000000017a914b7f5faf40e3d40a5a459b1db3535f2b72fa921e8870103040100000001042200208c2353173743b595dfb4a07b72ba8e42e3797da74e87fe7d9d7497e3b2028903010547522103089dc10c7ac6db54f91329af617333db388cead0c231f723379d1b99030b02dc21023add904f3d6dcf59ddb906b0dee23529b7ffb9ed50e5e86151926860221f0e7352ae2206023add904f3d6dcf59ddb906b0dee23529b7ffb9ed50e5e86151926860221f0e7310d90c6a4f000000800000008003000080220603089dc10c7ac6db54f91329af617333db388cead0c231f723379d1b99030b02dc10d90c6a4f00000080000000800200008000220203a9a4c37f5996d3aa25dbac6b570af0650394492942460b354753ed9eeca5877110d90c6a4f000000800000008004000080002202027f6399757d2eff55a136ad02c684b1838b6556e5f1b6b34282a94b6b5005109610d90c6a4f00000080000000800500008000", extkey.Network);
			Assert.Equal(expectedPsbt, actualPsbt);

			actualPsbt.Settings.UseLowR = false;
			expectedPsbt = PSBT.Parse("70736274ff01009a020000000258e87a21b56daf0c23be8e7070456c336f7cbaa5c8757924f545887bb2abdd750000000000ffffffff838d0427d0ec650a68aa46bb0b098aea4422c071b2ca78352a077959d07cea1d0100000000ffffffff0270aaf00800000000160014d85c2b71d0060b09c9886aeb815e50991dda124d00e1f5050000000016001400aea9a2e5f0f876a588df5546e8742d1d87008f00000000000100bb0200000001aad73931018bd25f84ae400b68848be09db706eac2ac18298babee71ab656f8b0000000048473044022058f6fc7c6a33e1b31548d481c826c015bd30135aad42cd67790dab66d2ad243b02204a1ced2604c6735b6393e5b41691dd78b00f0c5942fb9f751856faa938157dba01feffffff0280f0fa020000000017a9140fb9463421696b82c833af241c78c17ddbde493487d0f20a270100000017a91429ca74f8a08f81999428185c97b5d852e4063f6187650000002202029583bf39ae0a609747ad199addd634fa6108559d6c5cd39b4c2183f1ab96e07f473044022074018ad4180097b873323c0015720b3684cc8123891048e7dbcd9b55ad679c99022073d369b740e3eb53dcefa33823c8070514ca55a7dd9544f157c167913261118c01010304010000000104475221029583bf39ae0a609747ad199addd634fa6108559d6c5cd39b4c2183f1ab96e07f2102dab61ff49a14db6a7d02b0cd1fbb78fc4b18312b5b4e54dae4dba2fbfef536d752ae2206029583bf39ae0a609747ad199addd634fa6108559d6c5cd39b4c2183f1ab96e07f10d90c6a4f000000800000008000000080220602dab61ff49a14db6a7d02b0cd1fbb78fc4b18312b5b4e54dae4dba2fbfef536d710d90c6a4f0000008000000080010000800001012000c2eb0b0000000017a914b7f5faf40e3d40a5a459b1db3535f2b72fa921e887220203089dc10c7ac6db54f91329af617333db388cead0c231f723379d1b99030b02dc473044022062eb7a556107a7c73f45ac4ab5a1dddf6f7075fb1275969a7f383efff784bcb202200c05dbb7470dbf2f08557dd356c7325c1ed30913e996cd3840945db12228da5f010103040100000001042200208c2353173743b595dfb4a07b72ba8e42e3797da74e87fe7d9d7497e3b2028903010547522103089dc10c7ac6db54f91329af617333db388cead0c231f723379d1b99030b02dc21023add904f3d6dcf59ddb906b0dee23529b7ffb9ed50e5e86151926860221f0e7352ae2206023add904f3d6dcf59ddb906b0dee23529b7ffb9ed50e5e86151926860221f0e7310d90c6a4f000000800000008003000080220603089dc10c7ac6db54f91329af617333db388cead0c231f723379d1b99030b02dc10d90c6a4f00000080000000800200008000220203a9a4c37f5996d3aa25dbac6b570af0650394492942460b354753ed9eeca5877110d90c6a4f000000800000008004000080002202027f6399757d2eff55a136ad02c684b1838b6556e5f1b6b34282a94b6b5005109610d90c6a4f00000080000000800500008000", extkey.Network);
			var tmp = actualPsbt.Clone();
			// Given the above updated PSBT, a signer that supports SIGHASH_ALL for P2PKH and P2WPKH spends and uses RFC6979 for nonce generation and has the following keys:
			actualPsbt.SignWithKeys(extkey.Derive(new KeyPath("m/0'/0'/0'")), extkey.Derive(new KeyPath("m/0'/0'/2'")));
			Assert.Equal(expectedPsbt, actualPsbt);

			actualPsbt = tmp.Clone();
			actualPsbt.SignWithKeys(new BitcoinSecret("cP53pDbR5WtAD8dYAW9hhTjuvvTVaEiQBdrz9XPrgLBeRFiyCbQr", Network.TestNet));
			actualPsbt.SignWithKeys(new BitcoinSecret("cR6SXDoyfQrcp4piaiHE97Rsgta9mNhGTen9XeonVgwsh4iSgw6d", Network.TestNet));
			var part1 = actualPsbt;
			Assert.Equal(expectedPsbt, actualPsbt);

			actualPsbt = tmp.Clone();
			// Given the above updated PSBT, a signer with the following keys:
			expectedPsbt = PSBT.Parse("70736274ff01009a020000000258e87a21b56daf0c23be8e7070456c336f7cbaa5c8757924f545887bb2abdd750000000000ffffffff838d0427d0ec650a68aa46bb0b098aea4422c071b2ca78352a077959d07cea1d0100000000ffffffff0270aaf00800000000160014d85c2b71d0060b09c9886aeb815e50991dda124d00e1f5050000000016001400aea9a2e5f0f876a588df5546e8742d1d87008f00000000000100bb0200000001aad73931018bd25f84ae400b68848be09db706eac2ac18298babee71ab656f8b0000000048473044022058f6fc7c6a33e1b31548d481c826c015bd30135aad42cd67790dab66d2ad243b02204a1ced2604c6735b6393e5b41691dd78b00f0c5942fb9f751856faa938157dba01feffffff0280f0fa020000000017a9140fb9463421696b82c833af241c78c17ddbde493487d0f20a270100000017a91429ca74f8a08f81999428185c97b5d852e4063f618765000000220202dab61ff49a14db6a7d02b0cd1fbb78fc4b18312b5b4e54dae4dba2fbfef536d7483045022100f61038b308dc1da865a34852746f015772934208c6d24454393cd99bdf2217770220056e675a675a6d0a02b85b14e5e29074d8a25a9b5760bea2816f661910a006ea01010304010000000104475221029583bf39ae0a609747ad199addd634fa6108559d6c5cd39b4c2183f1ab96e07f2102dab61ff49a14db6a7d02b0cd1fbb78fc4b18312b5b4e54dae4dba2fbfef536d752ae2206029583bf39ae0a609747ad199addd634fa6108559d6c5cd39b4c2183f1ab96e07f10d90c6a4f000000800000008000000080220602dab61ff49a14db6a7d02b0cd1fbb78fc4b18312b5b4e54dae4dba2fbfef536d710d90c6a4f0000008000000080010000800001012000c2eb0b0000000017a914b7f5faf40e3d40a5a459b1db3535f2b72fa921e8872202023add904f3d6dcf59ddb906b0dee23529b7ffb9ed50e5e86151926860221f0e73473044022065f45ba5998b59a27ffe1a7bed016af1f1f90d54b3aa8f7450aa5f56a25103bd02207f724703ad1edb96680b284b56d4ffcb88f7fb759eabbe08aa30f29b851383d2010103040100000001042200208c2353173743b595dfb4a07b72ba8e42e3797da74e87fe7d9d7497e3b2028903010547522103089dc10c7ac6db54f91329af617333db388cead0c231f723379d1b99030b02dc21023add904f3d6dcf59ddb906b0dee23529b7ffb9ed50e5e86151926860221f0e7352ae2206023add904f3d6dcf59ddb906b0dee23529b7ffb9ed50e5e86151926860221f0e7310d90c6a4f000000800000008003000080220603089dc10c7ac6db54f91329af617333db388cead0c231f723379d1b99030b02dc10d90c6a4f00000080000000800200008000220203a9a4c37f5996d3aa25dbac6b570af0650394492942460b354753ed9eeca5877110d90c6a4f000000800000008004000080002202027f6399757d2eff55a136ad02c684b1838b6556e5f1b6b34282a94b6b5005109610d90c6a4f00000080000000800500008000", extkey.Network);
			actualPsbt.SignWithKeys(new BitcoinSecret("cT7J9YpCwY3AVRFSjN6ukeEeWY6mhpbJPxRaDaP5QTdygQRxP9Au", Network.TestNet));
			actualPsbt.SignWithKeys(new BitcoinSecret("cNBc3SWUip9PPm1GjRoLEJT6T41iNzCYtD7qro84FMnM5zEqeJsE", Network.TestNet));
			var part2 = actualPsbt;
			Assert.Equal(expectedPsbt, actualPsbt);

			// Given both of the above PSBTs, a combiner must create this PSBT:
			expectedPsbt = PSBT.Parse("70736274ff01009a020000000258e87a21b56daf0c23be8e7070456c336f7cbaa5c8757924f545887bb2abdd750000000000ffffffff838d0427d0ec650a68aa46bb0b098aea4422c071b2ca78352a077959d07cea1d0100000000ffffffff0270aaf00800000000160014d85c2b71d0060b09c9886aeb815e50991dda124d00e1f5050000000016001400aea9a2e5f0f876a588df5546e8742d1d87008f00000000000100bb0200000001aad73931018bd25f84ae400b68848be09db706eac2ac18298babee71ab656f8b0000000048473044022058f6fc7c6a33e1b31548d481c826c015bd30135aad42cd67790dab66d2ad243b02204a1ced2604c6735b6393e5b41691dd78b00f0c5942fb9f751856faa938157dba01feffffff0280f0fa020000000017a9140fb9463421696b82c833af241c78c17ddbde493487d0f20a270100000017a91429ca74f8a08f81999428185c97b5d852e4063f6187650000002202029583bf39ae0a609747ad199addd634fa6108559d6c5cd39b4c2183f1ab96e07f473044022074018ad4180097b873323c0015720b3684cc8123891048e7dbcd9b55ad679c99022073d369b740e3eb53dcefa33823c8070514ca55a7dd9544f157c167913261118c01220202dab61ff49a14db6a7d02b0cd1fbb78fc4b18312b5b4e54dae4dba2fbfef536d7483045022100f61038b308dc1da865a34852746f015772934208c6d24454393cd99bdf2217770220056e675a675a6d0a02b85b14e5e29074d8a25a9b5760bea2816f661910a006ea01010304010000000104475221029583bf39ae0a609747ad199addd634fa6108559d6c5cd39b4c2183f1ab96e07f2102dab61ff49a14db6a7d02b0cd1fbb78fc4b18312b5b4e54dae4dba2fbfef536d752ae2206029583bf39ae0a609747ad199addd634fa6108559d6c5cd39b4c2183f1ab96e07f10d90c6a4f000000800000008000000080220602dab61ff49a14db6a7d02b0cd1fbb78fc4b18312b5b4e54dae4dba2fbfef536d710d90c6a4f0000008000000080010000800001012000c2eb0b0000000017a914b7f5faf40e3d40a5a459b1db3535f2b72fa921e887220203089dc10c7ac6db54f91329af617333db388cead0c231f723379d1b99030b02dc473044022062eb7a556107a7c73f45ac4ab5a1dddf6f7075fb1275969a7f383efff784bcb202200c05dbb7470dbf2f08557dd356c7325c1ed30913e996cd3840945db12228da5f012202023add904f3d6dcf59ddb906b0dee23529b7ffb9ed50e5e86151926860221f0e73473044022065f45ba5998b59a27ffe1a7bed016af1f1f90d54b3aa8f7450aa5f56a25103bd02207f724703ad1edb96680b284b56d4ffcb88f7fb759eabbe08aa30f29b851383d2010103040100000001042200208c2353173743b595dfb4a07b72ba8e42e3797da74e87fe7d9d7497e3b2028903010547522103089dc10c7ac6db54f91329af617333db388cead0c231f723379d1b99030b02dc21023add904f3d6dcf59ddb906b0dee23529b7ffb9ed50e5e86151926860221f0e7352ae2206023add904f3d6dcf59ddb906b0dee23529b7ffb9ed50e5e86151926860221f0e7310d90c6a4f000000800000008003000080220603089dc10c7ac6db54f91329af617333db388cead0c231f723379d1b99030b02dc10d90c6a4f00000080000000800200008000220203a9a4c37f5996d3aa25dbac6b570af0650394492942460b354753ed9eeca5877110d90c6a4f000000800000008004000080002202027f6399757d2eff55a136ad02c684b1838b6556e5f1b6b34282a94b6b5005109610d90c6a4f00000080000000800500008000", extkey.Network);
			actualPsbt = part1.Combine(part2);
			Assert.Equal(expectedPsbt, actualPsbt);

			// Given the above PSBT, an input finalizer must create this PSBT:
			expectedPsbt = PSBT.Parse("70736274ff01009a020000000258e87a21b56daf0c23be8e7070456c336f7cbaa5c8757924f545887bb2abdd750000000000ffffffff838d0427d0ec650a68aa46bb0b098aea4422c071b2ca78352a077959d07cea1d0100000000ffffffff0270aaf00800000000160014d85c2b71d0060b09c9886aeb815e50991dda124d00e1f5050000000016001400aea9a2e5f0f876a588df5546e8742d1d87008f00000000000100bb0200000001aad73931018bd25f84ae400b68848be09db706eac2ac18298babee71ab656f8b0000000048473044022058f6fc7c6a33e1b31548d481c826c015bd30135aad42cd67790dab66d2ad243b02204a1ced2604c6735b6393e5b41691dd78b00f0c5942fb9f751856faa938157dba01feffffff0280f0fa020000000017a9140fb9463421696b82c833af241c78c17ddbde493487d0f20a270100000017a91429ca74f8a08f81999428185c97b5d852e4063f6187650000000107da00473044022074018ad4180097b873323c0015720b3684cc8123891048e7dbcd9b55ad679c99022073d369b740e3eb53dcefa33823c8070514ca55a7dd9544f157c167913261118c01483045022100f61038b308dc1da865a34852746f015772934208c6d24454393cd99bdf2217770220056e675a675a6d0a02b85b14e5e29074d8a25a9b5760bea2816f661910a006ea01475221029583bf39ae0a609747ad199addd634fa6108559d6c5cd39b4c2183f1ab96e07f2102dab61ff49a14db6a7d02b0cd1fbb78fc4b18312b5b4e54dae4dba2fbfef536d752ae0001012000c2eb0b0000000017a914b7f5faf40e3d40a5a459b1db3535f2b72fa921e8870107232200208c2353173743b595dfb4a07b72ba8e42e3797da74e87fe7d9d7497e3b20289030108da0400473044022062eb7a556107a7c73f45ac4ab5a1dddf6f7075fb1275969a7f383efff784bcb202200c05dbb7470dbf2f08557dd356c7325c1ed30913e996cd3840945db12228da5f01473044022065f45ba5998b59a27ffe1a7bed016af1f1f90d54b3aa8f7450aa5f56a25103bd02207f724703ad1edb96680b284b56d4ffcb88f7fb759eabbe08aa30f29b851383d20147522103089dc10c7ac6db54f91329af617333db388cead0c231f723379d1b99030b02dc21023add904f3d6dcf59ddb906b0dee23529b7ffb9ed50e5e86151926860221f0e7352ae00220203a9a4c37f5996d3aa25dbac6b570af0650394492942460b354753ed9eeca5877110d90c6a4f000000800000008004000080002202027f6399757d2eff55a136ad02c684b1838b6556e5f1b6b34282a94b6b5005109610d90c6a4f00000080000000800500008000", extkey.Network);
			actualPsbt.Finalize();
			Assert.Equal(expectedPsbt, actualPsbt);

			// Given the above PSBT, a transaction extractor must create this Bitcoin transaction:
			var expectedTx = Transaction.Parse("0200000000010258e87a21b56daf0c23be8e7070456c336f7cbaa5c8757924f545887bb2abdd7500000000da00473044022074018ad4180097b873323c0015720b3684cc8123891048e7dbcd9b55ad679c99022073d369b740e3eb53dcefa33823c8070514ca55a7dd9544f157c167913261118c01483045022100f61038b308dc1da865a34852746f015772934208c6d24454393cd99bdf2217770220056e675a675a6d0a02b85b14e5e29074d8a25a9b5760bea2816f661910a006ea01475221029583bf39ae0a609747ad199addd634fa6108559d6c5cd39b4c2183f1ab96e07f2102dab61ff49a14db6a7d02b0cd1fbb78fc4b18312b5b4e54dae4dba2fbfef536d752aeffffffff838d0427d0ec650a68aa46bb0b098aea4422c071b2ca78352a077959d07cea1d01000000232200208c2353173743b595dfb4a07b72ba8e42e3797da74e87fe7d9d7497e3b2028903ffffffff0270aaf00800000000160014d85c2b71d0060b09c9886aeb815e50991dda124d00e1f5050000000016001400aea9a2e5f0f876a588df5546e8742d1d87008f000400473044022062eb7a556107a7c73f45ac4ab5a1dddf6f7075fb1275969a7f383efff784bcb202200c05dbb7470dbf2f08557dd356c7325c1ed30913e996cd3840945db12228da5f01473044022065f45ba5998b59a27ffe1a7bed016af1f1f90d54b3aa8f7450aa5f56a25103bd02207f724703ad1edb96680b284b56d4ffcb88f7fb759eabbe08aa30f29b851383d20147522103089dc10c7ac6db54f91329af617333db388cead0c231f723379d1b99030b02dc21023add904f3d6dcf59ddb906b0dee23529b7ffb9ed50e5e86151926860221f0e7352ae00000000", extkey.Network);
			Assert.True(actualPsbt.CanExtractTransaction());
			var actualTx = actualPsbt.ExtractTransaction().ToHex();
			Assert.Equal(expectedTx.ToHex(), actualTx);

			// Given these two PSBTs with unknown key-value pairs:
			var psbt1 = PSBT.Parse("70736274ff01003f0200000001ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff0000000000ffffffff010000000000000000036a0100000000000a0f0102030405060708090f0102030405060708090a0b0c0d0e0f000a0f0102030405060708090f0102030405060708090a0b0c0d0e0f000a0f0102030405060708090f0102030405060708090a0b0c0d0e0f00", extkey.Network);
			var psbt2 = PSBT.Parse("70736274ff01003f0200000001ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff0000000000ffffffff010000000000000000036a0100000000000a0f0102030405060708100f0102030405060708090a0b0c0d0e0f000a0f0102030405060708100f0102030405060708090a0b0c0d0e0f000a0f0102030405060708100f0102030405060708090a0b0c0d0e0f00", extkey.Network);
			// A combiner which orders keys lexicographically must produce the following PSBT:
			expectedPsbt = PSBT.Parse("70736274ff01003f0200000001ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff0000000000ffffffff010000000000000000036a0100000000000a0f0102030405060708090f0102030405060708090a0b0c0d0e0f0a0f0102030405060708100f0102030405060708090a0b0c0d0e0f000a0f0102030405060708090f0102030405060708090a0b0c0d0e0f0a0f0102030405060708100f0102030405060708090a0b0c0d0e0f000a0f0102030405060708090f0102030405060708090a0b0c0d0e0f0a0f0102030405060708100f0102030405060708090a0b0c0d0e0f00", extkey.Network);
			actualPsbt = psbt1.Combine(psbt2);
			Assert.Equal(expectedPsbt, actualPsbt);
		}

		static internal ICoin[] DummyFundsToCoins(IEnumerable<Transaction> txs, Script redeem, BitcoinSecret key)
		{
			var barecoins = txs.SelectMany(tx => tx.Outputs.AsCoins()).ToArray();
			var coins = new ICoin[barecoins.Length];
			coins[0] = barecoins[0];
			coins[1] = barecoins[1];
			coins[2] = redeem != null ? new ScriptCoin(barecoins[2], redeem) : barecoins[2]; // p2sh
			coins[3] = redeem != null ? new ScriptCoin(barecoins[3], redeem) : barecoins[3]; // p2wsh
			coins[4] = key != null ? new ScriptCoin(barecoins[4], key.PubKey.WitHash.ScriptPubKey) : barecoins[4]; // p2sh-p2wpkh
			coins[5] = redeem != null ? new ScriptCoin(barecoins[5], redeem) : barecoins[5]; // p2sh-p2wsh
			return coins;
		}

		static internal Transaction CreateTxToSpendFunds(
				Transaction[] funds,
				BitcoinSecret[] keys,
				Script redeem,
				bool withScript,
				bool sign
			)
		{
			var tx = Network.Main.CreateTransaction();
			tx.Inputs.Add(new OutPoint(funds[0].GetHash(), 0)); // p2pkh
			tx.Inputs.Add(new OutPoint(funds[0].GetHash(), 1)); // p2wpkh
			tx.Inputs.Add(new OutPoint(funds[1].GetHash(), 0)); // p2sh
			tx.Inputs.Add(new OutPoint(funds[2].GetHash(), 0)); // p2wsh
			tx.Inputs.Add(new OutPoint(funds[3].GetHash(), 0)); // p2sh-p2wpkh
			tx.Inputs.Add(new OutPoint(funds[4].GetHash(), 0)); // p2sh-p2wsh

			var dummyOut = new TxOut(Money.Coins(0.599m), keys[0]);
			tx.Outputs.Add(dummyOut);

			if (withScript)
			{
				// OP_0 + three empty signatures
				var emptySigPush = new Script(OpcodeType.OP_0, OpcodeType.OP_0, OpcodeType.OP_0, OpcodeType.OP_0);
				tx.Inputs[0].ScriptSig = PayToPubkeyHashTemplate.Instance.GenerateScriptSig(null, keys[0].PubKey);
				tx.Inputs[1].WitScript = PayToWitPubKeyHashTemplate.Instance.GenerateWitScript(null, keys[0].PubKey);
				tx.Inputs[2].ScriptSig = emptySigPush + Op.GetPushOp(redeem.ToBytes());
				tx.Inputs[3].WitScript = PayToWitScriptHashTemplate.Instance.GenerateWitScript(emptySigPush, redeem);
				tx.Inputs[4].ScriptSig = new Script(Op.GetPushOp(keys[0].PubKey.WitHash.ScriptPubKey.ToBytes()));
				tx.Inputs[4].WitScript = PayToWitPubKeyHashTemplate.Instance.GenerateWitScript(null, keys[0].PubKey);
				tx.Inputs[5].ScriptSig = new Script(Op.GetPushOp(redeem.WitHash.ScriptPubKey.ToBytes()));
				tx.Inputs[5].WitScript = PayToWitScriptHashTemplate.Instance.GenerateWitScript(emptySigPush, redeem);
			}

			if (sign)
			{
				tx.Sign(keys, DummyFundsToCoins(funds, redeem, keys[0]));
			}
			return tx;
		}

		static internal Transaction[] CreateDummyFunds(Network network, BitcoinSecret[] keyForOutput, Script redeem)
		{
			// 1. p2pkh and p2wpkh
			var tx1 = network.CreateTransaction();
			tx1.Inputs.Add(TxIn.CreateCoinbase(200));
			tx1.Outputs.Add(new TxOut(Money.Coins(0.1m), keyForOutput[0].PubKey.Hash));
			tx1.Outputs.Add(new TxOut(Money.Coins(0.1m), keyForOutput[0].PubKey.WitHash));

			// 2. p2sh-multisig
			var tx2 = network.CreateTransaction();
			tx2.Inputs.Add(TxIn.CreateCoinbase(200));
			tx2.Outputs.Add(new TxOut(Money.Coins(0.1m), redeem.Hash));

			// 3. p2wsh
			var tx3 = network.CreateTransaction();
			tx3.Inputs.Add(TxIn.CreateCoinbase(200));
			tx3.Outputs.Add(new TxOut(Money.Coins(0.1m), redeem.WitHash));

			// 4. p2sh-p2wpkh
			var tx4 = network.CreateTransaction();
			tx4.Inputs.Add(TxIn.CreateCoinbase(200));
			tx4.Outputs.Add(new TxOut(Money.Coins(0.1m), keyForOutput[0].PubKey.WitHash.ScriptPubKey.Hash));

			// 5. p2sh-p2wsh
			var tx5 = network.CreateTransaction();
			tx5.Inputs.Add(TxIn.CreateCoinbase(200));
			tx5.Outputs.Add(new TxOut(Money.Coins(0.1m), redeem.WitHash.ScriptPubKey.Hash.ScriptPubKey));
			return new Transaction[] { tx1, tx2, tx3, tx4, tx5 };
		}

	}
}
