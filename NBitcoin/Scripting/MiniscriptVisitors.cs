#if !NO_RECORDS
#nullable enable
using NBitcoin.Crypto;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using static NBitcoin.Scripting.MiniscriptNode;

namespace NBitcoin.Scripting
{
	internal class RemoveKeyInformation : MiniscriptRewriterVisitor
	{
		public RemoveKeyInformation(bool preferShortForm)
		{
			PreferShortForm = preferShortForm;
		}
		int index = 0;

		public bool PreferShortForm { get; }

		public override MiniscriptNode Visit(MiniscriptNode node)
		{
			if (node is MultiPathKeyInformationNode { KeyInformation : var mpi })
				return MiniscriptNode.Create(new KeyPlaceholder(index++, mpi.DepositIndex, mpi.ChangeIndex, PreferShortForm));
			return base.Visit(node);
		}
	}
	internal class DeriveVisitor : MiniscriptRewriterVisitor
	{
		public DeriveVisitor(AddressIntent intent, int index, KeyType keyType)
		{
			Intent = intent;
			Index = index;
			KeyType = keyType;
		}

		public AddressIntent Intent { get; }
		public int Index { get; }
		public KeyType KeyType { get; }

		public override MiniscriptNode Visit(MiniscriptNode node)
		{
			if (node is MiniscriptNode.MultiPathKeyInformationNode mki)
			{
				var pubkey = mki.KeyInformation.Derive(Intent, Index).ExtPubKey.PubKey;
				if (KeyType == KeyType.Taproot)
					return MiniscriptNode.Create(pubkey.TaprootPubKey);
				else
					return MiniscriptNode.Create(pubkey);
			}
			return base.Visit(node);
		}
	}
	// Check if the nodes aren't using several network type and doesn't conflict
	// with current settings.
	internal class GuessSettingsVisitor : MiniscriptVisitor
	{
		public GuessSettingsVisitor(MiniscriptSettings settings)
		{
			Settings = settings;
		}
		public override void Visit(MiniscriptNode node)
		{
			if (node is MiniscriptNode.KeyInformationNode ki)
				SetNetwork(ki.KeyInformation.PubKey.Network);
			else if (node is MiniscriptNode.MultiPathKeyInformationNode ki2)
				SetNetwork(ki2.KeyInformation.PubKey.Network);

			if (node is MiniscriptNode.Fragment f)
			{
				if (f.Descriptor == FragmentDescriptor.multi)
					SetKeyType(KeyType.Classic);
				if (f.Descriptor == FragmentDescriptor.multi_a)
					SetKeyType(KeyType.Taproot);
			}
			base.Visit(node);
		}
		public MiniscriptError? Error { get; set; }
		private void SetKeyType(KeyType keyType)
		{
			if (Settings.KeyType is null)
				Settings = Settings with { KeyType = keyType };
			else if (Settings.KeyType.Value != keyType)
				Error = new MiniscriptError.MixedKeyTypes();
		}

		public MiniscriptSettings Settings { get; set; }
		private void SetNetwork(Network network)
		{
			if (Settings.Network is null)
				Settings = Settings with { Network = network };
			else if (Settings.Network != network)
				Error = new MiniscriptError.MixedNetworks();
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
					if (p is MiniscriptNode.Parameter.KeyPlaceholderParameter k &&
						replacement is MiniscriptNode.KeyInformationNode ki)
					{
						// Replace @0/<1;2>/* (KeyPlaceholder) by
						// by [aabc/33']xpub/<1;2>/* (MultiPathKeyInformation)
						replacement = MiniscriptNode.Create(ki.KeyInformation.ToMultiPathKeyInformation(k.KeyPlaceholder));
					}
					return replacement;
				}
			}
			return base.Visit(node);
		}
	}

	internal class SatisfactionRequirementVisitor : MiniscriptVisitor
	{
		private SatisfactionPath satPath;
		private int satIndex = 0;
		public List<SatisfactionRequirement> Requirements { get; } = new List<SatisfactionRequirement>();
		public SatisfactionRequirementVisitor(SatisfactionPath satisfactionPath)
		{
			this.satPath = satisfactionPath;
		}

		public override void Visit(MiniscriptNode node)
		{
			if (node is Fragment f)
			{
				if (f.Descriptor.IsHash() &&
				f.Parameters.First() is MiniscriptNode.Value.HashValue hv)
					Requirements.Add(SatisfactionRequirement.Create(hv, f.Descriptor));
				else if (
					(f.Descriptor == FragmentDescriptor.pk_k ||
					 f.Descriptor == FragmentDescriptor.pk_h) &&
					f.Parameters.First() is MiniscriptNode.Value.PubKeyValue kv)
					Requirements.Add(SatisfactionRequirement.CreateSignature(kv, f.Descriptor));
				else if (f.Descriptor == FragmentDescriptor.andor)
				{
					var f3p = (FragmentThreeParameters)f;
					var choice = satPath[satIndex++].Choices[0];
					if (choice == 0)
					{
						VisitParameters(f3p.Y, f3p.X);
					}
					else if (choice == 1)
					{
						VisitParameters(f3p.Z, DSat(f3p.X));
					}
				}
				else if (f.Descriptor == FragmentDescriptor.and_n)
				{
					var f2p = (FragmentTwoParameters)f;
					Visit(MiniscriptNode.Fragment.Create(FragmentDescriptor.andor, f2p.X, f2p.Y, MiniscriptNode.Fragment.Zero()));
				}
				else if (f.Descriptor == FragmentDescriptor.and_v ||
					f.Descriptor == FragmentDescriptor.and_b)
				{
					var f2p = (FragmentTwoParameters)f;
					VisitParameters(f2p.Y, f2p.X);
				}
				else if (f.Descriptor.IsOr())
				{
					var f2p = (FragmentTwoParameters)f;
					var choice = satPath[satIndex++].Choices[0];
					if (f.Descriptor == FragmentDescriptor.or_b)
					{
						if (choice == 0)
							VisitParameters(DSat(f2p.Y), f2p.X);
						else if (choice == 1)
							VisitParameters(f2p.Y, DSat(f2p.X));
						else if (choice == 2)
							VisitParameters(f2p.Y, f2p.X);
					}
				}
			}
		}

		record DSatNode(MiniscriptNode X) : MiniscriptNode;
		private MiniscriptNode DSat(MiniscriptNode x) => new DSatNode(x);

		private void VisitParameters(params MiniscriptNode[] nodes)
		{
			foreach (var n in nodes)
			{
				Visit(n);
			}
		}
	}
}
#endif
