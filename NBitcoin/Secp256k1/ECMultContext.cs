#if HAS_SPAN
#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;

namespace NBitcoin.Secp256k1
{
#if SECP256K1_LIB
	public partial class ECMultContext
#else
	partial class ECMultContext
#endif
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

		static int WNAF_BITS = 128;
		internal static int WNAF_SIZE(int w) => WNAF_SIZE_BITS(WNAF_BITS, w);
		static int WNAF_SIZE_BITS(int bits, int w) => (((bits) + (w) - 1) / (w));
		static int ECMULT_TABLE_SIZE(int w) => (1 << ((w) - 2));
		

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

		/* Computes ecmult_multi by simply multiplying and adding each point. Does not
 * require a scratch space */
		internal static GEJ secp256k1_ecmult_multi_simple_var(ECMultContext ctx, in Scalar? inp_g_sc, ReadOnlySpan<Scalar> scalars, ReadOnlySpan<GE> points)
		{
			int point_idx;
			Scalar szero = Scalar.Zero;
			GEJ tmpj = GEJ.Infinity;

			GEJ r = GEJ.Infinity;
			/* r = inp_g_sc*G */
			r = ctx.Mult(tmpj, szero, inp_g_sc);
			for (point_idx = 0; point_idx < points.Length; point_idx++)
			{
				/* r += scalar*point */
				GEJ pointj = points[point_idx].ToGroupElementJacobian();
				tmpj = ctx.Mult(pointj, scalars[point_idx], null);
				r = r.AddVariable(tmpj, out _);
			}
			return r;
		}
		static void secp256k1_ecmult_endo_split(ref Scalar s1, ref Scalar s2, ref GE p1, ref GE p2)
		{
			Scalar tmp = s1;
			tmp.SplitLambda(out s1, out s2);
			p2 = p1.MultiplyLambda();

			if (s1.IsHigh)
			{
				s1 = s1.Negate();
				p1 = p1.Negate();
			}
			if (s2.IsHigh)
			{
				s2 = s2.Negate();
				p2 = p2.Negate();
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
		public GEJ Mult(in GEJ a, in Scalar na, in Scalar? ng)
		{
			Span<Scalar> nas = stackalloc Scalar[1];
			nas[0] = na;
			Span<GEJ> @as = stackalloc GEJ[1];
			@as[0] = a;
			StraussState state = new StraussState(ArraySize_A, 1);
			var r = secp256k1_ecmult_strauss_wnaf(state, @as, nas, ng, 1);
			state.Dispose();
			return r;
		}

		/// <summary>
		/// R = inp_g_sc * G + SUM(scalars[i] * points[i])
		/// </summary>
		/// <param name="inp_g_sc">The scalar for point G</param>
		/// <param name="scalars">The scalars</param>
		/// <param name="points">The points</param>
		/// <param name="options">Advanced options</param>
		/// <returns>R</returns>
		public GEJ MultBatch(in Scalar? inp_g_sc, ReadOnlySpan<Scalar> scalars, ReadOnlySpan<GE> points, MultBatchOptions? options)
		{
			options ??= new MultBatchOptions();
			GEJ r = GEJ.Infinity;
			if (scalars.Length != points.Length)
				throw new ArgumentException("The number of scalar should be equal to the number of poits");
			if (inp_g_sc is null && scalars.Length == 0)
				return r;
			else if (scalars.Length == 0)
			{
				r = r.AddVariable(Mult(r, Scalar.Zero, inp_g_sc), out _);
				return r;
			}
			var implementation = options.Implementation;
			if (implementation == ECMultiImplementation.Auto)
			{
				if (scalars.Length >= options.PippengerThreshold)
				{
					implementation = ECMultiImplementation.Pippenger;
				}
				else
				{
					implementation = ECMultiImplementation.Strauss;
				}
			}
			GEJ tmp;
			switch (implementation)
			{
				case ECMultiImplementation.Pippenger:
					ECMultContext.secp256k1_ecmult_pippenger_batch(inp_g_sc, scalars, points, out tmp);
					break;
				case ECMultiImplementation.Strauss:
					tmp = secp256k1_ecmult_strauss_batch(inp_g_sc, scalars, points);
					break;
				case ECMultiImplementation.Simple:
					tmp = ECMultContext.secp256k1_ecmult_multi_simple_var(this, inp_g_sc, scalars, points);
					break;
				default:
					throw new NotSupportedException(options.Implementation.ToString());
			}
			return r.AddVariable(tmp, out _);
		}

		/// <summary>
		/// R = inp_g_sc * G + SUM(scalars[i] * points[i])
		/// </summary>
		/// <param name="inp_g_sc">The scalar for point G</param>
		/// <param name="scalars">The scalars</param>
		/// <param name="points">The points</param>
		/// <param name="implementation">The implementation</param>
		public GEJ MultBatch(in Scalar? inp_g_sc, ReadOnlySpan<Scalar> scalars, ReadOnlySpan<GE> points, ECMultiImplementation implementation)
		{
			return MultBatch(inp_g_sc, scalars, points, new MultBatchOptions(implementation));
		}
		/// <summary>
		/// R = inp_g_sc * G + SUM(scalars[i] * points[i])
		/// </summary>
		/// <param name="inp_g_sc">The scalar for point G</param>
		/// <param name="scalars">The scalars</param>
		/// <param name="points">The points</param>
		public GEJ MultBatch(in Scalar? inp_g_sc, ReadOnlySpan<Scalar> scalars, ReadOnlySpan<GE> points)
		{
			return MultBatch(inp_g_sc, scalars, points, null);
		}

		/// <summary>
		/// R = SUM(scalars[i] * points[i])
		/// </summary>
		/// <param name="scalars">The scalars</param>
		/// <param name="points">The points</param>
		public GEJ MultBatch(ReadOnlySpan<Scalar> scalars, ReadOnlySpan<GE> points)
		{
			return MultBatch(null, scalars, points, null);
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
		internal static int secp256k1_ecmult_wnaf(Span<int> wnaf, int len, in Scalar a, int w)
		{
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
