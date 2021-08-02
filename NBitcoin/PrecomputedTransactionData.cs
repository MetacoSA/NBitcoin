#nullable enable
using System;
using System.Linq;

namespace NBitcoin
{
	/// <summary>
	/// A data structure precomputing some hash values that are needed for all inputs to be signed in the transaction.
	/// </summary>
	public class PrecomputedTransactionData
	{
		public PrecomputedTransactionData(Transaction tx)
		{
			if (tx == null)
				throw new ArgumentNullException(nameof(tx));
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


	public class TaprootReadyPrecomputedTransactionData : PrecomputedTransactionData
	{
		public TaprootReadyPrecomputedTransactionData(Transaction tx, TxOut[] spentOutputs) : base(tx)
		{
			SpentOutputs = spentOutputs;
			HashOutputsSingle = tx.GetHashOutputs(HashVersion.Taproot);
			HashSequenceSingle = tx.GetHashSequence(HashVersion.Taproot);
			HashPrevoutsSingle = tx.GetHashPrevouts(HashVersion.Taproot);
			HashAmountsSingle = tx.GetHashAmounts(HashVersion.Taproot, spentOutputs);
			HashScriptsSingle = tx.GetHashScripts(HashVersion.Taproot, spentOutputs);
		}

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
		public TxOut[] SpentOutputs { get; }
	}
}
