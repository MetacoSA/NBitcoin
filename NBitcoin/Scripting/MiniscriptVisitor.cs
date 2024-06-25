#if !NO_RECORDS
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NBitcoin.Scripting.MiniscriptNode;

namespace NBitcoin.Scripting
{
	public class MiniscriptVisitor
	{
		public MiniscriptVisitor()
		{
		}

		public virtual void Visit(MiniscriptNode node)
		{
			if (node is Fragment p)
			{
				foreach (var param in p.Parameters)
				{
					Visit(param);
				}
			}
		}
	}
}
#endif
