using System;
using System.Linq;
using NBitcoin.Crypto;

namespace NBitcoin.Altcoins
{
  	public class BPrivateTransaction : ForkIdTransaction
  	{
		public BPrivateTransaction(ConsensusFactory consensusFactory) : base(42, false, consensusFactory)
		{
			// BTCP is a fork of Zclassic, FORKID = 42
			// No Segwit (For now)
		}

        protected override bool UsesForkId(SigHash nHashType)
		{
			return true;
		}
  	}
}