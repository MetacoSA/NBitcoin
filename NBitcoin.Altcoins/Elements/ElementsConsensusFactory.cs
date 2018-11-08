using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using NBitcoin.Policy;

namespace NBitcoin.Altcoins.Elements
{
	public class ElementsConsensusFactory<TNetwork> : ConsensusFactory
	{
		public static ElementsConsensusFactory<TNetwork> Instance { get; } = new ElementsConsensusFactory<TNetwork>();

		public override bool TryCreateNew(Type type, out IBitcoinSerializable result)
		{
			if (typeof(TxIn).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
			{
				result = new ElementsTxIn<TNetwork>();
				return true;
			}
			if (typeof(TxOut).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
			{
				result = new ElementsTxOut<TNetwork>();
				return true;
			}
			return base.TryCreateNew(type, out result);
		}
		public override BlockHeader CreateBlockHeader()
		{
			return new ElementsBlockHeader();
		}
		public override Block CreateBlock()
		{
			return new ElementsBlock(new ElementsBlockHeader(), this);
		}

		public override Transaction CreateTransaction()
		{
			return new ElementsTransaction<TNetwork>();
		}

		protected override TransactionBuilder CreateTransactionBuilderCore()
		{
			var builder = new ElementsTransactionBuilder();
			builder.StandardTransactionPolicy.Strategy = new StandardElementsTransactionPolicyStrategy();
			return builder;
		}
#pragma warning disable CS0618 // Type or member is obsolete
		class ElementsTransactionBuilder : TransactionBuilder
		{
			protected override void AfterBuild(Transaction transaction)
			{
				if (transaction.Outputs.OfType<ElementsTxOut>().All(o => !o.IsFee))
				{
					var totalInput =
						this.FindSpentCoins(transaction)
						.Select(c => c.TxOut)
						.OfType<ElementsTxOut>()
						.Where(o => o.IsPeggedAsset == true)
						.Select(c => c.Value)
						.OfType<Money>()
						.Sum();
					var totalOutput =
						transaction.Outputs.OfType<ElementsTxOut>()
						.Where(o => o.IsPeggedAsset == true)
						.Select(o => o.Value)
						.Sum();
					var fee = totalInput - totalOutput;
					if(fee > Money.Zero)
						transaction.Outputs.Add(fee, Script.Empty);
				}
			}
		}
#pragma warning restore CS0618 // Type or member is obsolete
		class StandardElementsTransactionPolicyStrategy : StandardTransactionPolicyStrategy
		{
			public override bool IsStandardOutput(TxOut txout)
			{
				if (txout is ElementsTxOut elTxout && elTxout.IsFee)
					return true;
				return base.IsStandardOutput(txout);
			}
		}
	}
}
