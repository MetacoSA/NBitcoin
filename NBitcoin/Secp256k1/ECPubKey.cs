﻿#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.Secp256k1
{
#if SECP256K1_LIB
	public
#endif
	partial class ECPubKey
	{

#if SECP256K1_LIB
		public
#else
		internal
# endif
		readonly GE Q;

#if SECP256K1_LIB
		public
#else
		internal
# endif
		readonly Context ctx;
		public ECPubKey(in GE groupElement, Context context)
		{
			if (groupElement.IsInfinity)
			{
				throw new InvalidOperationException("A pubkey can't be an infinite group element");
			}
			var x = groupElement.x.NormalizeVariable();
			var y = groupElement.y.NormalizeVariable();
			Q = new GE(x, y);
			this.ctx = context ?? Context.Instance;
		}

		public void WriteToSpan(bool compressed, Span<byte> output, out int length)
		{
			length = 0;
			var len = (compressed ? 33 : 65);
			if (output.Length < len)
				throw new ArgumentException(paramName: nameof(output), message: $"output should be at least {len} bytes");

			// We are already normalized, the constructor enforce it.
			// var elemx = Q.x.NormalizeVariable();
			// var elemy = Q.y.NormalizeVariable();

			Q.x.WriteToSpan(output.Slice(1));
			if (compressed)
			{
				length = 33;
				output[0] = Q.y.IsOdd ? GE.SECP256K1_TAG_PUBKEY_ODD : GE.SECP256K1_TAG_PUBKEY_EVEN;
			}
			else
			{
				length = 65;
				output[0] = GE.SECP256K1_TAG_PUBKEY_UNCOMPRESSED;
				Q.y.WriteToSpan(output.Slice(33));
			}
		}

		public static void Serialize(GE q, bool compressed, Span<byte> output, out int length)
		{
			length = 0;
			var len = (compressed ? 33 : 65);
			if (output.Length < len)
				throw new ArgumentException(paramName: nameof(output), message: $"output should be at least {len} bytes");

			var elemx = q.x.NormalizeVariable();
			var elemy = q.y.NormalizeVariable();

			elemx.WriteToSpan(output.Slice(1));
			if (compressed)
			{
				length = 33;
				output[0] = elemy.IsOdd ? GE.SECP256K1_TAG_PUBKEY_ODD : GE.SECP256K1_TAG_PUBKEY_EVEN;
			}
			else
			{
				length = 65;
				output[0] = GE.SECP256K1_TAG_PUBKEY_UNCOMPRESSED;
				elemy.WriteToSpan(output.Slice(33));
			}
		}

		public static bool TryCreate(ReadOnlySpan<byte> input, Context ctx, out bool compressed, out ECPubKey? pubkey)
		{
			GE Q;
			pubkey = null;
			if (!GE.TryParse(input, out compressed, out Q))
				return false;
			pubkey = new ECPubKey(Q, ctx);
			GE.Clear(ref Q);
			return true;
		}
		public static bool TryCreateRawFormat(ReadOnlySpan<byte> input, Context ctx, out ECPubKey? pubkey)
		{
			if (input.Length != 64)
			{
				pubkey = default;
				return false;
			}
			if (FE.TryCreate(input.Slice(0, 32), out var x) &&
				FE.TryCreate(input.Slice(32), out var y))
			{
				pubkey = new ECPubKey(new GE(x, y), ctx);
				return true;
			}
			pubkey = default;
			return false;
		}

		public bool SigVerify(SecpECDSASignature signature, ReadOnlySpan<byte> msg32)
		{
			if (msg32.Length != 32)
				return false;
			if (signature is null)
				return false;
			Scalar m;

			m = new Scalar(msg32);

			var (r, s) = signature;
			return (!s.IsHigh &&
					secp256k1_ecdsa_sig_verify(ctx.EcMultContext, r, s, Q, m));
		}
		/** Group order for secp256k1 defined as 'n' in "Standards for Efficient Cryptography" (SEC2) 2.7.1
*  sage: for t in xrange(1023, -1, -1):
*     ..   p = 2**256 - 2**32 - t
*     ..   if p.is_prime():
*     ..     print '%x'%p
*     ..     break
*   'fffffffffffffffffffffffffffffffffffffffffffffffffffffffefffffc2f'
*  sage: a = 0
*  sage: b = 7
*  sage: F = FiniteField (p)
*  sage: '%x' % (EllipticCurve ([F (a), F (b)]).order())
*   'fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141'
*/
		internal static readonly FE order_as_fe = FE.CONST(
			0xFFFFFFFFU, 0xFFFFFFFFU, 0xFFFFFFFFU, 0xFFFFFFFEU,
			0xBAAEDCE6U, 0xAF48A03BU, 0xBFD25E8CU, 0xD0364141U
		);


		/** Difference between field and order, values 'p' and 'n' values defined in
 *  "Standards for Efficient Cryptography" (SEC2) 2.7.1.
 *  sage: p = 0xFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFFC2F
 *  sage: a = 0
 *  sage: b = 7
 *  sage: F = FiniteField (p)
 *  sage: '%x' % (p - EllipticCurve ([F (a), F (b)]).order())
 *   '14551231950b75fc4402da1722fc9baee'
 */
		internal static readonly FE p_minus_order = FE.CONST(
			0, 0, 0, 1, 0x45512319U, 0x50B75FC4U, 0x402DA172U, 0x2FC9BAEEU
		);
		internal static bool secp256k1_ecdsa_sig_verify(ECMultContext ctx, in Scalar sigr, in Scalar sigs, in GE pubkey, in Scalar message)
		{
			Span<byte> c = stackalloc byte[32];
			Scalar sn, u1, u2;
			FE xr;
			GEJ pubkeyj;
			GEJ pr;

			if (sigr.IsZero || sigs.IsZero)
			{
				return false;
			}

			sn = sigs.InverseVariable();
			u1 = sn * message;
			u2 = sn * sigr;
			pubkeyj = pubkey.ToGroupElementJacobian();
			pr = ctx.Mult(pubkeyj, u2, u1);
			if (pr.IsInfinity)
			{
				return false;
			}
			sigr.WriteToSpan(c);
			xr = new FE(c);

			/* We now have the recomputed R point in pr, and its claimed x coordinate (modulo n)
			 *  in xr. Naively, we would extract the x coordinate from pr (requiring a inversion modulo p),
			 *  compute the remainder modulo n, and compare it to xr. However:
			 *
			 *        xr == X(pr) mod n
			 *    <=> exists h. (xr + h * n < p && xr + h * n == X(pr))
			 *    [Since 2 * n > p, h can only be 0 or 1]
			 *    <=> (xr == X(pr)) || (xr + n < p && xr + n == X(pr))
			 *    [In Jacobian coordinates, X(pr) is pr.x / pr.z^2 mod p]
			 *    <=> (xr == pr.x / pr.z^2 mod p) || (xr + n < p && xr + n == pr.x / pr.z^2 mod p)
			 *    [Multiplying both sides of the equations by pr.z^2 mod p]
			 *    <=> (xr * pr.z^2 mod p == pr.x) || (xr + n < p && (xr + n) * pr.z^2 mod p == pr.x)
			 *
			 *  Thus, we can avoid the inversion, but we have to check both cases separately.
			 *  secp256k1_gej_eq_x implements the (xr * pr.z^2 mod p == pr.x) test.
			 */
			if (xr.EqualsXVariable(pr))
			{
				/* xr * pr.z^2 mod p == pr.x, so the signature is valid. */
				return true;
			}
			if (xr.CompareToVariable(p_minus_order) >= 0)
			{
				/* xr + n >= p, so we can skip testing the second case. */
				return false;
			}
			xr += order_as_fe;
			if (xr.EqualsXVariable(pr))
			{
				/* (xr + n) * pr.z^2 mod p == pr.x, so the signature is valid. */
				return true;
			}
			return false;
		}

		public ECPubKey Negate()
		{
			return new ECPubKey(Q.Negate(), ctx);
		}


		public override bool Equals(object obj)
		{
			if (obj is ECPubKey item)
				return this == item;
			return false;
		}
		public static bool operator ==(ECPubKey? a, ECPubKey? b)
		{
			if (a is ECPubKey aa && b is ECPubKey bb)
			{
				// Need to be constant time so no &&
				return aa.Q.x == bb.Q.x & aa.Q.y == bb.Q.y;
			}
			return a is null && b is null;
		}

		public static bool operator !=(ECPubKey? a, ECPubKey? b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + Q.x.GetHashCode();
				hash = hash * 23 + Q.y.GetHashCode();
				return hash;
			}
		}

		public ECPubKey AddTweak(ReadOnlySpan<byte> tweak)
		{
			if (TryAddTweak(tweak, out var r))
				return r!;
			throw new ArgumentException(paramName: nameof(tweak), message: "Invalid tweak");
		}
		public bool TryAddTweak(ReadOnlySpan<byte> tweak, out ECPubKey? tweakedPubKey)
		{
			tweakedPubKey = null;
			if (tweak.Length != 32)
				return false;
			Scalar term;
			bool ret = false;
			int overflow = 0;
			term = new Scalar(tweak, out overflow);
			ret = overflow == 0;
			var p = Q;
			if (ret)
			{
				if (secp256k1_eckey_pubkey_tweak_add(ctx.EcMultContext, ref p, term))
				{
					tweakedPubKey = new ECPubKey(p, ctx);
				}
				else
				{
					ret = false;
				}
			}
			return ret;
		}

		private bool secp256k1_eckey_pubkey_tweak_add(ECMultContext ctx, ref GE key, in Scalar tweak)
		{
			GEJ pt;
			Scalar one;
			pt = key.ToGroupElementJacobian();
			one = Scalar.One;

			pt = ctx.Mult(pt, one, tweak);

			if (pt.IsInfinity)
			{
				return false;
			}
			key = pt.ToGroupElement();
			return true;
		}

		public ECPubKey MultTweak(ReadOnlySpan<byte> tweak)
		{
			if (TryMultTweak(tweak, out var r))
				return r!;
			throw new ArgumentException(paramName: nameof(tweak), message: "Invalid tweak");
		}
		public bool TryMultTweak(ReadOnlySpan<byte> tweak, out ECPubKey? tweakedPubKey)
		{
			tweakedPubKey = null;
			if (tweak.Length != 32)
				return false;
			Scalar factor;
			bool ret = false;
			int overflow = 0;

			factor = new Scalar(tweak, out overflow);
			ret = overflow == 0;
			var p = Q;
			if (ret)
			{
				if (secp256k1_eckey_pubkey_tweak_mul(ctx.EcMultContext, ref p, factor))
				{
					tweakedPubKey = new ECPubKey(p, ctx);
				}
				else
				{
					ret = false;
				}
			}

			return ret;
		}

		public byte[] ToBytes()
		{
			return ToBytes(true);
		}
		public byte[] ToBytes(bool compressed)
		{
			Span<byte> b = stackalloc byte[compressed ? 33 : 65];
			WriteToSpan(compressed, b, out _);
			return b.ToArray();
		}

		private static bool secp256k1_eckey_pubkey_tweak_mul(ECMultContext ctx, ref GE key, in Scalar tweak)
		{
			Scalar zero;
			GEJ pt;
			if (tweak.IsZero)
			{
				return false;
			}
			zero = Scalar.Zero;
			pt = key.ToGroupElementJacobian();
			pt = ctx.Mult(pt, tweak, zero);
			key = pt.ToGroupElement();
			return true;
		}

		public ECPubKey GetSharedPubkey(ECPrivKey key)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			Secp256k1.GEJ res;
			Secp256k1.GE pt = this.Q;
			ref readonly Secp256k1.Scalar s = ref key.sec;
			key.AssertNotDiposed();
			// Can't happen, NBitcoin enforces invariants.
			//secp256k1_scalar_set_b32(&s, scalar, &overflow);
			//if (overflow || secp256k1_scalar_is_zero(&s))
			//{
			//	ret = 0;
			//}
			Span<byte> x = stackalloc byte[32];
			Span<byte> y = stackalloc byte[32];

			res = pt.MultConst(s, 256);
			pt = res.ToGroupElement();

			return new ECPubKey(new GE(pt.x.Normalize(), pt.y.Normalize()), ctx);
			/* Compute a hash of the point */
			//ret = hashfp(output, x, y, data);

			// We have a ref over an undisposed secret here
			//secp256k1_scalar_clear(&s);
			//return ret;
		}
	}
}
#nullable restore
#endif
