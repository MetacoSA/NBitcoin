using System;
using System.Linq;
using FsCheck;
using NBitcoin;
using static NBitcoin.Tests.Generators.CryptoGenerator;

namespace NBitcoin.Tests.Generators
{
	public class ScriptGenerator
	{

		#region script sig
		// -------- legacy -------
		// 1. p2pkh scriptSig
		public static Gen<Tuple<Script, Script>> P2PKHScriptSigSet(Key privKey, TransactionSignature sig) =>
			P2PKHScriptPubKey().Zip(P2PKHScriptSig(privKey.PubKey, sig));

		public static Gen<Script> P2PKHScriptSig(PubKey pk, TransactionSignature sig) =>
			Gen.Fresh(() => PayToPubkeyHashTemplate.Instance.GenerateScriptSig(sig, pk));

		public static Gen<Script> P2PKHScriptSig() =>
			from s in TransactionSignature()
			from pk in PublicKey()
			from s2 in P2PKHScriptSig(pk, s)
			select s2;

		// 2. p2sh scriptSig
		public static Gen<Script> MultiSignatureScriptSig() =>
			from N in Gen.Choose(1, (int) 20)
			from hash in Hash256()
			from M in Gen.Choose(1, N)
			from pks in PrivateKeys(M)
			from hashType in SigHashType()
			select PayToMultiSigTemplate
							 .Instance
							 .GenerateScriptSig(pks.Select(pk => new TransactionSignature(pk.Sign(hash), hashType)));

		public static Gen<Script> LegacyNonLockTimeScriptSig() =>
			Gen.OneOf(P2PKHScriptSig(), MultiSignatureScriptSig());

		// -------- witness -------
		public static Gen<WitScript> MultiSignatureWitScript() =>
			from ss in MultiSignatureScriptSig()
			select new WitScript(ss);

		public static Gen<WitScript> RandomWitScript() =>
			from op in PushOnlyOpcodes()
			select new WitScript(op);

		// -------- opcodes -------
		private static Gen<Op[]> PushOnlyOpcodes() =>
			from ops in Gen.ListOf<Op>(PushOnlyOpcode())
			select ops.ToArray();

		private static Gen<Op> PushOnlyOpcode() =>
			from bytes in PrimitiveGenerator.RandomBytes(4)
			select Op.GetPushOp(bytes);

		public static Gen<Op[]> RandomOpcodes() =>
			from ops in Gen.ListOf(RandomOpcode())
			select ops.ToArray();
		public static Gen<Op> RandomOpcode() =>
			Gen.OneOf(PushOnlyOpcode(), NonPushOpcode());

		public static Gen<Op> NonPushOpcode()
			=> from b in PrimitiveGenerator.RandomByte()
				 where Enum.IsDefined(typeof(OpcodeType), b)
				 let op = (OpcodeType) b
				 where !Op.IsPushCode(op)
				 let opc = (Op) (OpcodeType) b
				 where !opc.IsInvalid
				 select opc;

		// ------- facades -------
		public static Gen<Script> ScriptSig() => Gen.OneOf(P2PKHScriptSig(), MultiSignatureScriptSig());

		public static Gen<Script> RandomScriptSig() =>
			from op in RandomOpcodes()
			select new Script(op);

		#endregion

		#region scriptPubKey
		public static Gen<Script> P2PKHScriptPubKey() =>
			from pk in PublicKey()
			select pk.Hash.ScriptPubKey;

		public static Gen<Script> P2MultisigScriptPubKey() =>
			from t in PublicKey().Zip(Gen.Choose(0, (int) 16))
			select PayToMultiSigTemplate.Instance.GenerateScriptPubKey(t.Item2, t.Item1);

		public static Gen<Script> LegacyScriptPubKey() =>
			Gen.OneOf(P2PKHScriptPubKey(), P2MultisigScriptPubKey());


		// ------- witness ------
		public static Gen<Script> P2WPKHScriptPubKey() =>
			from t in PublicKey()
			select t.WitHash.ScriptPubKey;
		public static Gen<Script> P2WSHScriptPubKey() =>
			from w in MultiSignatureScriptSig()
			select w.WitHash.ScriptPubKey;

		public static Gen<Script> WitnessScriptPubKey() =>
			Gen.OneOf(P2WSHScriptPubKey(), P2WPKHScriptPubKey());

		// -------- facades --------
		public static Gen<Script> ScriptPubKey() =>
			Gen.OneOf(LegacyScriptPubKey(), WitnessScriptPubKey());

		#endregion

	}
}