#if !NO_RECORDS
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NBitcoin.WalletPolicies.MiniscriptNode;

namespace NBitcoin.WalletPolicies
{
	public class MiniscriptVisitor
	{
		public virtual void Visit(MiniscriptNode node)
		{
			if (node is Fragment p)
			{
				foreach (var param in p.Parameters)
				{
					Visit(param);
				}
			}
			else if (node is TaprootBranchNode tb)
			{
				Visit(tb.Left);
				Visit(tb.Right);
			}
			else if (node is MultipathNode n)
			{
				Visit(n.Target);
			}
		}
	}
}
#endif
