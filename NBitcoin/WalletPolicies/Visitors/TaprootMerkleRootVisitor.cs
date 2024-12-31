#if !NO_RECORDS
#nullable enable
using static NBitcoin.WalletPolicies.MiniscriptNode;

namespace NBitcoin.WalletPolicies.Visitors;

internal class TaprootMerkleRootVisitor
{
	TaprootBuilder taprootBuilder = new TaprootBuilder();
	public static uint256? GetMerkleRoot(TaprootNode node)
	{
		if (node.ScriptTreeRootNode is null)
			return null;
		return new TaprootMerkleRootVisitor().Visit(node.ScriptTreeRootNode).Hash;
	}

	public TaprootNodeInfo Visit(MiniscriptNode node)
	=> node switch
	{
		TaprootBranchNode tbn => Visit(tbn.Left) + Visit(tbn.Right),
		_ => TaprootNodeInfo.NewLeaf(new TapScript(node.GetScript(), TapLeafVersion.C0))
	};
}
#endif
