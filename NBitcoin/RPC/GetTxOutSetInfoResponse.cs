using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.RPC
{
	public class GetTxOutSetInfoResponse
	{
		public int Height { get; set; }
		public uint256 Bestblock { get; set; }
		public long Transactions { get; set; }
		public long Txouts { get; set; }
		public long Bogosize { get; set; }
		public string HashSerialized2 { get; set; }
		public long DiskSize { get; set; }
		public Money TotalAmount { get; set; }
	}
}
