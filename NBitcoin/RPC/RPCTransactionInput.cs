#if !NOJSONNET
using Newtonsoft.Json;
namespace NBitcoin.RPC
{
	public class RPCTransactionInput
	{
		[JsonProperty("txid", NullValueHandling = NullValueHandling.Ignore)]
		public uint256 TxId { get; set; }
		[JsonProperty("vout", NullValueHandling = NullValueHandling.Ignore)]
		public uint vout { get; set; }
		[JsonProperty("sequence", NullValueHandling = NullValueHandling.Ignore)]
		public Sequence nSequence { get; set; }

		public RPCTransactionInput(TxIn txin)
		{
			TxId = txin.PrevOut.Hash;
			vout = txin.PrevOut.N;
			nSequence = txin.Sequence;
		}
	}

	public static class TxInExtension
	{
		public static RPCTransactionInput ToRPCInputs(this TxIn txin)
		{
			return new RPCTransactionInput(txin);
		}
	}
}
#endif