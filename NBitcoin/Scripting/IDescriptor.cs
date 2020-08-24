namespace NBitcoin.Scripting
{
    // Interface for parsed descriptor objects.
    //
    // Descriptors are strings that describe a set of scriptPubKeys, together with
    // all information necessary to solve them. By combining all information into
    // one, they avoid the need to separately import keys and scripts.
    //
    // Descriptors may be ranged, which occurs when the public keys inside are
    // specified in the form of HD chains (xpubs).
    //
    // Descriptors always represent public information - public keys and scripts -
    // but in cases where private keys need to be conveyed along with a descriptor,
    // they can be included inside by changing public keys to private keys (WIF
    // format), and changing xpubs by xprvs.
    //
    // Reference documentation about the descriptor language can be found in
    // doc/descriptors.md.
    ///
    public interface IDescriptor
	{
		// Whether the expansion of this descriptor depends on the position.
		bool IsRange { get; }

		// Whether this descriptor has all information about signing ignoring lack of private keys.
		//  This is true for all descriptors except ones that use `raw` or `addr` constructions.
		bool IsSolvable { get; }

		// Whether this descriptor will return one scriptPubKey or multiple (aka is or is not combo)
		bool IsSingleType { get; }

		// Convert the descriptor to a private string. This fails if the provided provider does not have the relevant private keys.
		bool ToPrivateString(SigningProvider provider, out string o);

		// Expand a descriptor at a specified position.
		//
		// @param[in] pos The position at which to expand the descriptor. If IsRange() is false, this is ignored.
		// @param[in] provider The provider to query for private keys in case of hardened derivation.
		// @param[out] output_scripts The expanded scriptPubKeys.
		// @param[out] out Scripts and public keys necessary for solving the expanded scriptPubKeys (may be equal to `provider`).
		// @param[out] write_cache Cache data necessary to evaluate the descriptor at this point without access to private keys.
		///
		bool Expand(int pos, SigningProvider provider, Script output_scripts, FlatSigningProvider output, DescriptorCache* write_cache = nullptr);

		// Expand a descriptor at a specified position using cached expansion data.
		//
		// @param[in] pos The position at which to expand the descriptor. If IsRange() is false, this is ignored.
		// @param[in] read_cache Cached expansion data.
		// @param[out] output_scripts The expanded scriptPubKeys.
		// @param[out] out Scripts and public keys necessary for solving the expanded scriptPubKeys (may be equal to `provider`).
		///
		bool ExpandFromCache(int pos, DescriptorCache read_cache, Script output_scripts, FlatSigningProvider output);

		// Expand the private key for a descriptor at a specified position, if possible.
		//
		// @param[in] pos The position at which to expand the descriptor. If IsRange() is false, this is ignored.
		// @param[in] provider The provider to query for the private keys.
		// @param[out] out Any private keys available for the specified `pos`.
		///
		void ExpandPrivate(int pos, SigningProvider provider, FlatSigningProvider output);

		// @return The OutputType of the scriptPubKey(s) produced by this descriptor. Or nullopt if indeterminate (multiple or none)
		OutputType? GetOutputType();
	}
}