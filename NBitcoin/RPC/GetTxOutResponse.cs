using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
	public class GetTxOutResponse
	{
		/// <summary>
		/// the block hash
		/// </summary>
		public uint256 BestBlock
		{
			get; set;
		}

		/// <summary>
		/// The number of confirmations
		/// </summary>
		public int Confirmations
		{
			get; set;
		}

		public TxOut TxOut
		{
			get; set;
		}

		/// <summary>
		/// Coinbase or not
		/// </summary>
		public bool IsCoinBase
		{
			get; set;
		}

		/// <summary>
		/// The type, eg pubkeyhash
		/// </summary>
		public string ScriptPubKeyType
		{
			get; set;
		}
	}
}
