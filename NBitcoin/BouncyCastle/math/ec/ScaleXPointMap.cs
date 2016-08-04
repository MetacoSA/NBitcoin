﻿namespace NBitcoin.BouncyCastle.Math.EC
{
	internal class ScaleXPointMap
		: ECPointMap
	{
		protected readonly ECFieldElement scale;

		public ScaleXPointMap(ECFieldElement scale)
		{
			this.scale = scale;
		}

		public virtual ECPoint Map(ECPoint p)
		{
			return p.ScaleX(scale);
		}
	}
}
