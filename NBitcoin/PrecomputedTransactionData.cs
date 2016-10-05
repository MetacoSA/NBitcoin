using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
