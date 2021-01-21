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
	unsafe struct StraussPointState
	{
		internal Scalar na_1, na_lam;
		internal fixed int wnaf_na_1[130];
		internal fixed int wnaf_na_lam[130];
		internal int bits_na_1;
		internal int bits_na_lam;
		internal int input_pos;
	}
	struct StraussState
	{
		public StraussState(int size, int pointStates)
		{
			prej = System.Buffers.ArrayPool<GEJ>.Shared.Rent(pointStates * size);
			zr = System.Buffers.ArrayPool<FE>.Shared.Rent(pointStates * size);
			pre_a = System.Buffers.ArrayPool<GE>.Shared.Rent(pointStates * size);
			pre_a_lam = System.Buffers.ArrayPool<GE>.Shared.Rent(pointStates * size);
			ps = System.Buffers.ArrayPool<StraussPointState>.Shared.Rent(pointStates);
			Array.Clear(ps, 0, pointStates);
		}
		internal GEJ[] prej;
		internal FE[] zr;
		internal GE[] pre_a;
		internal GE[] pre_a_lam;
		internal StraussPointState[] ps;

		internal void Dispose()
		{
			System.Buffers.ArrayPool<GEJ>.Shared.Return(prej);
			System.Buffers.ArrayPool<FE>.Shared.Return(zr);
			System.Buffers.ArrayPool<GE>.Shared.Return(pre_a);
			System.Buffers.ArrayPool<GE>.Shared.Return(pre_a_lam);
			System.Buffers.ArrayPool<StraussPointState>.Shared.Return(ps);
		}
	}

#if SECP256K1_LIB
	public partial class ECMultContext
#else
	partial class ECMultContext
#endif
	{

		internal GEJ secp256k1_ecmult_strauss_batch(in Scalar? inp_g_sc, ReadOnlySpan<Scalar> scalars, ReadOnlySpan<GE> points)
		{
			GEJ r = GEJ.Infinity;
			StraussState state = new StraussState(ArraySize_A, points.Length);
			var pointsJ = System.Buffers.ArrayPool<GEJ>.Shared.Rent(points.Length);
			for (int i = 0; i < points.Length; i++)
			{
				pointsJ[i] = points[i].ToGroupElementJacobian();
			}
			r = secp256k1_ecmult_strauss_wnaf(state, pointsJ.AsSpan(), scalars, inp_g_sc, points.Length);
			System.Buffers.ArrayPool<GEJ>.Shared.Return(pointsJ);
			state.Dispose();
			return r;
		}
		unsafe GEJ secp256k1_ecmult_strauss_wnaf(in StraussState state, in ReadOnlySpan<GEJ> a, in ReadOnlySpan<Scalar> na, in Scalar? ng, int num)
		{
			var r = GEJ.Infinity;

			// secp256k1_ecmult_strauss_wnaf
			GE tmpa;
			FE Z = FE.Zero;

			var pre_a_span = state.pre_a.AsSpan().Slice(0, num * ArraySize_A);
			var pre_a_lam_span = state.pre_a_lam.AsSpan().Slice(0, num * ArraySize_A);
			var prej_span = state.prej.AsSpan().Slice(0, num * ArraySize_A);
			var zr_span = state.zr.AsSpan().Slice(0, num * ArraySize_A);
			var ps_span = state.ps.AsSpan().Slice(0, num);
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
				fixed (int* naPointer = ps_span[no].wnaf_na_1)
				fixed (int* nalamPointer = ps_span[no].wnaf_na_lam)
				{
					var ps_wnaf_na_1 = new Span<int>(naPointer, 130);
					var ps_wnaf_na_lam = new Span<int>(nalamPointer, 130);
					if (na[np].IsZero || a[np].IsInfinity)
					{
						continue;
					}
					ps_span[no].input_pos = np;
					/* split na into na_1 and na_lam (where na = na_1 + na_lam*lambda, and na_1 and na_lam are ~128 bit) */
					na[np].SplitLambda(out ps_span[no].na_1, out ps_span[no].na_lam);

					/* build wnaf representation for na_1 and na_lam. */
					ps_span[no].bits_na_1 = secp256k1_ecmult_wnaf(ps_wnaf_na_1, 129, ps_span[no].na_1, WINDOW_A);
					ps_span[no].bits_na_lam = secp256k1_ecmult_wnaf(ps_wnaf_na_lam, 129, ps_span[no].na_lam, WINDOW_A);
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

				bits_ng_1 = secp256k1_ecmult_wnaf(wnaf_ng_1, 129, ng_1, WINDOW_G);
				bits_ng_128 = secp256k1_ecmult_wnaf(wnaf_ng_128, 129, ng_128, WINDOW_G);
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
					fixed (int* naPointer = ps_span[np].wnaf_na_1)
					fixed (int* nalamPointer = ps_span[np].wnaf_na_lam)
					{
						var ps_wnaf_na_1 = new Span<int>(naPointer, 130);
						var ps_wnaf_na_lam = new Span<int>(nalamPointer, 130);

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
	}
}
#nullable restore
#endif
