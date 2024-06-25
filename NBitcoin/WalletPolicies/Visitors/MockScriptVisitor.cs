#if !NO_RECORDS
#nullable enable

using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Text;
using static NBitcoin.WalletPolicies.MiniscriptNode;

namespace NBitcoin.WalletPolicies.Visitors;

internal class MockScriptVisitor(Network Network, KeyType KeyType) : MiniscriptRewriterVisitor
{
	List<Op> ops = new();
	List<(string Old, string New)> replacements = new();
	internal string GenerateScript(MiniscriptNode rootNode)
	{
		// We create a fragment with dummy values to generate the script
		var frag = (Fragment)Visit(rootNode);
		var script = frag.GetScript();
		var sb = new StringBuilder(script.ToString());
		// Then we replace the dummy values by the user-friendly parameter names
		foreach (var replacement in replacements)
		{
			sb.Replace(replacement.Old, replacement.New);
		}
		return sb.ToString();
	}

	public override MiniscriptNode Visit(MiniscriptNode node)
	{
		return node switch
		{
			MultipathNode { Target: Parameter p } => CreateDummy(p, node.ToString()),
			Parameter p => CreateDummy(p),
			_ => base.Visit(node)
		};
	}

	private MiniscriptNode CreateDummy(Parameter p, string? paramName = null)
	{
		MiniscriptNode modifiedParameter;
		if (p.Requirement is MiniscriptNode.ParameterRequirement.Key or MiniscriptNode.ParameterRequirement.HDKey)
		{
			paramName ??= p.Name;
			var pk = new Key().PubKey;
			if (KeyType is KeyType.Taproot)
			{
				var pkk = pk.TaprootPubKey;
				replacements.Add((pkk.ToString(), $"<{paramName}>"));
				modifiedParameter = new Value.TaprootPubKeyValue(pkk);
			}
			else
			{
				replacements.Add((pk.ToString(), $"<{paramName}>"));
				replacements.Add((pk.Hash.ToString(), $"<HASH160({paramName})>"));
				modifiedParameter = new Value.PubKeyValue(pk);
			}
		}
		else if (p.Requirement is MiniscriptNode.ParameterRequirement.Hash h)
		{
			var bytes = RandomUtils.GetBytes(h.RequiredBytes);
			replacements.Add((Encoders.Hex.EncodeData(bytes), $"<{p.Name}>"));
			modifiedParameter = new Value.HashValue(bytes);
		}
		else if (p.Requirement is MiniscriptNode.ParameterRequirement.Locktime l)
		{
			var rand = new LockTime(RandomUtils.GetUInt32());
			replacements.Add((Encoders.Hex.EncodeData(Op.GetPushOp(rand.Value).PushData), $"<{p.Name}>"));
			modifiedParameter = new Value.LockTimeValue(rand);
		}
		else if (p.Requirement is MiniscriptNode.ParameterRequirement.Fragment)
		{
			var pkStr = new Key().PubKey.ToString();
			var pk = $"pk_k({pkStr})";
			replacements.Add((pkStr, $"<{p.Name}>"));
			modifiedParameter = Miniscript.Parse(pk, new MiniscriptParsingSettings(Network, KeyType)).RootNode;
		}
		else
			throw new InvalidOperationException($"Unable to generate the script's string with a requirement of type {p.Requirement.GetType().Name}. (This shouldn't happen)");
		return modifiedParameter;
	}
}
#endif
