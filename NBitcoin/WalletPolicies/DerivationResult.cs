#if !NO_RECORDS
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NBitcoin.WalletPolicies.MiniscriptNode;

namespace NBitcoin.WalletPolicies;

public class DerivationResult
{
	internal DerivationResult(Miniscript miniscript, List<Derivation> derivations)
	{
		Miniscript = miniscript;
		DerivedKeys = derivations;
	}

	public Miniscript Miniscript { get; }
	/// <summary>
	/// The derived keys. The values are either <see cref="Value.PubKeyValue"/> or a <see cref="Value.TaprootPubKeyValue"/>.
	/// </summary>
	public List<Derivation> DerivedKeys { get; }
}

/// <summary>
/// Represents a derivation that has been made from a <see cref="HDKeyNode"/>.
/// </summary>
/// <param name="KeyPath">The derived key path.</param>
/// <param name="Pubkey">The derived public key (either <see cref="Value.PubKeyValue"/> or <see cref="Value.TaprootPubKeyValue"/>).</param>
/// <param name="TaprootBranch">The taproot branch in which this has been derived, if any.</param>
/// <param name="Source">The source node that generated this key.</param>
public record Derivation(
	KeyPath KeyPath,
	Value Pubkey,
	TaprootBranchNode? TaprootBranch,
	HDKeyNode Source);
#endif
