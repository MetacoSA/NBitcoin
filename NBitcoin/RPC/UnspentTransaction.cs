using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.RPC
{
    public class UnspentTransaction
    {
		public UnspentTransaction(JObject unspent)
		{
			this.bestblock = uint256.Parse((string)unspent[nameof(bestblock)]);
			this.confirmations = (int)unspent[(nameof(confirmations))];
			this.value = (decimal)unspent[(nameof(value))];
			this.scriptPubKey = unspent[nameof(scriptPubKey)].ToObject<RPCScriptPubKey>();
			this.coinbase = (bool)unspent[(nameof(coinbase))];
		}

		public uint256 bestblock { get; set; }
		public int confirmations { get; set; }
		public decimal value { get; set; }
		public RPCScriptPubKey scriptPubKey { get; set; }
		public bool coinbase { get; set; }
	}
}
