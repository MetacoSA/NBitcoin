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

			// 1. should preserve redeem and witScript if not signed (and non_witness_utxo only in case of witness output.)
			var tx = CompleteTransaction(network.CreateTransaction(), funds, keys, redeem, false);
			var psbt = new PSBT(tx);
			Assert.Null(psbt.inputs[0].FinalScriptSig); // it is not finalized since it is not signed
			Assert.Null(psbt.inputs[1].FinalScriptWitness); // This too
			Assert.NotNull(psbt.inputs[2].RedeemScript); // But it holds redeem script.
			Assert.NotNull(psbt.inputs[3].WitnessScript); // And witness script.
			Assert.NotNull(psbt.inputs[5].WitnessScript); // even in p2sh-nested-p2wsh
			Assert.NotNull(psbt.inputs[5].RedeemScript);

			tx = CompleteTransaction(network.CreateTransaction(), funds, keys, redeem, true);
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
			var alice = new Key();
			var bob = new Key();
			var carol = new Key();
			var keys = new Key[]{ alice, bob, carol };
			var redeem = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, keys.Select(k => k.PubKey).ToArray());
			var network = Network.Main;
			var funds = CreateDummyFunds(network, keys, redeem);

			var tx = CompleteTransaction(network.CreateTransaction(), funds, keys, redeem, false);
			var PSBTWithCoins = PSBT.FromTransaction(tx)
				.AddCoins(DummyFundsToCoins(funds, redeem, alice));

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
			foreach (var psbtin in PSBTWithTXs.inputs)
			{
				Assert.Null(psbtin.WitnessUtxo);
				Assert.NotNull(psbtin.NonWitnessUtxo);
			}
			var SignedPSBTWithTXs = PSBTWithTXs
				.TrySignAll(keys);

			// must sign only once for whole kinds of non-multisig tx.
			Assert.Single(SignedPSBTWithTXs.inputs[0].PartialSigs);
			Assert.Single(SignedPSBTWithTXs.inputs[1].PartialSigs);
			Assert.Single(SignedPSBTWithTXs.inputs[4].PartialSigs);
			// for multisig
			Assert.Equal(3, SignedPSBTWithTXs.inputs[2].PartialSigs.Count);
			Assert.Equal(3, SignedPSBTWithTXs.inputs[3].PartialSigs.Count);
			Assert.Equal(3, SignedPSBTWithTXs.inputs[5].PartialSigs.Count);

			Assert.False(SignedPSBTWithTXs.CanExtractTX());

			var FinalizedPSBT = SignedPSBTWithTXs.Finalize();
			Assert.True(FinalizedPSBT.CanExtractTX());

			var finalTX = FinalizedPSBT.ExtractTX();
			var result = finalTX.Check();
			Assert.Equal(TransactionCheckResult.Success, result);
			// TODO: Contextual check for tx.
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ShouldPreserveSignatureInOriginalTX()
		{}

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

		private Transaction CompleteTransaction(
				Transaction tx,
				Transaction[] funds,
				Key[] keys,
				Script redeem,
				bool sign
			)
		{
			tx.Inputs.Add(new OutPoint(funds[0].GetHash(), 0)); // p2pkh
			tx.Inputs.Add(new OutPoint(funds[0].GetHash(), 1), Script.Empty, p2wpkhWit); // p2wpkh
			tx.Inputs.Add(new OutPoint(funds[1].GetHash(), 0), new Script(Op.GetPushOp(redeem.ToBytes()))); // p2sh
			tx.Inputs.Add(new OutPoint(funds[2].GetHash(), 0), Script.Empty, p2shWit); // p2wsh
			tx.Inputs.Add(new OutPoint(funds[3].GetHash(), 0), new Script(Op.GetPushOp(keys[0].PubKey.WitHash.ScriptPubKey.ToBytes())), p2wpkhWit); // p2sh-p2wpkh
			tx.Inputs.Add(new OutPoint(funds[4].GetHash(), 0), new Script(Op.GetPushOp(redeem.WitHash.ScriptPubKey.ToBytes())), p2shWit); // p2sh-p2wsh

			var dummyOut = new TxOut(Money.Coins(0.1m), keys[0]);
			tx.Outputs.Add(dummyOut);

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
		public void ShouldPassLongestTestCaseInBIP()
		{
		}

		private class PSBTComparer : IEqualityComparer<PSBT>
		{
			public bool Equals(PSBT a, PSBT b) => a.Equals(b);
			public int GetHashCode(PSBT psbt) => psbt.GetHashCode();
		}

	}
}
