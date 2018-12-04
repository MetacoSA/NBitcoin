using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NBitcoin.BIP174;
using NBitcoin.Crypto;
using Xunit;

namespace NBitcoin.Tests.Generators
{
	using HDKeyPathKVMap = Dictionary<PubKey, Tuple<uint, KeyPath>>;
	using PartialSigKVMap = Dictionary<KeyId, Tuple<PubKey, ECDSASignature>>;
	using UnknownKVMap = Dictionary<byte[], byte[]>;
	public class PSBTGenerator
	{
		public static Arbitrary<PSBTInput> PSBTInputArb() => Arb.From(PSBTInput());
		public static Arbitrary<PSBTOutput> PSBTOutputArb() => Arb.From(PSBTOutput());
		public static Arbitrary<PSBT> PSBTArb() => Arb.From(SanePSBT());
		#region PSBTInput

		public static Gen<PSBTInput> PSBTInput() => Gen.OneOf(PSBTInputFinal(), PSBTInputNonFinal());
		public static Gen<PSBTInput> PSBTInputNonFinal() =>
			from nonWitnessUtxo in SegwitTransactionGenerators.TX()
			from witnessUtxo in LegacyTransactionGenerators.Output()
			from redeem in ScriptGenerator.RandomScriptSig()
			from witnessS in ScriptGenerator.RandomScriptSig()
			from hdKeyPaths in HDKeyPaths()
			from partialSigs in PartialSigs()
			from unknown in UnknownKVMap()
			select ComposePSBTInput(new PSBTInput()
			{
				NonWitnessUtxo = nonWitnessUtxo,
				WitnessUtxo = witnessUtxo,
				RedeemScript = redeem,
				WitnessScript = witnessS,
			}, hdKeyPaths, partialSigs, unknown);

		public static Gen<PSBTInput> PSBTInputFinal() =>
			from finalScriptSig in ScriptGenerator.RandomScriptSig()
			from finalScriptWitness in ScriptGenerator.RandomWitScript()
			select new PSBTInput()
			{
				FinalScriptSig = finalScriptSig,
				FinalScriptWitness = finalScriptWitness,
			};

		private static PSBTInput ComposePSBTInput(
						PSBTInput psbtin, HDKeyPathKVMap hdKeyPaths,
						PartialSigKVMap partialSigs,
						UnknownKVMap unknown
						)
		{
			foreach (var item in hdKeyPaths)
			{
				psbtin.HDKeyPaths.Add(item.Key, item.Value);
			}

			foreach (var item in partialSigs)
			{
				psbtin.PartialSigs.Add(item.Key, item.Value);
			}
			foreach (var item in unknown)
			{
				psbtin.Unknown.Add(item.Key, item.Value);
			}
			return psbtin;
		}
		public static Gen<Dictionary<KeyId, Tuple<PubKey, ECDSASignature>>> PartialSigs() =>
								from itemNum in Gen.Choose(0, 15)
								from sigs in CryptoGenerator.ECDSAs(itemNum)
								from pks in CryptoGenerator.PublicKeys(itemNum)
								let items = pks.ToList().Zip(sigs.ToList(), (pk, sig) => Tuple.Create(pk, sig)).ToList()
								select Utils.DictionaryFromList<KeyId, Tuple<PubKey, ECDSASignature>>(pks.Select(pk => pk.Hash).ToList(), items);

		#endregion

		#region PSBTOutput
		public static Gen<PSBTOutput> PSBTOutput() =>
				from redeem in ScriptGenerator.RandomScriptSig()
				from witnessS in ScriptGenerator.RandomScriptSig()
				from keyPaths in HDKeyPaths()
				from unknown in UnknownKVMap()
				select ComposePSBTOutput(new PSBTOutput() { RedeemScript = redeem, WitnessScript = witnessS }, keyPaths, unknown);

		private static PSBTOutput ComposePSBTOutput(PSBTOutput output, HDKeyPathKVMap keyPaths, UnknownKVMap unknown)
		{
			foreach (var item in keyPaths)
			{
				output.HDKeyPaths.Add(item.Key, item.Value);
			}
			foreach (var item in unknown)
			{
				output.Unknown.Add(item.Key, item.Value);
			}

			return output;
		}


		#endregion
		#region PSBT


		public static Gen<PSBT> SanePSBT() =>
			from network in ChainParamsGenerator.NetworkGen()
			from psbt in SanePSBT(network)
			select psbt;

		/// <summary>
		/// This is slow, provably because `Add*` methods will iterate over inputs.
		/// </summary>
		/// <param name="network"></param>
		/// <returns></returns>
		public static Gen<PSBT> SanePSBT(Network network) =>
			from inputN in Gen.Choose(0, 15)
			from scripts in Gen.ListOf(inputN, ScriptGenerator.RandomScriptSig())
			from txOuts in Gen.Sequence(scripts.Select(sc => OutputFromRedeem(sc)))
			from prevN in Gen.Choose(0, 5)
			from prevTxs in Gen.Sequence(txOuts.Select(o => TXFromOutput(o, network, prevN)))
			let txins = prevTxs.Select(tx => new TxIn(new OutPoint(tx.GetHash(), prevN)))
			from locktime in PrimitiveGenerator.UInt32()
			let tx = LegacyTransactionGenerators.ComposeTx(network.CreateTransaction(), txins.ToList(), txOuts.ToList(), locktime)
			from TxsToAdd in Gen.SubListOf(prevTxs)
			from CoinsToAdd in Gen.SubListOf(prevTxs.SelectMany(tx => tx.Outputs.AsCoins()))
			from scriptsToAdd in Gen.SubListOf<Script>(scripts)
			let psbt = PSBT
				.FromTransaction(tx)
				.AddTransactions(prevTxs.ToArray())
				.AddCoins(CoinsToAdd.ToArray())
				.TryAddScript(scriptsToAdd.ToArray())
			select psbt;
		public static Gen<PSBT> PSBTGen(Transaction tx) =>
			from psbtins in Gen.ListOf(tx.Inputs.Count, PSBTInput())
			from psbtouts in Gen.ListOf(tx.Outputs.Count, PSBTOutput())
			select new PSBT(tx) { outputs = new PSBTOutputList(psbtouts.ToList()), inputs = new PSBTInputList(psbtins.ToList()) };

		private static Gen<TxOut> OutputFromRedeem(Script sc) =>
			from money in MoneyGenerator.Money()
			from isP2WSH in PrimitiveGenerator.Bool()
			from isP2SH in PrimitiveGenerator.Bool()
			where isP2WSH || isP2SH
			let redeem = (isP2SH && isP2WSH) ? sc.WitHash.ScriptPubKey : sc
			let scriptPubKey = isP2SH ? redeem.Hash.ScriptPubKey : redeem.WitHash.ScriptPubKey
			select new TxOut(money, scriptPubKey);

		private static Gen<Transaction> TXFromOutput(TxOut txout, Network network, int vout) =>
			from outNum in Gen.Choose(0, 4)
			from tx in SegwitTransactionGenerators.TX(network)
			where tx.Outputs.Count > vout
			select ReplaceTxOut(tx, txout, vout);

		private static Transaction ReplaceTxOut (Transaction tx, TxOut txout, int index)
		{
			tx.Outputs[index] = txout;
			return tx;
		}

		#endregion
		public static Gen<UnknownKVMap> UnknownKVMap() =>
				from itemNum in Gen.Choose(0, 15)
				from keys in Gen.ListOf(itemNum, PrimitiveGenerator.RandomBytes())
				where PSBTConstants.PSBT_IN_ALL.All(i => keys.Select(k => k.First()).All(k => k != i))
				let uniqKeys = keys.Distinct()
				from values in Gen.ListOf(uniqKeys.Count(), PrimitiveGenerator.RandomBytes())
				select Utils.DictionaryFromList<byte[], byte[]>(uniqKeys.ToList(), values.ToList());

		public static Gen<HDKeyPathKVMap> HDKeyPaths() =>
				from itemNum in Gen.Choose(0, 15)
				from pks in Gen.ListOf(itemNum, CryptoGenerator.PublicKey())
				from map in HDKeyPaths(pks, itemNum)
				select map;

		public static Gen<HDKeyPathKVMap> HDKeyPaths(IEnumerable<PubKey> pks, int itemNum) =>
				from MasterKeyFingerPrints in Gen.ListOf(itemNum, PrimitiveGenerator.UInt32())
				from paths in Gen.ListOf(itemNum, CryptoGenerator.KeyPath())
				let fingerPrintAndPath = MasterKeyFingerPrints.ToArray().Zip(paths.ToArray(), (m, p) => Tuple.Create(m, p)).ToList()
				select Utils.DictionaryFromList<PubKey, Tuple<uint, KeyPath>>(pks.ToList(), fingerPrintAndPath);
	}
}