

namespace NBitcoin.RPC
{
	public class WalletCreateFundedPSBTResponse
	{
		public PSBT PSBT { get; internal set; }
		public Money Fee { get; internal set; }
		public int? ChangePos { get; internal set; }
	}
}