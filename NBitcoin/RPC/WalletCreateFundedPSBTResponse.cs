using NBitcoin.BIP174;

namespace NBitcoin.RPC
{
	public class WalletCreateFundedPSBTResponse
	{
		public PSBT PSBT { get; internal set; }
		public Money Fee { get; internal set; }
		// -1 if no change output.
		public int ChangePos { get; internal set; }
	}
}