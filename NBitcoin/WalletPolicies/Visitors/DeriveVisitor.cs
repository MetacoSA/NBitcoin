#if !NO_RECORDS
#nullable enable
using NBitcoin.DataEncoders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NBitcoin.WalletPolicies.MiniscriptNode;

namespace NBitcoin.WalletPolicies.Visitors;

internal class DeriveVisitor(AddressIntent Intent, int[] Indexes, DerivationCache DerivationCache, KeyType KeyType) : MiniscriptRewriterVisitor
{
	Dictionary<MiniscriptNode.MultipathNode, BitcoinExtPubKey[]> _Replacements = new();
	int idx = -1;

	public DerivationResult[] Derive(MiniscriptNode node, Network network)
	{
		DerivationResult[] result = new DerivationResult[Indexes.Length];
		Parallel.For(0, Indexes.Length, i =>
		{
			var visitor = new DeriveVisitor(Intent, Indexes, DerivationCache, KeyType)
			{
				idx = Indexes[i]
			};
			var miniscript = new Miniscript(visitor.Visit(node), network, KeyType);
			result[i] = new DerivationResult(miniscript, visitor._Derivations);
		});
		return result;
	}

	internal static readonly byte[] BIP0328CC = Encoders.Hex.DecodeData("868087ca02a6f974c4598924c36b57762d32cb45717167e300622c7167e38965");
	Stack<TaprootBranchNode> _TaprootBranches = new();
	TaprootBranchNode? TaprootBranch => _TaprootBranches.Count > 0 ? _TaprootBranches.Peek() : null;
	public override MiniscriptNode Visit(MiniscriptNode node)
	{
		if (node is TaprootBranchNode b)
		{
			_TaprootBranches.Push(b);
			try
			{
				return base.Visit(b);
			}
			finally
			{
				_TaprootBranches.Pop();
			}
		}
		if (node is MultipathNode { Target: MusigNode })
		{
			var wasNestedMusig = _nestedMusig;
			_nestedMusig = true;
			try
			{
				node = base.Visit(node);
			}
			finally
			{
				_nestedMusig = wasNestedMusig;
			}
		}
		else
		{
			node = base.Visit(node);
		}
		if (node is MiniscriptNode.MultipathNode mki && mki.CanDerive(Intent))
		{
			if (mki.Target is HDKeyNode xpub)
			{
				var value = GetPublicKey(mki, xpub.Key, xpub);
				_Derivations.Add(new(value.KeyPath, value.Pubkey, TaprootBranch, xpub));
				node = value.Pubkey;
			}
			else if (mki.Target is MusigNode musig)
			{
				var aggregatePk = musig.GetAggregatePubKey();
				var aggregatePkExt = new ExtPubKey(aggregatePk, BIP0328CC);
				node = GetPublicKey(mki, aggregatePkExt).Pubkey;
			}
		}
		return node;
	}

	private (KeyPath KeyPath, Value Pubkey) GetPublicKey(MiniscriptNode.MultipathNode mki, IHDKey k, HDKeyNode? source = null)
	{
		var type = mki.GetTypeIndex(Intent);
		k = DeriveIntent(k, type) ?? throw new InvalidOperationException($"Unable to derive the key for {type}");
		k = k.Derive((uint)idx) ?? throw new InvalidOperationException($"Unable to derive the key for {type}:{idx}");
		var keyType = _nestedMusig ? KeyType.Classic : KeyType;
		return (
			new KeyPath([(uint)type, (uint)idx]),
			keyType switch
			{
				KeyType.Taproot => MiniscriptNode.Create(k.GetPublicKey().TaprootPubKey),
				_ => MiniscriptNode.Create(k.GetPublicKey())
			});
	}
	List<Derivation> _Derivations = new();

	public bool _nestedMusig = false;

	private IHDKey? DeriveIntent(IHDKey k, int typeIndex)
	{
		// When we derive 0/1/*, "0/1" is common to multiple derivations, so we cache it
		return DerivationCache.GetOrAdd((k, typeIndex), new Lazy<IHDKey?>(() => k.Derive((uint)typeIndex))).Value;
	}
}
#endif
