#if !NO_RECORDS
#nullable enable
using NBitcoin.Scripting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NBitcoin.WalletPolicies.MiniscriptNode;

namespace NBitcoin.WalletPolicies
{
	/// <summary>
	/// Use this class to parse a wallet policy as from documented by <a href="https://github.com/bitcoin/bips/blob/master/bip-0388.mediawiki">BIP0388</a>
	/// </summary>
	public class WalletPolicy
	{
		/// <summary>
		/// The full descriptor with all the multi path hdkeys
		/// </summary>
		public Miniscript FullDescriptor { get; }
		/// <summary>
		/// The descriptor with the keys replaced by placeholder, easier to read.
		/// </summary>
		public Miniscript DescriptorTemplate { get; }
		/// <summary>
		/// The HD Keys used in the policy
		/// </summary>
		public HDKeyNode[] KeyInformationVector { get; }

		public WalletPolicy(Miniscript miniscript)
		{
			ArgumentNullException.ThrowIfNull(miniscript);
			if (miniscript.Parameters.Count != 0)
				throw new ArgumentException("Policy should not have parameters", paramName: nameof(miniscript));
			this.DescriptorTemplate = miniscript.ReplaceHDKeysByKeyPlaceholders(out var keys);
			if (!IsBIP388(miniscript))
				throw new ArgumentException("A policy should either be wsh, sh, pkh, tr or wpkh as top level node", paramName: nameof(miniscript));
			this.KeyInformationVector = keys;
			this.FullDescriptor = miniscript;
		}

		public static WalletPolicy Parse(string str, Network network)
		{
			var miniscript = Miniscript.Parse(str, new MiniscriptParsingSettings(network) { Dialect = MiniscriptDialect.BIP388, AllowedParameters = ParameterTypeFlags.None });
			return new WalletPolicy(miniscript);
		}

		public static bool TryParse(string str, Network network,[MaybeNullWhen(false)] out WalletPolicy policy)
		{
			policy = null;
			if (!Miniscript.TryParse(str, new MiniscriptParsingSettings(network) { Dialect = MiniscriptDialect.BIP388, AllowedParameters = ParameterTypeFlags.None }, out var miniscript))
				return false;
			policy = new WalletPolicy(miniscript);
			return true;
		}

		private static bool IsBIP388(Miniscript miniscript)
		=> miniscript.RootNode switch
		{
			Fragment f when f.Descriptor == FragmentDescriptor.wsh => true,
			Fragment f when f.Descriptor == FragmentDescriptor.sh => true,
			Fragment f when f.Descriptor == FragmentDescriptor.pkh => true,
			Fragment f when f.Descriptor == FragmentDescriptor.tr => true,
			Fragment f when f.Descriptor == FragmentDescriptor.wpkh => true,
			_ => false
		};

		public override string ToString() => ToString(false);
		public string ToString(bool checksum) => this.FullDescriptor.ToString(checksum);
	}
}
#endif
