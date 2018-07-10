namespace NBitcoin
{
	public class MemPoolInfo
	{
		public int Size { get; set; }

		public int Bytes { get; set; }

		public int Usage { get; set; }

		public double MaxMemPool { get; set; }

		public double MemPoolMinFee { get; set; }

		public double MinRelayTxFee { get; set; }

		public MemPoolInfo() { }
	}
}
