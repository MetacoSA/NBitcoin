namespace NBitcoin.RPC
{
	public enum FeeRateEstimator
	{
		/// <summary>Use the lower of the block-policy and mempool-policy estimates when both are available.</summary>
		Auto,
		/// <summary>Use the historical block confirmation estimator.</summary>
		BlockPolicy,
		/// <summary>Use the estimator based on the current mempool and recently mined blocks.</summary>
		MempoolPolicy
	}

	public class EstimateSmartFeeOptions
	{
		/// <summary>Selects the Bitcoin Core fee-rate estimator. The node defaults to <see cref="NBitcoin.RPC.FeeRateEstimator.Auto"/>.</summary>
		public FeeRateEstimator? FeeRateEstimator { get; set; }
		/// <summary>Verbosity 1 returns the estimate or errors; verbosity 2 also returns mempool health statistics.</summary>
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
		/// <summary>The estimator selected by Bitcoin Core when automatic selection was requested.</summary>
		public FeeRateEstimator? Estimator { get; set; }
		/// <summary>Errors reported when an estimate could not be produced.</summary>
		public string[] Errors { get; set; }
		/// <summary>Recent-block statistics returned when verbosity is at least 2.</summary>
		public MempoolHealthStatistics[] MempoolHealthStatistics { get; set; }
	}

	public class MempoolHealthStatistics
	{
		public int BlockHeight { get; set; }
		/// <summary>Total weight of non-coinbase transactions in the block.</summary>
		public long BlockWeight { get; set; }
		/// <summary>Total weight of transactions removed from the mempool for this block.</summary>
		public long MempoolTransactionsWeight { get; set; }
	}
}
