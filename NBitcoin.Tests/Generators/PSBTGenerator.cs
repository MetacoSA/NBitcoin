using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NBitcoin.BIP174;
using NBitcoin.Crypto;
using Xunit;

namespace NBitcoin.Tests.Generators
{
	using HDKeyPathKVMap = Dictionary<PubKey, byte[]>;
	using PartialSigKVMap = Dictionary<KeyId, Tuple<PubKey, ECDSASignature>>;
	using UnknownKVMap = Dictionary<byte[], byte[]>;
	public class PSBTGenerator
	{
		public static Arbitrary<PSBTInput> PSBTInputArb() => Arb.From(PSBTInput());
		public static Arbitrary<PSBTOutput> PSBTOutputArb() => Arb.From(PSBTOutput());
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
				from paths in Gen.ListOf(itemNum, PrimitiveGenerator.RandomBytes(itemNum))
				select Utils.DictionaryFromList<PubKey, byte[]>(pks.ToList(), paths.ToList());

	}
}