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
	struct PippengerPointState
	{
		internal int skew_na;
		internal int input_pos;
	}
	struct PippengerState
	{
		public PippengerState(int entries, int wnaf_size)
		{
			ps = System.Buffers.ArrayPool<PippengerPointState>.Shared.Rent(entries);
			wnaf_na = System.Buffers.ArrayPool<int>.Shared.Rent(entries * wnaf_size);
			Array.Clear(wnaf_na, 0, entries * wnaf_size);
		}
		internal int[] wnaf_na;
		internal PippengerPointState[] ps;
		public void Dispose()
		{
			System.Buffers.ArrayPool<PippengerPointState>.Shared.Return(ps);
			System.Buffers.ArrayPool<int>.Shared.Return(wnaf_na);
		}
	}

#if SECP256K1_LIB
	public partial class ECMultContext
#else
	partial class ECMultContext
#endif
	{
		const int PIPPENGER_MAX_BUCKET_WINDOW = 12;
		/**
 * Returns optimal bucket_window (number of bits of a scalar represented by a
 * set of buckets) for a given number of points.
 */
		static int secp256k1_pippenger_bucket_window(int n)
		{
			if (n <= 1)
			{
				return 1;
			}
			else if (n <= 4)
			{
				return 2;
			}
			else if (n <= 20)
			{
				return 3;
			}
			else if (n <= 57)
			{
				return 4;
			}
			else if (n <= 136)
			{
				return 5;
			}
			else if (n <= 235)
			{
				return 6;
			}
			else if (n <= 1260)
			{
				return 7;
			}
			else if (n <= 4420)
			{
				return 9;
			}
			else if (n <= 7880)
			{
				return 10;
			}
			else if (n <= 16050)
			{
				return 11;
			}
			else
			{
				return PIPPENGER_MAX_BUCKET_WINDOW;
			}
		}

		internal static void secp256k1_ecmult_pippenger_batch(in Scalar? inp_g_sc, ReadOnlySpan<Scalar> scalarsInput, ReadOnlySpan<GE> pointsInput, out GEJ r)
		{
			/* Use 2(n+1) with the endomorphism, when calculating batch
     * sizes. The reason for +1 is that we add the G scalar to the list of
     * other scalars. */
			int n_points = scalarsInput.Length;
			int entries = 2 * n_points + 2;
			GE[] points;
			Scalar[] scalars;
			GEJ[] buckets;

			PippengerState state_space;
			int idx = 0;
			int point_idx = 0;
			int bucket_window;
			r = GEJ.Infinity;
			if (inp_g_sc is null && n_points == 0)
			{
				return;
			}
			bucket_window = secp256k1_pippenger_bucket_window(n_points);
			int wnaf_size = WNAF_SIZE(bucket_window + 1);
			points = System.Buffers.ArrayPool<GE>.Shared.Rent(entries);
			scalars = System.Buffers.ArrayPool<Scalar>.Shared.Rent(entries);
			state_space = new PippengerState(entries, wnaf_size);
			buckets = System.Buffers.ArrayPool<GEJ>.Shared.Rent(1 << bucket_window);

			if (inp_g_sc is Scalar c)
			{
				scalars[0] = c;
				points[0] = EC.G;
				idx++;
				secp256k1_ecmult_endo_split(ref scalars[0], ref scalars[1], ref points[0], ref points[1]);
				idx++;
			}

			while (point_idx < n_points)
			{
				points[idx] = pointsInput[point_idx];
				scalars[idx] = scalarsInput[point_idx];
				idx++;
				secp256k1_ecmult_endo_split(ref scalars[idx - 1], ref scalars[idx], ref points[idx - 1], ref points[idx]);
				idx++;
				point_idx++;
			}
			r = secp256k1_ecmult_pippenger_wnaf(buckets, bucket_window, state_space, scalars, points, idx);

			/* Clear data */
			state_space.Dispose();
			System.Buffers.ArrayPool<GEJ>.Shared.Return(buckets, true);
			System.Buffers.ArrayPool<GE>.Shared.Return(points, true);
			System.Buffers.ArrayPool<Scalar>.Shared.Return(scalars, true);
		}

		/*
		 * pippenger_wnaf computes the result of a multi-point multiplication as
		 * follows: The scalars are brought into wnaf with n_wnaf elements each. Then
		 * for every i < n_wnaf, first each point is added to a "bucket" corresponding
		 * to the point's wnaf[i]. Second, the buckets are added together such that
		 * r += 1*bucket[0] + 3*bucket[1] + 5*bucket[2] + ...
		 */
		static GEJ secp256k1_ecmult_pippenger_wnaf(Span<GEJ> buckets, int bucket_window, PippengerState state, ReadOnlySpan<Scalar> sc, ReadOnlySpan<GE> pt, int num)
		{
			GEJ r;
			int n_wnaf = WNAF_SIZE(bucket_window + 1);
			int np;
			int no = 0;
			int i;
			int j;
			var statewnaf_na = state.wnaf_na.AsSpan();
			for (np = 0; np < num; ++np)
			{
				if (sc[np].IsZero || pt[np].IsInfinity)
				{
					continue;
				}
				state.ps[no].input_pos = np;
				state.ps[no].skew_na = Wnaf.Fixed(statewnaf_na.Slice(no * n_wnaf), sc[np], bucket_window + 1);
				no++;
			}

			r = GEJ.Infinity;
			if (no == 0)
			{
				return r;
			}

			var tableSize = ECMULT_TABLE_SIZE(bucket_window + 2);
			for (i = n_wnaf - 1; i >= 0; i--)
			{
				GEJ running_sum;


				for (j = 0; j < tableSize; j++)
				{
					buckets[j] = GEJ.Infinity;

				}

				for (np = 0; np < no; ++np)
				{
					int n = state.wnaf_na[np * n_wnaf + i];


					var point_state = state.ps[np];
					GE tmp;
					int idx;

					if (i == 0)
					{
						/* correct for wnaf skew */
						int skew = point_state.skew_na;
						if (skew != 0)
						{
							tmp = pt[point_state.input_pos].Negate();
							buckets[0] = buckets[0].AddVariable(tmp, out _);
						}
					}
					if (n > 0)
					{
						idx = (n - 1) / 2;
						buckets[idx] = buckets[idx].AddVariable(pt[point_state.input_pos], out _);
					}
					else if (n < 0)
					{
						idx = -(n + 1) / 2;
						tmp = pt[point_state.input_pos].Negate();
						buckets[idx] = buckets[idx].AddVariable(tmp, out _);
					}
				}

				for (j = 0; j < bucket_window; j++)
				{
					r = r.DoubleVariable();
				}

				running_sum = GEJ.Infinity;
				/* Accumulate the sum: bucket[0] + 3*bucket[1] + 5*bucket[2] + 7*bucket[3] + ...
				 *                   = bucket[0] +   bucket[1] +   bucket[2] +   bucket[3] + ...
				 *                   +         2 *  (bucket[1] + 2*bucket[2] + 3*bucket[3] + ...)
				 * using an intermediate running sum:
				 * running_sum = bucket[0] +   bucket[1] +   bucket[2] + ...
				 *
				 * The doubling is done implicitly by deferring the final window doubling (of 'r').
				 */
				for (j = tableSize - 1; j > 0; j--)
				{
					running_sum = running_sum.AddVariable(buckets[j], out _);
					r = r.AddVariable(running_sum, out _);
				}

				running_sum = running_sum.AddVariable(buckets[0], out _);
				r = r.DoubleVariable();
				r = r.AddVariable(running_sum, out _);
			}
			return r;
		}
	}
}
#nullable restore
#endif
