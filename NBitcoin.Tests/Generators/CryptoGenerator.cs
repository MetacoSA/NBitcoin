using System;
using NBitcoin;
using FsCheck;
using System.Collections.Generic;
using Microsoft.FSharp.Collections;
using System.Linq;
using NBitcoin.Crypto;

namespace NBitcoin.Tests.Generators
{
  public class CryptoGenerator
  {
    #region PrivateKey
    public static Arbitrary<Key> KeysArb()
    {
      return Arb.From(privateKey());
    }

    public static Arbitrary<List<Key>> KeysListArb()
    {
      return Arb.From(privateKeys(15));
    }

    public static Gen<Key> privateKey() => Gen.Fresh(() => new Key());

    public static Gen<List<Key>> privateKeys(int n) => Gen.ListOf<Key>(n, privateKey()).Select(pk => pk.ToList());

    public static Gen<Tuple<List<Key>, int>> privateKeysWithRequiredSigs(int n)
    {
      if (n <= 0)
        return Gen.Constant(Tuple.Create<List<Key>, int>(new List<Key> { null, null }, 0));
      else
      {
        var keys = privateKeys(n);
        var sigs = Gen.Choose(0, n);
        return keys.Zip(sigs);
      }
    }

    public static Gen<Tuple<List<Key>, int>> privateKeysWithRequiredSigs()
    {
      var ng = Gen.Choose(0, 15);
      return ng.SelectMany((n) => privateKeysWithRequiredSigs(n));
    }
    #endregion

    public static Gen<PubKey> publicKey() =>
      privateKey().Select(p => p.PubKey);

    public static Gen<List<PubKey>> publicKeys() =>
      from n in Gen.Choose(0, 15)
      from pks in Gen.ListOf(n, publicKey())
      select pks.ToList();

    #region hash
    public static Gen<uint256> hash256() =>
      PrimitiveGenerator.randomBytes().Select(bs => Hashes.Hash256(bs));

    public static Gen<uint160> hash160() =>
      PrimitiveGenerator.randomBytes().Select(bs => Hashes.Hash160(bs));
    #endregion

    #region ECDSASignature
    public static Gen<ECDSASignature> ecdsa()
    {
      return hash256().SelectMany(h => privateKey().Select(p => p.Sign(h)));
    }
    #endregion

    #region TransactionSignature
    public static Gen<TransactionSignature> transactionSignature() =>
      from rawsig in ecdsa()
      from sighash in sigHashType()
      select new TransactionSignature(rawsig, sighash);

    public static Gen<SigHash> sigHashType() =>
      Gen.OneOf<SigHash>(new List<Gen<SigHash>> {
        Gen.Constant(SigHash.All),
        Gen.Constant(SigHash.Single),
        Gen.Constant(SigHash.None)
      });
    #endregion

    public static Gen<ExtKey> extPrivateKey() => Gen.Fresh(() => new ExtKey());

    public static Gen<ExtPubKey> extPublicKey() => extPrivateKey().Select(ek => ek.Neuter());
  }
}