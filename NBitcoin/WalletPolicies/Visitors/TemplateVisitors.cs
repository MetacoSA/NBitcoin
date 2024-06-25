#if !NO_RECORDS
#nullable enable
using System.Collections.Generic;
using static NBitcoin.WalletPolicies.MiniscriptNode;

namespace NBitcoin.WalletPolicies.Visitors;

internal class ExtractTemplateVisitor : MiniscriptRewriterVisitor
{
	int index = 0;
	public List<HDKeyNode> HDKeys { get; } = new();
	public override MiniscriptNode Visit(MiniscriptNode node)
	{
		if (node is MultipathNode { Target: HDKeyNode { } hdKey } mpi)
		{
			HDKeys.Add(hdKey);
			var target = new Parameter("@" + index++, new ParameterRequirement.HDKey());
			return mpi with { Target = target };
		}
		return base.Visit(node);
	}
}
internal class FillTemplateVisitor(HDKeyNode[] Keys) : MiniscriptRewriterVisitor
{
	public override MiniscriptNode Visit(MiniscriptNode node)
	{
		if (node is MultipathNode { Target: Parameter { } p } mpi)
		{
			var index = int.Parse(p.Name[1..].ToString());
			return mpi with { Target = Keys[index] };
		}
		return base.Visit(node);
	}
}
#endif
