#if !NO_RECORDS
#nullable enable
using NBitcoin.Crypto;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using static NBitcoin.WalletPolicies.MiniscriptNode;
using static NBitcoin.WalletPolicies.MiniscriptNode.Value;
using NBitcoin.DataEncoders;
using Microsoft.VisualBasic;
using static NBitcoin.WalletPolicies.Miniscript;

namespace NBitcoin.WalletPolicies
{
	internal class RemoveKeyInformation : MiniscriptRewriterVisitor
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
	internal class AddKeyInformation(HDKeyNode[] Keys) : MiniscriptRewriterVisitor
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

	internal class GetMerkleRootVisitor
	{
		TaprootBuilder taprootBuilder = new TaprootBuilder();
		public static uint256? GetMerkleRoot(TaprootNode node)
		{
			if (node.ScriptTreeRootNode is null)
				return null;
			return new GetMerkleRootVisitor().Visit(node.ScriptTreeRootNode).Hash;
		}

		public TaprootNodeInfo Visit(MiniscriptNode node)
		=> node switch
		{
			TaprootBranchNode tbn => Visit(tbn.Left) + Visit(tbn.Right),
			_ => TaprootNodeInfo.NewLeaf(new TapScript(node.GetScript(), TapLeafVersion.C0))
		};
	}

	class GenerateScriptVisitor : MiniscriptVisitor
	{
		internal Stack<List<Op>> ops = new();
		public override void Visit(MiniscriptNode node)
		{
			if (node is TaprootNode trn)
			{
				this.Visit(trn.InternalKeyNode);
				var merkleRoot = GetMerkleRootVisitor.GetMerkleRoot(trn);
				if (merkleRoot is not null)
				{
					ops.Push(new List<Op> { Op.GetPushOp(merkleRoot.ToBytes()) });
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
				if (node is Fragment f)
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
	internal class ReadableScriptVisitor(Network Network, KeyType KeyType) : MiniscriptRewriterVisitor
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
	internal class DeriveVisitor(AddressIntent Intent, int[] Indexes, KeyType KeyType) : MiniscriptRewriterVisitor
	{
		Dictionary<(PubKey, int), MultipathNode> _Derivations = new();
		Dictionary<(PubKey, int), BitcoinExtPubKey[]> _DerivedKeys = new();
		int pass;
		int idx;

		public MiniscriptNode[] Derive(MiniscriptNode node)
		{
			MiniscriptNode[] result = new MiniscriptNode[Indexes.Length];
			pass = 0;
			this.Visit(node);
			pass = 1;
			this.Visit(node);
			pass = 2;
			for (int i = 0; i < Indexes.Length; i++)
			{
				idx = i;
				result[i] = this.Visit(node);
			}
			return result;
		}

		public override MiniscriptNode Visit(MiniscriptNode node)
		{
			if (node is MiniscriptNode.MultipathNode { Target: HDKeyNode hdKeyNode } mki && mki.CanDerive(Intent))
			{
				int typeIdx = mki.GetTypeIndex(Intent);
				var derivationPath = (hdKeyNode.PubKey.GetPublicKey(), typeIdx);
				// First pass is noting what need to be derived
				if (pass is 0)
				{
					_Derivations.TryAdd(derivationPath, mki);
				}
				// Second pass derives everything in batch
				else if (pass is 1 && !_DerivedKeys.ContainsKey(derivationPath))
				{
					var multipathKeyExpression = _Derivations[derivationPath];
					var keys = mki.Derive(Intent, Indexes);
					_DerivedKeys.Add(derivationPath, keys);
				}
				// Third pass replace the Multipath key expressions by the derived keys
				else if (pass is 2)
				{
					var pubkey = _DerivedKeys[derivationPath][idx].GetPublicKey();
					if (KeyType == KeyType.Taproot)
						return MiniscriptNode.Create(pubkey.TaprootPubKey);
					else
						return MiniscriptNode.Create(pubkey);
				}
			}
			return base.Visit(node);
		}
	}

	internal class MiniscriptParametersVisitor : MiniscriptVisitor
	{
		internal static bool TryCreateParameters(MiniscriptNode node, [MaybeNullWhen(true)] out MiniscriptError error, [MaybeNullWhen(false)] out IReadOnlyDictionary<string, IReadOnlyCollection<Parameter>> parameters)
		{
			error = null;
			parameters = null;
			var visitor = new MiniscriptParametersVisitor();
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

	public class MiniscriptReplacementException : Exception
	{
		public MiniscriptReplacementException(string parameterName, MiniscriptNode.ParameterRequirement requirement)
			: base($"The parameter {parameterName} doesn't fit the requirement ({requirement})")
		{
			ParameterName = parameterName;
			Requirement = requirement;
		}
		public string ParameterName { get; }
		public MiniscriptNode.ParameterRequirement Requirement { get; }
	}
	internal class MiniscriptParameterReplacementVisitor : MiniscriptRewriterVisitor
	{
		private readonly Dictionary<string, MiniscriptNode> _Parameters;

		public MiniscriptParameterReplacementVisitor(Dictionary<string, MiniscriptNode> parameters)
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
}
#endif
