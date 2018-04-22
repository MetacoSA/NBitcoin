namespace NBitcoin
{
	public class PrecomputedTransactionData
	{
		public PrecomputedTransactionData(Transaction tx)
		{
			HashOutputs = tx.GetHashOutputs();
			HashSequence = tx.GetHashSequence();
			HashPrevouts = tx.GetHashPrevouts();
		}
		public uint256 HashPrevouts
		{
			get;
			set;
		}
		public uint256 HashSequence
		{
			get;
			set;
		}
		public uint256 HashOutputs
		{
			get;
			set;
		}
	}
}
