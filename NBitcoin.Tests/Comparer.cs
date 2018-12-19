using System.Collections.Generic;
using NBitcoin.BIP174;

namespace NBitcoin.Tests
{
	public static class Comparer
	{
		public class PSBTComparer : EqualityComparer<PSBT>
		{
			public override bool Equals(PSBT a, PSBT b) => a.Equals(b);
			public override int GetHashCode(PSBT psbt) => psbt.GetHashCode();
		}
	}
}