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
      return Arb.From(PrivateKey());
    }

    public static Arbitrary<List<Key>> KeysListArb()
    {
      return Arb.From(PrivateKeys(15));
    }

    public static Gen<Key> PrivateKey() => Gen.Fresh(() => new Key());

    public static Gen<List<Key>> PrivateKeys(int n) =>
      Gen.ListOf<Key>(n, PrivateKey()).Select(pk => pk.ToList());

    public static Gen<Tuple<List<Key>, int>> PrivateKeysWithRequiredSigs(int n)
    {
      if (n <= 0)
        return Gen.Constant(Tuple.Create<List<Key>, int>(new List<Key> { null, null }, 0));
      else
      {
        var keys = PrivateKeys(n);
        var sigs = Gen.Choose(0, n);
        return keys.Zip(sigs);
      }
    }

    public static Gen<Tuple<List<Key>, int>> PrivateKeysWithRequiredSigs()
    {
      var ng = Gen.Choose(0, 15);
      return ng.SelectMany((n) => PrivateKeysWithRequiredSigs(n));
    }
    #endregion

    public static Gen<PubKey> PublicKey() =>
      PrivateKey().Select(p => p.PubKey);

    public static Gen<List<PubKey>> PublicKeys() =>
      from n in Gen.Choose(0, 15)
      from pks in Gen.ListOf(n, PublicKey())
      select pks.ToList();

    #region hash
    public static Gen<uint256> Hash256() =>
      from bytes in PrimitiveGenerator.RandomBytes(32)
      select new uint256(bytes);

    public static Gen<uint160> Hash160() =>
      from bytes in PrimitiveGenerator.RandomBytes(20)
      select new uint160(bytes);
    #endregion

    #region ECDSASignature
    public static Gen<ECDSASignature> ECDSA()
    {
      return Hash256().SelectMany(h => PrivateKey().Select(p => p.Sign(h)));
    }
    #endregion

    #region TransactionSignature
    public static Gen<TransactionSignature> TransactionSignature() =>
      from rawsig in ECDSA()
      from sighash in SigHashType()
      select new TransactionSignature(rawsig, sighash);

    public static Gen<SigHash> SigHashType() =>
      Gen.OneOf<SigHash>(new List<Gen<SigHash>> {
        Gen.Constant(SigHash.All),
        Gen.Constant(SigHash.Single),
        Gen.Constant(SigHash.None),
        Gen.Constant(SigHash.AnyoneCanPay | SigHash.All),
        Gen.Constant(SigHash.AnyoneCanPay | SigHash.Single),
        Gen.Constant(SigHash.AnyoneCanPay | SigHash.None)
      });
    #endregion

    public static Gen<ExtKey> ExtPrivateKey() => Gen.Fresh(() => new ExtKey());

    public static Gen<ExtPubKey> ExtPublicKey() => ExtPrivateKey().Select(ek => ek.Neuter());
  }
}