namespace NBitcoin.RPC
{
	public class FundRawTransactionOptions
	{
		public BitcoinAddress ChangeAddress
		{
			get; set;
		}

		public int? ChangePosition
		{
			get; set;
		}

		public bool IncludeWatching
		{
			get; set;
		}

		public bool LockUnspents
		{
			get; set;
		}

		public bool? ReserveChangeKey
		{
			get; set;
		}

		public FeeRate FeeRate
		{
			get; set;
		}

		public int[] SubtractFeeFromOutputs
		{
			get; set;
		}
	}
}
