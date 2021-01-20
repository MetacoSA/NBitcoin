#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace NBitcoin.Secp256k1
{
	static class Wnaf
	{
		public const int BITS = 128;
		/* Convert a number to WNAF notation.
		 *  The number becomes represented by sum(2^{wi} * wnaf[i], i=0..WNAF_SIZE(w)+1) - return_val.
		 *  It has the following guarantees:
		 *  - each wnaf[i] an odd integer between -(1 << w) and (1 << w)
		 *  - each wnaf[i] is nonzero
		 *  - the number of words set is always WNAF_SIZE(w) + 1
		 *
		 *  Adapted from `The Width-w NAF Method Provides Small Memory and Fast Elliptic Scalar
		 *  Multiplications Secure against Side Channel Attacks`, Okeya and Tagaki. M. Joye (Ed.)
		 *  CT-RSA 2003, LNCS 2612, pp. 328-443, 2003. Springer-Verlagy Berlin Heidelberg 2003
		 *
		 *  Numbers reference steps of `Algorithm SPA-resistant Width-w NAF with Odd Scalar` on pp. 335
		 */
		public static int Const(Span<int> wnaf, Scalar s, int w, int size)
		{
			int global_sign;
			int word = 0;

			/* 1 2 3 */
			int u_last;
			int u = 0;

			int flip;
			int bit;
			Scalar neg_s;
			int not_neg_one;
			/* Note that we cannot handle even numbers by negating them to be odd, as is
			 * done in other implementations, since if our scalars were specified to have
			 * width < 256 for performance reasons, their negations would have width 256
			 * and we'd lose any performance benefit. Instead, we use a technique from
			 * Section 4.2 of the Okeya/Tagaki paper, which is to add either 1 (for even)
			 * or 2 (for odd) to the number we are encoding, returning a skew value indicating
			 * this, and having the caller compensate after doing the multiplication.
			 *
			 * In fact, we _do_ want to negate numbers to minimize their bit-lengths (and in
			 * particular, to ensure that the outputs from the endomorphism-split fit into
			 * 128 bits). If we negate, the parity of our number flips, inverting which of
			 * {1, 2} we want to add to the scalar when ensuring that it's odd. Further
			 * complicating things, -1 interacts badly with `secp256k1_scalar_cadd_bit` and
			 * we need to special-case it in this logic. */
			flip = s.IsHigh ? 1 : 0;
			/* We add 1 to even numbers, 2 to odd ones, noting that negation flips parity */
			bit = flip ^ (s.IsEven ? 0 : 1);
			/* We check for negative one, since adding 2 to it will cause an overflow */
			neg_s = s.Negate();

			not_neg_one = neg_s.IsOne ? 0 : 1;
			s = s.CAddBit((uint)bit, not_neg_one);
			/* If we had negative one, flip == 1, s.d[0] == 0, bit == 1, so caller expects
			 * that we added two to it and flipped it. In fact for -1 these operations are
			 * identical. We only flipped, but since skewing is required (in the sense that
			 * the skew must be 1 or 2, never zero) and flipping is not, we need to change
			 * our flags to claim that we only skewed. */
			global_sign = s.CondNegate(flip, out s);
			global_sign *= not_neg_one * 2 - 1;
			int skew = 1 << bit;

			/* 4 */
			u_last = s.ShrInt(w, out s);
			while (word * w < size)
			{
				int sign;
				int even;

				/* 4.1 4.4 */
				u = s.ShrInt(w, out s);
				/* 4.2 */
				even = ((u & 1) == 0) ? 1 : 0;
				sign = 2 * (u_last > 0 ? 1 : 0) - 1;
				u += sign * even;
				u_last -= sign * even * (1 << w);

				/* 4.3, adapted for global sign change */
				wnaf[word++] = u_last * global_sign;

				u_last = u;
			}
			wnaf[word] = u * global_sign;

			VERIFY_CHECK(s.IsZero);
			VERIFY_CHECK(word == SIZE_BITS(size, w));
			return skew;
		}

		/** Convert a number to WNAF notation.
		 *  The number becomes represented by sum(2^{wi} * wnaf[i], i=0..WNAF_SIZE(w)+1) - return_val.
		 *  It has the following guarantees:
		 *  - each wnaf[i] is either 0 or an odd integer between -(1 << w) and (1 << w)
		 *  - the number of words set is always WNAF_SIZE(w)
		 *  - the returned skew is 0 or 1
		 */
		internal static int Fixed(Span<int> wnaf, in Scalar s, int w)
		{
			int skew = 0;
			int pos;
			int max_pos;
			int last_w;
			ref readonly Scalar work = ref s;

			if (s.IsZero)
			{
				for (pos = 0; pos < SIZE(w); pos++)
				{
					wnaf[pos] = 0;
				}
				return 0;
			}

			if (s.IsEven)
			{
				skew = 1;
			}

			wnaf[0] = (int)work.GetBitsVariable(0, w) + skew;
			/* Compute last window size. Relevant when window size doesn't divide the
			 * number of bits in the scalar */
			last_w = BITS - (SIZE(w) - 1) * w;

			/* Store the position of the first nonzero word in max_pos to allow
			 * skipping leading zeros when calculating the wnaf. */
			for (pos = SIZE(w) - 1; pos > 0; pos--)
			{
				int val = (int)work.GetBitsVariable(pos * w, pos == SIZE(w) - 1 ? last_w : w);
				if (val != 0)
				{
					break;
				}
				wnaf[pos] = 0;
			}
			max_pos = pos;
			pos = 1;

			while (pos <= max_pos)
			{
				int val = (int)work.GetBitsVariable(pos * w, pos == SIZE(w) - 1 ? last_w : w);
				if ((val & 1) == 0)
				{
					wnaf[pos - 1] -= (1 << w);
					wnaf[pos] = (val + 1);
				}
				else
				{
					wnaf[pos] = val;
				}
				/* Set a coefficient to zero if it is 1 or -1 and the proceeding digit
				 * is strictly negative or strictly positive respectively. Only change
				 * coefficients at previous positions because above code assumes that
				 * wnaf[pos - 1] is odd.
				 */
				if (pos >= 2 && ((wnaf[pos - 1] == 1 && wnaf[pos - 2] < 0) || (wnaf[pos - 1] == -1 && wnaf[pos - 2] > 0)))
				{
					if (wnaf[pos - 1] == 1)
					{
						wnaf[pos - 2] += 1 << w;
					}
					else
					{
						wnaf[pos - 2] -= 1 << w;
					}
					wnaf[pos - 1] = 0;
				}
				++pos;
			}

			return skew;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int SIZE(int w)
		{
			return SIZE_BITS(BITS, w);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int SIZE_BITS(int bits, int w)
		{
			return ((bits) + (w) - 1) / (w);
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

