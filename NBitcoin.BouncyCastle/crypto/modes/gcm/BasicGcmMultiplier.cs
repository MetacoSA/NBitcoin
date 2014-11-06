using System;

using NBitcoin.BouncyCastle.Utilities;

namespace NBitcoin.BouncyCastle.Crypto.Modes.Gcm
{
	public class BasicGcmMultiplier
		: IGcmMultiplier
	{
		private byte[] H;

		public void Init(byte[] H)
		{
            this.H = Arrays.Clone(H);
		}

        public void MultiplyH(byte[] x)
		{
			GcmUtilities.Multiply(x, H);
		}
	}
}
