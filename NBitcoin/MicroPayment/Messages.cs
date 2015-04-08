using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.MicroPayment
{
	public class OpenChannelMessage
	{
		public Transaction UnsignedRefund
		{
			get;
			set;
		}
	}
	public class OpenChannelAckMessage
	{
		public Transaction SignedRefund
		{
			get;
			set;
		}
	}
	public class OpenedChannelMessage : PayMessage
	{
		public uint256 FundId
		{
			get;
			set;
		}
	}
	public class PayMessage
	{
		public int Sequence
		{
			get;
			set;
		}
		public Money Amount
		{
			get;
			set;
		}
		public Transaction Payment
		{
			get;
			set;
		}
	}
}
