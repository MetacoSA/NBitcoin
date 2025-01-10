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

internal class DeriveVisitor(AddressIntent Intent, int[] Indexes, KeyType KeyType) : MiniscriptRewriterVisitor
{
	Dictionary<MiniscriptNode.MultipathNode, BitcoinExtPubKey[]> _Replacements = new();
	int idx = -1;

	public DerivationResult[] Derive(MiniscriptNode node, Network network)
	{
		DerivationResult[] result = new DerivationResult[Indexes.Length];
		Parallel.For(0, Indexes.Length, i =>
		{
			var visitor = new DeriveVisitor(Intent, Indexes, KeyType)
			{
				idx = Indexes[i],
				_DerivedCache = _DerivedCache,
			};
			var miniscript = new Miniscript(visitor.Visit(node), network, KeyType);
			result[i] = new DerivationResult(miniscript, visitor._Derivations);
		});
		return result;
	}

	internal static readonly byte[] BIP0328CC = Encoders.Hex.DecodeData("868087ca02a6f974c4598924c36b57762d32cb45717167e300622c7167e38965");
	public override MiniscriptNode Visit(MiniscriptNode node)
	{
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
			if (mki.Target is HDKeyNode { Key: var pk } xpub)
			{
				var value = GetPublicKey(mki, pk);
				_Derivations.TryAdd(xpub, value);
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

	private Derivation GetPublicKey(MiniscriptNode.MultipathNode mki, IHDKey k)
	{
		var type = mki.GetTypeIndex(Intent);
		k = DeriveIntent(k, type);
		k = k.Derive((uint)idx);
		var keyType = _nestedMusig ? KeyType.Classic : KeyType;
		return new Derivation(new KeyPath([(uint)type, (uint)idx]), keyType switch
		{
			KeyType.Taproot => MiniscriptNode.Create(k.GetPublicKey().TaprootPubKey),
			_ => MiniscriptNode.Create(k.GetPublicKey())
		});
	}
	Dictionary<HDKeyNode, Derivation> _Derivations = new();
	ConcurrentDictionary<(IHDKey, int), Lazy<IHDKey>> _DerivedCache = new();

	public bool _nestedMusig = false;

	private IHDKey DeriveIntent(IHDKey k, int typeIndex)
	{
		// When we derive 0/1/*, "0/1" is common to multiple derivations, so we cache it
		return _DerivedCache.GetOrAdd((k, typeIndex), new Lazy<IHDKey>(() => k.Derive((uint)typeIndex))).Value;
	}
}
#endif
