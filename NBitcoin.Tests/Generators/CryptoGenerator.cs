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
		public static Arbitrary<Key> KeysArb() =>
			 Arb.From(PrivateKey());

		public static Arbitrary<ExtKey> ExtKeysArb() =>
			Arb.From(ExtKey());

		public static Arbitrary<List<Key>> KeysListArb() =>
			Arb.From(PrivateKeys(15));

		public static Arbitrary<KeyPath> ExtPathArb() =>
			Arb.From(KeyPath());

		public static Arbitrary<PubKey> PubKeyArb() =>
			Arb.From(PublicKey());

		public static Gen<Key> PrivateKey() => Gen.Fresh(() => new Key());

		public static Gen<List<Key>> PrivateKeys(int n) =>
			from pk in Gen.ListOf<Key>(n, PrivateKey())
			select pk.ToList();

		#endregion

		public static Gen<PubKey> PublicKey() =>
			PrivateKey().Select(p => p.PubKey);

		public static Gen<List<PubKey>> PublicKeys() =>
			from n in Gen.Choose(0, 15)
			from pks in PublicKeys(n)
			select pks;

		public static Gen<List<PubKey>> PublicKeys(int n) =>
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
		public static Gen<ECDSASignature> ECDSA() =>
			from hash in Hash256()
			from priv in PrivateKey()
			select priv.Sign(hash);

		public static Gen<List<ECDSASignature>> ECDSAs() =>
			from n in Gen.Choose(0, 20)
			from sigs in ECDSAs(n)
			select sigs.ToList();

		public static Gen<List<ECDSASignature>> ECDSAs(int n) =>
			from sigs in Gen.ListOf(n, ECDSA())
			select sigs.ToList();
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

		public static Gen<ExtKey> ExtKey() => Gen.Fresh(() => new ExtKey());

		public static Gen<KeyPath> KeyPath() =>
			from raw in Gen.NonEmptyListOf(PrimitiveGenerator.RandomBytes(4))
			let flattenBytes = raw.ToList().Aggregate((a, b) => a.Concat(b))
			select NBitcoin.KeyPath.FromBytes(flattenBytes);

		public static Gen<ExtPubKey> ExtPubKey() => ExtKey().Select(ek => ek.Neuter());
	}
}