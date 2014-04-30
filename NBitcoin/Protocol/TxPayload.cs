using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	[Payload("tx")]
	public class TxPayload : Payload
	{
		public TxPayload()
		{

		}
		public TxPayload(Transaction transaction)
		{
			_Transaction = transaction;
		}
		Transaction _Transaction = new Transaction();
		public Transaction Transaction
		{
			get
			{
				return _Transaction;
			}
			set
			{
				_Transaction = value;
			}
		}
		public override void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _Transaction);
		}
	}
}
