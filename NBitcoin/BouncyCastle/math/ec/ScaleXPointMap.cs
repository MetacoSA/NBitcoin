namespace nStratis.BouncyCastle.math.ec
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
