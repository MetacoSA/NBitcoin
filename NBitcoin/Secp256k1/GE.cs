#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace NBitcoin.Secp256k1
{
#if SECP256K1_LIB
	public
#endif
	readonly struct GE
	{
		/** Prefix byte used to tag various encoded curvepoints for specific purposes */
		public const byte SECP256K1_TAG_PUBKEY_EVEN = 0x02;
		public const byte SECP256K1_TAG_PUBKEY_ODD = 0x03;
		public const byte SECP256K1_TAG_PUBKEY_UNCOMPRESSED = 0x04;
		public const byte SECP256K1_TAG_PUBKEY_HYBRID_EVEN = 0x06;
		public const byte SECP256K1_TAG_PUBKEY_HYBRID_ODD = 0x07;

#if SECP256K1_LIB
		public
#else
		internal
#endif
		readonly FE x, y;
#if SECP256K1_LIB
		public
#else
		internal
#endif
		readonly bool infinity; /* whether this represents the point at infinity */
		static readonly GE _Infinity = new GE(FE.Zero, FE.Zero, true);
		/** Generator for secp256k1, value 'g' defined in
 *  "Standards for Efficient Cryptography" (SEC2) 2.7.1.
 */
		public static ref readonly GE Infinity => ref _Infinity;

		public static GE CONST(uint a, uint b, uint c, uint d, uint e, uint f, uint g, uint h, uint i, uint j, uint k, uint l, uint m, uint n, uint o, uint p)
		{
			return new GE(
				FE.CONST(a, b, c, d, e, f, g, h),
				FE.CONST(i, j, k, l, m, n, o, p),
				false
				);
		}

		public readonly bool IsInfinity
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return infinity;
			}
		}

		static readonly GE _Zero = new GE(FE.Zero, FE.Zero, false);
		public static ref readonly GE Zero => ref _Zero;

		public bool IsValidVariable
		{
			get
			{
				FE y2, x3, c;
				if (infinity)
				{
					return false;
				}
				/* y^2 = x^3 + 7 */
				y2 = y.Sqr();
				x3 = x.Sqr();
				x3 = x3 * x;
				c = new FE(EC.CURVE_B);
				x3 += c;
				x3 = x3.NormalizeWeak();
				return y2.EqualsVariable(x3);
			}
		}

		const int SIZE_MAX = int.MaxValue;
		public static void SetAllGroupElementJacobianVariable(Span<GE> r, ReadOnlySpan<GEJ> a, int len)
		{
			FE u;
			int i;
			int last_i = SIZE_MAX;

			for (i = 0; i < len; i++)
			{
				if (!a[i].infinity)
				{
					/* Use destination's x coordinates as scratch space */
					if (last_i == SIZE_MAX)
					{
						r[i] = new GE(a[i].z, r[i].y, r[i].infinity);
					}
					else
					{
						FE rx = r[last_i].x * a[i].z;
						r[i] = new GE(rx, r[i].y, r[i].infinity);
					}
					last_i = i;
				}
			}
			if (last_i == SIZE_MAX)
			{
				return;
			}
			u = r[last_i].x.InverseVariable();

			i = last_i;
			while (i > 0)
			{
				i--;
				if (!a[i].infinity)
				{
					FE rx = r[i].x * u;
					r[last_i] = new GE(rx, r[last_i].y, r[last_i].infinity);
					u = u * a[last_i].z;
					last_i = i;
				}
			}
			VERIFY_CHECK(!a[last_i].infinity);
			r[last_i] = new GE(u, r[last_i].y, r[last_i].infinity);

			for (i = 0; i < len; i++)
			{
				r[i] = new GE(r[i].x, r[i].y, a[i].infinity);
				if (!a[i].infinity)
				{
					r[i] = a[i].ToGroupElementZInv(r[i].x);
				}
			}
		}

		[Conditional("SECP256K1_VERIFY")]
		private static void VERIFY_CHECK(bool value)
		{
			if (!value)
				throw new InvalidOperationException("VERIFY_CHECK failed (bug in C# secp256k1)");
		}

		public static bool TryCreateXQuad(FE x, out GE result)
		{
			result = GE.Zero;
			FE rx, ry;
			bool rinfinity;
			FE x2, x3, c;
			rx = x;
			x2 = x.Sqr();
			x3 = x * x2;
			rinfinity = false;
			c = new FE(EC.CURVE_B);
			c += x3;
			if (!c.Sqrt(out ry))
				return false;
			result = new GE(rx, ry, rinfinity);
			return true;
		}
		public static bool TryCreateXOVariable(FE x, bool odd, out GE result)
		{
			if (!TryCreateXQuad(x, out result))
				return false;
			var ry = result.y.NormalizeVariable();
			if (ry.IsOdd != odd)
			{
				ry = ry.Negate(1);
			}
			result = new GE(result.x, ry, result.infinity);
			return true;
		}

		static readonly FE beta = FE.CONST(
	0x7ae96a2bu, 0x657c0710u, 0x6e64479eu, 0xac3434e9u,
	0x9cf04975u, 0x12f58995u, 0xc1396c28u, 0x719501eeu
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly GE MultiplyLambda()
		{
			return new GE(x * beta, y, infinity);
		}

		public GE(in FE x, in FE y, bool infinity)
		{
			this.x = x;
			this.y = y;
			this.infinity = infinity;
		}
		public GE(in FE x, in FE y)
		{
			this.x = x;
			this.y = y;
			this.infinity = false;
		}

		public GE NormalizeVariable()
		{
			return new GE(
				x.NormalizeVariable(),
				y.NormalizeVariable()
				);
		}

		public readonly void Deconstruct(out FE x, out FE y, out bool infinity)
		{
			x = this.x;
			y = this.y;
			infinity = this.infinity;
		}
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly GE ZInv(in GE a, in FE zi)
		{
			var (x, y, infinity) = this;
			FE zi2 = zi.Sqr();
			FE zi3 = zi2 * zi;
			x = a.x * zi2;
			y = a.y * zi3;
			infinity = a.infinity;
			return new GE(x, y, infinity);
		}

		[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.AggressiveInlining)]
		public readonly GE NormalizeY()
		{
			return new GE(x, this.y.Normalize(), infinity);
		}
		[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.AggressiveInlining)]
		public readonly GE NormalizeXVariable()
		{
			return new GE(x.NormalizeVariable(), this.y, infinity);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly GE NormalizeYVariable()
		{
			return new GE(x, this.y.NormalizeVariable(), infinity);
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly GE Negate()
		{
			var ry = y.NormalizeWeak();
			ry = ry.Negate(1);
			return new GE(x, ry, infinity);
		}
		public static bool TryParse(ReadOnlySpan<byte> pub, out GE elem)
		{
			return TryParse(pub, out _, out elem);
		}
		public static bool TryParse(ReadOnlySpan<byte> pub, out bool compressed, out GE elem)
		{
			compressed = false;
			elem = default;
			if (pub.Length == 33 && (pub[0] == SECP256K1_TAG_PUBKEY_EVEN || pub[0] == SECP256K1_TAG_PUBKEY_ODD))
			{
				compressed = true;
				return
					FE.TryCreate(pub.Slice(1), out var x) &&
					GE.TryCreateXOVariable(x, pub[0] == SECP256K1_TAG_PUBKEY_ODD, out elem);
			}
			else if (pub.Length == 65 && (pub[0] == SECP256K1_TAG_PUBKEY_UNCOMPRESSED || pub[0] == SECP256K1_TAG_PUBKEY_HYBRID_EVEN || pub[0] == SECP256K1_TAG_PUBKEY_HYBRID_ODD))
			{
				if (!FE.TryCreate(pub.Slice(1), out var x) || !FE.TryCreate(pub.Slice(33), out var y))
				{
					return false;
				}
				elem = new GE(x, y);
				if ((pub[0] == SECP256K1_TAG_PUBKEY_HYBRID_EVEN || pub[0] == SECP256K1_TAG_PUBKEY_HYBRID_ODD) &&
					y.IsOdd != (pub[0] == SECP256K1_TAG_PUBKEY_HYBRID_ODD))
				{
					return false;
				}
				return elem.IsValidVariable;
			}
			else
			{
				return false;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly GEJ ToGroupElementJacobian()
		{
			return new GEJ(x, y, new FE(1), infinity);
		}


		/// <summary>
		/// Keeps a group element as is if it has an even Y and otherwise negates it.
		/// parity is set to 0 in the former case and to 1 in the latter case.
		/// Requires that the coordinates of r are normalized.
		/// </summary>
		public readonly GE ToEvenY(out bool parity)
		{
			if (IsInfinity)
				throw new InvalidOperationException("Should not be infinity point");
			if (!y.IsOdd)
			{
				parity = false;
				return this;
			}
			parity = true;
			return new GE(x, y.Negate(1));
		}
		/// <summary>
		/// Keeps a group element as is if it has an even Y and otherwise negates it.
		/// Requires that the coordinates of r are normalized.
		/// </summary>
		public readonly GE ToEvenY()
		{
			return ToEvenY(out _);
		}

		public readonly string ToC(string varName)
		{
			StringBuilder b = new StringBuilder();
			b.AppendLine(x.ToC($"{varName}x"));
			b.AppendLine(y.ToC($"{varName}y"));
			var infinitystr = infinity ? 1 : 0;
			b.AppendLine($"int {varName}infinity = {infinitystr};");
			b.AppendLine($"secp256k1_ge {varName} = {{ {varName}x, {varName}y, {varName}infinity }};");
			return b.ToString();
		}

		public readonly GEStorage ToStorage()
		{
			VERIFY_CHECK(!infinity);
			return new GEStorage(x, y);
		}

		public static GEJ operator *(in GE groupElement, in Scalar scalar)
		{
			return groupElement.MultConst(scalar, 256);
		}

		/// <summary>
		/// Multiply this group element by q in constant time
		/// (secp256k1_ecmult_const)
		/// </summary>
		/// <param name="q">The scalar to multiply to</param>
		/// <param name="bits">Here `bits` should be set to the maximum bitlength of the _absolute value_ of `q` plus one because we internally sometimes add 2 to the number during the WNAF conversion.</param>
		/// <returns></returns>
		public readonly GEJ MultConst(in Scalar q, int bits)
		{
			Span<GE> pre_a = stackalloc GE[ECMultContext.ArraySize_A];
			GE tmpa;
			FE Z = default;

			int skew_1;

			Span<GE> pre_a_lam = stackalloc GE[ECMultContext.ArraySize_A];
			Span<int> wnaf_lam = stackalloc int[1 + ECMultContext.WNAFT_SIZE_A];
			int skew_lam;
			Scalar q_1, q_lam;

			Span<int> wnaf_1 = stackalloc int[1 + ECMultContext.WNAFT_SIZE_A];

			int i;
			Scalar sc = q;

			/* build wnaf representation for q. */
		int rsize = bits;
			if (bits > 128)
			{
				rsize = 128;
				/* split q into q_1 and q_lam (where q = q_1 + q_lam*lambda, and q_1 and q_lam are ~128 bit) */
				sc.SplitLambda(out q_1, out q_lam);
				skew_1 = Wnaf.Const(wnaf_1, q_1, ECMultContext.WINDOW_A - 1, 128);
				skew_lam = Wnaf.Const(wnaf_lam, q_lam, ECMultContext.WINDOW_A - 1, 128);
			}
			else
			{
				skew_1 = Wnaf.Const(wnaf_1, sc, ECMultContext.WINDOW_A - 1, bits);
				skew_lam = 0;
			}

			/* Calculate odd multiples of a.
     * All multiples are brought to the same Z 'denominator', which is stored
     * in Z. Due to secp256k1' isomorphism we can do all operations pretending
     * that the Z coordinate was 1, use affine addition formulae, and correct
     * the Z coordinate of the result once at the end.
     */
			GEJ r = this.ToGroupElementJacobian();
			ECMultContext.secp256k1_ecmult_odd_multiples_table_globalz_windowa(pre_a, ref Z, r);
			for (i = 0; i < ECMultContext.ArraySize_A; i++)
			{
				pre_a[i] = new GE(pre_a[i].x, pre_a[i].y.NormalizeWeak(), pre_a[i].infinity);
			}
			if (bits > 128)
			{
				for (i = 0; i < ECMultContext.ArraySize_A; i++)
				{
					pre_a_lam[i] = pre_a[i].MultiplyLambda();
				}
			}

			/* first loop iteration (separated out so we can directly set r, rather
			 * than having it start at infinity, get doubled several times, then have
			 * its new value added to it) */
			i = wnaf_1[Wnaf.SIZE_BITS(rsize, ECMultContext.WINDOW_A - 1)];
			VERIFY_CHECK(i != 0);
			tmpa = ECMULT_CONST_TABLE_GET_GE(pre_a, i, ECMultContext.WINDOW_A);
			r = tmpa.ToGroupElementJacobian();
			if (bits > 128)
			{
				i = wnaf_lam[Wnaf.SIZE_BITS(rsize, ECMultContext.WINDOW_A - 1)];
				VERIFY_CHECK(i != 0);
				tmpa = ECMULT_CONST_TABLE_GET_GE(pre_a_lam, i, ECMultContext.WINDOW_A);
				r = r + tmpa;
			}
			/* remaining loop iterations */
			for (i = Wnaf.SIZE_BITS(rsize, ECMultContext.WINDOW_A - 1) - 1; i >= 0; i--)
			{
				int n;
				int j;
				for (j = 0; j < ECMultContext.WINDOW_A - 1; ++j)
				{
					r = r.Double();
				}

				n = wnaf_1[i];
				tmpa = ECMULT_CONST_TABLE_GET_GE(pre_a, n, ECMultContext.WINDOW_A);
				VERIFY_CHECK(n != 0);
				r = r + tmpa;
				if (bits > 128)
				{
					n = wnaf_lam[i];
					tmpa = tmpa.ECMULT_CONST_TABLE_GET_GE(pre_a_lam, n, ECMultContext.WINDOW_A);
					VERIFY_CHECK(n != 0);
					r = r + tmpa;
				}
			}

			r = new GEJ(r.x, r.y, r.z * Z, r.infinity);

			{
				/* Correct for wNAF skew */
				GE correction = this;
				GEStorage correction_1_stor;
				GEStorage correction_lam_stor = default;
				GEStorage a2_stor;
				GEJ tmpj = correction.ToGroupElementJacobian();
				tmpj = tmpj.DoubleVariable();
				correction = tmpj.ToGroupElement();
				correction_1_stor = this.ToStorage();
				if (bits > 128)
				{
					correction_lam_stor = this.ToStorage();
				}
				a2_stor = correction.ToStorage();

				/* For odd numbers this is 2a (so replace it), for even ones a (so no-op) */
				GEStorage.CMov(ref correction_1_stor, a2_stor, skew_1 == 2 ? 1 : 0);
				if (bits > 128)
				{
					GEStorage.CMov(ref correction_lam_stor, a2_stor, skew_lam == 2 ? 1 : 0);
				}

				/* Apply the correction */
				correction = correction_1_stor.ToGroupElement();
				correction = correction.Negate();
				r = r + correction;

				if (bits > 128)
				{
					correction = correction_lam_stor.ToGroupElement();
					correction = correction.Negate();
					correction = correction.MultiplyLambda();
					r = r + correction;
				}
			}
			return r;
		}

		public static void Clear(ref GE groupElement)
		{
			groupElement = new GE();
		}

		/* This is like `ECMULT_TABLE_GET_GE` but is constant time */
		private GE ECMULT_CONST_TABLE_GET_GE(Span<GE> pre, int n, int w)
		{
			int m;
			int abs_n = (n) * (((n) > 0 ? 1 : 0) * 2 - 1);
			int idx_n = abs_n / 2;
			FE neg_y;
			VERIFY_CHECK(((n) & 1) == 1);
			VERIFY_CHECK((n) >= -((1 << ((w) - 1)) - 1));
			VERIFY_CHECK((n) <= ((1 << ((w) - 1)) - 1));
			var rx = FE.Zero;
			var ry = FE.Zero;

			for (m = 0; m < ECMULT_TABLE_SIZE(w); m++)
			{
				/* This loop is used to avoid secret data in array indices. See
				 * the comment in ecmult_gen_impl.h for rationale. */
				FE.CMov(ref rx, (pre)[m].x, m == idx_n ? 1 : 0);
				FE.CMov(ref ry, (pre)[m].y, m == idx_n ? 1 : 0);
			}
			var rinfinity = false;
			neg_y = ry.Negate(1);
			FE.CMov(ref ry, neg_y, (n) != abs_n ? 1 : 0);
			return new GE(rx, ry, rinfinity);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int ECMULT_TABLE_SIZE(int w)
		{
			return 1 << ((w) - 2);
		}
	}
}
#nullable restore
#endif
