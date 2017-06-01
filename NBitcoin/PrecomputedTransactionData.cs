namespace NBitcoin
{
	public class PrecomputedTransactionData
	{
		public PrecomputedTransactionData(Transaction tx)
		{
			HashOutputs = Script.GetHashOutputs(tx);
			HashSequence = Script.GetHashSequence(tx);
			HashPrevouts = Script.GetHashPrevouts(tx);
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
