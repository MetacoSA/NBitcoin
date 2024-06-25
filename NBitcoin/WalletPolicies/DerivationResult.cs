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
	internal DerivationResult(Miniscript miniscript, Dictionary<HDKeyNode, Derivation> derivations)
	{
		Miniscript = miniscript;
		DerivedKeys = derivations;
	}

	public Miniscript Miniscript { get; }
	/// <summary>
	/// The derived keys. The values are either <see cref="Value.PubKeyValue"/> or a <see cref="Value.TaprootPubKeyValue"/>.
	/// </summary>
	public Dictionary<HDKeyNode, Derivation> DerivedKeys { get; }
}

public class Derivation
{
	public Derivation(KeyPath keyPath, Value pubkey)
	{
		KeyPath = keyPath;
		Pubkey = pubkey;
	}
	public KeyPath KeyPath { get; }
	/// <summary>
	/// The derived key. This is either a <see cref="Value.PubKeyValue"/> or a <see cref="Value.TaprootPubKeyValue"/>.
	/// </summary>
	public Value Pubkey { get; }
}
#endif
