using System;

namespace NBitcoin
{
	public class TaprootHashContext
	{
		public TaprootHashContext(Transaction transaction, TxOut[] spentOutputs)
		{
			if (transaction == null)
				throw new ArgumentNullException(nameof(transaction));
			if (spentOutputs == null)
				throw new ArgumentNullException(nameof(spentOutputs));
			SpentOutputs = spentOutputs;
			HashOutputsSingle = transaction.GetHashOutputs(HashVersion.Taproot);
			HashSequenceSingle = transaction.GetHashSequence(HashVersion.Taproot);
			HashPrevoutsSingle = transaction.GetHashPrevouts(HashVersion.Taproot);
			HashAmountsSingle = transaction.GetHashAmounts(HashVersion.Taproot, spentOutputs);
			HashScriptsSingle = transaction.GetHashScripts(HashVersion.Taproot, spentOutputs);
			SpentOutputs = spentOutputs;
		}

		public uint256 TapleafHash { get; set; }
		public uint CodeseparatorPosition { get; set; } = 0xffffffff;


		public uint256 HashPrevoutsSingle
		{
			get;
			set;
		}
		public uint256 HashSequenceSingle
		{
			get;
			set;
		}
		public uint256 HashOutputsSingle
		{
			get;
			set;
		}
		public uint256 HashAmountsSingle
		{
			get;
			set;
		}
		public uint256 HashScriptsSingle
		{
			get;
			set;
		}
		public TxOut[] SpentOutputs { get; set; }
	}
	public class PrecomputedTransactionData
	{
		public PrecomputedTransactionData(Transaction tx):this(tx, null)
		{

		}
		public PrecomputedTransactionData(Transaction tx, TxOut[] spentOutputs)
		{
			HashOutputs = tx.GetHashOutputs(HashVersion.WitnessV0);
			HashSequence = tx.GetHashSequence(HashVersion.WitnessV0);
			HashPrevouts = tx.GetHashPrevouts(HashVersion.WitnessV0);
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
