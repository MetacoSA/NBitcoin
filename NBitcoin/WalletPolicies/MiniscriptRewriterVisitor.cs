#if !NO_RECORDS
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NBitcoin.WalletPolicies.MiniscriptNode;

namespace NBitcoin.WalletPolicies
{
	public class MiniscriptRewriterVisitor
	{
		public virtual MiniscriptNode Visit(MiniscriptNode node)
		{
			return node switch
			{
				TaprootBranchNode t => t with { Left = Visit(t.Left), Right = Visit(t.Right) },
				TaprootNode t => t with { InternalKeyNode = Visit(t.InternalKeyNode), ScriptTreeRootNode = t.ScriptTreeRootNode is null ? null : Visit(t.ScriptTreeRootNode) },
				MusigNode f => new MusigNode(f.IsNested, f.Parameters.Select(Visit).ToArray()),
				Wrapper w => w with { X = Visit(w.X) },
				FragmentSingleParameter f => f with { X = Visit(f.X) },
				FragmentTwoParameters f => f with { X = Visit(f.X), Y = Visit(f.Y) },
				FragmentThreeParameters f => f with { X = Visit(f.X), Y = Visit(f.Y), Z = Visit(f.Z) },
				FragmentUnboundedParameters f => new FragmentUnboundedParameters(f.Descriptor, f.Parameters.Select(Visit).ToArray()),
				MultipathNode n => n with { Target = Visit(n.Target) },
				_ => node
			};
		}
	}
}
#endif
