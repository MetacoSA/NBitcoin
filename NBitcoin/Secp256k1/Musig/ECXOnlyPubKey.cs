#if HAS_SPAN
#nullable enable
using NBitcoin.Secp256k1.Musig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Secp256k1
{
#if SECP256K1_LIB
	public
#else
	internal
#endif
	partial class ECXOnlyPubKey
	{
		/* Computes ell = SHA256(pk[0], ..., pk[np-1]) */
		static void secp256k1_musig_compute_ell(Span<byte> ell32, ECXOnlyPubKey[] pk)
		{
			using SHA256 sha = new SHA256();
			sha.Initialize();
			Span<byte> ser = stackalloc byte[32];
			for (int i = 0; i < pk.Length; i++)
			{
				pk[i].WriteToSpan(ser);
				sha.Write(ser);
			}
			sha.GetHash(ell32);
		}

		internal static void secp256k1_musig_compute_messagehash(Span<byte> msghash, ReadOnlySpan<byte> combined_nonce32, ReadOnlySpan<byte> combined_pk32, ReadOnlySpan<byte> msg)
		{
			using SHA256 sha = new SHA256();
			sha.InitializeTagged(TAG_BIP0340Challenge);
			sha.Write(combined_nonce32.Slice(0, 32));
			sha.Write(combined_pk32.Slice(0, 32));
			sha.Write(msg.Slice(0, 32));
			sha.GetHash(msghash);
		}

		internal static void secp256k1_xonly_ge_serialize(Span<byte> output32, ref GE ge)
		{
			if (ge.IsInfinity)
			{
				throw new InvalidOperationException("ge should not be infinite");
			}

			ge = ge.NormalizeXVariable();
			ge.x.WriteToSpan(output32);
		}

		/* Compute KeyAgg coefficient which is constant 1 for the second pubkey and
 * SHA256(ell, x) where ell is the hash of public keys otherwise. second_pk_x
 * can be 0 in case there is no second_pk. Assumes both field elements x and
 * second_pk_x are normalized. */
		static Scalar secp256k1_musig_keyaggcoef_internal(ReadOnlySpan<byte> ell, in FE x, in FE second_pk_x)
		{

			using SHA256 sha = new SHA256();
			Span<byte> buf = stackalloc byte[32];

			if (x.CompareToVariable(second_pk_x) == 0)
			{
				return Scalar.One;
			}
			else
			{
				sha.InitializeTagged(MusigTag);
				sha.Write(ell.Slice(0, 32));
				x.WriteToSpan(buf);
				sha.Write(buf);
				sha.GetHash(buf);
				return new Scalar(buf);
			}
		}

		internal static Scalar secp256k1_musig_keyaggcoef(MusigContext pre_session, in FE x)
		{
			return secp256k1_musig_keyaggcoef_internal(pre_session.pk_hash, x, pre_session.second_pk_x);
		}

		const string MusigTag = "KeyAgg coefficient";

		public static ECXOnlyPubKey MusigCombine(ECXOnlyPubKey[] pubkeys)
		{
			return MusigCombine(pubkeys, null);
		}

		internal static ECXOnlyPubKey MusigCombine(ECXOnlyPubKey[] pubkeys, MusigContext? preSession)
		{
			if (pubkeys == null)
				throw new ArgumentNullException(nameof(pubkeys));
			if (pubkeys.Length is 0)
				throw new ArgumentNullException(nameof(pubkeys), "At least one pubkey should be passed");
			/* No point on the curve has an X coordinate equal to 0 */
			var second_pk_x = FE.Zero;
			for (int i = 1; i < pubkeys.Length; i++)
			{
				if (pubkeys[0] != pubkeys[i])
				{
					second_pk_x = pubkeys[i].Q.x;
					break;
				}
			}

			Span<byte> ell = stackalloc byte[32];
			secp256k1_musig_compute_ell(ell, pubkeys);
			var ctx = pubkeys[0].ctx;

			var s = new Scalar[pubkeys.Length];
			var p = new GE[pubkeys.Length];

			for (int i = 0; i < pubkeys.Length; i++)
			{
				p[i] = pubkeys[i].Q;
				s[i] = secp256k1_musig_keyaggcoef_internal(ell, p[i].x, second_pk_x);
			}
			var pkj = ctx.EcMultContext.MultBatch(s, p);
			var pkp = pkj.ToGroupElement().NormalizeYVariable();
			pkp = pkp.ToEvenY(out var pk_parity);
			var combined_pk = new ECXOnlyPubKey(pkp, ctx);
			if (preSession is MusigContext)
			{
				ell.CopyTo(preSession.pk_hash);
				preSession.pk_parity = pk_parity;
				preSession.is_tweaked = false;
				preSession.second_pk_x = second_pk_x;
			}
			return combined_pk;
		}
	}
}
#endif
