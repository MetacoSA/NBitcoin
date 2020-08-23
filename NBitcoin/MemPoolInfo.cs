using System;
using System.Collections.Generic;

namespace NBitcoin
{
	public class MemPoolInfo
	{
		public int Size { get; set; }

		public int Bytes { get; set; }

		public int Usage { get; set; }

		public double MaxMemPool { get; set; }

		public double MemPoolMinFee { get; set; }

		public double MinRelayTxFee { get; set; }

		public FeeRateGroup[] Histogram { get; set; }

		public MemPoolInfo() { }
	}

	public class FeeRateGroup
	{
		public int Group { get;  set; }
		public ulong Sizes { get; set; }
		public uint Count { get; set; }
		public Money Fees { get; set; }
		public FeeRate From { get; set; }
		public FeeRate To { get; set; }
	}	
}
