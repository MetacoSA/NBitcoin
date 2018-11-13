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
    public static Gen<Tuple<Script, Script>> p2pkhScriptSigSet(Key privKey, TransactionSignature sig) =>
      p2pkhScriptPubKey().Zip(p2pkhScriptSig(privKey.PubKey, sig));

    public static Gen<Script> p2pkhScriptSig(PubKey pk, TransactionSignature sig) =>
      Gen.Fresh(() => PayToPubkeyHashTemplate.Instance.GenerateScriptSig(sig, pk));

    public static Gen<Script> p2pkhScriptSig() =>
      from s in transactionSignature()
      from pk in publicKey()
      from s2 in p2pkhScriptSig(pk, s)
      select s2;

    // 2. p2sh scriptSig
    public static Gen<Script> multiSignatureScriptSig() =>
      from N in Gen.Choose(1, (int)20)
      from hash in hash256()
      from M in Gen.Choose(1, N)
      from pks in privateKeys(M)
      from hashType in sigHashType()
      select PayToMultiSigTemplate
               .Instance
               .GenerateScriptSig(pks.Select(pk => new TransactionSignature(pk.Sign(hash), hashType)));

    public static Gen<Script> legacyNonLockTimeScriptSig() =>
      Gen.OneOf(p2pkhScriptSig(), multiSignatureScriptSig());

    // -------- witness -------
    public static Gen<WitScript> multiSignatureWitScript() =>
      from ss in multiSignatureScriptSig()
      select new WitScript(ss);

    public static Gen<WitScript> randomWitScript() =>
      from op in pushOnlyOpcodes()
      select new WitScript(op);

    private static Gen<Op[]> pushOnlyOpcodes() =>
      from ops in Gen.ListOf<Op>(pushOnlyOpcode())
      select ops.ToArray();

    private static Gen<Op> pushOnlyOpcode() =>
      from bytes in PrimitiveGenerator.randomBytes(4)
      select Op.GetPushOp(bytes);

    // ------- facades -------
    public static Gen<Script> scriptSig() => Gen.OneOf(p2pkhScriptSig(), multiSignatureScriptSig());
    public static Gen<Script> pickCorrespondingScriptSignature(Script scriptPubKey)
    {
      if (PayToPubkeyHashTemplate.Instance.CheckScriptPubKey(scriptPubKey))
      {
        return p2pkhScriptSig();
      }

      if (PayToScriptHashTemplate.Instance.CheckScriptPubKey(scriptPubKey))
      {
        return multiSignatureScriptSig();
      }

      throw new Exception("Unknown Script PubKey");
    }

    #endregion

    #region scriptPubKey
    public static Gen<Script> p2pkhScriptPubKey() =>
      from pk in publicKey()
      select pk.Hash.ScriptPubKey;

    public static Gen<Script> p2MultisigScriptPubKey() =>
      from t in publicKey().Zip(Gen.Choose(0, (int)16))
       select PayToMultiSigTemplate.Instance.GenerateScriptPubKey(t.Item2, t.Item1);

    public static Gen<Script> legacyScriptPubKey() =>
      Gen.OneOf(p2pkhScriptPubKey(), p2MultisigScriptPubKey());


    // ------- witness ------
    public static Gen<Script> p2wpkhScriptPubKey() =>
      from t in publicKey()
      select t.WitHash.ScriptPubKey;
    public static Gen<Script> p2wshScriptPubKey() =>
      from w in multiSignatureScriptSig()
      select w.WitHash.ScriptPubKey;

    public static Gen<Script> witnessScriptPubKey() =>
      Gen.OneOf(p2wshScriptPubKey(), p2wpkhScriptPubKey());

    #endregion

  }
}