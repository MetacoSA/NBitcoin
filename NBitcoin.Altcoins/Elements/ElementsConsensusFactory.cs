using System;
using System.Linq;
using System.Reflection;
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
			if (IsTxOut(type))
			{
				result = new ElementsTxOut<TNetwork>();
				return true;
			}
			return base.TryCreateNew(type, out result);
		}
		public override BlockHeader CreateBlockHeader()
		{
			return new ElementsBlockHeader<TNetwork>();
		}
		public override Block CreateBlock()
		{
			return new ElementsBlock<TNetwork>((ElementsBlockHeader<TNetwork>) CreateBlockHeader());
		}

		public override Transaction CreateTransaction()
		{
			return new ElementsTransaction<TNetwork>();
		}

		public override TxOut CreateTxOut()
		{
			return new ElementsTxOut<TNetwork>();
		}

		public override TxIn CreateTxIn()
		{
			return new ElementsTxIn<TNetwork>();
		}

		protected override TransactionBuilder CreateTransactionBuilderCore(Network network)
		{
			var builder = new ElementsTransactionBuilder(network);
			builder.StandardTransactionPolicy.Strategy = new StandardElementsTransactionPolicyStrategy();
			return builder;
		}

		class ElementsTransactionBuilder : TransactionBuilder
		{
			public ElementsTransactionBuilder(Network network): base(network)
			{

			}
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
