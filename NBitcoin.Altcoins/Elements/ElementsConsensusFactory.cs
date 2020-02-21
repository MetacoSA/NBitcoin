#nullable enable
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
			var builder = new ElementsTransactionBuilder<TNetwork>(network);
			builder.StandardTransactionPolicy.Strategy = new StandardElementsTransactionPolicyStrategy();
			builder.CoinSelector = new ElementsCoinSelector<TNetwork>();
			return builder;
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
