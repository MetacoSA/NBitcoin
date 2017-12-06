using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
    public class GetTxOutResponse
	{
		// the block hash
		public uint256 BestBlock
		{
			get; set;
		}
		// The number of confirmations
		public int Confirmations
		{
			get; set;
		}
		public TxOut TxOut
		{
			get; set;
		}
		// Coinbase or not
		public bool IsCoinBase
		{
			get; set;
		}
		// The type, eg pubkeyhash
		public string ScriptPubKeyType
		{
			get; set;
		}
	}
}
