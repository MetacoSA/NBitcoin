using NBitcoin.BIP174;

namespace NBitcoin.RPC
{
	public class FinalizePSBTResponse
	{
		public PSBT Psbt { get; }
		public Transaction Transaction { get; }
		public bool Complete { get; }

		public FinalizePSBTResponse(PSBT psbt, Transaction tx, bool complete)
		{
			this.Psbt = psbt;
			this.Transaction = tx;
			this.Complete = complete;
		}
	}
}