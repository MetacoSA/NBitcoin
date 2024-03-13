#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Secp256k1
{
#if SECP256K1_LIB
	public
#endif
	partial class ECPrivKey
	{
		/// <summary>
		/// Create a proof such that, X = aG, Z = bY, a == b == x, while keeping x secret
		/// </summary>
		/// <param name="Y"></param>
		/// <param name="Z"></param>
#if SECP256K1_LIB
		public
#else
		internal
#endif
		DLEQProof ProveDLEQ(ECPubKey Y, ECPubKey Z)
		{
			if (Y is null)
				throw new ArgumentNullException(nameof(Y));
			if (Z is null)
				throw new ArgumentNullException(nameof(Z));

			var X = this.CreatePubKey();
			GE r1, r2;
			Scalar k;
			Span<byte> sk32 = stackalloc byte[32];
			Span<byte> gen2_33 = stackalloc byte[33];
			Span<byte> p1_33 = stackalloc byte[33];
			Span<byte> p2_33 = stackalloc byte[33];

			this.WriteToSpan(sk32);
			X.WriteToSpan(true, p1_33, out _);
			Z.WriteToSpan(true, p2_33, out _);
			Y.WriteToSpan(true, gen2_33, out _);
			if (!secp256k1_dleq_nonce(sk32, gen2_33, p1_33, p2_33, out k))
				throw new InvalidOperationException("Failure to generate the nonce for dleq proof");

			/* R1 = k*G, R2 = k*Y */
			secp256k1_dleq_pair(ctx.EcMultGenContext, k, Y.Q, out r1, out r2);

			/* e = tagged hash(p1, gen2, p2, r1, r2) */
			/* s = k + e * sk */
			secp256k1_dleq_challenge(Y.Q, r1, r2, X.Q, Z.Q, out var e);
			var s = e * sec;
			s = s + k;
			k = default;
			return new DLEQProof(e, s);
		}

#if SECP256K1_LIB
		public
#else
		internal
#endif
		SecpECDSASignature DecryptECDSASignature(ECDSAEncryptedSignature encryptedSignature)
		{
			if (encryptedSignature is null)
				throw new ArgumentNullException(nameof(encryptedSignature));
			var s = sec.Inverse();
			s = s * encryptedSignature.sp;
			var high = s.IsHigh;
			s.CondNegate(high ? 1 : 0, out s);
			var sig = new SecpECDSASignature(encryptedSignature.GetRAsScalar(), s, true);
			Scalar.Clear(ref s);
			return sig;
		}

		private bool secp256k1_dleq_nonce(Span<byte> sk32, Span<byte> gen2_33, Span<byte> p1_33, Span<byte> p2_33, out Scalar scalar)
		{
			Span<byte> buf32 = stackalloc byte[33];
			using SHA256 sha = new SHA256();
			sha.Initialize();
			sha.Write(p1_33);
			sha.Write(p2_33);
			sha.GetHash(buf32);
			Span<byte> nonce = stackalloc byte[32];
			return TryGetNonce(nonce, buf32, sk32, gen2_33, out scalar);
		}


		private bool TryGetNonce(Span<byte> nonce, ReadOnlySpan<byte> msg32, ReadOnlySpan<byte> key32, ReadOnlySpan<byte> pk33, out Scalar scalar)
		{
			using SHA256 sha = new SHA256();
			sha.InitializeTagged("ECDSAadaptor/non");
			sha.Write(key32);
			sha.Write(pk33);
			sha.Write(msg32);
			sha.GetHash(nonce);
			scalar = new Scalar(nonce);
			var ret = !scalar.IsZero;
			Scalar.CMov(ref scalar, Scalar.One, ret ? 0 : 1);
			return ret;
		}

		internal static void secp256k1_dleq_challenge(in GE gen2, in GE r1, in GE r2, in GE p1, in GE p2, out Scalar e)
		{
			Span<byte> buf32 = stackalloc byte[33];
			using SHA256 sha = new SHA256();
			sha.InitializeTagged("DLEQ");
			secp256k1_dleq_hash_point(sha, p1);
			secp256k1_dleq_hash_point(sha, gen2);
			secp256k1_dleq_hash_point(sha, p2);
			secp256k1_dleq_hash_point(sha, r1);
			secp256k1_dleq_hash_point(sha, r2);
			sha.GetHash(buf32);
			e = new Scalar(buf32);
		}

		private static void secp256k1_dleq_hash_point(SHA256 sha, GE p)
		{
			Span<byte> buf33 = stackalloc byte[33];
			var y = p.y.Normalize();
			buf33[0] = (byte)(y.IsOdd ? GE.SECP256K1_TAG_PUBKEY_ODD : GE.SECP256K1_TAG_PUBKEY_EVEN);
			var x = p.x.Normalize();
			x.WriteToSpan(buf33.Slice(1));
			sha.Write(buf33);
		}


		/// <summary>
		/// p1 = x*G, p2 = x*gen2, constant time
		/// </summary>
		/// <param name="ecmult_gen_ctx"></param>
		/// <param name="sk"></param>
		/// <param name="gen2"></param>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		public static void secp256k1_dleq_pair(ECMultGenContext ecmult_gen_ctx, in Scalar sk, in GE gen2, out GE p1, out GE p2)
		{
			var p1j = ecmult_gen_ctx.MultGen(sk);
			p1 = p1j.ToGroupElement();
			var p2j = gen2.MultConst(sk, 256);
			p2 = p2j.ToGroupElement();
		}

		public bool TrySignEncryptedECDSA(ReadOnlySpan<byte> msg32, ECPubKey encryptionKey, [MaybeNullWhen(false)] out ECDSAEncryptedSignature signature)
		{
			bool ret = true;
			if (encryptionKey == null)
				throw new ArgumentNullException(nameof(encryptionKey));
			if (msg32.Length < 32)
				throw new ArgumentException(paramName: nameof(msg32), message: "msg32 should be at least 32 bytes");
			var enckey_ge = encryptionKey.Q;
			Span<byte> buf33 = stackalloc byte[33];
			DLEQProof.secp256k1_dleq_serialize_point(buf33, enckey_ge);
			Span<byte> nonce32 = stackalloc byte[32];
			Span<byte> seckey32 = stackalloc byte[32];
			sec.WriteToSpan(seckey32);
			ret &= TryGetNonce(nonce32, msg32, seckey32, buf33, out var k);
			var kEcKey = new ECPrivKey(k, ctx, false);
			secp256k1_dleq_pair(ctx.EcMultGenContext, k, enckey_ge, out GE rp, out GE r);
			/* R' := k*G */
			var rpj = rp.ToGroupElementJacobian();
			/* R = k*Y; */
			var rj = r.ToGroupElementJacobian();

			/* s' = k⁻¹(H(m) + x_coord(R)x) */
			var msg = new Scalar(msg32);
			ret &= ECDSAEncryptedSignature.TryGetScalar(r.x, out var sigr);
			var n = sigr * sec;
			n = n + msg;
			var sp = k.Inverse();
			sp = sp * n;

			ret &= !sp.IsZero;

			/* return (R, R', s', proof) */
			ret &= !enckey_ge.IsInfinity;
			ret &= !r.IsInfinity;

			if (!ret)
				signature = default;
			else
			{
				var proof = kEcKey.ProveDLEQ(new ECPubKey(enckey_ge, ctx), new ECPubKey(r, ctx));
				signature = new ECDSAEncryptedSignature(r, rp, sp, proof);
			}
			k = default;
			nonce32.Clear();
			seckey32.Clear();
			return ret;
		}
	}
}
#endif
