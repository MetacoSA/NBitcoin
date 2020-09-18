using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NBitcoin.Crypto;
using Xunit;

namespace NBitcoin.Tests.Generators
{
	using HDKeyPathKVMap = Dictionary<PubKey, Tuple<HDFingerprint, KeyPath>>;
	using PartialSigKVMap = Dictionary<KeyId, Tuple<PubKey, ECDSASignature>>;
	using UnknownKVMap = Dictionary<byte[], byte[]>;
	public class PSBTGenerator
	{
		public static Arbitrary<PSBT> PSBTArb() => Arb.From(SanePSBT());

		#region PSBTInput

		public static Gen<Dictionary<KeyId, Tuple<PubKey, ECDSASignature>>> PartialSigs() =>
								from itemNum in Gen.Choose(0, 15)
								from sigs in CryptoGenerator.ECDSAs(itemNum)
								from pks in CryptoGenerator.PublicKeys(itemNum)
								let items = pks.ToList().Zip(sigs.ToList(), (pk, sig) => Tuple.Create(pk, sig)).ToList()
								select Utils.DictionaryFromList<KeyId, Tuple<PubKey, ECDSASignature>>(pks.Select(pk => pk.Hash).ToList(), items);

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
			from inputN in Gen.Choose(0, 8)
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
			let psbt = tx.CreatePSBT(network)
				.AddTransactions(prevTxs.ToArray())
				.AddCoins(CoinsToAdd.ToArray())
				.AddScripts(scriptsToAdd.ToArray())
			select psbt;
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

		private static Transaction ReplaceTxOut(Transaction tx, TxOut txout, int index)
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
				from MasterKeyFingerPrints in Gen.ListOf(itemNum, Utils.HDFingerprint())
				from paths in Gen.ListOf(itemNum, CryptoGenerator.KeyPath())
				let fingerPrintAndPath = MasterKeyFingerPrints.ToArray().Zip(paths.ToArray(), (m, p) => Tuple.Create(m, p)).ToList()
				select Utils.DictionaryFromList<PubKey, Tuple<HDFingerprint, KeyPath>>(pks.ToList(), fingerPrintAndPath);
	}
}
