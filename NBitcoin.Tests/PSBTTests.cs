using NBitcoin.BIP174;
using Xunit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System;
using NBitcoin.DataEncoders;
using System.Collections.Generic;
using System.Linq;

namespace NBitcoin.Tests
{
	public class PSBTTests
	{
		private static JObject testdata { get; }
		static PSBTTests()
		{

			testdata = JObject.Parse(File.ReadAllText("data/psbt.json"));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public static void ShouldThrowExceptionForInvalidData()
		{
			JArray invalidTestCases = (JArray)testdata["invalid"];
			foreach (string i in invalidTestCases)
			{
				Assert.Throws<FormatException>(() => PSBT.Parse(i));
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public static void ShouldParseValidDataDeterministically()
		{
			JArray validTestCases = (JArray)testdata["valid"];
			foreach (string i in validTestCases)
			{
				var psbt = PSBT.Parse(i);
				var psbtBase64 = Encoders.Base64.EncodeData(psbt.ToBytes());
				var psbt2 = PSBT.Parse(psbtBase64);
				Assert.Equal(psbt, psbt2, new PSBTComparer());
			}
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ShouldPreserveOriginalTxPropertyAsPossible()
		{
			var keys = new Key[] { new Key(), new Key(), new Key() };
			var redeem = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(3, keys.Select(k => k.PubKey).ToArray());
			var network = Network.Main;
			var funds = CreateDummyFunds(network, keys, redeem);

			// 1. without signature nor scripts.
			var tx = CreateTxToSpendFunds(funds, keys, redeem, false, false);

			// 2. with (unsigned) scriptSig and witness.
			tx = CreateTxToSpendFunds(funds, keys, redeem, true, false);
			var psbt = new PSBT(tx);
			Assert.Null(psbt.inputs[0].FinalScriptSig); // it is not finalized since it is not signed
			Assert.Null(psbt.inputs[1].FinalScriptWitness); // This too
			Assert.NotNull(psbt.inputs[2].RedeemScript); // But it holds redeem script.
			Assert.NotNull(psbt.inputs[3].WitnessScript); // And witness script.
			Assert.NotNull(psbt.inputs[5].WitnessScript); // even in p2sh-nested-p2wsh
			Assert.NotNull(psbt.inputs[5].RedeemScript);

			// 3. with finalized scriptSig and witness
			tx = CreateTxToSpendFunds(funds, keys, redeem, true, true);
			psbt = new PSBT(tx);
			Assert.NotNull(psbt.inputs[0].FinalScriptSig); // it should be finalized
			Assert.NotNull(psbt.inputs[1].FinalScriptWitness); // p2wpkh too
			Assert.NotNull(psbt.inputs[2].RedeemScript); // But it holds redeem script.
			Assert.NotNull(psbt.inputs[3].WitnessScript); // And witness script.

			Assert.NotNull(psbt.inputs[4].FinalScriptSig); // Same principle holds for p2sh-nested version.
			Assert.NotNull(psbt.inputs[4].FinalScriptWitness);
			Assert.NotNull(psbt.inputs[5].WitnessScript);
			Assert.NotNull(psbt.inputs[5].RedeemScript);

			Assert.Empty(psbt.inputs[2].PartialSigs); // But it still can not hold partial_sigs
			Assert.Empty(psbt.inputs[3].PartialSigs); // Even in p2wsh
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanUpdate()
		{
			var network = Network.Main;
			var alice = Key.Parse("L23Ng7B8iXTcQ9emwDYpabUJVsxDQDxKwePrTwAZo1VT9xcDPfBF", network);
			var bob = Key.Parse("L2nRrbzZytXSTjn95a4droGrAj5uwSEeG3JUHeNwdB9pUHu8Znjo", network);
			var carol = Key.Parse("KzHNhJn4P3FML22cQ9yr6rc35hmTPwVmaCnXkc114fQ8ZKRR9hoK", network);
			var keys = new Key[]{ alice, bob, carol };
			var redeem = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(3, keys.Select(k => k.PubKey).ToArray());
			var funds = CreateDummyFunds(network, keys, redeem);

			var tx = CreateTxToSpendFunds(funds, keys, redeem, true, false);
			var coins = DummyFundsToCoins(funds, redeem, alice);
			var PSBTWithCoins = PSBT.FromTransaction(tx)
				.AddCoins(coins);

			Assert.Null(PSBTWithCoins.inputs[0].WitnessUtxo);
			Assert.NotNull(PSBTWithCoins.inputs[1].WitnessUtxo);
			Assert.Null(PSBTWithCoins.inputs[2].WitnessUtxo);
			Assert.NotNull(PSBTWithCoins.inputs[3].WitnessUtxo);
			Assert.NotNull(PSBTWithCoins.inputs[4].WitnessUtxo);
			Assert.NotNull(PSBTWithCoins.inputs[5].WitnessUtxo);

			// Check if it holds scripts as expected.
			Assert.Null(PSBTWithCoins.inputs[0].RedeemScript); // p2pkh
			Assert.Null(PSBTWithCoins.inputs[0].WitnessScript); // p2pkh
			Assert.Null(PSBTWithCoins.inputs[1].WitnessScript); // p2wpkh
			Assert.NotNull(PSBTWithCoins.inputs[2].RedeemScript); // p2sh
			Assert.NotNull(PSBTWithCoins.inputs[4].RedeemScript); // p2sh-p2wpkh
			Assert.NotNull(PSBTWithCoins.inputs[5].RedeemScript); // p2sh-p2wsh
			Assert.NotNull(PSBTWithCoins.inputs[3].WitnessScript); // p2wsh
			Assert.NotNull(PSBTWithCoins.inputs[5].WitnessScript); // p2sh-p2wsh

			var SignedPSBTWithCoins = PSBTWithCoins
				.TrySignAll(alice);
			Assert.Empty(SignedPSBTWithCoins.inputs[0].PartialSigs); // can not sign for non segwit input without non-witness UTXO
			Assert.Empty(SignedPSBTWithCoins.inputs[2].PartialSigs); // This too.
			// otherwise, It will increase Partial sigs count.
			Assert.Single(SignedPSBTWithCoins.inputs[1].PartialSigs);
			Assert.Single(SignedPSBTWithCoins.inputs[3].PartialSigs);
			Assert.Single(SignedPSBTWithCoins.inputs[4].PartialSigs);
			Assert.Single(SignedPSBTWithCoins.inputs[5].PartialSigs);
			SignedPSBTWithCoins.TryFinalize(out bool HasFinalizationSucceedForPSBTWithoutPrevTX);
			Assert.False(HasFinalizationSucceedForPSBTWithoutPrevTX);

			var PSBTWithTXs = PSBT.FromTransaction(tx)
				.AddTransactions(funds);
			Assert.Null(PSBTWithTXs.inputs[0].WitnessUtxo);
			Assert.NotNull(PSBTWithTXs.inputs[0].NonWitnessUtxo);
			Assert.NotNull(PSBTWithTXs.inputs[1].WitnessUtxo);
			Assert.Null(PSBTWithTXs.inputs[2].WitnessUtxo);
			Assert.NotNull(PSBTWithTXs.inputs[2].NonWitnessUtxo);
			Assert.NotNull(PSBTWithTXs.inputs[3].WitnessUtxo);
			Assert.NotNull(PSBTWithTXs.inputs[4].WitnessUtxo);
			Assert.NotNull(PSBTWithTXs.inputs[5].WitnessUtxo);

			var SignedPSBTWithTXs = PSBTWithTXs
				.TrySignAll(keys);

			// must sign only once for whole kinds of non-multisig tx.
			Assert.Single(SignedPSBTWithTXs.inputs[0].PartialSigs);
			Assert.Single(SignedPSBTWithTXs.inputs[1].PartialSigs);
			Assert.Single(SignedPSBTWithTXs.inputs[4].PartialSigs);
			// for multisig
			Assert.Equal(3, SignedPSBTWithTXs.inputs[2].PartialSigs.Count);
			Assert.Equal(3, SignedPSBTWithTXs.inputs[2].PartialSigs.Values.Select(v => v.Item2).Distinct().Count());
			Assert.Equal(3, SignedPSBTWithTXs.inputs[3].PartialSigs.Count);
			Assert.Equal(3, SignedPSBTWithTXs.inputs[3].PartialSigs.Values.Select(v => v.Item2).Distinct().Count());
			Assert.Equal(3, SignedPSBTWithTXs.inputs[5].PartialSigs.Count);
			Assert.Equal(3, SignedPSBTWithTXs.inputs[5].PartialSigs.Values.Select(v => v.Item2).Distinct().Count());

			Assert.False(SignedPSBTWithTXs.CanExtractTX());

			var FinalizedPSBT = SignedPSBTWithTXs.Finalize();
			Assert.True(FinalizedPSBT.CanExtractTX());

			var finalTX = FinalizedPSBT.ExtractTX();
			var result = finalTX.Check();
			Assert.Equal(TransactionCheckResult.Success, result);

			var builder = network.CreateTransactionBuilder();
			builder.AddCoins(coins).AddKeys(keys);
			if (!builder.Verify(finalTX, (Money)null, out var errors))
				throw new InvalidOperationException(errors.Aggregate(string.Empty, (a, b) => a + ";\n" + b));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ShouldPairSignaturesAndPubkeyInOriginalTXWhenCoinsAreAdd()
		{}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ShouldFailToSignForTestcaseInvalidForSigner()
		{
			JArray testcases = (JArray)testdata["invalidForSigners"];
			foreach (string i in testcases)
			{
				var psbt = PSBT.Parse(i);
				Assert.Throws<FormatException>(() => psbt.TrySignAll(new Key()));
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ShouldPassTheLongestTestInBIP174()
		{
			JObject testcase = (JObject)testdata["final"];
			var network = Network.TestNet;
			var master = ExtKey.Parse((string)testcase["master"], network);
			var masterFP = BitConverter.ToUInt32(master.PrivateKey.PubKey.Hash.ToBytes().SafeSubarray(0, 4), 0);
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

			var expected = PSBT.Parse((string)testcase["psbt1"]);

			var psbt = PSBT.FromTransaction(tx);
			Assert.Equal(expected, psbt, new PSBTComparer());

			var prevtx1 = Transaction.Parse((string)testcase["prevtx1"], network);
			var prevtx2 = Transaction.Parse((string)testcase["prevtx2"], network);
			psbt.AddTransactions(prevtx1, prevtx2);
			var redeem1 = Script.FromBytesUnsafe(Encoders.Hex.DecodeData((string)testcase["redeem1"]));
			var redeem2 = Script.FromBytesUnsafe(Encoders.Hex.DecodeData((string)testcase["redeem2"]));
			var witness_script1 = Script.FromBytesUnsafe(Encoders.Hex.DecodeData((string)testcase["witness1"]));
			foreach (var sc in new Script[] {redeem1, redeem2, witness_script1})
				psbt.TryAddScript(sc);

			for (int i = 0; i < 6; i++)
			{
				var pk = testcase[$"pubkey{i}"];
				var pubkey = new PubKey((string)pk["hex"]);
				var path = KeyPath.Parse((string)pk["path"]);
				psbt.TryAddKeyPath(pubkey, Tuple.Create(masterFP, path));
			}

			expected = PSBT.Parse((string)testcase["psbt2"]);
			Assert.Equal(expected, psbt, new PSBTComparer());

			foreach(var psbtin in psbt.inputs)
				psbtin.SighashType = SigHash.All;
			expected = PSBT.Parse((string)testcase["psbt3"]);
			Assert.Equal(expected, psbt, new PSBTComparer());

			psbt.CheckSanity();
			var psbtForBob = psbt.Clone();

			// path 1 ... alice
			Assert.Equal(psbt, psbtForBob, new PSBTComparer());
			var aliceKey1 = master.Derive(new KeyPath((string)testcase["key7"]["path"])).PrivateKey;
			var aliceKey2 = master.Derive(new KeyPath((string)testcase["key8"]["path"])).PrivateKey;
			psbt.TrySignAll(aliceKey1, aliceKey2);
			expected = PSBT.Parse((string)testcase["psbt4"]);
			Assert.Equal(expected, psbt);

			// path 2 ... bob.
			var bobKey1 = master.Derive(new KeyPath((string)testcase["key9"]["path"])).PrivateKey;
			var bobKey2 = master.Derive(new KeyPath((string)testcase["key10"]["path"])).PrivateKey;
			var bobKeyhex1 = (string)testcase["key9"]["wif"];
			var bobKeyhex2 = (string)testcase["key10"]["wif"];
			Assert.Equal(bobKey1, new BitcoinSecret(bobKeyhex1, network).PrivateKey);
			Assert.Equal(bobKey2, new BitcoinSecret(bobKeyhex2, network).PrivateKey);
			psbtForBob.TrySignAll(bobKey1, bobKey2);
			// from here, we are using (slightly modified by us) hex representation of psbt for expected data.
			// TODO: check compatibility against updated test vector in the bip when it has been updated.
			expected = PSBT.Parse((string)testcase["psbt5"]);
			Assert.Equal(expected, psbtForBob);

			// merge above 2
			var combined = psbt.Combine(psbtForBob);
			expected = PSBT.Parse((string)testcase["psbtcombined"]);
			Assert.Equal(expected, combined);
		}

		private class PSBTComparer : EqualityComparer<PSBT>
		{
			public override bool Equals(PSBT a, PSBT b) => a.Equals(b);
			public override int GetHashCode(PSBT psbt) => psbt.GetHashCode();
		}


		private ICoin[] DummyFundsToCoins(IEnumerable<Transaction> txs, Script redeem, Key key)
		{
			var barecoins = txs.SelectMany(tx => tx.Outputs.AsCoins()).ToArray();
			var coins = new ICoin[barecoins.Length];
			coins[0] = barecoins[0];
			coins[1] = barecoins[1];
			coins[2] = new ScriptCoin(barecoins[2], redeem); // p2sh
			coins[3] = new ScriptCoin(barecoins[3], redeem); // p2wsh
			coins[4] = new ScriptCoin(barecoins[4], key.PubKey.WitHash.ScriptPubKey); // p2sh-p2wpkh
			coins[5] = new ScriptCoin(barecoins[5], redeem); // p2sh-p2wsh
			return coins;
		}

		private Transaction CreateTxToSpendFunds(
				Transaction[] funds,
				Key[] keys,
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

		private Transaction[] CreateDummyFunds(Network network, Key[] keyForOutput, Script redeem)
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
