#if !NO_RECORDS
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NBitcoin.Scripting.MiniscriptNode;

namespace NBitcoin.Scripting
{
	public class MiniscriptRewriterVisitor
	{
		public virtual MiniscriptNode Visit(MiniscriptNode node)
		{
			return node switch
			{
				Wrapper w => new Wrapper(w.Descriptor, Visit(w.X)),
				FragmentSingleParameter f => new FragmentSingleParameter(f.Descriptor, Visit(f.X)),
				FragmentTwoParameters f => new FragmentTwoParameters(f.Descriptor, Visit(f.X), Visit(f.Y)),
				FragmentThreeParameters f => new FragmentThreeParameters(f.Descriptor, Visit(f.X), Visit(f.Y), Visit(f.Z)),
				FragmentUnboundedParameters f => new FragmentUnboundedParameters(f.Descriptor, f.Parameters.Select(Visit).ToArray()),
				_ => node
			};
		}
	}
}
#endif
