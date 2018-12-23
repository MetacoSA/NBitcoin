using NBitcoin.BIP174;

namespace NBitcoin.RPC
{
	public class WalletProcessPSBTResponse
	{
		public WalletProcessPSBTResponse(PSBT psbt, bool complete)
		{
			this.psbt = psbt;
			this.complete = complete;

		}
		public PSBT psbt { get; }
		public bool complete { get; }
	}
}