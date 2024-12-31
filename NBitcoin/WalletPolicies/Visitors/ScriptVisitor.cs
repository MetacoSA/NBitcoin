#if !NO_RECORDS
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using static NBitcoin.WalletPolicies.MiniscriptNode;

namespace NBitcoin.WalletPolicies.Visitors;

internal class ScriptVisitor : MiniscriptVisitor
{
	internal Stack<List<Op>> ops = new();
	public override void Visit(MiniscriptNode node)
	{
		if (node is TaprootNode trn)
		{
			this.Visit(trn.InternalKeyNode);
			var merkleRoot = TaprootMerkleRootVisitor.GetMerkleRoot(trn);
			if (merkleRoot is not null)
			{
				ops.Push([Op.GetPushOp(merkleRoot.ToBytes())]);
			}
			var parameters = GetParameters(trn.ScriptTreeRootNode is null ? 1 : 2);
			var script = new List<Op>();
			trn.Descriptor.AddOps(parameters, script);
			ops.Push(script);
		}
		else
		{
			var stackSizeBefore = ops.Count;
			base.Visit(node);
			var actualParameterCount = ops.Count - stackSizeBefore;
			if (node is MiniscriptNode.Value v)
			{
				ops.Push([v.CreatePushOp()]);
			}
			else if (node is Fragment f)
			{
				var parameterCount = f.Parameters.Count();
				AssertParameterCount(parameterCount, actualParameterCount, f.Descriptor.Name);
				List<Op>[] parameters = GetParameters(parameterCount);
				var script = new List<Op>();
				f.Descriptor.AddOps(parameters, script);
				ops.Push(script);
			}
		}
	}

	private List<Op>[] GetParameters(int parameterCount)
	{
		var parameters = new List<Op>[parameterCount];
		for (int i = 0; i < parameterCount; i++)
		{
			parameters[i] = ops.Pop();
		}
		Array.Reverse(parameters);
		return parameters;
	}

	private void AssertParameterCount(int expectedParameterCount, int actualParameterCount, string name)
	{
		if (expectedParameterCount != actualParameterCount)
			throw new InvalidOperationException($"Expected {expectedParameterCount} parameters, got {actualParameterCount}. ({name})");
	}
}
#endif
