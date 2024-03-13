#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace NBitcoin.Secp256k1
{
#if SECP256K1_LIB
	public
#endif
	readonly struct FE : IEquatable<FE>
	{
#if SECP256K1_LIB
		public
#else
		internal
#endif
		readonly uint n0, n1, n2, n3, n4, n5, n6, n7, n8, n9;
#if SECP256K1_LIB
		public
#else
		internal
#endif
		readonly int magnitude;
#if SECP256K1_LIB
		public
#else
		internal
#endif
		readonly bool normalized;

		static readonly FE _Zero = new FE(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, true);

		public static ref readonly FE Zero => ref _Zero;

		public FE(uint a)
		{
			n0 = a;
			n1 = n2 = n3 = n4 = n5 = n6 = n7 = n8 = n9 = 0;
			magnitude = 1;
			normalized = true;
			VERIFY();
		}

		public static bool TryCreate(ReadOnlySpan<byte> bytes, [MaybeNullWhen(false)] out FE fieldElement)
		{
			var fe = new FE(bytes, false, out var isValid);
			if (isValid)
			{
				fieldElement = fe;
				return true;
			}
			fieldElement = default;
			return false;
		}

		public FE(ReadOnlySpan<byte> in32): this(in32, true, out _)
		{

		}
		FE(ReadOnlySpan<byte> in32, bool throws, out bool isValid)
		{
			n0 = in32[31] | ((uint)in32[30] << 8) | ((uint)in32[29] << 16) | ((uint)(in32[28] & 0x3) << 24);
			n1 = (uint)((in32[28] >> 2) & 0x3f) | ((uint)in32[27] << 6) | ((uint)in32[26] << 14) | ((uint)(in32[25] & 0xf) << 22);
			n2 = (uint)((in32[25] >> 4) & 0xf) | ((uint)in32[24] << 4) | ((uint)in32[23] << 12) | ((uint)(in32[22] & 0x3f) << 20);
			n3 = (uint)((in32[22] >> 6) & 0x3) | ((uint)in32[21] << 2) | ((uint)in32[20] << 10) | ((uint)in32[19] << 18);
			n4 = in32[18] | ((uint)in32[17] << 8) | ((uint)in32[16] << 16) | ((uint)(in32[15] & 0x3) << 24);
			n5 = (uint)((in32[15] >> 2) & 0x3f) | ((uint)in32[14] << 6) | ((uint)in32[13] << 14) | ((uint)(in32[12] & 0xf) << 22);
			n6 = (uint)((in32[12] >> 4) & 0xf) | ((uint)in32[11] << 4) | ((uint)in32[10] << 12) | ((uint)(in32[9] & 0x3f) << 20);
			n7 = (uint)((in32[9] >> 6) & 0x3) | ((uint)in32[8] << 2) | ((uint)in32[7] << 10) | ((uint)in32[6] << 18);
			n8 = in32[5] | ((uint)in32[4] << 8) | ((uint)in32[3] << 16) | ((uint)(in32[2] & 0x3) << 24);
			n9 = (uint)((in32[2] >> 2) & 0x3f) | ((uint)in32[1] << 6) | ((uint)in32[0] << 14);
			if (n9 == 0x3FFFFFUL && (n8 & n7 & n6 & n5 & n4 & n3 & n2) == 0x3FFFFFFUL && (n1 + 0x40UL + ((n0 + 0x3D1UL) >> 26)) > 0x3FFFFFFUL)
			{
				if (throws)
					throw new ArgumentException(paramName: nameof(in32), message: "Invalid Field");
				else
				{
					isValid = false;
					magnitude = 1;
					normalized = true;
					return;
				}
			}
			magnitude = 1;
			normalized = true;
			isValid = true;
			VERIFY();
		}

		/// <summary>
		/// Create a field element from uint, most significative uint first. (big endian)
		/// </summary>
		/// <param name="d7"></param>
		/// <param name="d6"></param>
		/// <param name="d5"></param>
		/// <param name="d4"></param>
		/// <param name="d3"></param>
		/// <param name="d2"></param>
		/// <param name="d1"></param>
		/// <param name="d0"></param>
		/// <returns>A field element</returns>
		public static FE CONST(uint d7, uint d6, uint d5, uint d4, uint d3, uint d2, uint d1, uint d0)
		{
			return new FE((d0) & 0x3FFFFFFU,
	(d0 >> 26) | ((d1 & 0xFFFFFU) << 6),
	(d1 >> 20) | ((d2 & 0x3FFFU) << 12),
	(d2 >> 14) | ((d3 & 0xFFU) << 18),
	(d3 >> 8) | ((d4 & 0x3U) << 24),
	(d4 >> 2) & 0x3FFFFFFU,
	(d4 >> 28) | ((d5 & 0x3FFFFFU) << 4),
	(d5 >> 22) | ((d6 & 0xFFFFU) << 10),
	(d6 >> 16) | ((d7 & 0x3FFU) << 16),
	(d7 >> 10), 1, true);
		}


		public readonly bool NormalizesToZeroVariable()
		{
			Span<uint> t = stackalloc uint[NCount];
			uint z0, z1;
			uint x;

			t[0] = n0;
			t[9] = n9;

			/* Reduce t9 at the start so there will be at most a single carry from the first pass */
			x = t[9] >> 22;

			/* The first pass ensures the magnitude is 1, ... */
			t[0] += x * 0x3D1U;

			/* z0 tracks a possible raw value of 0, z1 tracks a possible raw value of P */
			z0 = t[0] & 0x3FFFFFFU;
			z1 = z0 ^ 0x3D0U;

			/* Fast return path should catch the majority of cases */
			if ((z0 != 0UL) & (z1 != 0x3FFFFFFUL))
			{
				return false;
			}


			t[1] = n1;
			t[2] = n2;
			t[3] = n3;
			t[4] = n4;
			t[5] = n5;
			t[6] = n6;
			t[7] = n7;
			t[8] = n8;

			t[9] &= 0x03FFFFFU;
			t[1] += (x << 6);

			t[1] += (t[0] >> 26);
			t[2] += (t[1] >> 26); t[1] &= 0x3FFFFFFU; z0 |= t[1]; z1 &= t[1] ^ 0x40U;
			t[3] += (t[2] >> 26); t[2] &= 0x3FFFFFFU; z0 |= t[2]; z1 &= t[2];
			t[4] += (t[3] >> 26); t[3] &= 0x3FFFFFFU; z0 |= t[3]; z1 &= t[3];
			t[5] += (t[4] >> 26); t[4] &= 0x3FFFFFFU; z0 |= t[4]; z1 &= t[4];
			t[6] += (t[5] >> 26); t[5] &= 0x3FFFFFFU; z0 |= t[5]; z1 &= t[5];
			t[7] += (t[6] >> 26); t[6] &= 0x3FFFFFFU; z0 |= t[6]; z1 &= t[6];
			t[8] += (t[7] >> 26); t[7] &= 0x3FFFFFFU; z0 |= t[7]; z1 &= t[7];
			t[9] += (t[8] >> 26); t[8] &= 0x3FFFFFFU; z0 |= t[8]; z1 &= t[8];
			z0 |= t[9]; z1 &= t[9] ^ 0x3C00000U;

			/* ... except for a possible carry at bit 22 of t9 (i.e. bit 256 of the field element) */
			VERIFY_CHECK(t[9] >> 23 == 0);

			return (z0 == 0) | (z1 == 0x3FFFFFFUL);
		}

		public readonly bool EqualsXVariable(in GEJ a)
		{
			FE r, r2;
			VERIFY_CHECK(!a.infinity);
			r = a.z.Sqr();
			r *= this;
			r2 = a.x;
			r2 = r2.NormalizeWeak();
			return r.EqualsVariable(r2);
		}

		public static void Clear(ref FE s)
		{
			s = FE.Zero;
		}

		public readonly FE InverseVariable()
		{
			return this.Inverse();
		}

		public FE(uint n0, uint n1, uint n2, uint n3, uint n4, uint n5, uint n6, uint n7, uint n8, uint n9)
		{
			this.n0 = n0;
			this.n1 = n1;
			this.n2 = n2;
			this.n3 = n3;
			this.n4 = n4;
			this.n5 = n5;
			this.n6 = n6;
			this.n7 = n7;
			this.n8 = n8;
			this.n9 = n9;
			if (n9 == 0x3FFFFFUL && (n8 & n7 & n6 & n5 & n4 & n3 & n2) == 0x3FFFFFFUL && (n1 + 0x40UL + ((n0 + 0x3D1UL) >> 26)) > 0x3FFFFFFUL)
			{
				throw new ArgumentException(paramName: "n", message: "Invalid Field");
			}
			magnitude = 1;
			normalized = true;
			VERIFY();
		}

		public readonly bool Sqrt(out FE result)
		{
			ref readonly FE a = ref this;
			/* Given that p is congruent to 3 mod 4, we can compute the square root of
			 *  a mod p as the (p+1)/4'th power of a.
			 *
			 *  As (p+1)/4 is an even number, it will have the same result for a and for
			 *  (-a). Only one of these two numbers actually has a square root however,
			 *  so we test at the end by squaring and comparing to the input.
			 *  Also because (p+1)/4 is an even number, the computed square root is
			 *  itself always a square (a ** ((p+1)/4) is the square of a ** ((p+1)/8)).
			 */
			FE x2, x3, x6, x9, x11, x22, x44, x88, x176, x220, x223, t1;

			/* The binary representation of (p + 1)/4 has 3 blocks of 1s, with lengths in
			 *  { 2, 22, 223 }. Use an addition chain to calculate 2^n - 1 for each block:
			 *  1, [2], 3, 6, 9, 11, [22], 44, 88, 176, 220, [223]
			 */

			x2 = a.Sqr();
			x2 = x2 * a;

			x3 = x2.Sqr();
			x3 = x3 * a;

			x6 = x3;
			x6 = x6.Sqr(3);
			x6 = x6 * x3;

			x9 = x6;
			x9 = x9.Sqr(3);
			x9 = x9 * x3;

			x11 = x9;
			x11 = x11.Sqr(2);
			x11 = x11 * x2;

			x22 = x11;
			x22 = x22.Sqr(11);
			x22 = x22 * x11;

			x44 = x22;
			x44 = x44.Sqr(22);
			x44 = x44 * x22;

			x88 = x44;
			x88 = x88.Sqr(44);
			x88 = x88 * x44;

			x176 = x88;
			x176 = x176.Sqr(88);
			x176 = x176 * x88;

			x220 = x176;
			x220 = x220.Sqr(44);
			x220 = x220 * x44;

			x223 = x220;
			x223 = x223.Sqr(3);
			x223 = x223 * x3;

			/* The final result is then assembled using a sliding window over the blocks. */

			t1 = x223;
			t1 = t1.Sqr(23);
			t1 = t1 * x22;
			t1 = t1.Sqr(6);
			t1 = t1 * x2;
			t1 = t1.Sqr();
			result = t1.Sqr();

			/* Check that a square root was actually calculated */

			t1 = result.Sqr();
			return t1.Equals(a);
		}

		public readonly FEStorage ToStorage()
		{
			Span<uint> n = stackalloc uint[FEStorage.NCount];
			ref readonly FE a = ref this;
			VERIFY_CHECK(a.normalized);
			n[0] = a.n0 | a.n1 << 26;
			n[1] = a.n1 >> 6 | a.n2 << 20;
			n[2] = a.n2 >> 12 | a.n3 << 14;
			n[3] = a.n3 >> 18 | a.n4 << 8;
			n[4] = a.n4 >> 24 | a.n5 << 2 | a.n6 << 28;
			n[5] = a.n6 >> 4 | a.n7 << 22;
			n[6] = a.n7 >> 10 | a.n8 << 16;
			n[7] = a.n8 >> 16 | a.n9 << 10;
			return new FEStorage(n);
		}

		public readonly int CompareToVariable(in FE b)
		{
			ref readonly FE a = ref this;
			int i;
			VERIFY_CHECK(a.normalized);
			VERIFY_CHECK(b.normalized);
			a.VERIFY();
			b.VERIFY();
			for (i = 9; i >= 0; i--)
			{
				if (a.At(i) > b.At(i))
				{
					return 1;
				}
				if (a.At(i) < b.At(i))
				{
					return -1;
				}
			}
			return 0;
		}

		internal uint At(int index)
		{
			switch (index)
			{
				case 0:
					return n0;
				case 1:
					return n1;
				case 2:
					return n2;
				case 3:
					return n3;
				case 4:
					return n4;
				case 5:
					return n5;
				case 6:
					return n6;
				case 7:
					return n7;
				case 8:
					return n8;
				case 9:
					return n9;
				default:
					throw new ArgumentOutOfRangeException(nameof(index), "index should 0-7 inclusive");
			}
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly FE Inverse()
		{
			FE x2, x3, x6, x9, x11, x22, x44, x88, x176, x220, x223, t1;
			ref readonly FE a = ref this;
			/* The binary representation of (p - 2) has 5 blocks of 1s, with lengths in
			 *  { 1, 2, 22, 223 }. Use an addition chain to calculate 2^n - 1 for each block:
			 *  [1], [2], 3, 6, 9, 11, [22], 44, 88, 176, 220, [223]
			 */

			x2 = a.Sqr();
			x2 = x2 * a;

			x3 = x2.Sqr();
			x3 = x3 * a;

			x6 = x3;
			x6 = x6.Sqr(3);
			x6 = x6 * x3;

			x9 = x6;
			x9 = x9.Sqr(3);
			x9 = x9 * x3;

			x11 = x9;
			x11 = x11.Sqr(2);

			x11 = x11 * x2;

			x22 = x11;
			x22 = x22.Sqr(11);
			x22 = x22 * x11;

			x44 = x22;
			x44 = x44.Sqr(22);
			x44 = x44 * x22;

			x88 = x44;
			x88 = x88.Sqr(44);
			x88 = x88 * x44;

			x176 = x88;
			x176 = x176.Sqr(88);
			x176 = x176 * x88;

			x220 = x176;
			x220 = x220.Sqr(44);
			x220 = x220 * x44;

			x223 = x220;
			x223 = x223.Sqr(3);
			x223 = x223 * x3;

			/* The final result is then assembled using a sliding window over the blocks. */

			t1 = x223;
			t1 = t1.Sqr(23);
			t1 = t1 * x22;

			t1 = t1.Sqr(5);
			t1 = t1 * a;
			t1 = t1.Sqr(3);
			t1 = t1 * x2;
			t1 = t1.Sqr(2);
			return a * t1;
		}
		public readonly FE Sqr()
		{
			return Sqr(1);
		}
		public readonly FE Sqr(int times)
		{
			VERIFY_CHECK(this.magnitude <= 8);
			VERIFY();
			var r = secp256k1_fe_sqr_inner(times, 1, false);
			r.VERIFY();
			return r;
		}

		public static void InverseAllVariable(FE[] r, FE[] a, int len)
		{
			FE u;
			int i;
			if (len < 1)
			{
				return;
			}

			VERIFY_CHECK(r != a);

			r[0] = a[0];

			i = 0;
			while (++i < len)
			{
				r[i] = r[i - 1] * a[i];
			}

			u = r[--i].InverseVariable();

			while (i > 0)
			{
				int j = i--;
				r[j] = r[i] * u;
				u = u * a[j];
			}

			r[0] = u;
		}

		private readonly FE secp256k1_fe_sqr_inner(int times, int magnitude, bool normalized)
		{
			Span<uint> n = stackalloc uint[NCount];
			this.Deconstruct(ref n, out _, out _);
			ulong c, d;
			ulong u0, u1, u2, u3, u4, u5, u6, u7, u8;
			uint t9, t0, t1, t2, t3, t4, t5, t6, t7;
			const uint M = 0x3FFFFFFU, R0 = 0x3D10U, R1 = 0x400U;
			for (int i = 0; i < times; i++)
			{
				VERIFY_BITS(n[0], 30);
				VERIFY_BITS(n[1], 30);
				VERIFY_BITS(n[2], 30);
				VERIFY_BITS(n[3], 30);
				VERIFY_BITS(n[4], 30);
				VERIFY_BITS(n[5], 30);
				VERIFY_BITS(n[6], 30);
				VERIFY_BITS(n[7], 30);
				VERIFY_BITS(n[8], 30);
				VERIFY_BITS(n[9], 26);
				/* [... a b c] is a shorthand for ... + a<<52 + b<<26 + c<<0 mod n.
				 *  px is a shorthand for sum(n[i]*a[x-i], i=0..x).
				 *  Note that [x 0 0 0 0 0 0 0 0 0 0] = [x*R1 x*R0].
				 */

				d = (ulong)(n[0] * 2) * n[9]
				   + (ulong)(n[1] * 2) * n[8]
				   + (ulong)(n[2] * 2) * n[7]
				   + (ulong)(n[3] * 2) * n[6]
				   + (ulong)(n[4] * 2) * n[5];
				/* VERIFY_BITS(d, 64); */
				/* [d 0 0 0 0 0 0 0 0 0] = [p9 0 0 0 0 0 0 0 0 0] */
				t9 = (uint)(d & M); d >>= 26;
				VERIFY_BITS(t9, 26);
				VERIFY_BITS(d, 38);
				/* [d t9 0 0 0 0 0 0 0 0 0] = [p9 0 0 0 0 0 0 0 0 0] */

				c = (ulong)n[0] * n[0];
				VERIFY_BITS(c, 60);
				/* [d t9 0 0 0 0 0 0 0 0 c] = [p9 0 0 0 0 0 0 0 0 p0] */
				d += (ulong)(n[1] * 2) * n[9]
				   + (ulong)(n[2] * 2) * n[8]
				   + (ulong)(n[3] * 2) * n[7]
				   + (ulong)(n[4] * 2) * n[6]
				   + (ulong)n[5] * n[5];
				VERIFY_BITS(d, 63);
				/* [d t9 0 0 0 0 0 0 0 0 c] = [p10 p9 0 0 0 0 0 0 0 0 p0] */
				u0 = (uint)(d & M); d >>= 26; c += u0 * R0;
				VERIFY_BITS(u0, 26);
				VERIFY_BITS(d, 37);
				VERIFY_BITS(c, 61);
				/* [d u0 t9 0 0 0 0 0 0 0 0 c-u0*R0] = [p10 p9 0 0 0 0 0 0 0 0 p0] */
				t0 = (uint)(c & M); c >>= 26; c += u0 * R1;
				VERIFY_BITS(t0, 26);
				VERIFY_BITS(c, 37);
				/* [d u0 t9 0 0 0 0 0 0 0 c-u0*R1 t0-u0*R0] = [p10 p9 0 0 0 0 0 0 0 0 p0] */
				/* [d 0 t9 0 0 0 0 0 0 0 c t0] = [p10 p9 0 0 0 0 0 0 0 0 p0] */

				c += (ulong)(n[0] * 2) * n[1];
				VERIFY_BITS(c, 62);
				/* [d 0 t9 0 0 0 0 0 0 0 c t0] = [p10 p9 0 0 0 0 0 0 0 p1 p0] */
				d += (ulong)(n[2] * 2) * n[9]
				   + (ulong)(n[3] * 2) * n[8]
				   + (ulong)(n[4] * 2) * n[7]
				   + (ulong)(n[5] * 2) * n[6];
				VERIFY_BITS(d, 63);
				/* [d 0 t9 0 0 0 0 0 0 0 c t0] = [p11 p10 p9 0 0 0 0 0 0 0 p1 p0] */
				u1 = (uint)(d & M); d >>= 26; c += u1 * R0;
				VERIFY_BITS(u1, 26);
				VERIFY_BITS(d, 37);
				VERIFY_BITS(c, 63);
				/* [d u1 0 t9 0 0 0 0 0 0 0 c-u1*R0 t0] = [p11 p10 p9 0 0 0 0 0 0 0 p1 p0] */
				t1 = (uint)(c & M); c >>= 26; c += u1 * R1;
				VERIFY_BITS(t1, 26);
				VERIFY_BITS(c, 38);
				/* [d u1 0 t9 0 0 0 0 0 0 c-u1*R1 t1-u1*R0 t0] = [p11 p10 p9 0 0 0 0 0 0 0 p1 p0] */
				/* [d 0 0 t9 0 0 0 0 0 0 c t1 t0] = [p11 p10 p9 0 0 0 0 0 0 0 p1 p0] */

				c += (ulong)(n[0] * 2) * n[2]
				   + (ulong)n[1] * n[1];
				VERIFY_BITS(c, 62);
				/* [d 0 0 t9 0 0 0 0 0 0 c t1 t0] = [p11 p10 p9 0 0 0 0 0 0 p2 p1 p0] */
				d += (ulong)(n[3] * 2) * n[9]
				   + (ulong)(n[4] * 2) * n[8]
				   + (ulong)(n[5] * 2) * n[7]
				   + (ulong)n[6] * n[6];
				VERIFY_BITS(d, 63);
				/* [d 0 0 t9 0 0 0 0 0 0 c t1 t0] = [p12 p11 p10 p9 0 0 0 0 0 0 p2 p1 p0] */
				u2 = (uint)(d & M); d >>= 26; c += u2 * R0;
				VERIFY_BITS(u2, 26);
				VERIFY_BITS(d, 37);
				VERIFY_BITS(c, 63);
				/* [d u2 0 0 t9 0 0 0 0 0 0 c-u2*R0 t1 t0] = [p12 p11 p10 p9 0 0 0 0 0 0 p2 p1 p0] */
				t2 = (uint)(c & M); c >>= 26; c += u2 * R1;
				VERIFY_BITS(t2, 26);
				VERIFY_BITS(c, 38);
				/* [d u2 0 0 t9 0 0 0 0 0 c-u2*R1 t2-u2*R0 t1 t0] = [p12 p11 p10 p9 0 0 0 0 0 0 p2 p1 p0] */
				/* [d 0 0 0 t9 0 0 0 0 0 c t2 t1 t0] = [p12 p11 p10 p9 0 0 0 0 0 0 p2 p1 p0] */

				c += (ulong)(n[0] * 2) * n[3]
				   + (ulong)(n[1] * 2) * n[2];
				VERIFY_BITS(c, 63);
				/* [d 0 0 0 t9 0 0 0 0 0 c t2 t1 t0] = [p12 p11 p10 p9 0 0 0 0 0 p3 p2 p1 p0] */
				d += (ulong)(n[4] * 2) * n[9]
				   + (ulong)(n[5] * 2) * n[8]
				   + (ulong)(n[6] * 2) * n[7];
				VERIFY_BITS(d, 63);
				/* [d 0 0 0 t9 0 0 0 0 0 c t2 t1 t0] = [p13 p12 p11 p10 p9 0 0 0 0 0 p3 p2 p1 p0] */
				u3 = (uint)(d & M); d >>= 26; c += u3 * R0;
				VERIFY_BITS(u3, 26);
				VERIFY_BITS(d, 37);
				/* VERIFY_BITS(c, 64); */
				/* [d u3 0 0 0 t9 0 0 0 0 0 c-u3*R0 t2 t1 t0] = [p13 p12 p11 p10 p9 0 0 0 0 0 p3 p2 p1 p0] */
				t3 = (uint)(c & M); c >>= 26; c += u3 * R1;
				VERIFY_BITS(t3, 26);
				VERIFY_BITS(c, 39);
				/* [d u3 0 0 0 t9 0 0 0 0 c-u3*R1 t3-u3*R0 t2 t1 t0] = [p13 p12 p11 p10 p9 0 0 0 0 0 p3 p2 p1 p0] */
				/* [d 0 0 0 0 t9 0 0 0 0 c t3 t2 t1 t0] = [p13 p12 p11 p10 p9 0 0 0 0 0 p3 p2 p1 p0] */

				c += (ulong)(n[0] * 2) * n[4]
				   + (ulong)(n[1] * 2) * n[3]
				   + (ulong)n[2] * n[2];
				VERIFY_BITS(c, 63);
				/* [d 0 0 0 0 t9 0 0 0 0 c t3 t2 t1 t0] = [p13 p12 p11 p10 p9 0 0 0 0 p4 p3 p2 p1 p0] */
				d += (ulong)(n[5] * 2) * n[9]
				   + (ulong)(n[6] * 2) * n[8]
				   + (ulong)n[7] * n[7];
				VERIFY_BITS(d, 62);
				/* [d 0 0 0 0 t9 0 0 0 0 c t3 t2 t1 t0] = [p14 p13 p12 p11 p10 p9 0 0 0 0 p4 p3 p2 p1 p0] */
				u4 = (uint)(d & M); d >>= 26; c += u4 * R0;
				VERIFY_BITS(u4, 26);
				VERIFY_BITS(d, 36);
				/* VERIFY_BITS(c, 64); */
				/* [d u4 0 0 0 0 t9 0 0 0 0 c-u4*R0 t3 t2 t1 t0] = [p14 p13 p12 p11 p10 p9 0 0 0 0 p4 p3 p2 p1 p0] */
				t4 = (uint)(c & M); c >>= 26; c += u4 * R1;
				VERIFY_BITS(t4, 26);
				VERIFY_BITS(c, 39);
				/* [d u4 0 0 0 0 t9 0 0 0 c-u4*R1 t4-u4*R0 t3 t2 t1 t0] = [p14 p13 p12 p11 p10 p9 0 0 0 0 p4 p3 p2 p1 p0] */
				/* [d 0 0 0 0 0 t9 0 0 0 c t4 t3 t2 t1 t0] = [p14 p13 p12 p11 p10 p9 0 0 0 0 p4 p3 p2 p1 p0] */

				c += (ulong)(n[0] * 2) * n[5]
				   + (ulong)(n[1] * 2) * n[4]
				   + (ulong)(n[2] * 2) * n[3];
				VERIFY_BITS(c, 63);
				/* [d 0 0 0 0 0 t9 0 0 0 c t4 t3 t2 t1 t0] = [p14 p13 p12 p11 p10 p9 0 0 0 p5 p4 p3 p2 p1 p0] */
				d += (ulong)(n[6] * 2) * n[9]
				   + (ulong)(n[7] * 2) * n[8];
				VERIFY_BITS(d, 62);
				/* [d 0 0 0 0 0 t9 0 0 0 c t4 t3 t2 t1 t0] = [p15 p14 p13 p12 p11 p10 p9 0 0 0 p5 p4 p3 p2 p1 p0] */
				u5 = (uint)(d & M); d >>= 26; c += u5 * R0;
				VERIFY_BITS(u5, 26);
				VERIFY_BITS(d, 36);
				/* VERIFY_BITS(c, 64); */
				/* [d u5 0 0 0 0 0 t9 0 0 0 c-u5*R0 t4 t3 t2 t1 t0] = [p15 p14 p13 p12 p11 p10 p9 0 0 0 p5 p4 p3 p2 p1 p0] */
				t5 = (uint)(c & M); c >>= 26; c += u5 * R1;
				VERIFY_BITS(t5, 26);
				VERIFY_BITS(c, 39);
				/* [d u5 0 0 0 0 0 t9 0 0 c-u5*R1 t5-u5*R0 t4 t3 t2 t1 t0] = [p15 p14 p13 p12 p11 p10 p9 0 0 0 p5 p4 p3 p2 p1 p0] */
				/* [d 0 0 0 0 0 0 t9 0 0 c t5 t4 t3 t2 t1 t0] = [p15 p14 p13 p12 p11 p10 p9 0 0 0 p5 p4 p3 p2 p1 p0] */

				c += (ulong)(n[0] * 2) * n[6]
				   + (ulong)(n[1] * 2) * n[5]
				   + (ulong)(n[2] * 2) * n[4]
				   + (ulong)n[3] * n[3];
				VERIFY_BITS(c, 63);
				/* [d 0 0 0 0 0 0 t9 0 0 c t5 t4 t3 t2 t1 t0] = [p15 p14 p13 p12 p11 p10 p9 0 0 p6 p5 p4 p3 p2 p1 p0] */
				d += (ulong)(n[7] * 2) * n[9]
				   + (ulong)n[8] * n[8];
				VERIFY_BITS(d, 61);
				/* [d 0 0 0 0 0 0 t9 0 0 c t5 t4 t3 t2 t1 t0] = [p16 p15 p14 p13 p12 p11 p10 p9 0 0 p6 p5 p4 p3 p2 p1 p0] */
				u6 = (uint)(d & M); d >>= 26; c += u6 * R0;
				VERIFY_BITS(u6, 26);
				VERIFY_BITS(d, 35);
				/* VERIFY_BITS(c, 64); */
				/* [d u6 0 0 0 0 0 0 t9 0 0 c-u6*R0 t5 t4 t3 t2 t1 t0] = [p16 p15 p14 p13 p12 p11 p10 p9 0 0 p6 p5 p4 p3 p2 p1 p0] */
				t6 = (uint)(c & M); c >>= 26; c += u6 * R1;
				VERIFY_BITS(t6, 26);
				VERIFY_BITS(c, 39);
				/* [d u6 0 0 0 0 0 0 t9 0 c-u6*R1 t6-u6*R0 t5 t4 t3 t2 t1 t0] = [p16 p15 p14 p13 p12 p11 p10 p9 0 0 p6 p5 p4 p3 p2 p1 p0] */
				/* [d 0 0 0 0 0 0 0 t9 0 c t6 t5 t4 t3 t2 t1 t0] = [p16 p15 p14 p13 p12 p11 p10 p9 0 0 p6 p5 p4 p3 p2 p1 p0] */

				c += (ulong)(n[0] * 2) * n[7]
				   + (ulong)(n[1] * 2) * n[6]
				   + (ulong)(n[2] * 2) * n[5]
				   + (ulong)(n[3] * 2) * n[4];
				/* VERIFY_BITS(c, 64); */
				VERIFY_CHECK(c <= 0x8000007C00000007UL);
				/* [d 0 0 0 0 0 0 0 t9 0 c t6 t5 t4 t3 t2 t1 t0] = [p16 p15 p14 p13 p12 p11 p10 p9 0 p7 p6 p5 p4 p3 p2 p1 p0] */
				d += (ulong)(n[8] * 2) * n[9];
				VERIFY_BITS(d, 58);
				/* [d 0 0 0 0 0 0 0 t9 0 c t6 t5 t4 t3 t2 t1 t0] = [p17 p16 p15 p14 p13 p12 p11 p10 p9 0 p7 p6 p5 p4 p3 p2 p1 p0] */
				u7 = (uint)(d & M); d >>= 26; c += u7 * R0;
				VERIFY_BITS(u7, 26);
				VERIFY_BITS(d, 32);
				/* VERIFY_BITS(c, 64); */
				VERIFY_CHECK(c <= 0x800001703FFFC2F7UL);
				/* [d u7 0 0 0 0 0 0 0 t9 0 c-u7*R0 t6 t5 t4 t3 t2 t1 t0] = [p17 p16 p15 p14 p13 p12 p11 p10 p9 0 p7 p6 p5 p4 p3 p2 p1 p0] */
				t7 = (uint)(c & M); c >>= 26; c += u7 * R1;
				VERIFY_BITS(t7, 26);
				VERIFY_BITS(c, 38);
				/* [d u7 0 0 0 0 0 0 0 t9 c-u7*R1 t7-u7*R0 t6 t5 t4 t3 t2 t1 t0] = [p17 p16 p15 p14 p13 p12 p11 p10 p9 0 p7 p6 p5 p4 p3 p2 p1 p0] */
				/* [d 0 0 0 0 0 0 0 0 t9 c t7 t6 t5 t4 t3 t2 t1 t0] = [p17 p16 p15 p14 p13 p12 p11 p10 p9 0 p7 p6 p5 p4 p3 p2 p1 p0] */

				c += (ulong)(n[0] * 2) * n[8]
				   + (ulong)(n[1] * 2) * n[7]
				   + (ulong)(n[2] * 2) * n[6]
				   + (ulong)(n[3] * 2) * n[5]
				   + (ulong)n[4] * n[4];
				/* VERIFY_BITS(c, 64); */
				VERIFY_CHECK(c <= 0x9000007B80000008UL);
				/* [d 0 0 0 0 0 0 0 0 t9 c t7 t6 t5 t4 t3 t2 t1 t0] = [p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
				d += (ulong)n[9] * n[9];
				VERIFY_BITS(d, 57);
				/* [d 0 0 0 0 0 0 0 0 t9 c t7 t6 t5 t4 t3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
				u8 = (uint)(d & M); d >>= 26; c += u8 * R0;
				VERIFY_BITS(u8, 26);
				VERIFY_BITS(d, 31);
				/* VERIFY_BITS(c, 64); */
				VERIFY_CHECK(c <= 0x9000016FBFFFC2F8UL);
				/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 t7 t6 t5 t4 t3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */

				n[3] = t3;
				VERIFY_BITS(n[3], 26);
				/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 t7 t6 t5 t4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
				n[4] = t4;
				VERIFY_BITS(n[4], 26);
				/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 t7 t6 t5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
				n[5] = t5;
				VERIFY_BITS(n[5], 26);
				/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 t7 t6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
				n[6] = t6;
				VERIFY_BITS(n[6], 26);
				/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 t7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
				n[7] = t7;
				VERIFY_BITS(n[7], 26);
				/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */

				n[8] = (uint)(c & M); c >>= 26; c += u8 * R1;
				VERIFY_BITS(n[8], 26);
				VERIFY_BITS(c, 39);
				/* [d u8 0 0 0 0 0 0 0 0 t9+c-u8*R1 r8-u8*R0 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
				/* [d 0 0 0 0 0 0 0 0 0 t9+c r8 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
				c += d * R0 + t9;
				VERIFY_BITS(c, 45);
				/* [d 0 0 0 0 0 0 0 0 0 c-d*R0 r8 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
				n[9] = (uint)(c & (M >> 4)); c >>= 22; c += d * (R1 << 4);
				VERIFY_BITS(n[9], 22);
				VERIFY_BITS(c, 46);
				/* [d 0 0 0 0 0 0 0 0 r9+((c-d*R1<<4)<<22)-d*R0 r8 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
				/* [d 0 0 0 0 0 0 0 -d*R1 r9+(c<<22)-d*R0 r8 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
				/* [r9+(c<<22) r8 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */

				d = c * (R0 >> 4) + t0;
				VERIFY_BITS(d, 56);
				/* [r9+(c<<22) r8 r7 r6 r5 r4 r3 t2 t1 d-c*R0>>4] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
				n[0] = (uint)(d & M); d >>= 26;
				VERIFY_BITS(n[0], 26);
				VERIFY_BITS(d, 30);
				/* [r9+(c<<22) r8 r7 r6 r5 r4 r3 t2 t1+d r0-c*R0>>4] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
				d += c * (R1 >> 4) + t1;
				VERIFY_BITS(d, 53);
				VERIFY_CHECK(d <= 0x10000003FFFFBFUL);
				/* [r9+(c<<22) r8 r7 r6 r5 r4 r3 t2 d-c*R1>>4 r0-c*R0>>4] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
				/* [r9 r8 r7 r6 r5 r4 r3 t2 d r0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
				n[1] = (uint)(d & M); d >>= 26;
				VERIFY_BITS(n[1], 26);
				VERIFY_BITS(d, 27);
				VERIFY_CHECK(d <= 0x4000000UL);
				/* [r9 r8 r7 r6 r5 r4 r3 t2+d r1 r0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
				d += t2;
				VERIFY_BITS(d, 27);
				/* [r9 r8 r7 r6 r5 r4 r3 d r1 r0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
				n[2] = (uint)d;
				VERIFY_BITS(n[2], 27);
				/* [r9 r8 r7 r6 r5 r4 r3 r2 r1 r0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			}
			return new FE(n, magnitude, normalized);
		}

		[Conditional("SECP256K1_VERIFY")]
		static void VERIFY_BITS(ulong x, int n)
		{
			VERIFY_CHECK(((x) >> (n)) == 0);
		}

		public readonly FE Multiply(in FE b)
		{
			VERIFY_CHECK(this.magnitude <= 8);
			VERIFY();
			VERIFY_CHECK(b.magnitude <= 8);
			b.VERIFY();
			var r = secp256k1_fe_mul_inner(b, 1, false);
			r.VERIFY();
			return r;
		}

		public readonly FE Multiply(uint a)
		{
			var r = new FE(
				n0 * a,
				n1 * a,
				n2 * a,
				n3 * a,
				n4 * a,
				n5 * a,
				n6 * a,
				n7 * a,
				n8 * a,
				n9 * a,
				magnitude * (int)a, false);
			r.VERIFY();
			return r;
		}

		public readonly FE Add(in FE a)
		{
			a.VERIFY();
			var r = new FE(
				n0 + a.n0,
				n1 + a.n1,
				n2 + a.n2,
				n3 + a.n3,
				n4 + a.n4,
				n5 + a.n5,
				n6 + a.n6,
				n7 + a.n7,
				n8 + a.n8,
				n9 + a.n9,
				magnitude + a.magnitude,
				false);
			r.VERIFY();
			return r;
		}
		internal const int NCount = 10;
		private readonly FE secp256k1_fe_mul_inner(in FE b, int magnitude, bool normalized)
		{

			ref readonly FE a = ref this;
			Span<uint> n = stackalloc uint[NCount];
			ulong c, d;
			ulong u0, u1, u2, u3, u4, u5, u6, u7, u8;
			uint t9, t1, t0, t2, t3, t4, t5, t6, t7;
			const uint M = 0x3FFFFFFU, R0 = 0x3D10U, R1 = 0x400U;

			VERIFY_BITS(a.n0, 30);
			VERIFY_BITS(a.n1, 30);
			VERIFY_BITS(a.n2, 30);
			VERIFY_BITS(a.n3, 30);
			VERIFY_BITS(a.n4, 30);
			VERIFY_BITS(a.n5, 30);
			VERIFY_BITS(a.n6, 30);
			VERIFY_BITS(a.n7, 30);
			VERIFY_BITS(a.n8, 30);
			VERIFY_BITS(a.n9, 26);
			VERIFY_BITS(b.n0, 30);
			VERIFY_BITS(b.n1, 30);
			VERIFY_BITS(b.n2, 30);
			VERIFY_BITS(b.n3, 30);
			VERIFY_BITS(b.n4, 30);
			VERIFY_BITS(b.n5, 30);
			VERIFY_BITS(b.n6, 30);
			VERIFY_BITS(b.n7, 30);
			VERIFY_BITS(b.n8, 30);
			VERIFY_BITS(b.n9, 26);

			/* [... a b c] is a shorthand for ... + a<<52 + b<<26 + c<<0 mod n.
			 *  px is a shorthand for sum(a.ni*b[x-i], i=0..x).
			 *  Note that [x 0 0 0 0 0 0 0 0 0 0] = [x*R1 x*R0].
			 */

			d = (ulong)a.n0 * b.n9
			   + (ulong)a.n1 * b.n8
			   + (ulong)a.n2 * b.n7
			   + (ulong)a.n3 * b.n6
			   + (ulong)a.n4 * b.n5
			   + (ulong)a.n5 * b.n4
			   + (ulong)a.n6 * b.n3
			   + (ulong)a.n7 * b.n2
			   + (ulong)a.n8 * b.n1
			   + (ulong)a.n9 * b.n0;
			/* VERIFY_BITS(d, 64); */
			/* [d 0 0 0 0 0 0 0 0 0] = [p9 0 0 0 0 0 0 0 0 0] */
			t9 = (uint)(d & M); d >>= 26;
			VERIFY_BITS(t9, 26);
			VERIFY_BITS(d, 38);
			/* [d t9 0 0 0 0 0 0 0 0 0] = [p9 0 0 0 0 0 0 0 0 0] */

			c = (ulong)a.n0 * b.n0;
			VERIFY_BITS(c, 60);
			/* [d t9 0 0 0 0 0 0 0 0 c] = [p9 0 0 0 0 0 0 0 0 p0] */
			d += (ulong)a.n1 * b.n9
			   + (ulong)a.n2 * b.n8
			   + (ulong)a.n3 * b.n7
			   + (ulong)a.n4 * b.n6
			   + (ulong)a.n5 * b.n5
			   + (ulong)a.n6 * b.n4
			   + (ulong)a.n7 * b.n3
			   + (ulong)a.n8 * b.n2
			   + (ulong)a.n9 * b.n1;
			VERIFY_BITS(d, 63);
			/* [d t9 0 0 0 0 0 0 0 0 c] = [p10 p9 0 0 0 0 0 0 0 0 p0] */
			u0 = (uint)(d & M); d >>= 26; c += u0 * R0;
			VERIFY_BITS(u0, 26);
			VERIFY_BITS(d, 37);
			VERIFY_BITS(c, 61);
			/* [d u0 t9 0 0 0 0 0 0 0 0 c-u0*R0] = [p10 p9 0 0 0 0 0 0 0 0 p0] */
			t0 = (uint)(c & M); c >>= 26; c += u0 * R1;
			VERIFY_BITS(t0, 26);
			VERIFY_BITS(c, 37);
			/* [d u0 t9 0 0 0 0 0 0 0 c-u0*R1 t0-u0*R0] = [p10 p9 0 0 0 0 0 0 0 0 p0] */
			/* [d 0 t9 0 0 0 0 0 0 0 c t0] = [p10 p9 0 0 0 0 0 0 0 0 p0] */

			c += (ulong)a.n0 * b.n1
			   + (ulong)a.n1 * b.n0;
			VERIFY_BITS(c, 62);
			/* [d 0 t9 0 0 0 0 0 0 0 c t0] = [p10 p9 0 0 0 0 0 0 0 p1 p0] */
			d += (ulong)a.n2 * b.n9
			   + (ulong)a.n3 * b.n8
			   + (ulong)a.n4 * b.n7
			   + (ulong)a.n5 * b.n6
			   + (ulong)a.n6 * b.n5
			   + (ulong)a.n7 * b.n4
			   + (ulong)a.n8 * b.n3
			   + (ulong)a.n9 * b.n2;
			VERIFY_BITS(d, 63);
			/* [d 0 t9 0 0 0 0 0 0 0 c t0] = [p11 p10 p9 0 0 0 0 0 0 0 p1 p0] */
			u1 = (uint)(d & M); d >>= 26; c += u1 * R0;
			VERIFY_BITS(u1, 26);
			VERIFY_BITS(d, 37);
			VERIFY_BITS(c, 63);
			/* [d u1 0 t9 0 0 0 0 0 0 0 c-u1*R0 t0] = [p11 p10 p9 0 0 0 0 0 0 0 p1 p0] */
			t1 = (uint)(c & M); c >>= 26; c += u1 * R1;
			VERIFY_BITS(t1, 26);
			VERIFY_BITS(c, 38);
			/* [d u1 0 t9 0 0 0 0 0 0 c-u1*R1 t1-u1*R0 t0] = [p11 p10 p9 0 0 0 0 0 0 0 p1 p0] */
			/* [d 0 0 t9 0 0 0 0 0 0 c t1 t0] = [p11 p10 p9 0 0 0 0 0 0 0 p1 p0] */

			c += (ulong)a.n0 * b.n2
			   + (ulong)a.n1 * b.n1
			   + (ulong)a.n2 * b.n0;
			VERIFY_BITS(c, 62);
			/* [d 0 0 t9 0 0 0 0 0 0 c t1 t0] = [p11 p10 p9 0 0 0 0 0 0 p2 p1 p0] */
			d += (ulong)a.n3 * b.n9
			   + (ulong)a.n4 * b.n8
			   + (ulong)a.n5 * b.n7
			   + (ulong)a.n6 * b.n6
			   + (ulong)a.n7 * b.n5
			   + (ulong)a.n8 * b.n4
			   + (ulong)a.n9 * b.n3;
			VERIFY_BITS(d, 63);
			/* [d 0 0 t9 0 0 0 0 0 0 c t1 t0] = [p12 p11 p10 p9 0 0 0 0 0 0 p2 p1 p0] */
			u2 = (uint)(d & M); d >>= 26; c += u2 * R0;
			VERIFY_BITS(u2, 26);
			VERIFY_BITS(d, 37);
			VERIFY_BITS(c, 63);
			/* [d u2 0 0 t9 0 0 0 0 0 0 c-u2*R0 t1 t0] = [p12 p11 p10 p9 0 0 0 0 0 0 p2 p1 p0] */
			t2 = (uint)(c & M); c >>= 26; c += u2 * R1;
			VERIFY_BITS(t2, 26);
			VERIFY_BITS(c, 38);
			/* [d u2 0 0 t9 0 0 0 0 0 c-u2*R1 t2-u2*R0 t1 t0] = [p12 p11 p10 p9 0 0 0 0 0 0 p2 p1 p0] */
			/* [d 0 0 0 t9 0 0 0 0 0 c t2 t1 t0] = [p12 p11 p10 p9 0 0 0 0 0 0 p2 p1 p0] */

			c += (ulong)a.n0 * b.n3
			   + (ulong)a.n1 * b.n2
			   + (ulong)a.n2 * b.n1
			   + (ulong)a.n3 * b.n0;
			VERIFY_BITS(c, 63);
			/* [d 0 0 0 t9 0 0 0 0 0 c t2 t1 t0] = [p12 p11 p10 p9 0 0 0 0 0 p3 p2 p1 p0] */
			d += (ulong)a.n4 * b.n9
			   + (ulong)a.n5 * b.n8
			   + (ulong)a.n6 * b.n7
			   + (ulong)a.n7 * b.n6
			   + (ulong)a.n8 * b.n5
			   + (ulong)a.n9 * b.n4;
			VERIFY_BITS(d, 63);
			/* [d 0 0 0 t9 0 0 0 0 0 c t2 t1 t0] = [p13 p12 p11 p10 p9 0 0 0 0 0 p3 p2 p1 p0] */
			u3 = (uint)(d & M); d >>= 26; c += u3 * R0;
			VERIFY_BITS(u3, 26);
			VERIFY_BITS(d, 37);
			/* VERIFY_BITS(c, 64); */
			/* [d u3 0 0 0 t9 0 0 0 0 0 c-u3*R0 t2 t1 t0] = [p13 p12 p11 p10 p9 0 0 0 0 0 p3 p2 p1 p0] */
			t3 = (uint)(c & M); c >>= 26; c += u3 * R1;
			VERIFY_BITS(t3, 26);
			VERIFY_BITS(c, 39);
			/* [d u3 0 0 0 t9 0 0 0 0 c-u3*R1 t3-u3*R0 t2 t1 t0] = [p13 p12 p11 p10 p9 0 0 0 0 0 p3 p2 p1 p0] */
			/* [d 0 0 0 0 t9 0 0 0 0 c t3 t2 t1 t0] = [p13 p12 p11 p10 p9 0 0 0 0 0 p3 p2 p1 p0] */

			c += (ulong)a.n0 * b.n4
			   + (ulong)a.n1 * b.n3
			   + (ulong)a.n2 * b.n2
			   + (ulong)a.n3 * b.n1
			   + (ulong)a.n4 * b.n0;
			VERIFY_BITS(c, 63);
			/* [d 0 0 0 0 t9 0 0 0 0 c t3 t2 t1 t0] = [p13 p12 p11 p10 p9 0 0 0 0 p4 p3 p2 p1 p0] */
			d += (ulong)a.n5 * b.n9
			   + (ulong)a.n6 * b.n8
			   + (ulong)a.n7 * b.n7
			   + (ulong)a.n8 * b.n6
			   + (ulong)a.n9 * b.n5;
			VERIFY_BITS(d, 62);
			/* [d 0 0 0 0 t9 0 0 0 0 c t3 t2 t1 t0] = [p14 p13 p12 p11 p10 p9 0 0 0 0 p4 p3 p2 p1 p0] */
			u4 = (uint)(d & M); d >>= 26; c += u4 * R0;
			VERIFY_BITS(u4, 26);
			VERIFY_BITS(d, 36);
			/* VERIFY_BITS(c, 64); */
			/* [d u4 0 0 0 0 t9 0 0 0 0 c-u4*R0 t3 t2 t1 t0] = [p14 p13 p12 p11 p10 p9 0 0 0 0 p4 p3 p2 p1 p0] */
			t4 = (uint)(c & M); c >>= 26; c += u4 * R1;
			VERIFY_BITS(t4, 26);
			VERIFY_BITS(c, 39);
			/* [d u4 0 0 0 0 t9 0 0 0 c-u4*R1 t4-u4*R0 t3 t2 t1 t0] = [p14 p13 p12 p11 p10 p9 0 0 0 0 p4 p3 p2 p1 p0] */
			/* [d 0 0 0 0 0 t9 0 0 0 c t4 t3 t2 t1 t0] = [p14 p13 p12 p11 p10 p9 0 0 0 0 p4 p3 p2 p1 p0] */

			c += (ulong)a.n0 * b.n5
			   + (ulong)a.n1 * b.n4
			   + (ulong)a.n2 * b.n3
			   + (ulong)a.n3 * b.n2
			   + (ulong)a.n4 * b.n1
			   + (ulong)a.n5 * b.n0;
			VERIFY_BITS(c, 63);
			/* [d 0 0 0 0 0 t9 0 0 0 c t4 t3 t2 t1 t0] = [p14 p13 p12 p11 p10 p9 0 0 0 p5 p4 p3 p2 p1 p0] */
			d += (ulong)a.n6 * b.n9
			   + (ulong)a.n7 * b.n8
			   + (ulong)a.n8 * b.n7
			   + (ulong)a.n9 * b.n6;
			VERIFY_BITS(d, 62);
			/* [d 0 0 0 0 0 t9 0 0 0 c t4 t3 t2 t1 t0] = [p15 p14 p13 p12 p11 p10 p9 0 0 0 p5 p4 p3 p2 p1 p0] */
			u5 = (uint)(d & M); d >>= 26; c += u5 * R0;
			VERIFY_BITS(u5, 26);
			VERIFY_BITS(d, 36);
			/* VERIFY_BITS(c, 64); */
			/* [d u5 0 0 0 0 0 t9 0 0 0 c-u5*R0 t4 t3 t2 t1 t0] = [p15 p14 p13 p12 p11 p10 p9 0 0 0 p5 p4 p3 p2 p1 p0] */
			t5 = (uint)(c & M); c >>= 26; c += u5 * R1;
			VERIFY_BITS(t5, 26);
			VERIFY_BITS(c, 39);
			/* [d u5 0 0 0 0 0 t9 0 0 c-u5*R1 t5-u5*R0 t4 t3 t2 t1 t0] = [p15 p14 p13 p12 p11 p10 p9 0 0 0 p5 p4 p3 p2 p1 p0] */
			/* [d 0 0 0 0 0 0 t9 0 0 c t5 t4 t3 t2 t1 t0] = [p15 p14 p13 p12 p11 p10 p9 0 0 0 p5 p4 p3 p2 p1 p0] */

			c += (ulong)a.n0 * b.n6
			   + (ulong)a.n1 * b.n5
			   + (ulong)a.n2 * b.n4
			   + (ulong)a.n3 * b.n3
			   + (ulong)a.n4 * b.n2
			   + (ulong)a.n5 * b.n1
			   + (ulong)a.n6 * b.n0;
			VERIFY_BITS(c, 63);
			/* [d 0 0 0 0 0 0 t9 0 0 c t5 t4 t3 t2 t1 t0] = [p15 p14 p13 p12 p11 p10 p9 0 0 p6 p5 p4 p3 p2 p1 p0] */
			d += (ulong)a.n7 * b.n9
			   + (ulong)a.n8 * b.n8
			   + (ulong)a.n9 * b.n7;
			VERIFY_BITS(d, 61);
			/* [d 0 0 0 0 0 0 t9 0 0 c t5 t4 t3 t2 t1 t0] = [p16 p15 p14 p13 p12 p11 p10 p9 0 0 p6 p5 p4 p3 p2 p1 p0] */
			u6 = (uint)(d & M); d >>= 26; c += u6 * R0;
			VERIFY_BITS(u6, 26);
			VERIFY_BITS(d, 35);
			/* VERIFY_BITS(c, 64); */
			/* [d u6 0 0 0 0 0 0 t9 0 0 c-u6*R0 t5 t4 t3 t2 t1 t0] = [p16 p15 p14 p13 p12 p11 p10 p9 0 0 p6 p5 p4 p3 p2 p1 p0] */
			t6 = (uint)(c & M); c >>= 26; c += u6 * R1;
			VERIFY_BITS(t6, 26);
			VERIFY_BITS(c, 39);
			/* [d u6 0 0 0 0 0 0 t9 0 c-u6*R1 t6-u6*R0 t5 t4 t3 t2 t1 t0] = [p16 p15 p14 p13 p12 p11 p10 p9 0 0 p6 p5 p4 p3 p2 p1 p0] */
			/* [d 0 0 0 0 0 0 0 t9 0 c t6 t5 t4 t3 t2 t1 t0] = [p16 p15 p14 p13 p12 p11 p10 p9 0 0 p6 p5 p4 p3 p2 p1 p0] */

			c += (ulong)a.n0 * b.n7
			   + (ulong)a.n1 * b.n6
			   + (ulong)a.n2 * b.n5
			   + (ulong)a.n3 * b.n4
			   + (ulong)a.n4 * b.n3
			   + (ulong)a.n5 * b.n2
			   + (ulong)a.n6 * b.n1
			   + (ulong)a.n7 * b.n0;
			/* VERIFY_BITS(c, 64); */
			VERIFY_CHECK(c <= 0x8000007C00000007UL);
			/* [d 0 0 0 0 0 0 0 t9 0 c t6 t5 t4 t3 t2 t1 t0] = [p16 p15 p14 p13 p12 p11 p10 p9 0 p7 p6 p5 p4 p3 p2 p1 p0] */
			d += (ulong)a.n8 * b.n9
			   + (ulong)a.n9 * b.n8;
			VERIFY_BITS(d, 58);
			/* [d 0 0 0 0 0 0 0 t9 0 c t6 t5 t4 t3 t2 t1 t0] = [p17 p16 p15 p14 p13 p12 p11 p10 p9 0 p7 p6 p5 p4 p3 p2 p1 p0] */
			u7 = (uint)(d & M); d >>= 26; c += u7 * R0;
			VERIFY_BITS(u7, 26);
			VERIFY_BITS(d, 32);
			/* VERIFY_BITS(c, 64); */
			VERIFY_CHECK(c <= 0x800001703FFFC2F7UL);
			/* [d u7 0 0 0 0 0 0 0 t9 0 c-u7*R0 t6 t5 t4 t3 t2 t1 t0] = [p17 p16 p15 p14 p13 p12 p11 p10 p9 0 p7 p6 p5 p4 p3 p2 p1 p0] */
			t7 = (uint)(c & M); c >>= 26; c += u7 * R1;
			VERIFY_BITS(t7, 26);
			VERIFY_BITS(c, 38);
			/* [d u7 0 0 0 0 0 0 0 t9 c-u7*R1 t7-u7*R0 t6 t5 t4 t3 t2 t1 t0] = [p17 p16 p15 p14 p13 p12 p11 p10 p9 0 p7 p6 p5 p4 p3 p2 p1 p0] */
			/* [d 0 0 0 0 0 0 0 0 t9 c t7 t6 t5 t4 t3 t2 t1 t0] = [p17 p16 p15 p14 p13 p12 p11 p10 p9 0 p7 p6 p5 p4 p3 p2 p1 p0] */

			c += (ulong)a.n0 * b.n8
			   + (ulong)a.n1 * b.n7
			   + (ulong)a.n2 * b.n6
			   + (ulong)a.n3 * b.n5
			   + (ulong)a.n4 * b.n4
			   + (ulong)a.n5 * b.n3
			   + (ulong)a.n6 * b.n2
			   + (ulong)a.n7 * b.n1
			   + (ulong)a.n8 * b.n0;
			/* VERIFY_BITS(c, 64); */
			VERIFY_CHECK(c <= 0x9000007B80000008UL);
			/* [d 0 0 0 0 0 0 0 0 t9 c t7 t6 t5 t4 t3 t2 t1 t0] = [p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			d += (ulong)a.n9 * b.n9;
			VERIFY_BITS(d, 57);
			/* [d 0 0 0 0 0 0 0 0 t9 c t7 t6 t5 t4 t3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			u8 = (uint)(d & M); d >>= 26; c += u8 * R0;
			VERIFY_BITS(u8, 26);
			VERIFY_BITS(d, 31);
			/* VERIFY_BITS(c, 64); */
			VERIFY_CHECK(c <= 0x9000016FBFFFC2F8UL);
			/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 t7 t6 t5 t4 t3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */

			n[3] = t3;
			VERIFY_BITS(n[3], 26);
			/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 t7 t6 t5 t4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n[4] = t4;
			VERIFY_BITS(n[4], 26);
			/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 t7 t6 t5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n[5] = t5;
			VERIFY_BITS(n[5], 26);
			/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 t7 t6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n[6] = t6;
			VERIFY_BITS(n[6], 26);
			/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 t7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n[7] = t7;
			VERIFY_BITS(n[7], 26);
			/* [d u8 0 0 0 0 0 0 0 0 t9 c-u8*R0 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */

			n[8] = (uint)(c & M); c >>= 26; c += u8 * R1;
			VERIFY_BITS(n[8], 26);
			VERIFY_BITS(c, 39);
			/* [d u8 0 0 0 0 0 0 0 0 t9+c-u8*R1 r8-u8*R0 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			/* [d 0 0 0 0 0 0 0 0 0 t9+c r8 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			c += d * R0 + t9;
			VERIFY_BITS(c, 45);
			/* [d 0 0 0 0 0 0 0 0 0 c-d*R0 r8 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n[9] = (uint)(c & (M >> 4)); c >>= 22; c += d * (R1 << 4);
			VERIFY_BITS(n[9], 22);
			VERIFY_BITS(c, 46);
			/* [d 0 0 0 0 0 0 0 0 r9+((c-d*R1<<4)<<22)-d*R0 r8 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			/* [d 0 0 0 0 0 0 0 -d*R1 r9+(c<<22)-d*R0 r8 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			/* [r9+(c<<22) r8 r7 r6 r5 r4 r3 t2 t1 t0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */

			d = c * (R0 >> 4) + t0;
			VERIFY_BITS(d, 56);
			/* [r9+(c<<22) r8 r7 r6 r5 r4 r3 t2 t1 d-c*R0>>4] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n[0] = (uint)(d & M); d >>= 26;
			VERIFY_BITS(n[0], 26);
			VERIFY_BITS(d, 30);
			/* [r9+(c<<22) r8 r7 r6 r5 r4 r3 t2 t1+d r0-c*R0>>4] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			d += c * (R1 >> 4) + t1;
			VERIFY_BITS(d, 53);
			VERIFY_CHECK(d <= 0x10000003FFFFBFUL);
			/* [r9+(c<<22) r8 r7 r6 r5 r4 r3 t2 d-c*R1>>4 r0-c*R0>>4] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			/* [r9 r8 r7 r6 r5 r4 r3 t2 d r0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n[1] = (uint)(d & M); d >>= 26;
			VERIFY_BITS(n[1], 26);
			VERIFY_BITS(d, 27);
			VERIFY_CHECK(d <= 0x4000000UL);
			/* [r9 r8 r7 r6 r5 r4 r3 t2+d r1 r0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			d += t2;
			VERIFY_BITS(d, 27);
			/* [r9 r8 r7 r6 r5 r4 r3 d r1 r0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			n[2] = (uint)d;
			VERIFY_BITS(n[2], 27);
			/* [r9 r8 r7 r6 r5 r4 r3 r2 r1 r0] = [p18 p17 p16 p15 p14 p13 p12 p11 p10 p9 p8 p7 p6 p5 p4 p3 p2 p1 p0] */
			return new FE(n, magnitude, normalized);
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public static void CMov(ref FE r, FE a, int flag)
		{
			uint mask0, mask1;
			mask0 = (uint)flag + ~((uint)0);
			mask1 = ~mask0;
			r = new FE(
				(r.n0 & mask0) | (a.n0 & mask1),
				(r.n1 & mask0) | (a.n1 & mask1),
				(r.n2 & mask0) | (a.n2 & mask1),
				(r.n3 & mask0) | (a.n3 & mask1),
				(r.n4 & mask0) | (a.n4 & mask1),
				(r.n5 & mask0) | (a.n5 & mask1),
				(r.n6 & mask0) | (a.n6 & mask1),
				(r.n7 & mask0) | (a.n7 & mask1),
				(r.n8 & mask0) | (a.n8 & mask1),
				(r.n9 & mask0) | (a.n9 & mask1),
				a.magnitude > r.magnitude ? a.magnitude : r.magnitude,
				r.normalized & a.normalized);
		}

		public FE(uint n0, uint n1, uint n2, uint n3, uint n4, uint n5, uint n6, uint n7, uint n8, uint n9, int magnitude, bool normalized)
		{
			this.n0 = n0;
			this.n1 = n1;
			this.n2 = n2;
			this.n3 = n3;
			this.n4 = n4;
			this.n5 = n5;
			this.n6 = n6;
			this.n7 = n7;
			this.n8 = n8;
			this.n9 = n9;
			this.magnitude = magnitude;
			this.normalized = normalized;
		}

		public FE(ReadOnlySpan<uint> n, int magnitude, bool normalized) : this()
		{
			this.n0 = n[0];
			this.n1 = n[1];
			this.n2 = n[2];
			this.n3 = n[3];
			this.n4 = n[4];
			this.n5 = n[5];
			this.n6 = n[6];
			this.n7 = n[7];
			this.n8 = n[8];
			this.n9 = n[9];
			this.magnitude = magnitude;
			this.normalized = normalized;
		}

		public readonly void Deconstruct(
			ref Span<uint> n,
			out int magnitude,
			out bool normalized
			)
		{
			n[0] = this.n0;
			n[1] = this.n1;
			n[2] = this.n2;
			n[3] = this.n3;
			n[4] = this.n4;
			n[5] = this.n5;
			n[6] = this.n6;
			n[7] = this.n7;
			n[8] = this.n8;
			n[9] = this.n9;
			magnitude = this.magnitude;
			normalized = this.normalized;
		}


		public readonly bool IsZero
		{
			[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.AggressiveInlining)]
			get
			{
				VERIFY_CHECK(normalized);
				VERIFY();
				return (n0 | n1 | n2 | n3 | n4 | n5 | n6 | n7 | n8 | n9) == 0;
			}
		}

		public readonly bool IsOdd
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				VERIFY_CHECK(normalized);
				VERIFY();
				return (n0 & 1) != 0;
			}
		}

		public readonly bool IsQuadVariable
		{
			get
			{
				return this.Sqrt(out _);
			}
		}

		public readonly FE Negate(int m)
		{
			VERIFY_CHECK(this.magnitude <= m);
			VERIFY();
			var result = new FE(
				(uint)(0x3FFFC2FUL * 2 * (uint)(m + 1) - n0),
				(uint)(0x3FFFFBFUL * 2 * (uint)(m + 1) - n1),
				(uint)(0x3FFFFFFUL * 2 * (uint)(m + 1) - n2),
				(uint)(0x3FFFFFFUL * 2 * (uint)(m + 1) - n3),
				(uint)(0x3FFFFFFUL * 2 * (uint)(m + 1) - n4),
				(uint)(0x3FFFFFFUL * 2 * (uint)(m + 1) - n5),
				(uint)(0x3FFFFFFUL * 2 * (uint)(m + 1) - n6),
				(uint)(0x3FFFFFFUL * 2 * (uint)(m + 1) - n7),
				(uint)(0x3FFFFFFUL * 2 * (uint)(m + 1) - n8),
				(uint)(0x03FFFFFUL * 2 * (uint)(m + 1) - n9),
				m + 1, false
				);
			result.VERIFY();
			return result;
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly FE NormalizeWeak()
		{
			Span<uint> t = stackalloc uint[NCount];
			int magnitude;
			bool normalized;
			this.Deconstruct(ref t, out magnitude, out normalized);

			/* Reduce t9 at the start so there will be at most a single carry from the first pass */
			uint x = t[9] >> 22; t[9] &= 0x03FFFFFU;

			/* The first pass ensures the magnitude is 1, ... */
			t[0] += x * 0x3D1U; t[1] += (x << 6);
			t[1] += (t[0] >> 26); t[0] &= 0x3FFFFFFU;
			t[2] += (t[1] >> 26); t[1] &= 0x3FFFFFFU;
			t[3] += (t[2] >> 26); t[2] &= 0x3FFFFFFU;
			t[4] += (t[3] >> 26); t[3] &= 0x3FFFFFFU;
			t[5] += (t[4] >> 26); t[4] &= 0x3FFFFFFU;
			t[6] += (t[5] >> 26); t[5] &= 0x3FFFFFFU;
			t[7] += (t[6] >> 26); t[6] &= 0x3FFFFFFU;
			t[8] += (t[7] >> 26); t[7] &= 0x3FFFFFFU;
			t[9] += (t[8] >> 26); t[8] &= 0x3FFFFFFU;

			/* ... except for a possible carry at bit 22 of t9 (i.e. bit 256 of the field element) */
			VERIFY_CHECK(t[9] >> 23 == 0);
			magnitude = 1;
			var result = new FE(t, magnitude, normalized);
			result.VERIFY();
			return result;
		}

		public static void NormalizeVariable(ref FE fe)
		{
			fe = fe.NormalizeVariable();
		}

		public readonly FE NormalizeVariable()
		{
			Span<uint> t = stackalloc uint[NCount];
			int magnitude;
			bool normalized;
			this.Deconstruct(ref t, out magnitude, out normalized);

			/* Reduce t9 at the start so there will be at most a single carry from the first pass */
			uint m;
			uint x = t[9] >> 22; t[9] &= 0x03FFFFFU;

			/* The first pass ensures the magnitude is 1, ... */
			t[0] += x * 0x3D1U; t[1] += (x << 6);
			t[1] += (t[0] >> 26); t[0] &= 0x3FFFFFFU;
			t[2] += (t[1] >> 26); t[1] &= 0x3FFFFFFU;
			t[3] += (t[2] >> 26); t[2] &= 0x3FFFFFFU; m = t[2];
			t[4] += (t[3] >> 26); t[3] &= 0x3FFFFFFU; m &= t[3];
			t[5] += (t[4] >> 26); t[4] &= 0x3FFFFFFU; m &= t[4];
			t[6] += (t[5] >> 26); t[5] &= 0x3FFFFFFU; m &= t[5];
			t[7] += (t[6] >> 26); t[6] &= 0x3FFFFFFU; m &= t[6];
			t[8] += (t[7] >> 26); t[7] &= 0x3FFFFFFU; m &= t[7];
			t[9] += (t[8] >> 26); t[8] &= 0x3FFFFFFU; m &= t[8];

			/* ... except for a possible carry at bit 22 of t9 (i.e. bit 256 of the field element) */
			VERIFY_CHECK(t[9] >> 23 == 0);

			/* At most a single final reduction is needed; check if the value is >= the field characteristic */
			x = (t[9] >> 22) | ((t[9] == 0x03FFFFFU ? 1U : 0) & (m == 0x3FFFFFFU ? 1U : 0)
				& ((t[1] + 0x40U + ((t[0] + 0x3D1U) >> 26)) > 0x3FFFFFFU ? 1U : 0));

			if (x != 0)
			{
				t[0] += 0x3D1U; t[1] += (x << 6);
				t[1] += (t[0] >> 26); t[0] &= 0x3FFFFFFU;
				t[2] += (t[1] >> 26); t[1] &= 0x3FFFFFFU;
				t[3] += (t[2] >> 26); t[2] &= 0x3FFFFFFU;
				t[4] += (t[3] >> 26); t[3] &= 0x3FFFFFFU;
				t[5] += (t[4] >> 26); t[4] &= 0x3FFFFFFU;
				t[6] += (t[5] >> 26); t[5] &= 0x3FFFFFFU;
				t[7] += (t[6] >> 26); t[6] &= 0x3FFFFFFU;
				t[8] += (t[7] >> 26); t[7] &= 0x3FFFFFFU;
				t[9] += (t[8] >> 26); t[8] &= 0x3FFFFFFU;

				/* If t9 didn't carry to bit 22 already, then it should have after any final reduction */
				VERIFY_CHECK(t[9] >> 22 == x);

				/* Mask off the possible multiple of 2^256 from the final reduction */
				t[9] &= 0x03FFFFFU;
			}

			magnitude = 1;
			normalized = true;
			var result = new FE(t, magnitude, normalized);
			result.VERIFY();
			return result;
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly FE Normalize()
		{
			Span<uint> t = stackalloc uint[NCount];
			int magnitude;
			bool normalized;
			this.Deconstruct(ref t, out magnitude, out normalized);

			/* Reduce t9 at the start so there will be at most a single carry from the first pass */
			uint m;
			uint x = t[9] >> 22; t[9] &= 0x03FFFFFU;

			/* The first pass ensures the magnitude is 1, ... */
			t[0] += x * 0x3D1U; t[1] += (x << 6);
			t[1] += (t[0] >> 26); t[0] &= 0x3FFFFFFU;
			t[2] += (t[1] >> 26); t[1] &= 0x3FFFFFFU;
			t[3] += (t[2] >> 26); t[2] &= 0x3FFFFFFU; m = t[2];
			t[4] += (t[3] >> 26); t[3] &= 0x3FFFFFFU; m &= t[3];
			t[5] += (t[4] >> 26); t[4] &= 0x3FFFFFFU; m &= t[4];
			t[6] += (t[5] >> 26); t[5] &= 0x3FFFFFFU; m &= t[5];
			t[7] += (t[6] >> 26); t[6] &= 0x3FFFFFFU; m &= t[6];
			t[8] += (t[7] >> 26); t[7] &= 0x3FFFFFFU; m &= t[7];
			t[9] += (t[8] >> 26); t[8] &= 0x3FFFFFFU; m &= t[8];

			/* ... except for a possible carry at bit 22 of t[9] (i.e. bit 256 of the field element) */
			VERIFY_CHECK(t[9] >> 23 == 0);

			/* At most a single final reduction is needed; check if the value is >= the field characteristic */
			x = (t[9] >> 22) | ((t[9] == 0x03FFFFFU ? 1u : 0) & (m == 0x3FFFFFFU ? 1u : 0)
				& ((t[1] + 0x40U + ((t[0] + 0x3D1U) >> 26)) > 0x3FFFFFFU ? 1u : 0));

			/* Apply the final reduction (for constant-time behaviour, we do it always) */
			t[0] += x * 0x3D1U; t[1] += (x << 6);
			t[1] += (t[0] >> 26); t[0] &= 0x3FFFFFFU;
			t[2] += (t[1] >> 26); t[1] &= 0x3FFFFFFU;
			t[3] += (t[2] >> 26); t[2] &= 0x3FFFFFFU;
			t[4] += (t[3] >> 26); t[3] &= 0x3FFFFFFU;
			t[5] += (t[4] >> 26); t[4] &= 0x3FFFFFFU;
			t[6] += (t[5] >> 26); t[5] &= 0x3FFFFFFU;
			t[7] += (t[6] >> 26); t[6] &= 0x3FFFFFFU;
			t[8] += (t[7] >> 26); t[7] &= 0x3FFFFFFU;
			t[9] += (t[8] >> 26); t[8] &= 0x3FFFFFFU;

			/* If t[9] didn't carry to bit 22 already, then it should have after any final reduction */
			VERIFY_CHECK(t[9] >> 22 == x);

			/* Mask off the possible multiple of 2^256 from the final reduction */
			t[9] &= 0x03FFFFFU;

			magnitude = 1;
			normalized = true;
			var result = new FE(t, magnitude, normalized);
			result.VERIFY();
			return result;
		}

		public readonly void WriteToSpan(Span<byte> r)
		{
			this.VERIFY();
			VERIFY_CHECK(normalized);
			r[0] = (byte)((n9 >> 14) & 0xff);
			r[1] = (byte)((n9 >> 6) & 0xff);
			r[2] = (byte)(((n9 & 0x3F) << 2) | ((n8 >> 24) & 0x3));
			r[3] = (byte)((n8 >> 16) & 0xff);
			r[4] = (byte)((n8 >> 8) & 0xff);
			r[5] = (byte)(n8 & 0xff);
			r[6] = (byte)((n7 >> 18) & 0xff);
			r[7] = (byte)((n7 >> 10) & 0xff);
			r[8] = (byte)((n7 >> 2) & 0xff);
			r[9] = (byte)(((n7 & 0x3) << 6) | ((n6 >> 20) & 0x3f));
			r[10] = (byte)((n6 >> 12) & 0xff);
			r[11] = (byte)((n6 >> 4) & 0xff);
			r[12] = (byte)(((n6 & 0xf) << 4) | ((n5 >> 22) & 0xf));
			r[13] = (byte)((n5 >> 14) & 0xff);
			r[14] = (byte)((n5 >> 6) & 0xff);
			r[15] = (byte)(((n5 & 0x3f) << 2) | ((n4 >> 24) & 0x3));
			r[16] = (byte)((n4 >> 16) & 0xff);
			r[17] = (byte)((n4 >> 8) & 0xff);
			r[18] = (byte)(n4 & 0xff);
			r[19] = (byte)((n3 >> 18) & 0xff);
			r[20] = (byte)((n3 >> 10) & 0xff);
			r[21] = (byte)((n3 >> 2) & 0xff);
			r[22] = (byte)(((n3 & 0x3) << 6) | ((n2 >> 20) & 0x3f));
			r[23] = (byte)((n2 >> 12) & 0xff);
			r[24] = (byte)((n2 >> 4) & 0xff);
			r[25] = (byte)(((n2 & 0xf) << 4) | ((n1 >> 22) & 0xf));
			r[26] = (byte)((n1 >> 14) & 0xff);
			r[27] = (byte)((n1 >> 6) & 0xff);
			r[28] = (byte)(((n1 & 0x3f) << 2) | ((n0 >> 24) & 0x3));
			r[29] = (byte)((n0 >> 16) & 0xff);
			r[30] = (byte)((n0 >> 8) & 0xff);
			r[31] = (byte)(n0 & 0xff);
		}

		[Conditional("SECP256K1_VERIFY")]
		private readonly void VERIFY()
		{
			int m = normalized ? 1 : 2 * magnitude, r = 1;
			r &= (n0 <= 0x3FFFFFFUL * (uint)m) ? 1 : 0;
			r &= (n1 <= 0x3FFFFFFUL * (uint)m) ? 1 : 0;
			r &= (n2 <= 0x3FFFFFFUL * (uint)m) ? 1 : 0;
			r &= (n3 <= 0x3FFFFFFUL * (uint)m) ? 1 : 0;
			r &= (n4 <= 0x3FFFFFFUL * (uint)m) ? 1 : 0;
			r &= (n5 <= 0x3FFFFFFUL * (uint)m) ? 1 : 0;
			r &= (n6 <= 0x3FFFFFFUL * (uint)m) ? 1 : 0;
			r &= (n7 <= 0x3FFFFFFUL * (uint)m) ? 1 : 0;
			r &= (n8 <= 0x3FFFFFFUL * (uint)m) ? 1 : 0;
			r &= (n9 <= 0x03FFFFFUL * (uint)m) ? 1 : 0;
			r &= (magnitude >= 0 ? 1 : 0);
			r &= (magnitude <= 32 ? 1 : 0);
			if (normalized)
			{
				r &= (magnitude <= 1 ? 1 : 0);
				if (r != 0 && (n9 == 0x03FFFFFUL))
				{
					uint mid = n8 & n7 & n6 & n5 & n4 & n3 & n2;
					if (mid == 0x3FFFFFFUL)
					{
						r &= ((n1 + 0x40UL + ((n0 + 0x3D1UL) >> 26)) <= 0x3FFFFFFUL) ? 1 : 0;
					}
				}
			}
			VERIFY_CHECK(r == 1);
		}
		[Conditional("SECP256K1_VERIFY")]
		private static void VERIFY_CHECK(bool value)
		{
			if (!value)
				throw new InvalidOperationException("VERIFY_CHECK failed (bug in C# secp256k1)");
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly bool Equals(FE b)
		{
			ref readonly FE a = ref this;
			var na = a.Negate(1);
			na += b;
			return na.NormalizesToZero();
		}
		public readonly bool EqualsVariable(in FE b)
		{
			ref readonly FE a = ref this;
			var na = a.Negate(1);
			na += b;
			return na.NormalizesToZero();
		}

		public readonly bool NormalizesToZero()
		{
			Span<uint> t = stackalloc uint[NCount];
			this.Deconstruct(ref t, out _, out _);

			/* z0 tracks a possible raw value of 0, z1 tracks a possible raw value of P */
			uint z0, z1;

			/* Reduce t[9] at the start so there will be at most a single carry from the first pass */
			uint x = t[9] >> 22; t[9] &= 0x03FFFFFU;

			/* The first pass ensures the magnitude is 1, ... */
			t[0] += x * 0x3D1U; t[1] += (x << 6);
			t[1] += (t[0] >> 26); t[0] &= 0x3FFFFFFU; z0 = t[0]; z1 = t[0] ^ 0x3D0U;
			t[2] += (t[1] >> 26); t[1] &= 0x3FFFFFFU; z0 |= t[1]; z1 &= t[1] ^ 0x40U;
			t[3] += (t[2] >> 26); t[2] &= 0x3FFFFFFU; z0 |= t[2]; z1 &= t[2];
			t[4] += (t[3] >> 26); t[3] &= 0x3FFFFFFU; z0 |= t[3]; z1 &= t[3];
			t[5] += (t[4] >> 26); t[4] &= 0x3FFFFFFU; z0 |= t[4]; z1 &= t[4];
			t[6] += (t[5] >> 26); t[5] &= 0x3FFFFFFU; z0 |= t[5]; z1 &= t[5];
			t[7] += (t[6] >> 26); t[6] &= 0x3FFFFFFU; z0 |= t[6]; z1 &= t[6];
			t[8] += (t[7] >> 26); t[7] &= 0x3FFFFFFU; z0 |= t[7]; z1 &= t[7];
			t[9] += (t[8] >> 26); t[8] &= 0x3FFFFFFU; z0 |= t[8]; z1 &= t[8];
			z0 |= t[9]; z1 &= t[9] ^ 0x3C00000U;

			/* ... except for a possible carry at bit 22 of t[9] (i.e. bit 256 of the field element) */
			VERIFY_CHECK(t[9] >> 23 == 0);

			return ((z0 == 0 ? 1 : 0) | (z1 == 0x3FFFFFFU ? 1 : 0)) != 0;
		}

		public static bool operator ==(in FE a, in FE b)
		{
			return a.Equals(b);
		}
		public static bool operator !=(in FE a, in FE b)
		{
			return !a.Equals(b);
		}
		public static FE operator *(in FE a, in FE b)
		{
			return a.Multiply(b);
		}
		public static FE operator *(in FE a, in uint b)
		{
			return a.Multiply(b);
		}
		public static FE operator +(in FE a, in FE b)
		{
			return a.Add(b);
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + n0.GetHashCode();
				hash = hash * 23 + n1.GetHashCode();
				hash = hash * 23 + n2.GetHashCode();
				hash = hash * 23 + n3.GetHashCode();
				hash = hash * 23 + n4.GetHashCode();
				hash = hash * 23 + n5.GetHashCode();
				hash = hash * 23 + n6.GetHashCode();
				hash = hash * 23 + n7.GetHashCode();
				hash = hash * 23 + n8.GetHashCode();
				hash = hash * 23 + n9.GetHashCode();
				return hash;
			}
		}

		public readonly override bool Equals(object? obj)
		{
			if (obj is FE other)
			{
				return this.Equals(other);
			}
			return false;
		}

		public readonly byte[] ToBytes()
		{
			var bytes = new byte[32];
			WriteToSpan(bytes);
			return bytes;
		}

		public readonly string ToC(string varName)
		{
			var normalizedStr = normalized ? "1" : "0";
			return $"secp256k1_fe {varName} = {{ 0x{n0.ToString("X8")}UL, 0x{n1.ToString("X8")}UL, 0x{n2.ToString("X8")}UL, 0x{n3.ToString("X8")}UL, 0x{n4.ToString("X8")}UL, 0x{n5.ToString("X8")}UL, 0x{n6.ToString("X8")}UL, 0x{n7.ToString("X8")}UL, 0x{n8.ToString("X8")}UL, 0x{n9.ToString("X8")}UL, {magnitude}, {normalizedStr} }};";
		}
	}
}
#nullable restore
#endif
