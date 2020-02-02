

namespace NBitcoin.RPC
{
	public class WalletProcessPSBTResponse
	{
		public WalletProcessPSBTResponse(PSBT psbt, bool complete)
		{
			this.PSBT = psbt;
			this.Complete = complete;

		}
		public PSBT PSBT { get; }
		public bool Complete { get; }
	}
}