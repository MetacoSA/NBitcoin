using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.RPC
{
	public class ListUnspentOptions
	{
		/// <summary>
		/// Minimum value of each UTXO
		/// </summary>
		public decimal? MinimumAmount
		{
			get; set;
		}

		/// <summary>
		/// Maximum value of each UTXO
		/// </summary>
		public decimal? MaximumAmount
		{
			get; set;
		}

		/// <summary>
		/// Maximum number of UTXOs
		/// </summary>
		public int? MaximumCount
		{
			get; set;
		}

		/// <summary>
		/// Minimum sum value of all UTXOs
		/// </summary>
		public decimal? MinimumSumAmount
		{
			get; set;
		}
	}
}
