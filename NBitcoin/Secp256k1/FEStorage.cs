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
	readonly struct FEStorage
	{
		public static FEStorage CONST(uint d7, uint d6, uint d5, uint d4, uint d3, uint d2, uint d1, uint d0)
		{
			return new FEStorage(d0, d1, d2, d3, d4, d5, d6, d7);
		}
#if SECP256K1_LIB
		public
#else
		internal
#endif
		readonly uint n0, n1, n2, n3, n4, n5, n6, n7;

		public FEStorage(uint n0, uint n1, uint n2, uint n3, uint n4, uint n5, uint n6, uint n7)
		{
			this.n0 = n0;
			this.n1 = n1;
			this.n2 = n2;
			this.n3 = n3;
			this.n4 = n4;
			this.n5 = n5;
			this.n6 = n6;
			this.n7 = n7;
		}
		public FEStorage(ReadOnlySpan<uint> n)
		{
			this.n0 = n[0];
			this.n1 = n[1];
			this.n2 = n[2];
			this.n3 = n[3];
			this.n4 = n[4];
			this.n5 = n[5];
			this.n6 = n[6];
			this.n7 = n[7];
		}

		public readonly void Deconstruct(
			out uint n0,
			out uint n1,
			out uint n2,
			out uint n3,
			out uint n4,
			out uint n5,
			out uint n6,
			out uint n7
			)
		{
			n0 = this.n0;
			n1 = this.n1;
			n2 = this.n2;
			n3 = this.n3;
			n4 = this.n4;
			n5 = this.n5;
			n6 = this.n6;
			n7 = this.n7;
		}
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly FE ToFieldElement()
		{
			ref readonly FEStorage a = ref this;
			Span<uint> n = stackalloc uint[FE.NCount];
			int magnitude;
			bool normalized;
			n[0] = a.n0 & 0x3FFFFFFU;
			n[1] = a.n0 >> 26 | ((a.n1 << 6) & 0x3FFFFFFU);
			n[2] = a.n1 >> 20 | ((a.n2 << 12) & 0x3FFFFFFU);
			n[3] = a.n2 >> 14 | ((a.n3 << 18) & 0x3FFFFFFU);
			n[4] = a.n3 >> 8 | ((a.n4 << 24) & 0x3FFFFFFU);
			n[5] = (a.n4 >> 2) & 0x3FFFFFFU;
			n[6] = a.n4 >> 28 | ((a.n5 << 4) & 0x3FFFFFFU);
			n[7] = a.n5 >> 22 | ((a.n6 << 10) & 0x3FFFFFFU);
			n[8] = a.n6 >> 16 | ((a.n7 << 16) & 0x3FFFFFFU);
			n[9] = a.n7 >> 10;
			magnitude = 1;
			normalized = true;
			return new FE(n, magnitude, normalized);
		}
		[Conditional("SECP256K1_VERIFY")]
		private static void VERIFY_CHECK(bool value)
		{
			if (!value)
				throw new InvalidOperationException("VERIFY_CHECK failed (bug in C# secp256k1)");
		}
		internal const int NCount = 8;
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public static void CMov(ref FEStorage r, in FEStorage a, int flag)
		{
			uint mask0, mask1;
			mask0 = (uint)flag + ~((uint)0);
			mask1 = ~mask0;
			r = new FEStorage(
				(r.n0 & mask0) | (a.n0 & mask1),
				(r.n1 & mask0) | (a.n1 & mask1),
				(r.n2 & mask0) | (a.n2 & mask1),
				(r.n3 & mask0) | (a.n3 & mask1),
				(r.n4 & mask0) | (a.n4 & mask1),
				(r.n5 & mask0) | (a.n5 & mask1),
				(r.n6 & mask0) | (a.n6 & mask1),
				(r.n7 & mask0) | (a.n7 & mask1));
		}

		public void Deconstruct(ref Span<uint> n)
		{
			n[0] = n0;
			n[1] = n1;
			n[2] = n2;
			n[3] = n3;
			n[4] = n4;
			n[5] = n5;
			n[6] = n6;
			n[7] = n7;
		}
	}
}
#nullable restore
#endif
