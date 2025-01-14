#if HAS_SPAN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Secp256k1;

namespace NBitcoin.BIP352
{
	public static class SilentPayment
	{
		private static readonly byte[] NUMS =
			Encoders.Hex.DecodeData("50929b74c1a04954b78b4b6035e97a5e078a5a0f28ec96d547bfee9ace803ac0");

		public static PubKey ComputeSharedSecretReceiver(OutPoint[] prevOuts, PubKey[] pubKeys, Key b) =>
			ComputeSharedSecret(prevOuts, A: SumPublicKeys(pubKeys), b);

		public static PubKey ComputeSharedSecretReceiver(PubKey tweakData, Key b) =>
			tweakData.GetSharedPubkey(b);

		public static PubKey TweakData(OutPoint[] inputs, PubKey[] pubKeys) =>
			TweakData(inputs, SumPublicKeys(pubKeys));

		public static Dictionary<SilentPaymentAddress, PubKey[]> GetPubKeys(
			IEnumerable<SilentPaymentAddress> recipients, Utxo[] utxos) =>
			recipients
				.GroupBy(x => x.ScanKey, (scanKey, addresses) =>
				{
					var sharedSecret = ComputeSharedSecretSender(utxos, scanKey);
					return addresses.Select((addr, k) => (
						Address: addr,
						PubKey: ComputePubKey(addr.SpendKey, sharedSecret, (uint) k)));
				})
				.SelectMany(x => x)
				.GroupBy(x => x.Address)
				.ToDictionary(x => x.Key, x => x.Select(y => y.PubKey).ToArray());

		public static (SilentPaymentAddress Address, PubKey PubKey)[] GetPubKeys(
			IEnumerable<SilentPaymentAddress> addresses,
			PubKey sharedSecret, PubKey[] outputs) =>
			Enumerable
				.Range(0, outputs.Length)
				.Select(n => addresses.Select(address =>
					(Address: address, PubKey: ComputePubKey(address.SpendKey, sharedSecret, (uint) n))))
				.SelectMany(x => x)
				.Where(x => outputs.Select(o => o.ECKey.Q).Contains(x.PubKey.ECKey.Q))
				.ToArray();

		public static Dictionary<SilentPaymentAddress, Script[]> ExtractSilentPaymentScriptPubKeys(
			SilentPaymentAddress[] addresses, PubKey tweakData, Transaction tx, Key scanKey)
		{
			if (!IsElegible(tx))
			{
				return [];
			}

			var pubKeys = tx.Outputs
				.Where(x => x.ScriptPubKey.IsScriptType(ScriptType.Taproot))
				.Select(x => PayToTaprootTemplate.Instance.ExtractScriptPubKeyParameters(x.ScriptPubKey))
				.DropNulls()
				.Select(x => new PubKey(x.ToBytes()))
				.ToArray();
			var sharedSecret = ComputeSharedSecretReceiver(tweakData, scanKey);
			var silentPaymentOutputs = GetPubKeys(addresses, sharedSecret, pubKeys).GroupBy(x => x.Address);
			return silentPaymentOutputs.ToDictionary(x => x.Key,
				x => x.Select(y => new TaprootPubKey(y.PubKey.ToBytes()).ScriptPubKey).ToArray());
		}

		private static bool IsElegible(Transaction tx) =>
			tx.Outputs.Any(x => x.ScriptPubKey.IsScriptType(ScriptType.Taproot));

		public static Key CreateLabel(Key scanKey, uint label) =>
			new (TaggedHash("BIP0352/Label", scanKey.ToBytes().Concat(Serialize32(label)).ToArray()));

		// Let ecdh_shared_secret = input_hash·a·Bscan
		private static PubKey ComputeSharedSecret(OutPoint[] outpoints, Key a, PubKey B) =>
			DHSharedSecret(InputHash(outpoints, a.PubKey), B, a);

		// Let ecdh_shared_secret = input_hash·bscan·A
		private static PubKey ComputeSharedSecret(OutPoint[] outpoints, PubKey A, Key b) =>
			DHSharedSecret(InputHash(outpoints, A), A, b);

		private static PubKey DHSharedSecret(Scalar inputHash, PubKey pubKey, Key privKey) =>
			TweakData(inputHash, pubKey).GetSharedPubkey(privKey);

		private static PubKey TweakData(OutPoint[] inputs, PubKey A) =>
			TweakData(InputHash(inputs, A), A);

		private static PubKey TweakData(Scalar inputHash, PubKey pubKey) =>
			new (new ECXOnlyPubKey((inputHash * pubKey.ECKey.Q).ToGroupElement(), null).ToBytes());

		// let tk = hash_BIP0352/SharedSecret(serP(ecdh_shared_secret) || ser32(k))
		public static Key TweakKey(PubKey sharedSecret, uint k) =>
			new ( TaggedHash( "BIP0352/SharedSecret", sharedSecret.ToBytes().Concat(Serialize32(k)).ToArray()));

		public static PubKey ComputeSharedSecretSender(Utxo[] utxos, PubKey B)
		{
			using var a = SumPrivateKeys(utxos);
			return ComputeSharedSecret(utxos.Select(x => x.OutPoint).ToArray(), a, B);
		}

		// Let input_hash = hashBIP0352/Inputs(outpointL || A)
		private static Scalar InputHash(OutPoint[] outpoints, PubKey A)
		{
			var outpointL = outpoints.Select(x => x.ToBytes()).OrderBy(x => x, BytesComparer.Instance).First();
			var hash = TaggedHash("BIP0352/Inputs", outpointL.Concat(A.ToBytes()).ToArray());
			return new Scalar(hash);
		}

		public static Key ComputePrivKey(Key spendKey, PubKey sharedSecret, uint k)
		{
			using var tweakKey = TweakKey(sharedSecret, k);
			return ComputePrivKey(spendKey, tweakKey);
		}

		public static Key ComputePrivKey(Key spendKey, Key tweakKey) =>
			new ((tweakKey._ECKey.sec + spendKey._ECKey.sec).ToBytes());

		public static PubKey? ExtractPubKey(Script scriptSig, WitScript txInWitness, Script prevOutScriptPubKey)
		{
			var spk = prevOutScriptPubKey;
			if (txInWitness != WitScript.Empty && spk.IsScriptType(ScriptType.Taproot))
			{
				var pubKeyParameters = PayToTaprootTemplate.Instance.ExtractScriptPubKeyParameters(spk);
				var annex = txInWitness[txInWitness.PushCount - 1][^1] == 0x50 ? 1 : 0;
				if (txInWitness.PushCount > annex &&
				    BytesComparer.Instance.Compare(txInWitness[txInWitness.PushCount - annex - 1][1..33], NUMS) == 0)
				{
					return null;
				}

				return new PubKey(pubKeyParameters.ToBytes());
			}

			if (txInWitness != WitScript.Empty && spk.IsScriptType(ScriptType.P2WPKH))
			{
				var witScriptParameters =
					PayToWitPubKeyHashTemplate.Instance.ExtractWitScriptParameters(txInWitness);
				if (witScriptParameters is { } nonNullWitScriptParameters &&
				    nonNullWitScriptParameters.PublicKey.IsCompressed)
				{
					var q = nonNullWitScriptParameters.PublicKey;
					return q.ToBytes()[0] == 0x02 ? q : new PubKey(new ECXOnlyPubKey(q.ECKey.Q.Negate(), null).ToBytes());
				}
			}

			if (scriptSig != Script.Empty && spk.IsScriptType(ScriptType.P2PKH))
			{
				var pubKeyId = PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(spk);
				var pubKeyHash = new uint160(pubKeyId.ToBytes());

				var scriptSigBytes = scriptSig.ToBytes();
				var pubKeyBytes = Enumerable.Range(33, scriptSigBytes.Length - 32)
					.Reverse()
					.Select(i => scriptSigBytes[(i - 33)..i])
					.Where(pkb => pkb[0] is 2 or 3 or 4)
					.FirstOrDefault(pkb => Hashes.Hash160(pkb) == pubKeyHash);

				return pubKeyBytes != null
					? new PubKey(pubKeyBytes)
					: null;
			}

			if (scriptSig != Script.Empty && spk.IsScriptType(ScriptType.P2SH))
			{
				var p2sh = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(scriptSig);
				if (txInWitness != WitScript.Empty && p2sh is not null &&
				    p2sh.RedeemScript.IsScriptType(ScriptType.P2WPKH))
				{
					var witScriptParameters =
						PayToWitPubKeyHashTemplate.Instance.ExtractWitScriptParameters(txInWitness);
					if (witScriptParameters is {PublicKey.IsCompressed: true})
					{
						var q = witScriptParameters.PublicKey;
						return q.ToBytes()[0] == 0x02 ? q : new PubKey(q.ECKey.Negate().ToXOnlyPubKey().ToBytes());
					}
				}
			}

			return null;
		}

		internal static PubKey ComputePubKey(PubKey Bm, PubKey sharedSecret, uint k)
		{
			using var tk = TweakKey(sharedSecret, k);

			// Let Pmk = k·G + Bm
			var pmk = tk.PubKey.ECKey.Q.ToGroupElementJacobian() + Bm.ECKey.Q;
			return new PubKey(new ECXOnlyPubKey(pmk.ToGroupElement(), null).ToBytes());
		}

		private static Key SumPrivateKeys(IEnumerable<Utxo> utxos)
		{
			var sum = utxos
				.Select(x => NegateKey(x.SigningKey, x.ScriptPubKey.IsScriptType(ScriptType.Taproot)))
				.Aggregate(Scalar.Zero, (acc, key) => acc.Add(key.sec));

			return new Key(sum.ToBytes());

			ECPrivKey NegateKey(Key key, bool isTaproot)
			{
				var pk = ECPrivKey.Create(key.ToBytes());
				pk.CreateXOnlyPubKey(out var parity);
				return isTaproot && parity ? ECPrivKey.Create(pk.sec.Negate().ToBytes()) : pk;
			}
		}

		// Let A = A1 + A2 + ... + An
		private static PubKey SumPublicKeys(IEnumerable<PubKey> pubKeys) =>
			new PubKey(new ECXOnlyPubKey(pubKeys.Aggregate(GEJ.Infinity, (acc, key) => acc + key.ECKey.Q).ToGroupElement(), null).ToBytes());

		private static byte[] TaggedHash(string tag, byte[] data)
		{
			var tagHash = Hashes.SHA256(Encoding.UTF8.GetBytes(tag));
			var concat = tagHash.Concat(tagHash).Concat(data);
			return Hashes.SHA256(concat);
		}

		private static byte[] Serialize32(uint i)
		{
			var result = new byte[4];
			BitConverter.GetBytes(i).CopyTo(result, 0);
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(result);
			}

			return result;
		}
	}

	public class Utxo
	{
		public Utxo(OutPoint OutPoint, Key SigningKey, Script ScriptPubKey)
		{
			this.OutPoint = OutPoint;
			this.SigningKey = SigningKey;
			this.ScriptPubKey = ScriptPubKey;
		}

		public OutPoint OutPoint { get; }
		public Key SigningKey { get; }
		public Script ScriptPubKey { get; }
	}

	static class LinqExtensions
	{
		public static IEnumerable<T> DropNulls<T>(this IEnumerable<T?> source) where T: class =>
			source.Where(x => x is not null).Select(x => x!);

		public static IEnumerable<T> DropNulls<T>(this IEnumerable<T?> source) where T: struct =>
			source.Where(x => x.HasValue).Select(x => x!.Value);
	}
}
#endif
