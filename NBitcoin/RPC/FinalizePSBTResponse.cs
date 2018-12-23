using NBitcoin.BIP174;

namespace NBitcoin.RPC
{
	public class FinalizePSBTResponse
	{
		public PSBT psbt { get; }
		public Transaction hex { get; }
		public bool complete { get; }

		public FinalizePSBTResponse(PSBT psbt, Transaction hex, bool complete)
		{
			this.psbt = psbt;
			this.hex = hex;
			this.complete = complete;
		}
	}
}