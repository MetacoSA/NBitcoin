using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	/// <summary>
	/// Represents a transaction being sent on the network, is sent after being requested by a getdata (of Transaction or MerkleBlock) message.
	/// </summary>
	[Payload("tx")]
	public class TxPayload : BitcoinSerializablePayload<Transaction>
	{
		public TxPayload()
		{

		}
		public TxPayload(Transaction transaction) : base(transaction)
		{

		}
	}
}
