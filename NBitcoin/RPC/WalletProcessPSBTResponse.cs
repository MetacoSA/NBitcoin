using NBitcoin.BIP174;

namespace NBitcoin.RPC
{
	public class WalletProcessPSBTResponse
	{
		public WalletProcessPSBTResponse(PSBT psbt, bool complete)
		{
			this.Psbt = psbt;
			this.Complete = complete;

		}
		public PSBT Psbt { get; }
		public bool Complete { get; }
	}
}