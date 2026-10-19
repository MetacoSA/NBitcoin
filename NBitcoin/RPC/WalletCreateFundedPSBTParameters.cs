#nullable enable
using System.Collections.Generic;

namespace NBitcoin.RPC
{
	public class WalletCreateFundedPSBTParameters
	{
		/// <summary>Inputs to include, or null to let the wallet select all inputs.</summary>
		public TxIn[]? Inputs { get; set; }
		/// <summary>Payment outputs keyed by destination.</summary>
		public Dictionary<BitcoinAddress, Money>? Outputs { get; set; }
		/// <summary>Data to include in an OP_RETURN output.</summary>
		public byte[]? Data { get; set; }
		/// <summary>Transaction locktime, or null to use Bitcoin Core's default.</summary>
		public LockTime? LockTime { get; set; }
		/// <summary>Transaction funding options, or null to use Bitcoin Core's defaults.</summary>
		public FundRawTransactionOptions? Options { get; set; }
		/// <summary>Whether to include BIP32 derivation paths, or null to use Bitcoin Core's default.</summary>
		public bool? IncludeBIP32Derivations { get; set; }
		/// <summary>PSBT version to create, or null to use Bitcoin Core's default.</summary>
		public PSBTVersion? PSBTVersion { get; set; }
	}
}
#nullable restore
