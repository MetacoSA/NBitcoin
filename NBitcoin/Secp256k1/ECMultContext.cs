#if HAS_SPAN
#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace NBitcoin.Secp256k1
{
#if SECP256K1_LIB
	public
#endif
	class ECMultContext
	{

		static readonly Lazy<ECMultContext> _Instance = new Lazy<ECMultContext>(CreateInstance, true);
		static ECMultContext CreateInstance()
		{
			return new ECMultContext();
		}
		public static ECMultContext Instance => _Instance.Value;


		// ECMULT_TABLE_SIZE(WINDOW_G)
		const int ArraySize = 8192;
		internal const int WINDOW_G = 15;
		internal const int WINDOW_A = 5;
		// ECMULT_TABLE_SIZE(WINDOW_A)
		internal const int ArraySize_A = 8;
		internal readonly GEStorage[] pre_g;
		internal readonly GEStorage[] pre_g_128;

		// WNAF_SIZE(WINDOW_A - 1)
		internal const int WNAFT_SIZE_A = 32;

		public ECMultContext()
		{
			//secp256k1_ecmult_context_build
			/* get the generator */
			GEJ gj = EC.G.ToGroupElementJacobian();
			this.pre_g = new GEStorage[ArraySize];

			/* precompute the tables with odd multiples */
			secp256k1_ecmult_odd_multiples_table_storage_var(ArraySize, pre_g, gj);

			{
				GEJ g_128j;
				int i;

				this.pre_g_128 = new GEStorage[ArraySize];

				/* calculate 2^128*generator */
				g_128j = gj;
				for (i = 0; i < 128; i++)
				{
					g_128j = g_128j.DoubleVariable();
				}
				secp256k1_ecmult_odd_multiples_table_storage_var(ArraySize, pre_g_128, g_128j);
			}
		}

		static void secp256k1_ecmult_odd_multiples_table_storage_var(int n, GEStorage[] pre, in GEJ a)
		{
			GEJ d;
			GE d_ge, p_ge;
			GEJ pj;
			FE zi;
			FE zr;
			FE dx_over_dz_squared;
			int i;

			VERIFY_CHECK(!a.infinity);
			d = a.DoubleVariable();

			/* First, we perform all the additions in an isomorphic curve obtained by multiplying
			 * all `z` coordinates by 1/`d.z`. In these coordinates `d` is affine so we can use
			 * `secp256k1_gej_add_ge_var` to perform the additions. For each addition, we store
			 * the resulting y-coordinate and the z-ratio, since we only have enough memory to
			 * store two field elements. These are sufficient to efficiently undo the isomorphism
			 * and recompute all the `x`s.
			 */
			d_ge = new GE(d.x, d.y, false);


			p_ge = a.ToGroupElementZInv(d.z);
			pj = new GEJ(p_ge.x, p_ge.y, a.z, false);

			for (i = 0; i < (n - 1); i++)
			{
				pj = new GEJ(pj.x, pj.y.NormalizeVariable(), pj.z);
				pre[i] = new GEStorage(pre[i].x, pj.y.ToStorage());
				pj = pj.AddVariable(d_ge, out zr);
				zr = zr.NormalizeVariable();
				pre[i] = new GEStorage(zr.ToStorage(), pre[i].y);
			}

			/* Invert d.z in the same batch, preserving pj.z so we can extract 1/d.z */
			zi = pj.z * d.z;
			zi = zi.InverseVariable();

			/* Directly set `pre[n - 1]` to `pj`, saving the inverted z-coordinate so
			 * that we can combine it with the saved z-ratios to compute the other zs
			 * without any more inversions. */
			p_ge = pj.ToGroupElementZInv(zi);
			pre[n - 1] = p_ge.ToStorage();

			/* Compute the actual x-coordinate of D, which will be needed below. */
			d = new GEJ(d.x, d.y, zi * pj.z, d.infinity); /* d.z = 1/d.z */
			dx_over_dz_squared = d.z.Sqr();
			dx_over_dz_squared = dx_over_dz_squared * d.x;

			/* Going into the second loop, we have set `pre[n-1]` to its final affine
			 * form, but still need to set `pre[i]` for `i` in 0 through `n-2`. We
			 * have `zi = (p.z * d.z)^-1`, where
			 *
			 *     `p.z` is the z-coordinate of the point on the isomorphic curve
			 *           which was ultimately assigned to `pre[n-1]`.
			 *     `d.z` is the multiplier that must be applied to all z-coordinates
			 *           to move from our isomorphic curve back to secp256k1; so the
			 *           product `p.z * d.z` is the z-coordinate of the secp256k1
			 *           point assigned to `pre[n-1]`.
			 *
			 * All subsequent inverse-z-coordinates can be obtained by multiplying this
			 * factor by successive z-ratios, which is much more efficient than directly
			 * computing each one.
			 *
			 * Importantly, these inverse-zs will be coordinates of points on secp256k1,
			 * while our other stored values come from computations on the isomorphic
			 * curve. So in the below loop, we will take care not to actually use `zi`
			 * or any derived values until we're back on secp256k1.
			 */
			i = n - 1;
			while (i > 0)
			{
				FE zi2, zi3;
				i--;
				p_ge = pre[i].ToGroupElement();

				/* For each remaining point, we extract the z-ratio from the stored
				 * x-coordinate, compute its z^-1 from that, and compute the full
				 * point from that. */
				ref readonly FE rzr = ref p_ge.x;
				zi = zi * rzr;
				zi2 = zi.Sqr();
				zi3 = zi2 * zi;
				/* To compute the actual x-coordinate, we use the stored z ratio and
				 * y-coordinate, which we obtained from `secp256k1_gej_add_ge_var`
				 * in the loop above, as well as the inverse of the square of its
				 * z-coordinate. We store the latter in the `zi2` variable, which is
				 * computed iteratively starting from the overall Z inverse then
				 * multiplying by each z-ratio in turn.
				 *
				 * Denoting the z-ratio as `rzr`, we observe that it is equal to `h`
				 * from the inside of the above `gej_add_ge_var` call. This satisfies
				 *
				 *    rzr = d_x * z^2 - x * d_z^2
				 *
				 * where (`d_x`, `d_z`) are Jacobian coordinates of `D` and `(x, z)`
				 * are Jacobian coordinates of our desired point -- except both are on
				 * the isomorphic curve that we were using when we called `gej_add_ge_var`.
				 * To get back to secp256k1, we must multiply both `z`s by `d_z`, or
				 * equivalently divide both `x`s by `d_z^2`. Our equation then becomes
				 *
				 *    rzr = d_x * z^2 / d_z^2 - x
				 *
				 * (The left-hand-side, being a ratio of z-coordinates, is unaffected
				 * by the isomorphism.)
				 *
				 * Rearranging to solve for `x`, we have
				 *
				 *     x = d_x * z^2 / d_z^2 - rzr
				 *
				 * But what we actually want is the affine coordinate `X = x/z^2`,
				 * which will satisfy
				 *
				 *     X = d_x / d_z^2 - rzr / z^2
				 *       = dx_over_dz_squared - rzr * zi2
				 */

				var p_gex = rzr * zi2;
				p_gex = p_gex.Negate(1);
				p_gex += dx_over_dz_squared;
				/* y is stored_y/z^3, as we expect */
				var p_gey = p_ge.y * zi3;
				p_ge = new GE(p_gex, p_gey, p_ge.infinity);
				/* Store */
				pre[i] = p_ge.ToStorage();
			}
		}


		/// <summary>
		/// Double multiply: R = na*A + ng*G
		/// (secp256k1_ecmult)
		/// </summary>
		/// <param name="a"></param>
		/// <param name="na"></param>
		/// <param name="ng"></param>
		/// <returns></returns>
		public GEJ Mult(in GEJ a, in Scalar na, in Scalar ng)
		{
			Span<Scalar> nas = stackalloc Scalar[1];
			nas[0] = na;
			Span<GEJ> @as = stackalloc GEJ[1];
			@as[0] = a;
			unsafe
			{
				GEJ* prej = stackalloc GEJ[ArraySize_A];
				FE* zr = stackalloc FE[ArraySize_A];
				GE* pre_a = stackalloc GE[ArraySize_A];
				GE* pre_a_lam = stackalloc GE[ArraySize_A];
				StraussPointState* ps = stackalloc StraussPointState[1];

				StraussState state = default;
				state.prej = prej;
				state.zr = zr;
				state.pre_a = pre_a;
				state.pre_a_lam = pre_a_lam;
				state.ps = ps;
				return ECMultiply(state, @as, nas, ng, 1);
			}
		}
		unsafe GEJ ECMultiply(in StraussState state, in Span<GEJ> a, in Span<Scalar> na, in Scalar? ng, int num)
		{
			var r = GEJ.Infinity;

			// secp256k1_ecmult_strauss_wnaf
			GE tmpa;
			FE Z = FE.Zero;

			var pre_a_span = new Span<GE>(state.pre_a, num * ArraySize_A);
			var pre_a_lam_span = new Span<GE>(state.pre_a_lam, num * ArraySize_A);
			var prej_span = new Span<GEJ>(state.prej, num * ArraySize_A);
			var zr_span = new Span<FE>(state.zr, num * ArraySize_A);
			var ps_span = new Span<StraussPointState>(state.ps, num);
			/* Splitted G factors. */
			Scalar ng_1, ng_128;
			Span<int> wnaf_ng_1 = stackalloc int[129];
			int bits_ng_1 = 0;
			Span<int> wnaf_ng_128 = stackalloc int[129];
			int bits_ng_128 = 0;
			int i;
			int bits = 0;
			int np;
			int no = 0;

			for (np = 0; np < num; ++np)
			{
				fixed (StraussPointState* ps_span_np = &ps_span[np])
				{
					var ps_wnaf_na_1 = new Span<int>(ps_span_np->wnaf_na_1, 130);
					var ps_wnaf_na_lam = new Span<int>(ps_span_np->wnaf_na_lam, 130);
					if (na[np].IsZero || a[np].IsInfinity)
					{
						continue;
					}
					ps_span[no].input_pos = np;
					/* split na into na_1 and na_lam (where na = na_1 + na_lam*lambda, and na_1 and na_lam are ~128 bit) */
					na[np].SplitLambda(out ps_span[no].na_1, out ps_span[no].na_lam);

					/* build wnaf representation for na_1 and na_lam. */
					ps_span[no].bits_na_1 = secp256k1_ecmult_wnaf(ps_wnaf_na_1, ps_span[no].na_1, WINDOW_A);
					ps_span[no].bits_na_lam = secp256k1_ecmult_wnaf(ps_wnaf_na_lam, ps_span[no].na_lam, WINDOW_A);
					VERIFY_CHECK(ps_span[no].bits_na_1 <= 130);
					VERIFY_CHECK(ps_span[no].bits_na_lam <= 130);
					if (ps_span[no].bits_na_1 > bits)
					{
						bits = ps_span[no].bits_na_1;
					}
					if (ps_span[no].bits_na_lam > bits)
					{
						bits = ps_span[no].bits_na_lam;
					}
					++no;
				}
			}

			/* Calculate odd multiples of a.
     * All multiples are brought to the same Z 'denominator', which is stored
     * in Z. Due to secp256k1' isomorphism we can do all operations pretending
     * that the Z coordinate was 1, use affine addition formulae, and correct
     * the Z coordinate of the result once at the end.
     * The exception is the precomputed G table points, which are actually
     * affine. Compared to the base used for other points, they have a Z ratio
     * of 1/Z, so we can use secp256k1_gej_add_zinv_var, which uses the same
     * isomorphism to efficiently add with a known Z inverse.
     */
			if (no > 0)
			{
				/* Compute the odd multiples in Jacobian form. */
				secp256k1_ecmult_odd_multiples_table(ArraySize_A, prej_span, zr_span, a[ps_span[0].input_pos]);
				for (np = 1; np < no; ++np)
				{
					GEJ tmp = a[ps_span[np].input_pos];

					ref GEJ prej_span_1 = ref prej_span[(np - 1) * ArraySize_A + ArraySize_A - 1];
#if SECP256K1_VERIFY
					prej_span_1 = new GEJ(prej_span_1.x, prej_span_1.y, prej_span_1.z.NormalizeVariable(), prej_span_1.infinity);
#endif

					tmp = tmp.Rescale(prej_span_1.z);
					secp256k1_ecmult_odd_multiples_table(ArraySize_A, prej_span.Slice(np * ArraySize_A), zr_span.Slice(np * ArraySize_A), tmp);

					state.zr[np * ArraySize_A] *= a[ps_span[np].input_pos].z;
				}
				/* Bring them to the same Z denominator. */
				secp256k1_ge_globalz_set_table_gej(ArraySize_A * no, pre_a_span, ref Z, prej_span, zr_span);
			}
			else
			{
				Z = new FE(1);
			}

			for (np = 0; np < no; ++np)
			{
				for (i = 0; i < ArraySize_A; i++)
				{
					pre_a_lam_span[np * ArraySize_A + i] = pre_a_span[np * ArraySize_A + i].MultiplyLambda();
				}
			}

			if (ng is Scalar ngv)
			{
				/* split ng into ng_1 and ng_128 (where gn = gn_1 + gn_128*2^128, and gn_1 and gn_128 are ~128 bit) */
				ngv.Split128(out ng_1, out ng_128);

				/* Build wnaf representation for ng_1 and ng_128 */

				bits_ng_1 = secp256k1_ecmult_wnaf(wnaf_ng_1, ng_1, WINDOW_G);
				bits_ng_128 = secp256k1_ecmult_wnaf(wnaf_ng_128, ng_128, WINDOW_G);
				if (bits_ng_1 > bits)
				{
					bits = bits_ng_1;
				}
				if (bits_ng_128 > bits)
				{
					bits = bits_ng_128;
				}
			}

			for (i = bits - 1; i >= 0; i--)
			{
				int n;
				r = r.DoubleVariable();
				for (np = 0; np < no; ++np)
				{
					fixed (StraussPointState* ps_span_np = &ps_span[np])
					{
						var ps_wnaf_na_1 = new Span<int>(ps_span_np->wnaf_na_1, 130);
						var ps_wnaf_na_lam = new Span<int>(ps_span_np->wnaf_na_lam, 130);

						if (i < ps_span[np].bits_na_1 && (n = ps_wnaf_na_1[i]) != 0)
						{
							ECMULT_TABLE_GET_GE(out tmpa, pre_a_span.Slice(np * ArraySize_A), n, WINDOW_A);
							r = r.AddVariable(tmpa, out _);
						}
						if (i < ps_span[np].bits_na_lam && (n = ps_wnaf_na_lam[i]) != 0)
						{
							ECMULT_TABLE_GET_GE(out tmpa, pre_a_lam_span.Slice(np * ArraySize_A), n, WINDOW_A);
							r = r.AddVariable(tmpa, out _);
						}
					}
				}
				if (i < bits_ng_1 && (n = wnaf_ng_1[i]) != 0)
				{
					ECMULT_TABLE_GET_GE_STORAGE(out tmpa, pre_g, n, WINDOW_G);
					r = r.AddZInvVariable(tmpa, Z);
				}
				if (i < bits_ng_128 && (n = wnaf_ng_128[i]) != 0)
				{
					ECMULT_TABLE_GET_GE_STORAGE(out tmpa, pre_g_128, n, WINDOW_G);
					r = r.AddZInvVariable(tmpa, Z);
				}
			}

			if (!r.infinity)
			{
				r = new GEJ(r.x, r.y, r.z * Z);
			}

			return r;
		}

		static void secp256k1_ge_globalz_set_table_gej(int len, Span<GE> r, ref FE globalz, Span<GEJ> a, Span<FE> zr)
		{
			int i = len - 1;
			FE zs;

			if (len > 0)
			{

				/* The z of the final point gives us the "global Z" for the table. */
				/* Ensure all y values are in weak normal form for fast negation of points */
				r[i] = new GE(a[i].x, a[i].y.NormalizeWeak(), false);
				globalz = a[i].z;
				zs = zr[i];

				/* Work our way backwards, using the z-ratios to scale the x/y values. */
				while (i > 0)
				{
					if (i != len - 1)
					{
						zs *= zr[i];
					}
					i--;
					r[i] = a[i].ToGroupElementZInv(zs);
				}
			}
		}

		/** Fill a table 'prej' with precomputed odd multiples of a. Prej will contain
 *  the values [1*a,3*a,...,(2*n-1)*a], so it space for n values. zr[0] will
 *  contain prej[0].z / a.z. The other zr[i] values = prej[i].z / prej[i-1].z.
 *  Prej's Z values are undefined, except for the last value.
 */
		internal static void secp256k1_ecmult_odd_multiples_table(int n, Span<GEJ> prej, Span<FE> zr, in GEJ a)
		{
			GEJ d;
			GE a_ge, d_ge;
			int i;

			VERIFY_CHECK(!a.infinity);
			d = a.DoubleVariable();

			/*
			 * Perform the additions on an isomorphism where 'd' is affine: drop the z coordinate
			 * of 'd', and scale the 1P starting value's x/y coordinates without changing its z.
			 */
			d_ge = new GE(d.x, d.y, false);

			a_ge = a.ToGroupElementZInv(d.z);

			prej[0] = new GEJ(a_ge.x, a_ge.y, a.z, false);

			zr[0] = d.z;
			for (i = 1; i < n; i++)
			{
				prej[i] = prej[i - 1].AddVariable(d_ge, out zr[i]);
			}

			/*
			 * Each point in 'prej' has a z coordinate too small by a factor of 'd.z'. Only
			 * the final point's z coordinate is actually used though, so just update that.
			 */
			prej[n - 1] = new GEJ(prej[n - 1].x, prej[n - 1].y, prej[n - 1].z * d.z, prej[n - 1].infinity);
		}

		private static void ECMULT_TABLE_GET_GE_STORAGE(out GE r, in Span<GEStorage> pre, int n, int w)
		{
			VERIFY_CHECK(((n) & 1) == 1);
			VERIFY_CHECK((n) >= -((1 << ((w) - 1)) - 1));
			VERIFY_CHECK((n) <= ((1 << ((w) - 1)) - 1));
			if ((n) > 0)
			{
				r = (pre)[((n) - 1) / 2].ToGroupElement();
			}
			else
			{
				r = (pre)[(-(n) - 1) / 2].ToGroupElement();
				r = new GE(r.x, r.y.Negate(1), r.infinity);
			}
		}

		private static void ECMULT_TABLE_GET_GE(out GE r, in Span<GE> pre, int n, int w)
		{
			VERIFY_CHECK(((n) & 1) == 1);
			VERIFY_CHECK((n) >= -((1 << ((w) - 1)) - 1));
			VERIFY_CHECK((n) <= ((1 << ((w) - 1)) - 1));
			if ((n) > 0)
			{
				r = pre[((n) - 1) / 2];
			}
			else
			{
				r = pre[(-(n) - 1) / 2];
				r = new GE(r.x, r.y.Negate(1), r.infinity);
			}
		}

		/* Convert a number to WNAF notation. The number becomes represented by sum(2^i * wnaf[i], i=0..bits),
 *  with the following guarantees:
 *  - each wnaf[i] is either 0, or an odd integer between -(1<<(w-1) - 1) and (1<<(w-1) - 1)
 *  - two non-zero entries in wnaf are separated by at least w-1 zeroes.
 *  - the number of set values in wnaf is returned. This number is at most 256, and at most one more
 *    than the number of bits in the (absolute value) of the input.
 */
		internal static int secp256k1_ecmult_wnaf(Span<int> wnaf, in Scalar a, int w)
		{
			int len = wnaf.Length;
			Scalar s = a;
			int last_set_bit = -1;
			int bit = 0;
			int sign = 1;
			int carry = 0;

			VERIFY_CHECK(0 <= len && len <= 256);
			VERIFY_CHECK(2 <= w && w <= 31);

			wnaf.Clear();

			if (s.GetBits(255, 1) != 0)
			{
				s = s.Negate();
				sign = -1;
			}

			while (bit < len)
			{
				int now;
				int word;
				if (s.GetBits(bit, 1) == (uint)carry)
				{
					bit++;
					continue;
				}

				now = w;
				if (now > len - bit)
				{
					now = len - bit;
				}

				word = (int)(s.GetBitsVariable(bit, now) + (uint)carry);

				carry = (word >> (w - 1)) & 1;
				word -= carry << w;

				wnaf[bit] = sign * word;
				last_set_bit = bit;

				bit += now;
			}
#if SECP256K1_VERIFY
			VERIFY_CHECK(carry == 0);
			while (bit < 256)
			{
				VERIFY_CHECK(s.GetBits(bit++, 1) == 0);
			}
#endif
			return last_set_bit + 1;
		}

		/** Fill a table 'pre' with precomputed odd multiples of a.
 *
 *  There are two versions of this function:
 *  - secp256k1_ecmult_odd_multiples_table_globalz_windowa which brings its
 *    resulting point set to a single constant Z denominator, stores the X and Y
 *    coordinates as ge_storage points in pre, and stores the global Z in rz.
 *    It only operates on tables sized for WINDOW_A wnaf multiples.
 *  - secp256k1_ecmult_odd_multiples_table_storage_var, which converts its
 *    resulting point set to actually affine points, and stores those in pre.
 *    It operates on tables of any size, but uses heap-allocated temporaries.
 *
 *  To compute a*P + b*G, we compute a table for P using the first function,
 *  and for G using the second (which requires an inverse, but it only needs to
 *  happen once).
 */
		internal static void secp256k1_ecmult_odd_multiples_table_globalz_windowa(Span<GE> pre, ref FE globalz, in GEJ a)
		{
			Span<GEJ> prej = stackalloc GEJ[ArraySize_A];
			Span<FE> zr = stackalloc FE[ArraySize_A];

			/* Compute the odd multiples in Jacobian form. */
			secp256k1_ecmult_odd_multiples_table(ArraySize_A, prej, zr, a);
			/* Bring them to the same Z denominator. */
			secp256k1_ge_globalz_set_table_gej(ArraySize_A, pre, ref globalz, prej, zr);
		}

		[Conditional("SECP256K1_VERIFY")]
		private static void VERIFY_CHECK(bool value)
		{
			if (!value)
				throw new InvalidOperationException("VERIFY_CHECK failed (bug in C# secp256k1)");
		}
	}
}
#nullable restore
#endif
