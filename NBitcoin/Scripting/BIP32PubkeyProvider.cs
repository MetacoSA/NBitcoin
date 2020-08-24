namespace NBitcoin.Scripting
{
	/** An object representing a parsed extended public key in a descriptor. */
	public class BIP32PubkeyProvider : PubkeyProvider
	{
		// Root xpub, path, and final derivation step type being used, if any
		private ExtPubKey m_root_extkey;
		private KeyPath m_path;
		private DeriveType m_derive;
		// Cache of the parent of the final derived pubkeys.
		// Primarily useful for situations when no read_cache is provided
		private ExtPubKey m_cached_xpub;

		private bool GetExtKey(SigningProvider arg, ExtKey ret)
		{
			Key key;
			if (!arg.GetKey(m_root_extkey.pubkey.GetID(), key)) return false;
			ret.nDepth = m_root_extkey.nDepth;
			std::copy(m_root_extkey.vchFingerprint, m_root_extkey.vchFingerprint + sizeof(ret.vchFingerprint), ret.vchFingerprint);
			ret.nChild = m_root_extkey.nChild;
			ret.chaincode = m_root_extkey.chaincode;
			ret.key = key;
			return true;
		}

		// Derives the last xprv
		private bool GetDerivedExtKey(SigningProvider arg, ExtKey xprv)
		{
			if (!GetExtKey(arg, xprv)) return false;
			foreach (var entry  in  m_path) {
				xprv.Derive(xprv, entry);
			}
			return true;
		}

		private bool IsHardened()
		{
			if (m_derive == DeriveType::HARDENED) return true;
			foreach (var entry  in  m_path) {
				if (entry >> 31) return true;
			}
			return false;
		}

		public BIP32PubkeyProvider(uint exp_index, ExtPubKey extkey, KeyPath path, DeriveType derive) 
			: base(exp_index)
		{
			m_root_extkey = extkey;
			m_path = path;
			m_derive = derive;
		} 
		public bool IsRange()  { return m_derive != DeriveType::NO; }
		public uint GetSize()  { return 33; }
		public bool GetPubKey(int pos, SigningProvider arg, PubKey key_out, KeyOriginInfo final_info_out, DescriptorCache read_cache = null, DescriptorCache write_cache = null) 
		{
			// Info of parent of the to be derived pubkey
			KeyOriginInfo parent_info;
			KeyID keyid = m_root_extkey.pubkey.GetID();
			std::copy(keyid.begin(), keyid.begin() + sizeof(parent_info.fingerprint), parent_info.fingerprint);
			parent_info.path = m_path;

			// Info of the derived key itself which is copied out upon successful completion
			KeyOriginInfo final_info_out_tmp = parent_info;
			if (m_derive == DeriveType::UNHARDENED) final_info_out_tmp.path.push_back((uint32_t)pos);
			if (m_derive == DeriveType::HARDENED) final_info_out_tmp.path.push_back(((uint32_t)pos) | 0x80000000L);

			// Derive keys or fetch them from cache
			ExtPubKey final_extkey = m_root_extkey;
			ExtPubKey parent_extkey = m_root_extkey;
			bool der = true;
			if (read_cache) {
				if (!read_cache.GetCachedDerivedExtPubKey(m_expr_index, pos, final_extkey)) {
					if (m_derive == DeriveType::HARDENED) return false;
					// Try to get the derivation parent
					if (!read_cache.GetCachedParentExtPubKey(m_expr_index, parent_extkey)) return false;
					final_extkey = parent_extkey;
					if (m_derive == DeriveType::UNHARDENED) der = parent_extkey.Derive(final_extkey, pos);
				}
			} else if (m_cached_xpub.pubkey.IsValid() && m_derive != DeriveType::HARDENED) {
				parent_extkey = final_extkey = m_cached_xpub;
				if (m_derive == DeriveType::UNHARDENED) der = parent_extkey.Derive(final_extkey, pos);
			} else if (IsHardened()) {
				ExtKey xprv;
				if (!GetDerivedExtKey(arg, xprv)) return false;
				parent_extkey = xprv.Neuter();
				if (m_derive == DeriveType::UNHARDENED) der = xprv.Derive(xprv, pos);
				if (m_derive == DeriveType::HARDENED) der = xprv.Derive(xprv, pos | 0x80000000UL);
				final_extkey = xprv.Neuter();
			} else {
				foreach (var entry  in  m_path) {
					der = parent_extkey.Derive(parent_extkey, entry);
					assert(der);
				}
				final_extkey = parent_extkey;
				if (m_derive == DeriveType::UNHARDENED) der = parent_extkey.Derive(final_extkey, pos);
				assert(m_derive != DeriveType::HARDENED);
			}
			assert(der);

			final_info_out = final_info_out_tmp;
			key_out = final_extkey.pubkey;

			// We rely on the consumer to check that m_derive isn't HARDENED as above
			// But we can't have already cached something in case we read something from the cache
			// and parent_extkey isn't actually the parent.
			if (!m_cached_xpub.pubkey.IsValid()) m_cached_xpub = parent_extkey;

			if (write_cache) {
				// Only cache parent if there is any unhardened derivation
				if (m_derive != DeriveType::HARDENED) {
					write_cache.CacheParentExtPubKey(m_expr_index, parent_extkey);
				} else if (final_info_out.path.size() > 0) {
					write_cache.CacheDerivedExtPubKey(m_expr_index, pos, final_extkey);
				}
			}

			return true;
		}
		public override string ToString() 
		{
			string ret = EncodeExtPubKey(m_root_extkey) + FormatHDKeypath(m_path);
			if (IsRange()) {
				ret += "/*";
				if (m_derive == DeriveType::HARDENED) ret += '\'';
			}
			return ret;
		}
		public bool ToPrivateString(SigningProvider arg, out string output) 
		{
			ExtKey key;
			if (!GetExtKey(arg, key)) return false;
			output = EncodeExtKey(key) + FormatHDKeypath(m_path);
			if (IsRange()) {
				output += "/*";
				if (m_derive == DeriveType::HARDENED) output += '\'';
			}
			return true;
		}
		public bool GetPrivKey(int pos, SigningProvider arg, out Key key) 
		{
			ExtKey extkey;
			if (!GetDerivedExtKey(arg, extkey)) return false;
			if (m_derive == DeriveType::UNHARDENED) extkey.Derive(extkey, pos);
			if (m_derive == DeriveType::HARDENED) extkey.Derive(extkey, pos | 0x80000000UL);
			key = extkey.key;
			return true;
		}
	}
}