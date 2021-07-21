using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.RPC
{
	public class ScanTxoutSetResponse
	{
		public int SearchedItems { get; internal set; }
		public bool Success { get; internal set; }
		public ScanTxoutOutput[] Outputs { get; set; }
		public Money TotalAmount { get; set; }
	}

	public class ScanTxoutOutput
	{
		public Coin Coin { get; set; }
		public int Height { get; set; }
	}
}
