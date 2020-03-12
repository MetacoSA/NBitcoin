#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.Secp256k1
{
#if SECP256K1_LIB
	public
#else
	internal
#endif
	readonly struct GEStorage
	{
		public static GEStorage CONST(uint a, uint b, uint c, uint d, uint e, uint f, uint g, uint h, uint i, uint j, uint k, uint l, uint m, uint n, uint o, uint p)
		{
			return new GEStorage(FE.CONST(a, b, c, d, e, f, g, h), FE.CONST(i, j, k, l, m, n, o, p));
		}

#if SECP256K1_LIB
		public
#else
		internal
#endif
		readonly FEStorage x, y;
		public GEStorage(in FE x, in FE y)
		{
			this.x = x.Normalize().ToStorage();
			this.y = y.Normalize().ToStorage();
		}
		public GEStorage(in FEStorage x, in FEStorage y)
		{
			this.x = x;
			this.y = y;
		}

		public readonly GE ToGroupElement()
		{
			return new GE(this.x.ToFieldElement(), this.y.ToFieldElement(), false);
		}
		public void Deconstruct(out FEStorage x, out FEStorage y)
		{
			x = this.x;
			y = this.y;
		}
		public static void CMov(ref GEStorage r, in GEStorage a, int flag)
		{
			var (rx, ry) = r;
			FEStorage.CMov(ref rx, a.x, flag);
			FEStorage.CMov(ref ry, a.y, flag);
			r = new GEStorage(rx, ry);
		}
	}
}
#nullable restore
#endif
