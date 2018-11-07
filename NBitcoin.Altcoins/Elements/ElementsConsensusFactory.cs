using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

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
	}
}
