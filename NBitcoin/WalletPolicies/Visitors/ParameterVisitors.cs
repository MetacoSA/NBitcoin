#if !NO_RECORDS
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static NBitcoin.WalletPolicies.MiniscriptNode;

namespace NBitcoin.WalletPolicies.Visitors;
internal class ParametersVisitor : MiniscriptVisitor
{
	internal static bool TryCreateParameters(MiniscriptNode node, [MaybeNullWhen(true)] out MiniscriptError error, [MaybeNullWhen(false)] out IReadOnlyDictionary<string, IReadOnlyCollection<Parameter>> parameters)
	{
		error = null;
		parameters = null;
		var visitor = new ParametersVisitor();
		visitor.Visit(node);
		foreach (var kv in visitor.Parameters)
		{
			if (kv.Value.ToHashSet().Count != 1)
			{
				error = new MiniscriptError.MixedParameterType(kv.Key);
				return false;
			}
		}
		parameters = visitor.Parameters.ToDictionary(kv => kv.Key, kv => (IReadOnlyCollection<Parameter>)kv.Value);
		return true;
	}
	public Dictionary<string, List<Parameter>> Parameters { get; } = new();
	public override void Visit(MiniscriptNode node)
	{
		if (node is Parameter p)
		{
			if (!Parameters.TryGetValue(p.Name, out var list))
			{
				list = new List<Parameter>();
				Parameters.Add(p.Name, list);
			}
			list.Add(p);
		}
		base.Visit(node);
	}
}

internal class ParameterReplacementVisitor : MiniscriptRewriterVisitor
{
	private readonly Dictionary<string, MiniscriptNode> _Parameters;

	public ParameterReplacementVisitor(Dictionary<string, MiniscriptNode> parameters)
	{
		_Parameters = parameters;
	}

	public bool SkipRequirements { get; set; }

	public override MiniscriptNode Visit(MiniscriptNode node)
	{
		if (node is MiniscriptNode.Parameter p)
		{
			if (_Parameters.TryGetValue(p.Name, out var replacement))
			{
				if (!SkipRequirements && !p.Requirement.Check(replacement))
					throw new MiniscriptReplacementException(p.Name, p.Requirement);
				return replacement;
			}
		}
		return base.Visit(node);
	}
}
#endif
