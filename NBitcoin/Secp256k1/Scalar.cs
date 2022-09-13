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
	readonly struct Scalar : IEquatable<Scalar>
	{
		// We ported secp256k1 code via the following regex
		//muladd_fast\((.*), (.*)\);
		//v = (ulong)$1 * $2;acc0 += v;VERIFY_CHECK(acc0 >= v); // muladd_fast($1, $2);
		//muladd\((.*), (.*)\);
		//v = (ulong)$1 * $2;acc0 += v;acc1 += (acc0 < v) ? 1U : 0;VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd($1, $2);
		//extract_fast\((.*)\);
		//$1 = (uint)acc0; acc0 >>= 32; VERIFY_CHECK(acc1 == 0); // extract_fast(out $1);
		//extract\((.*)\);
		//$1 = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out $1);
		//sumadd_fast\((.*)\);
		//acc0 += $1; VERIFY_CHECK(((acc0 >> 32) != 0) | ((uint)acc0 >= $1)); VERIFY_CHECK(acc1 == 0); // sumadd_fast($1);
		//sumadd\((.*)\);
		//acc0 += $1; acc1 += (acc0 < $1) ? 1U : 0; // sumadd_fast($1);
		//muladd2\((.*), (.*)\);
		//v = (ulong)$1 * $2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2($1, $2);
		static readonly Scalar _Zero = new Scalar(0, 0, 0, 0, 0, 0, 0, 0);
		public static ref readonly Scalar Zero => ref _Zero;
		static readonly Scalar _One = new Scalar(1, 0, 0, 0, 0, 0, 0, 0);
		public static ref readonly Scalar One => ref _One;
		static readonly Scalar _MinusOne = new Scalar(1, 0, 0, 0, 0, 0, 0, 0).Negate();
		public static ref readonly Scalar MinusOne => ref _MinusOne;

		internal const uint SECP256K1_N_0 = 0xD0364141U;
		internal const uint SECP256K1_N_1 = 0xBFD25E8CU;
		internal const uint SECP256K1_N_2 = 0xAF48A03BU;
		internal const uint SECP256K1_N_3 = 0xBAAEDCE6U;

		internal const uint SECP256K1_N_4 = 0xFFFFFFFEU;
		internal const uint SECP256K1_N_5 = 0xFFFFFFFFU;
		internal const uint SECP256K1_N_6 = 0xFFFFFFFFU;
		internal const uint SECP256K1_N_7 = 0xFFFFFFFFU;
		internal const uint SECP256K1_N_C_0 = ~SECP256K1_N_0 + 1;
		internal const uint SECP256K1_N_C_1 = ~SECP256K1_N_1;
		internal const uint SECP256K1_N_C_2 = ~SECP256K1_N_2;
		internal const uint SECP256K1_N_C_3 = ~SECP256K1_N_3;
		internal const uint SECP256K1_N_C_4 = 1;


		/* Limbs of half the secp256k1 order. */
		internal const uint SECP256K1_N_H_0 = (0x681B20A0U);
		internal const uint SECP256K1_N_H_1 = (0xDFE92F46U);
		internal const uint SECP256K1_N_H_2 = (0x57A4501DU);
		internal const uint SECP256K1_N_H_3 = (0x5D576E73U);
		internal const uint SECP256K1_N_H_4 = (0xFFFFFFFFU);
		internal const uint SECP256K1_N_H_5 = (0xFFFFFFFFU);
		internal const uint SECP256K1_N_H_6 = (0xFFFFFFFFU);
		internal const uint SECP256K1_N_H_7 = (0x7FFFFFFFU);

#if SECP256K1_LIB
		public
#else
		internal
#endif
		readonly uint d0, d1, d2, d3, d4, d5, d6, d7;
		public Scalar(uint d0, uint d1, uint d2, uint d3, uint d4, uint d5, uint d6, uint d7)
		{
			this.d0 = d0;
			this.d1 = d1;
			this.d2 = d2;
			this.d3 = d3;
			this.d4 = d4;
			this.d5 = d5;
			this.d6 = d6;
			this.d7 = d7;
		}
		public Scalar(Span<uint> d)
		{
			this.d0 = d[0];
			this.d1 = d[1];
			this.d2 = d[2];
			this.d3 = d[3];
			this.d4 = d[4];
			this.d5 = d[5];
			this.d6 = d[6];
			this.d7 = d[7];
			VERIFY_CHECK(CheckOverflow() == 0);
		}
#if SECP256K1_LIB
		public
#else
		internal
#endif
		Scalar(uint value)
		{
			d0 = d1 = d2 = d3 = d4 = d5 = d6 = d7 = 0;
			d0 = value;
		}
		
#if SECP256K1_LIB
		public
#else
		internal
#endif
		Scalar(ulong value)
		{
			d0 = d1 = d2 = d3 = d4 = d5 = d6 = d7 = 0;
			d0 = (uint)(value & uint.MaxValue);
			d1 = (uint)((value >> 32) & uint.MaxValue);
		}
	
#if SECP256K1_LIB
		public
#else
		internal
#endif
		Scalar(ReadOnlySpan<byte> b32) : this(b32, out _)
		{
		}
#if SECP256K1_LIB
		public
#else
		internal
#endif
		Scalar(ReadOnlySpan<byte> b32, out int overflow)
		{
			d0 = b32[31] | (uint)b32[30] << 8 | (uint)b32[29] << 16 | (uint)b32[28] << 24;
			d1 = b32[27] | (uint)b32[26] << 8 | (uint)b32[25] << 16 | (uint)b32[24] << 24;
			d2 = b32[23] | (uint)b32[22] << 8 | (uint)b32[21] << 16 | (uint)b32[20] << 24;
			d3 = b32[19] | (uint)b32[18] << 8 | (uint)b32[17] << 16 | (uint)b32[16] << 24;
			d4 = b32[15] | (uint)b32[14] << 8 | (uint)b32[13] << 16 | (uint)b32[12] << 24;
			d5 = b32[11] | (uint)b32[10] << 8 | (uint)b32[9] << 16 | (uint)b32[8] << 24;
			d6 = b32[7] | (uint)b32[6] << 8 | (uint)b32[5] << 16 | (uint)b32[4] << 24;
			d7 = b32[3] | (uint)b32[2] << 8 | (uint)b32[1] << 16 | (uint)b32[0] << 24;
			overflow = CheckOverflow();
			// Reduce(ref d0, ref d1, ref d2, ref d3, ref d4, ref d5, ref d6, ref d7, overflow);
			ulong t;
			VERIFY_CHECK(overflow == 0 || overflow == 1);
			t = (ulong)d0 + (uint)overflow * SECP256K1_N_C_0;
			d0 = (uint)t; t >>= 32;
			t += (ulong)d1 + (uint)overflow * SECP256K1_N_C_1;
			d1 = (uint)t; t >>= 32;
			t += (ulong)d2 + (uint)overflow * SECP256K1_N_C_2;
			d2 = (uint)t; t >>= 32;
			t += (ulong)d3 + (uint)overflow * SECP256K1_N_C_3;
			d3 = (uint)t; t >>= 32;
			t += (ulong)d4 + (uint)overflow * SECP256K1_N_C_4;
			d4 = (uint)t; t >>= 32;
			t += d5;
			d5 = (uint)t; t >>= 32;
			t += d6;
			d6 = (uint)t; t >>= 32;
			t += d7;
			d7 = (uint)t;
			VERIFY_CHECK(CheckOverflow() == 0);
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public static void CMov(ref Scalar r, Scalar a, int flag)
		{
			uint mask0, mask1;
			mask0 = (uint)flag + ~((uint)0);
			mask1 = ~mask0;
			r = new Scalar(
				(r.d0 & mask0) | (a.d0 & mask1),
				(r.d1 & mask0) | (a.d1 & mask1),
				(r.d2 & mask0) | (a.d2 & mask1),
				(r.d3 & mask0) | (a.d3 & mask1),
				(r.d4 & mask0) | (a.d4 & mask1),
				(r.d5 & mask0) | (a.d5 & mask1),
				(r.d6 & mask0) | (a.d6 & mask1),
				(r.d7 & mask0) | (a.d7 & mask1));
		}

		public static void Clear(ref Scalar s)
		{
			s = Scalar.Zero;
		}

#if SECP256K1_LIB
		public
#else
		internal
#endif
		readonly Scalar CAddBit(uint bit, int flag)
		{
			Span<uint> d = stackalloc uint[DCount];
			ulong t;
			VERIFY_CHECK(bit < 256);
			bit += ((uint)flag - 1) & 0x100;  /* forcing (bit >> 5) > 7 makes this a noop */
			t = (ulong)this.d0 + (((bit >> 5) == 0 ? 1U : 0) << (int)(bit & 0x1F));
			d[0] = (uint)t; t >>= 32;
			t += (ulong)this.d1 + (((bit >> 5) == 1 ? 1U : 0) << (int)(bit & 0x1F));
			d[1] = (uint)t; t >>= 32;
			t += (ulong)this.d2 + (((bit >> 5) == 2 ? 1U : 0) << (int)(bit & 0x1F));
			d[2] = (uint)t; t >>= 32;
			t += (ulong)this.d3 + (((bit >> 5) == 3 ? 1U : 0) << (int)(bit & 0x1F));
			d[3] = (uint)t; t >>= 32;
			t += (ulong)this.d4 + (((bit >> 5) == 4 ? 1U : 0) << (int)(bit & 0x1F));
			d[4] = (uint)t; t >>= 32;
			t += (ulong)this.d5 + (((bit >> 5) == 5 ? 1U : 0) << (int)(bit & 0x1F));
			d[5] = (uint)t; t >>= 32;
			t += (ulong)this.d6 + (((bit >> 5) == 6 ? 1U : 0) << (int)(bit & 0x1F));
			d[6] = (uint)t; t >>= 32;
			t += (ulong)this.d7 + (((bit >> 5) == 7 ? 1U : 0) << (int)(bit & 0x1F));
			d[7] = (uint)t;
			VERIFY_CHECK((t >> 32) == 0);
			var r = new Scalar(d);
			VERIFY_CHECK(!r.IsOverflow);
			return r;
		}

		private static int Reduce(Span<uint> d, int overflow)
		{
			ulong t;
			VERIFY_CHECK(overflow == 0 || overflow == 1);
			t = (ulong)d[0] + (uint)overflow * SECP256K1_N_C_0;
			d[0] = (uint)t; t >>= 32;
			t += (ulong)d[1] + (uint)overflow * SECP256K1_N_C_1;
			d[1] = (uint)t; t >>= 32;
			t += (ulong)d[2] + (uint)overflow * SECP256K1_N_C_2;
			d[2] = (uint)t; t >>= 32;
			t += (ulong)d[3] + (uint)overflow * SECP256K1_N_C_3;
			d[3] = (uint)t; t >>= 32;
			t += (ulong)d[4] + (uint)overflow * SECP256K1_N_C_4;
			d[4] = (uint)t; t >>= 32;
			t += d[5];
			d[5] = (uint)t; t >>= 32;
			t += d[6];
			d[6] = (uint)t; t >>= 32;
			t += d[7];
			d[7] = (uint)t;
			return overflow;
		}

		private static void reduce_512(Span<uint> d, Span<uint> l)
		{
			ulong c;
			ulong v;
			uint n0 = l[8], n1 = l[9], n2 = l[10], n3 = l[11], n4 = l[12], n5 = l[13], n6 = l[14], n7 = l[15];
			uint m0, m1, m2, m3, m4, m5, m6, m7, m8, m9, m10, m11, m12;
			uint p0, p1, p2, p3, p4, p5, p6, p7, p8;

			/* 160 bit accumulator. */
			ulong acc0;
			uint acc1 = 0;

			/* Reduce 512 bits into 385. */
			/* m[0..12] = l[0..7] + n[0..7] * SECP256K1_N_C. */
			acc0 = l[0];
			v = (ulong)n0 * SECP256K1_N_C_0; acc0 += v; VERIFY_CHECK(acc0 >= v); // muladd_fast(n0, SECP256K1_N_C_0);
			m0 = (uint)acc0; acc0 >>= 32; VERIFY_CHECK(acc1 == 0); // extract_fast(out m0);
			acc0 += l[1]; VERIFY_CHECK(((acc0 >> 32) != 0) | ((uint)acc0 >= l[1])); VERIFY_CHECK(acc1 == 0); // sumadd_fast(l[1]);
			v = (ulong)n1 * SECP256K1_N_C_0; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n1, SECP256K1_N_C_0);
			v = (ulong)n0 * SECP256K1_N_C_1; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n0, SECP256K1_N_C_1);
			m1 = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out m1);
			acc0 += l[2]; acc1 += (acc0 < l[2]) ? 1U : 0; // sumadd_fast(l[2]);
			v = (ulong)n2 * SECP256K1_N_C_0; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n2, SECP256K1_N_C_0);
			v = (ulong)n1 * SECP256K1_N_C_1; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n1, SECP256K1_N_C_1);
			v = (ulong)n0 * SECP256K1_N_C_2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n0, SECP256K1_N_C_2);
			m2 = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out m2);
			acc0 += l[3]; acc1 += (acc0 < l[3]) ? 1U : 0; // sumadd_fast(l[3]);
			v = (ulong)n3 * SECP256K1_N_C_0; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n3, SECP256K1_N_C_0);
			v = (ulong)n2 * SECP256K1_N_C_1; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n2, SECP256K1_N_C_1);
			v = (ulong)n1 * SECP256K1_N_C_2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n1, SECP256K1_N_C_2);
			v = (ulong)n0 * SECP256K1_N_C_3; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n0, SECP256K1_N_C_3);
			m3 = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out m3);
			acc0 += l[4]; acc1 += (acc0 < l[4]) ? 1U : 0; // sumadd_fast(l[4]);
			v = (ulong)n4 * SECP256K1_N_C_0; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n4, SECP256K1_N_C_0);
			v = (ulong)n3 * SECP256K1_N_C_1; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n3, SECP256K1_N_C_1);
			v = (ulong)n2 * SECP256K1_N_C_2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n2, SECP256K1_N_C_2);
			v = (ulong)n1 * SECP256K1_N_C_3; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n1, SECP256K1_N_C_3);
			acc0 += n0; acc1 += (acc0 < n0) ? 1U : 0; // sumadd_fast(n0);
			m4 = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out m4);
			acc0 += l[5]; acc1 += (acc0 < l[5]) ? 1U : 0; // sumadd_fast(l[5]);
			v = (ulong)n5 * SECP256K1_N_C_0; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n5, SECP256K1_N_C_0);
			v = (ulong)n4 * SECP256K1_N_C_1; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n4, SECP256K1_N_C_1);
			v = (ulong)n3 * SECP256K1_N_C_2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n3, SECP256K1_N_C_2);
			v = (ulong)n2 * SECP256K1_N_C_3; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n2, SECP256K1_N_C_3);
			acc0 += n1; acc1 += (acc0 < n1) ? 1U : 0; // sumadd_fast(n1);
			m5 = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out m5);
			acc0 += l[6]; acc1 += (acc0 < l[6]) ? 1U : 0; // sumadd_fast(l[6]);
			v = (ulong)n6 * SECP256K1_N_C_0; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n6, SECP256K1_N_C_0);
			v = (ulong)n5 * SECP256K1_N_C_1; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n5, SECP256K1_N_C_1);
			v = (ulong)n4 * SECP256K1_N_C_2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n4, SECP256K1_N_C_2);
			v = (ulong)n3 * SECP256K1_N_C_3; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n3, SECP256K1_N_C_3);
			acc0 += n2; acc1 += (acc0 < n2) ? 1U : 0; // sumadd_fast(n2);
			m6 = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out m6);
			acc0 += l[7]; acc1 += (acc0 < l[7]) ? 1U : 0; // sumadd_fast(l[7]);
			v = (ulong)n7 * SECP256K1_N_C_0; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n7, SECP256K1_N_C_0);
			v = (ulong)n6 * SECP256K1_N_C_1; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n6, SECP256K1_N_C_1);
			v = (ulong)n5 * SECP256K1_N_C_2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n5, SECP256K1_N_C_2);
			v = (ulong)n4 * SECP256K1_N_C_3; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n4, SECP256K1_N_C_3);
			acc0 += n3; acc1 += (acc0 < n3) ? 1U : 0; // sumadd_fast(n3);
			m7 = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out m7);
			v = (ulong)n7 * SECP256K1_N_C_1; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n7, SECP256K1_N_C_1);
			v = (ulong)n6 * SECP256K1_N_C_2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n6, SECP256K1_N_C_2);
			v = (ulong)n5 * SECP256K1_N_C_3; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n5, SECP256K1_N_C_3);
			acc0 += n4; acc1 += (acc0 < n4) ? 1U : 0; // sumadd_fast(n4);
			m8 = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out m8);
			v = (ulong)n7 * SECP256K1_N_C_2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n7, SECP256K1_N_C_2);
			v = (ulong)n6 * SECP256K1_N_C_3; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n6, SECP256K1_N_C_3);
			acc0 += n5; acc1 += (acc0 < n5) ? 1U : 0; // sumadd_fast(n5);
			m9 = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out m9);
			v = (ulong)n7 * SECP256K1_N_C_3; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(n7, SECP256K1_N_C_3);
			acc0 += n6; acc1 += (acc0 < n6) ? 1U : 0; // sumadd_fast(n6);
			m10 = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out m10);
			acc0 += n7; VERIFY_CHECK(((acc0 >> 32) != 0) | ((uint)acc0 >= n7)); VERIFY_CHECK(acc1 == 0); // sumadd_fast(n7);
			m11 = (uint)acc0; acc0 >>= 32; VERIFY_CHECK(acc1 == 0); // extract_fast(out m11);
			VERIFY_CHECK((uint)acc0 <= 1);
			m12 = (uint)acc0;

			/* Reduce 385 bits into 258. */
			/* p[0..8] = m[0..7] + m[8..12] * SECP256K1_N_C. */
			acc0 = m0; acc1 = 0;
			v = (ulong)m8 * SECP256K1_N_C_0; acc0 += v; VERIFY_CHECK(acc0 >= v); // muladd_fast(m8, SECP256K1_N_C_0);
			p0 = (uint)acc0; acc0 >>= 32; VERIFY_CHECK(acc1 == 0); // extract_fast(out p0);
			acc0 += m1; VERIFY_CHECK(((acc0 >> 32) != 0) | ((uint)acc0 >= m1)); VERIFY_CHECK(acc1 == 0); // sumadd_fast(m1);
			v = (ulong)m9 * SECP256K1_N_C_0; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(m9, SECP256K1_N_C_0);
			v = (ulong)m8 * SECP256K1_N_C_1; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(m8, SECP256K1_N_C_1);
			p1 = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out p1);
			acc0 += m2; acc1 += (acc0 < m2) ? 1U : 0; // sumadd_fast(m2);
			v = (ulong)m10 * SECP256K1_N_C_0; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(m10, SECP256K1_N_C_0);
			v = (ulong)m9 * SECP256K1_N_C_1; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(m9, SECP256K1_N_C_1);
			v = (ulong)m8 * SECP256K1_N_C_2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(m8, SECP256K1_N_C_2);
			p2 = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out p2);
			acc0 += m3; acc1 += (acc0 < m3) ? 1U : 0; // sumadd_fast(m3);
			v = (ulong)m11 * SECP256K1_N_C_0; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(m11, SECP256K1_N_C_0);
			v = (ulong)m10 * SECP256K1_N_C_1; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(m10, SECP256K1_N_C_1);
			v = (ulong)m9 * SECP256K1_N_C_2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(m9, SECP256K1_N_C_2);
			v = (ulong)m8 * SECP256K1_N_C_3; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(m8, SECP256K1_N_C_3);
			p3 = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out p3);
			acc0 += m4; acc1 += (acc0 < m4) ? 1U : 0; // sumadd_fast(m4);
			v = (ulong)m12 * SECP256K1_N_C_0; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(m12, SECP256K1_N_C_0);
			v = (ulong)m11 * SECP256K1_N_C_1; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(m11, SECP256K1_N_C_1);
			v = (ulong)m10 * SECP256K1_N_C_2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(m10, SECP256K1_N_C_2);
			v = (ulong)m9 * SECP256K1_N_C_3; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(m9, SECP256K1_N_C_3);
			acc0 += m8; acc1 += (acc0 < m8) ? 1U : 0; // sumadd_fast(m8);
			p4 = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out p4);
			acc0 += m5; acc1 += (acc0 < m5) ? 1U : 0; // sumadd_fast(m5);
			v = (ulong)m12 * SECP256K1_N_C_1; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(m12, SECP256K1_N_C_1);
			v = (ulong)m11 * SECP256K1_N_C_2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(m11, SECP256K1_N_C_2);
			v = (ulong)m10 * SECP256K1_N_C_3; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(m10, SECP256K1_N_C_3);
			acc0 += m9; acc1 += (acc0 < m9) ? 1U : 0; // sumadd_fast(m9);
			p5 = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out p5);
			acc0 += m6; acc1 += (acc0 < m6) ? 1U : 0; // sumadd_fast(m6);
			v = (ulong)m12 * SECP256K1_N_C_2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(m12, SECP256K1_N_C_2);
			v = (ulong)m11 * SECP256K1_N_C_3; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(m11, SECP256K1_N_C_3);
			acc0 += m10; acc1 += (acc0 < m10) ? 1U : 0; // sumadd_fast(m10);
			p6 = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out p6);
			acc0 += m7; VERIFY_CHECK(((acc0 >> 32) != 0) | ((uint)acc0 >= m7)); VERIFY_CHECK(acc1 == 0); // sumadd_fast(m7);
			v = (ulong)m12 * SECP256K1_N_C_3; acc0 += v; VERIFY_CHECK(acc0 >= v); // muladd_fast(m12, SECP256K1_N_C_3);
			acc0 += m11; VERIFY_CHECK(((acc0 >> 32) != 0) | ((uint)acc0 >= m11)); VERIFY_CHECK(acc1 == 0); // sumadd_fast(m11);
			p7 = (uint)acc0; acc0 >>= 32; VERIFY_CHECK(acc1 == 0); // extract_fast(out p7);
			p8 = (uint)acc0 + m12;
			VERIFY_CHECK(p8 <= 2);

			/* Reduce 258 bits into 256. */
			/* r[0..7] = p[0..7] + p[8] * SECP256K1_N_C. */
			c = p0 + (ulong)SECP256K1_N_C_0 * p8;
			d[0] = (uint)c; c >>= 32;
			c += p1 + (ulong)SECP256K1_N_C_1 * p8;
			d[1] = (uint)c; c >>= 32;
			c += p2 + (ulong)SECP256K1_N_C_2 * p8;
			d[2] = (uint)c; c >>= 32;
			c += p3 + (ulong)SECP256K1_N_C_3 * p8;
			d[3] = (uint)c; c >>= 32;
			c += p4 + (ulong)p8;
			d[4] = (uint)c; c >>= 32;
			c += p5;
			d[5] = (uint)c; c >>= 32;
			c += p6;
			d[6] = (uint)c; c >>= 32;
			c += p7;
			d[7] = (uint)c; c >>= 32;

			/* Final reduction of r. */
			Reduce(d, (int)c + CheckOverflow(d));
		}

#if SECP256K1_LIB
		public
#else
		internal
#endif
		int CondNegate(int flag, out Scalar r)
		{
			Span<uint> rd = stackalloc uint[DCount];
			Deconstruct(ref rd);
			/* If we are flag = 0, mask = 00...00 and this is a no-op;
     * if we are flag = 1, mask = 11...11 and this is identical to secp256k1_scalar_negate */
			uint mask = (flag == 0 ? 1U : 0) - 1;
			uint nonzero = 0xFFFFFFFFU * (IsZero ? 0U : 1);
			ulong t = (ulong)(rd[0] ^ mask) + ((SECP256K1_N_0 + 1) & mask);
			rd[0] = (uint)(t & nonzero); t >>= 32;
			t += (ulong)(rd[1] ^ mask) + (SECP256K1_N_1 & mask);
			rd[1] = (uint)(t & nonzero); t >>= 32;
			t += (ulong)(rd[2] ^ mask) + (SECP256K1_N_2 & mask);
			rd[2] = (uint)(t & nonzero); t >>= 32;
			t += (ulong)(rd[3] ^ mask) + (SECP256K1_N_3 & mask);
			rd[3] = (uint)(t & nonzero); t >>= 32;
			t += (ulong)(rd[4] ^ mask) + (SECP256K1_N_4 & mask);
			rd[4] = (uint)(t & nonzero); t >>= 32;
			t += (ulong)(rd[5] ^ mask) + (SECP256K1_N_5 & mask);
			rd[5] = (uint)(t & nonzero); t >>= 32;
			t += (ulong)(rd[6] ^ mask) + (SECP256K1_N_6 & mask);
			rd[6] = (uint)(t & nonzero); t >>= 32;
			t += (ulong)(rd[7] ^ mask) + (SECP256K1_N_7 & mask);
			rd[7] = (uint)(t & nonzero);
			r = new Scalar(rd);
			return 2 * (mask == 0 ? 1 : 0) - 1;
		}

		private static void mul_512(Span<uint> l, in Scalar a, in Scalar b)
		{
			/* 160 bit accumulator. */
			ulong v;
			ulong acc0 = 0;
			uint acc1 = 0;

			/* l[0..15] = a[0..7] * b[0..7]. */
			v = (ulong)a.d0 * b.d0; acc0 += v; VERIFY_CHECK(acc0 >= v); // muladd_fast(a.d0, b.d0);
			l[0] = (uint)acc0; acc0 >>= 32; VERIFY_CHECK(acc1 == 0); // extract_fast(out l[0]);
			v = (ulong)a.d0 * b.d1; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d0, b.d1);
			v = (ulong)a.d1 * b.d0; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d1, b.d0);
			l[1] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[1]);
			v = (ulong)a.d0 * b.d2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d0, b.d2);
			v = (ulong)a.d1 * b.d1; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d1, b.d1);
			v = (ulong)a.d2 * b.d0; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d2, b.d0);
			l[2] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[2]);
			v = (ulong)a.d0 * b.d3; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d0, b.d3);
			v = (ulong)a.d1 * b.d2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d1, b.d2);
			v = (ulong)a.d2 * b.d1; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d2, b.d1);
			v = (ulong)a.d3 * b.d0; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d3, b.d0);
			l[3] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[3]);
			v = (ulong)a.d0 * b.d4; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d0, b.d4);
			v = (ulong)a.d1 * b.d3; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d1, b.d3);
			v = (ulong)a.d2 * b.d2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d2, b.d2);
			v = (ulong)a.d3 * b.d1; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d3, b.d1);
			v = (ulong)a.d4 * b.d0; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d4, b.d0);
			l[4] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[4]);
			v = (ulong)a.d0 * b.d5; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d0, b.d5);
			v = (ulong)a.d1 * b.d4; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d1, b.d4);
			v = (ulong)a.d2 * b.d3; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d2, b.d3);
			v = (ulong)a.d3 * b.d2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d3, b.d2);
			v = (ulong)a.d4 * b.d1; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d4, b.d1);
			v = (ulong)a.d5 * b.d0; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d5, b.d0);
			l[5] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[5]);
			v = (ulong)a.d0 * b.d6; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d0, b.d6);
			v = (ulong)a.d1 * b.d5; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d1, b.d5);
			v = (ulong)a.d2 * b.d4; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d2, b.d4);
			v = (ulong)a.d3 * b.d3; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d3, b.d3);
			v = (ulong)a.d4 * b.d2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d4, b.d2);
			v = (ulong)a.d5 * b.d1; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d5, b.d1);
			v = (ulong)a.d6 * b.d0; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d6, b.d0);
			l[6] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[6]);
			v = (ulong)a.d0 * b.d7; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d0, b.d7);
			v = (ulong)a.d1 * b.d6; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d1, b.d6);
			v = (ulong)a.d2 * b.d5; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d2, b.d5);
			v = (ulong)a.d3 * b.d4; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d3, b.d4);
			v = (ulong)a.d4 * b.d3; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d4, b.d3);
			v = (ulong)a.d5 * b.d2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d5, b.d2);
			v = (ulong)a.d6 * b.d1; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d6, b.d1);
			v = (ulong)a.d7 * b.d0; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d7, b.d0);
			l[7] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[7]);
			v = (ulong)a.d1 * b.d7; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d1, b.d7);
			v = (ulong)a.d2 * b.d6; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d2, b.d6);
			v = (ulong)a.d3 * b.d5; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d3, b.d5);
			v = (ulong)a.d4 * b.d4; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d4, b.d4);
			v = (ulong)a.d5 * b.d3; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d5, b.d3);
			v = (ulong)a.d6 * b.d2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d6, b.d2);
			v = (ulong)a.d7 * b.d1; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d7, b.d1);
			l[8] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[8]);
			v = (ulong)a.d2 * b.d7; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d2, b.d7);
			v = (ulong)a.d3 * b.d6; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d3, b.d6);
			v = (ulong)a.d4 * b.d5; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d4, b.d5);
			v = (ulong)a.d5 * b.d4; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d5, b.d4);
			v = (ulong)a.d6 * b.d3; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d6, b.d3);
			v = (ulong)a.d7 * b.d2; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d7, b.d2);
			l[9] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[9]);
			v = (ulong)a.d3 * b.d7; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d3, b.d7);
			v = (ulong)a.d4 * b.d6; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d4, b.d6);
			v = (ulong)a.d5 * b.d5; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d5, b.d5);
			v = (ulong)a.d6 * b.d4; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d6, b.d4);
			v = (ulong)a.d7 * b.d3; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d7, b.d3);
			l[10] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[10]);
			v = (ulong)a.d4 * b.d7; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d4, b.d7);
			v = (ulong)a.d5 * b.d6; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d5, b.d6);
			v = (ulong)a.d6 * b.d5; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d6, b.d5);
			v = (ulong)a.d7 * b.d4; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d7, b.d4);
			l[11] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[11]);
			v = (ulong)a.d5 * b.d7; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d5, b.d7);
			v = (ulong)a.d6 * b.d6; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d6, b.d6);
			v = (ulong)a.d7 * b.d5; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d7, b.d5);
			l[12] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[12]);
			v = (ulong)a.d6 * b.d7; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d6, b.d7);
			v = (ulong)a.d7 * b.d6; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a.d7, b.d6);
			l[13] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[13]);
			v = (ulong)a.d7 * b.d7; acc0 += v; VERIFY_CHECK(acc0 >= v); // muladd_fast(a.d7, b.d7);
			l[14] = (uint)acc0; acc0 >>= 32; VERIFY_CHECK(acc1 == 0); // extract_fast(out l[14]);
			VERIFY_CHECK((acc0 >> 32) == 0);
			l[15] = (uint)acc0;
		}
		private const ulong M = 0xFFFFFFFFUL;
		internal static void sqr_512(Span<uint> l, Span<uint> a)
		{
			/* 160 bit accumulator. */
			ulong v;
			ulong acc0 = 0;
			uint acc1 = 0;

			/* l[0..15] = a[0..7]^2. */
			v = (ulong)a[0] * a[0]; acc0 += v; VERIFY_CHECK(acc0 >= v); // muladd_fast(a[0], a[0]);
			l[0] = (uint)acc0; acc0 >>= 32; VERIFY_CHECK(acc1 == 0); // extract_fast(out l[0]);
			v = (ulong)a[0] * a[1]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[0], a[1]);
			l[1] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[1]);
			v = (ulong)a[0] * a[2]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[0], a[2]);
			v = (ulong)a[1] * a[1]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a[1], a[1]);
			l[2] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[2]);
			v = (ulong)a[0] * a[3]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[0], a[3]);
			v = (ulong)a[1] * a[2]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[1], a[2]);
			l[3] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[3]);
			v = (ulong)a[0] * a[4]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[0], a[4]);
			v = (ulong)a[1] * a[3]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[1], a[3]);
			v = (ulong)a[2] * a[2]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a[2], a[2]);
			l[4] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[4]);
			v = (ulong)a[0] * a[5]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[0], a[5]);
			v = (ulong)a[1] * a[4]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[1], a[4]);
			v = (ulong)a[2] * a[3]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[2], a[3]);
			l[5] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[5]);
			v = (ulong)a[0] * a[6]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[0], a[6]);
			v = (ulong)a[1] * a[5]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[1], a[5]);
			v = (ulong)a[2] * a[4]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[2], a[4]);
			v = (ulong)a[3] * a[3]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a[3], a[3]);
			l[6] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[6]);
			v = (ulong)a[0] * a[7]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[0], a[7]);
			v = (ulong)a[1] * a[6]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[1], a[6]);
			v = (ulong)a[2] * a[5]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[2], a[5]);
			v = (ulong)a[3] * a[4]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[3], a[4]);
			l[7] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[7]);
			v = (ulong)a[1] * a[7]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[1], a[7]);
			v = (ulong)a[2] * a[6]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[2], a[6]);
			v = (ulong)a[3] * a[5]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[3], a[5]);
			v = (ulong)a[4] * a[4]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a[4], a[4]);
			l[8] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[8]);
			v = (ulong)a[2] * a[7]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[2], a[7]);
			v = (ulong)a[3] * a[6]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[3], a[6]);
			v = (ulong)a[4] * a[5]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[4], a[5]);
			l[9] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[9]);
			v = (ulong)a[3] * a[7]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[3], a[7]);
			v = (ulong)a[4] * a[6]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[4], a[6]);
			v = (ulong)a[5] * a[5]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a[5], a[5]);
			l[10] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[10]);
			v = (ulong)a[4] * a[7]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[4], a[7]);
			v = (ulong)a[5] * a[6]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[5], a[6]);
			l[11] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[11]);
			v = (ulong)a[5] * a[7]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[5], a[7]);
			v = (ulong)a[6] * a[6]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd(a[6], a[6]);
			l[12] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[12]);
			v = (ulong)a[6] * a[7]; acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); acc0 += v; acc1 += (acc0 < v) ? 1U : 0; VERIFY_CHECK((acc0 >= v) || (acc1 != 0)); // muladd2(a[6], a[7]);
			l[13] = (uint)acc0; acc0 >>= 32; acc0 |= (ulong)acc1 << 32; acc1 = 0;  // extract(out l[13]);
			v = (ulong)a[7] * a[7]; acc0 += v; VERIFY_CHECK(acc0 >= v); // muladd_fast(a[7], a[7]);
			l[14] = (uint)acc0; acc0 >>= 32; VERIFY_CHECK(acc1 == 0); // extract_fast(out l[14]);
			VERIFY_CHECK((acc0 >> 32) == 0);
			l[15] = (uint)acc0;
		}

		public readonly Scalar Add(in Scalar b)
		{
			return Add(b, out _);
		}

		public readonly Scalar Add(in Scalar b, out int overflow)
		{
			Span<uint> d = stackalloc uint[DCount];
			ref readonly Scalar a = ref this;
			ulong t = (ulong)a.d0 + b.d0;
			d[0] = (uint)t; t >>= 32;
			t += (ulong)a.d1 + b.d1;
			d[1] = (uint)t; t >>= 32;
			t += (ulong)a.d2 + b.d2;
			d[2] = (uint)t; t >>= 32;
			t += (ulong)a.d3 + b.d3;
			d[3] = (uint)t; t >>= 32;
			t += (ulong)a.d4 + b.d4;
			d[4] = (uint)t; t >>= 32;
			t += (ulong)a.d5 + b.d5;
			d[5] = (uint)t; t >>= 32;
			t += (ulong)a.d6 + b.d6;
			d[6] = (uint)t; t >>= 32;
			t += (ulong)a.d7 + b.d7;
			d[7] = (uint)t; t >>= 32;
			overflow = (int)(t + (uint)CheckOverflow(d));
			VERIFY_CHECK(overflow == 0 || overflow == 1);
			Reduce(d, overflow);
			return new Scalar(d);
		}
		/** Extract the lowest 32 bits of (c0,c1,c2) into n, and left shift the number 32 bits. */
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void extract(Span<uint> acc, out uint n)
		{
			(n) = acc[0];
			acc[0] = acc[1];
			acc[1] = acc[2];
			acc[2] = 0;
		}

		[Conditional("SECP256K1_VERIFY")]
		private static void VERIFY_CHECK(bool value)
		{
			if (!value)
				throw new InvalidOperationException("VERIFY_CHECK failed (bug in C# secp256k1)");
		}

		public readonly bool IsOverflow
		{
			get
			{
				return CheckOverflow() != 0;
			}
		}

		internal readonly int CheckOverflow()
		{
			int yes = 0;
			int no = 0;
			no |= (d7 < SECP256K1_N_7 ? 1 : 0);
			no |= (d6 < SECP256K1_N_6 ? 1 : 0);
			no |= (d5 < SECP256K1_N_5 ? 1 : 0);
			no |= (d4 < SECP256K1_N_4 ? 1 : 0);
			yes |= (d4 > SECP256K1_N_4 ? 1 : 0) & ~no;
			no |= (d3 < SECP256K1_N_3 ? 1 : 0) & ~yes;
			yes |= (d3 > SECP256K1_N_3 ? 1 : 0) & ~no;
			no |= (d2 < SECP256K1_N_2 ? 1 : 0) & ~yes;
			yes |= (d2 > SECP256K1_N_2 ? 1 : 0) & ~no;
			no |= (d1 < SECP256K1_N_1 ? 1 : 0) & ~yes;
			yes |= (d1 > SECP256K1_N_1 ? 1 : 0) & ~no;
			yes |= (d0 >= SECP256K1_N_0 ? 1 : 0) & ~no;
			return yes;
		}
		static int CheckOverflow(Span<uint> d)
		{
			int yes = 0;
			int no = 0;
			no |= (d[7] < SECP256K1_N_7 ? 1 : 0);
			no |= (d[6] < SECP256K1_N_6 ? 1 : 0);
			no |= (d[5] < SECP256K1_N_5 ? 1 : 0);
			no |= (d[4] < SECP256K1_N_4 ? 1 : 0);
			yes |= (d[4] > SECP256K1_N_4 ? 1 : 0) & ~no;
			no |= (d[3] < SECP256K1_N_3 ? 1 : 0) & ~yes;
			yes |= (d[3] > SECP256K1_N_3 ? 1 : 0) & ~no;
			no |= (d[2] < SECP256K1_N_2 ? 1 : 0) & ~yes;
			yes |= (d[2] > SECP256K1_N_2 ? 1 : 0) & ~no;
			no |= (d[1] < SECP256K1_N_1 ? 1 : 0) & ~yes;
			yes |= (d[1] > SECP256K1_N_1 ? 1 : 0) & ~no;
			yes |= (d[0] >= SECP256K1_N_0 ? 1 : 0) & ~no;
			return yes;
		}

		public readonly void WriteToSpan(Span<byte> bin)
		{
			bin[0] = (byte)(d7 >> 24); bin[1] = (byte)(d7 >> 16); bin[2] = (byte)(d7 >> 8); bin[3] = (byte)d7;
			bin[4] = (byte)(d6 >> 24); bin[5] = (byte)(d6 >> 16); bin[6] = (byte)(d6 >> 8); bin[7] = (byte)d6;
			bin[8] = (byte)(d5 >> 24); bin[9] = (byte)(d5 >> 16); bin[10] = (byte)(d5 >> 8); bin[11] = (byte)d5;
			bin[12] = (byte)(d4 >> 24); bin[13] = (byte)(d4 >> 16); bin[14] = (byte)(d4 >> 8); bin[15] = (byte)d4;
			bin[16] = (byte)(d3 >> 24); bin[17] = (byte)(d3 >> 16); bin[18] = (byte)(d3 >> 8); bin[19] = (byte)d3;
			bin[20] = (byte)(d2 >> 24); bin[21] = (byte)(d2 >> 16); bin[22] = (byte)(d2 >> 8); bin[23] = (byte)d2;
			bin[24] = (byte)(d1 >> 24); bin[25] = (byte)(d1 >> 16); bin[26] = (byte)(d1 >> 8); bin[27] = (byte)d1;
			bin[28] = (byte)(d0 >> 24); bin[29] = (byte)(d0 >> 16); bin[30] = (byte)(d0 >> 8); bin[31] = (byte)d0;
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
#if SECP256K1_LIB
		public
#else
		internal
#endif
		readonly uint GetBits(int offset, int count)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), "Offset should be more than 0");
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count), "Count should be more than 0");
			VERIFY_CHECK((offset + count - 1) >> 5 == offset >> 5);
			return (uint)((At(offset >> 5) >> (offset & 0x1F)) & ((1 << count) - 1));
		}
#if SECP256K1_LIB
		public
#else
		internal
#endif
		readonly uint GetBitsVariable(int offset, int count)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), "Offset should be more than 0");
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count), "Count should be more than 0");
			if (count >= 32)
				throw new ArgumentOutOfRangeException(nameof(count), "Count should be less than 32");
			if (offset + count > 256)
				throw new ArgumentOutOfRangeException(nameof(count), "End index should be less or eq to 256");
			if ((offset + count - 1) >> 5 == offset >> 5)
			{
				return GetBits(offset, count);
			}
			else
			{
				VERIFY_CHECK((offset >> 5) + 1 < 8);
				return ((At(offset >> 5) >> (offset & 0x1F)) | (At((offset >> 5) + 1) << (32 - (offset & 0x1F)))) & ((((uint)1) << count) - 1);
			}
		}

#if SECP256K1_LIB
		public
#else
		internal
#endif
		uint At(int index)
		{
			switch (index)
			{
				case 0:
					return d0;
				case 1:
					return d1;
				case 2:
					return d2;
				case 3:
					return d3;
				case 4:
					return d4;
				case 5:
					return d5;
				case 6:
					return d6;
				case 7:
					return d7;
				default:
					throw new ArgumentOutOfRangeException(nameof(index), "index should 0-7 inclusive");
			}
		}


		public readonly void Split128(out Scalar r1, out Scalar r2)
		{
			r1 = new Scalar(d0, d1, d2, d3, 0, 0, 0, 0);
			r2 = new Scalar(d4, d5, d6, d7, 0, 0, 0, 0);
		}

		/**
		* The Secp256k1 curve has an endomorphism, where lambda * (x, y) = (beta * x, y), where
		* lambda is {0x53,0x63,0xad,0x4c,0xc0,0x5c,0x30,0xe0,0xa5,0x26,0x1c,0x02,0x88,0x12,0x64,0x5a,
		*            0x12,0x2e,0x22,0xea,0x20,0x81,0x66,0x78,0xdf,0x02,0x96,0x7c,0x1b,0x23,0xbd,0x72}
		*
		* "Guide to Elliptic Curve Cryptography" (Hankerson, Menezes, Vanstone) gives an algorithm
		* (algorithm 3.74) to find k1 and k2 given k, such that k1 + k2 * lambda == k mod n, and k1
		* and k2 have a small size.
		* It relies on constants a1, b1, a2, b2. These constants for the value of lambda above are:
		*
		* - a1 =      {0x30,0x86,0xd2,0x21,0xa7,0xd4,0x6b,0xcd,0xe8,0x6c,0x90,0xe4,0x92,0x84,0xeb,0x15}
		* - b1 =     -{0xe4,0x43,0x7e,0xd6,0x01,0x0e,0x88,0x28,0x6f,0x54,0x7f,0xa9,0x0a,0xbf,0xe4,0xc3}
		* - a2 = {0x01,0x14,0xca,0x50,0xf7,0xa8,0xe2,0xf3,0xf6,0x57,0xc1,0x10,0x8d,0x9d,0x44,0xcf,0xd8}
		* - b2 =      {0x30,0x86,0xd2,0x21,0xa7,0xd4,0x6b,0xcd,0xe8,0x6c,0x90,0xe4,0x92,0x84,0xeb,0x15}
		*
		* The algorithm then computes c1 = round(b1 * k / n) and c2 = round(b2 * k / n), and gives
		* k1 = k - (c1*a1 + c2*a2) and k2 = -(c1*b1 + c2*b2). Instead, we use modular arithmetic, and
		* compute k1 as k - k2 * lambda, avoiding the need for constants a1 and a2.
		*
		* g1, g2 are precomputed constants used to replace division with a rounded multiplication
		* when decomposing the scalar for an endomorphism-based point multiplication.
		*
		* The possibility of using precomputed estimates is mentioned in "Guide to Elliptic Curve
		* Cryptography" (Hankerson, Menezes, Vanstone) in section 3.5.
		*
		* The derivation is described in the paper "Efficient Software Implementation of Public-Key
		* Cryptography on Sensor Networks Using the MSP430X Microcontroller" (Gouvea, Oliveira, Lopez),
		* Section 4.3 (here we use a somewhat higher-precision estimate):
		* d = a1*b2 - b1*a2
		* g1 = round((2^272)*b2/d)
		* g2 = round((2^272)*b1/d)
		*
		* (Note that 'd' is also equal to the curve order here because [a1,b1] and [a2,b2] are found
		* as outputs of the Extended Euclidean Algorithm on inputs 'order' and 'lambda').
		*
		* The function below splits a in r1 and r2, such that r1 + lambda * r2 == a (mod order).
*/

		public static Scalar CONST(uint d7, uint d6, uint d5, uint d4, uint d3, uint d2, uint d1, uint d0)
		{
			return new Scalar(d0, d1, d2, d3, d4, d5, d6, d7);
		}
		static readonly Scalar minus_lambda = CONST(
			0xAC9C52B3U, 0x3FA3CF1FU, 0x5AD9E3FDU, 0x77ED9BA4U,
			0xA880B9FCU, 0x8EC739C2U, 0xE0CFC810U, 0xB51283CFU
		);
		static readonly Scalar minus_b1 = CONST(
			0x00000000U, 0x00000000U, 0x00000000U, 0x00000000U,
			0xE4437ED6U, 0x010E8828U, 0x6F547FA9U, 0x0ABFE4C3U
		);
		static readonly Scalar minus_b2 = CONST(
			0xFFFFFFFFU, 0xFFFFFFFFU, 0xFFFFFFFFU, 0xFFFFFFFEU,
			0x8A280AC5U, 0x0774346DU, 0xD765CDA8U, 0x3DB1562CU
		);
		static readonly Scalar g1 = CONST(
			0x00000000U, 0x00000000U, 0x00000000U, 0x00003086U,
			0xD221A7D4U, 0x6BCDE86CU, 0x90E49284U, 0xEB153DABU
		);
		static readonly Scalar g2 = CONST(
			0x00000000U, 0x00000000U, 0x00000000U, 0x0000E443U,
			0x7ED6010EU, 0x88286F54U, 0x7FA90ABFU, 0xE4C42212U
		);
		public bool IsEven
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return (d0 & 1) == 0;
			}
		}

		public bool IsHigh
		{
			get
			{
				int yes = 0;
				int no = 0;
				no |= (d7 < SECP256K1_N_H_7 ? 1 : 0);
				yes |= (d7 > SECP256K1_N_H_7 ? 1 : 0) & ~no;
				no |= (d6 < SECP256K1_N_H_6 ? 1 : 0) & ~yes; /* No need for a > check. */
				no |= (d5 < SECP256K1_N_H_5 ? 1 : 0) & ~yes; /* No need for a > check. */
				no |= (d4 < SECP256K1_N_H_4 ? 1 : 0) & ~yes; /* No need for a > check. */
				no |= (d3 < SECP256K1_N_H_3 ? 1 : 0) & ~yes;
				yes |= (d3 > SECP256K1_N_H_3 ? 1 : 0) & ~no;
				no |= (d2 < SECP256K1_N_H_2 ? 1 : 0) & ~yes;
				yes |= (d2 > SECP256K1_N_H_2 ? 1 : 0) & ~no;
				no |= (d1 < SECP256K1_N_H_1 ? 1 : 0) & ~yes;
				yes |= (d1 > SECP256K1_N_H_1 ? 1 : 0) & ~no;
				yes |= (d0 > SECP256K1_N_H_0 ? 1 : 0) & ~no;
				return yes != 0;
			}
		}

		public readonly void SplitLambda(out Scalar r1, out Scalar r2)
		{
			/* these _var calls are constant time since the shift amount is constant */
			Scalar c1 = this.MultiplyShiftVariable(g1, 272);
			Scalar c2 = this.MultiplyShiftVariable(g2, 272);
			c1 = c1 * minus_b1;
			c2 = c2 * minus_b2;
			r2 = c1 + c2;
			r1 = r2 * minus_lambda;
			r1 = r1 + this;
		}

		public readonly Scalar MultiplyShiftVariable(in Scalar b, int shift)
		{
			Span<uint> l = stackalloc uint[16];
			int shiftlimbs;
			int shiftlow;
			int shifthigh;
			VERIFY_CHECK(shift >= 256);
			Scalar.mul_512(l, this, b);
			shiftlimbs = shift >> 5;
			shiftlow = shift & 0x1F;
			shifthigh = 32 - shiftlow;

			var r = new Scalar(
				shift < 512 ? (l[0 + shiftlimbs] >> shiftlow | (shift < 480 && shiftlow != 0 ? (l[1 + shiftlimbs] << shifthigh) : 0)) : 0,
				shift < 480 ? (l[1 + shiftlimbs] >> shiftlow | (shift < 448 && shiftlow != 0 ? (l[2 + shiftlimbs] << shifthigh) : 0)) : 0,
				shift < 448 ? (l[2 + shiftlimbs] >> shiftlow | (shift < 416 && shiftlow != 0 ? (l[3 + shiftlimbs] << shifthigh) : 0)) : 0,
				shift < 416 ? (l[3 + shiftlimbs] >> shiftlow | (shift < 384 && shiftlow != 0 ? (l[4 + shiftlimbs] << shifthigh) : 0)) : 0,
				shift < 384 ? (l[4 + shiftlimbs] >> shiftlow | (shift < 352 && shiftlow != 0 ? (l[5 + shiftlimbs] << shifthigh) : 0)) : 0,
				shift < 352 ? (l[5 + shiftlimbs] >> shiftlow | (shift < 320 && shiftlow != 0 ? (l[6 + shiftlimbs] << shifthigh) : 0)) : 0,
				shift < 320 ? (l[6 + shiftlimbs] >> shiftlow | (shift < 288 && shiftlow != 0 ? (l[7 + shiftlimbs] << shifthigh) : 0)) : 0,
				shift < 288 ? (l[7 + shiftlimbs] >> shiftlow) : 0
				);
			r = r.CAddBit(0, (int)((l[(shift - 1) >> 5] >> ((shift - 1) & 0x1f)) & 1));
			return r;
		}

		public readonly bool IsZero
		{
			[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.AggressiveInlining)]
			get
			{
				return (d0 | d1 | d2 | d3 | d4 | d5 | d6 | d7) == 0;
			}
		}
		public readonly bool IsOne
		{
			[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.AggressiveInlining)]
			get
			{
				return ((d0 ^ 1) | d1 | d2 | d3 | d4 | d5 | d6 | d7) == 0;
			}
		}

		const int DCount = 8;
		public readonly Scalar Sqr()
		{
			return Sqr(1);
		}
		public readonly Scalar Sqr(int times)
		{
			Span<uint> l = stackalloc uint[16];
			Span<uint> d = stackalloc uint[DCount];
			Deconstruct(ref d);
			for (int i = 0; i < times; i++)
			{
				sqr_512(l, d);
				reduce_512(d, l);
			}
			return new Scalar(d);
		}

		public readonly Scalar Inverse()
		{
			ref readonly Scalar x = ref this;
			Scalar r = Scalar.Zero;
			/* First compute xN as x ^ (2^N - 1) for some values of N,
			 * and uM as x ^ M for some values of M. */
			Scalar x2, x3, x6, x8, x14, x28, x56, x112, x126;
			Scalar u2, u5, u9, u11, u13;

			u2 = x.Sqr();
			x2 = u2 * x;
			u5 = u2 * x2;
			x3 = u5 * u2;
			u9 = x3 * u2;
			u11 = u9 * u2;
			u13 = u11 * u2;

			x6 = u13.Sqr();
			x6 = x6.Sqr();
			x6 = x6 * u11;

			x8 = x6.Sqr();
			x8 = x8.Sqr();
			x8 = x8 * x2;

			x14 = x8.Sqr();
			x14 = x14.Sqr(5);
			x14 = x14 * x6;

			x28 = x14.Sqr();
			x28 = x28.Sqr(13);
			x28 = x28 * x14;

			x56 = x28.Sqr();
			x56 = x56.Sqr(27);
			x56 = x56 * x28;

			x112 = x56.Sqr();
			x112 = x112.Sqr(55);
			x112 = x112 * x56;

			x126 = x112.Sqr();
			x126 = x126.Sqr(13);
			x126 = x126 * x14;

			/* Then accumulate the final result (t starts at x126). */
			ref Scalar t = ref x126;
			t = t.Sqr(3);
			t = t * u5; /* 101 */
			t = t.Sqr(4);
			t = t * x3; /* 111 */
			t = t.Sqr(4);
			t = t * u5; /* 101 */
			t = t.Sqr(5);
			t = t * u11; /* 1011 */
			t = t.Sqr(4);
			t = t * u11; /* 1011 */
			t = t.Sqr(4);
			t = t * x3; /* 111 */
			t = t.Sqr(5);
			t = t * x3; /* 111 */
			t = t.Sqr(6);
			t = t * u13; /* 1101 */
			t = t.Sqr(4);
			t = t * u5; /* 101 */
			t = t.Sqr(3);
			t = t * x3; /* 111 */
			t = t.Sqr(5);
			t = t * u9; /* 1001 */
			t = t.Sqr(6);
			t = t * u5; /* 101 */
			t = t.Sqr(10);
			t = t * x3; /* 111 */
			t = t.Sqr(4);
			t = t * x3; /* 111 */
			t = t.Sqr(9);
			t = t * x8; /* 11111111 */
			t = t.Sqr(5);
			t = t * u9; /* 1001 */
			t = t.Sqr(6);
			t = t * u11; /* 1011 */
			t = t.Sqr(4);
			t = t * u13; /* 1101 */
			t = t.Sqr(5);
			t = t * x2; /* 11 */
			t = t.Sqr(6);
			t = t * u13; /* 1101 */
			t = t.Sqr(10);
			t = t * u13; /* 1101 */
			t = t.Sqr(4);
			t = t * u9; /* 1001 */
			/* 00000 */
			t = t.Sqr(6);

			t = t * x; /* 1 */
			t = t.Sqr(8);
			r = t * x6; /* 111111 */
			return r;
		}

		public static Scalar operator *(in Scalar a, in Scalar b)
		{
			return a.Multiply(b);
		}
		public readonly Scalar Multiply(in Scalar b)
		{
			Span<uint> d = stackalloc uint[DCount];
			this.Deconstruct(ref d);
			Span<uint> l = stackalloc uint[16];
			mul_512(l, this, b);
			reduce_512(d, l);
			return new Scalar(d);
		}

		public readonly int ShrInt(int n, out Scalar ret)
		{
			VERIFY_CHECK(n > 0);
			VERIFY_CHECK(n < 16);
			var v = (int)(d0 & ((1 << n) - 1));
			ret = new Scalar
			(
				(d0 >> n) + (d1 << (32 - n)),
				(d1 >> n) + (d2 << (32 - n)),
				(d2 >> n) + (d3 << (32 - n)),
				(d3 >> n) + (d4 << (32 - n)),
				(d4 >> n) + (d5 << (32 - n)),
				(d5 >> n) + (d6 << (32 - n)),
				(d6 >> n) + (d7 << (32 - n)),
				(d7 >> n)
			);
			return v;
		}

		public readonly Scalar Negate()
		{
			Span<uint> d = stackalloc uint[DCount];
			ref readonly Scalar a = ref this;
			uint nonzero = 0xFFFFFFFFU * (a.IsZero ? 0U : 1);
			ulong t = (ulong)(~a.d0) + SECP256K1_N_0 + 1;
			d[0] = (uint)(t & nonzero); t >>= 32;
			t += (ulong)(~a.d1) + SECP256K1_N_1;
			d[1] = (uint)(t & nonzero); t >>= 32;
			t += (ulong)(~a.d2) + SECP256K1_N_2;
			d[2] = (uint)(t & nonzero); t >>= 32;
			t += (ulong)(~a.d3) + SECP256K1_N_3;
			d[3] = (uint)(t & nonzero); t >>= 32;
			t += (ulong)(~a.d4) + SECP256K1_N_4;
			d[4] = (uint)(t & nonzero); t >>= 32;
			t += (ulong)(~a.d5) + SECP256K1_N_5;
			d[5] = (uint)(t & nonzero); t >>= 32;
			t += (ulong)(~a.d6) + SECP256K1_N_6;
			d[6] = (uint)(t & nonzero); t >>= 32;
			t += (ulong)(~a.d7) + SECP256K1_N_7;
			d[7] = (uint)(t & nonzero);
			return new Scalar(d);
		}

		public static GEJ operator *(in Scalar scalar, in GE groupElement)
		{
			return groupElement.MultConst(scalar, 256);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]
		public readonly bool Equals(Scalar b)
		{
			ref readonly Scalar a = ref this;
			return ((a.d0 ^ b.d0) | (a.d1 ^ b.d1) | (a.d2 ^ b.d2) | (a.d3 ^ b.d3) | (a.d4 ^ b.d4) | (a.d5 ^ b.d5) | (a.d6 ^ b.d6) | (a.d7 ^ b.d7)) == 0;
		}
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]
		public readonly override bool Equals(object? obj)
		{
			if (obj is Scalar b)
			{
				ref readonly Scalar a = ref this;
				return ((a.d0 ^ b.d0) | (a.d1 ^ b.d1) | (a.d2 ^ b.d2) | (a.d3 ^ b.d3) | (a.d4 ^ b.d4) | (a.d5 ^ b.d5) | (a.d6 ^ b.d6) | (a.d7 ^ b.d7)) == 0;
			}
			return false;
		}

		public static bool operator ==(in Scalar a, in Scalar b)
		{
			return a.Equals(b);
		}
		public static bool operator !=(in Scalar a, in Scalar b)
		{
			return !a.Equals(b);
		}
		public static Scalar operator +(in Scalar a, in Scalar b)
		{
			return a.Add(b);
		}

		public readonly void Deconstruct(
				ref Span<uint> d)
		{
			d[0] = this.d0;
			d[1] = this.d1;
			d[2] = this.d2;
			d[3] = this.d3;
			d[4] = this.d4;
			d[5] = this.d5;
			d[6] = this.d6;
			d[7] = this.d7;
		}


		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + d0.GetHashCode();
				hash = hash * 23 + d1.GetHashCode();
				hash = hash * 23 + d2.GetHashCode();
				hash = hash * 23 + d3.GetHashCode();
				hash = hash * 23 + d4.GetHashCode();
				hash = hash * 23 + d5.GetHashCode();
				hash = hash * 23 + d6.GetHashCode();
				hash = hash * 23 + d7.GetHashCode();
				return hash;
			}
		}
		public readonly Scalar InverseVariable()
		{
			return Inverse();
		}
		public readonly string ToC(string varname)
		{
			return $"secp256k1_scalar {varname} = {{ 0x{d0.ToString("X8")}UL, 0x{d1.ToString("X8")}UL, 0x{d2.ToString("X8")}UL, 0x{d3.ToString("X8")}UL, 0x{d4.ToString("X8")}UL, 0x{d5.ToString("X8")}UL, 0x{d6.ToString("X8")}UL, 0x{d7.ToString("X8")}UL }}";
		}

		public readonly byte[] ToBytes()
		{
			Span<byte> tmp = stackalloc byte[32];
			WriteToSpan(tmp);
			return tmp.ToArray();
		}
	}
}
#nullable restore
#endif
