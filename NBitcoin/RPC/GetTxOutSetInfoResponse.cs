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

#pragma warning disable CS0618 // Type or member is obsolete
		public string HashSerialized => HashSerialized2 ?? HashSerialized3;
#pragma warning restore CS0618 // Type or member is obsolete
		[Obsolete("Use HashSerialized instead")]
		public string HashSerialized2 { get; set; }
		public string HashSerialized3 { get; set; }
		public long DiskSize { get; set; }
		public Money TotalAmount { get; set; }
	}
}
