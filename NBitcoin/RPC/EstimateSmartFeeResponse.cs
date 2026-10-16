namespace NBitcoin.RPC
{
	public enum FeeRateEstimator
	{
		Auto,
		BlockPolicy,
		MempoolPolicy
	}

	public class EstimateSmartFeeOptions
	{
		public FeeRateEstimator? FeeRateEstimator { get; set; }
		public int? Verbosity { get; set; }
	}

	public class EstimateSmartFeeResponse
	{
		public FeeRate FeeRate
		{
			get; set;
		}
		public int Blocks
		{
			get; set;
		}
		public FeeRateEstimator? Estimator { get; set; }
		public string[] Errors { get; set; }
		public MempoolHealthStatistics[] MempoolHealthStatistics { get; set; }
	}

	public class MempoolHealthStatistics
	{
		public int BlockHeight { get; set; }
		public long BlockWeight { get; set; }
		public long MempoolTransactionsWeight { get; set; }
	}
}
