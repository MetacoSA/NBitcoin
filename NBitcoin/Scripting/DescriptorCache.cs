using System.Collections.Generic;

namespace NBitcoin.Scripting
{
    /** Cache for single descriptor's derived extended pubkeys */
    public class DescriptorCache
	{
		/** Map key expression index . map of (key derivation index . xpub) */
		private IDictionary<uint, ExtPubKeyMap> m_derived_xpubs;
		/** Map key expression index . parent xpub */
		private ExtPubKeyMap m_parent_xpubs;

		/** Cache a parent xpub
		*
		* @param[in] key_exp_pos Position of the key expression within the descriptor
		* @param[in] xpub The ExtPubKey to cache
		*/
		public void CacheParentExtPubKey(uint key_exp_pos, ExtPubKey xpub)
		{
			m_parent_xpubs[key_exp_pos] = xpub;
		}

		/** Retrieve a cached parent xpub
		*
		* @param[in] key_exp_pos Position of the key expression within the descriptor
		* @param[in] xpub The ExtPubKey to get from cache
		*/
		public bool GetCachedParentExtPubKey(uint key_exp_pos, ExtPubKey xpub)
		{
			var it = m_parent_xpubs.find(key_exp_pos);
			if (it == m_parent_xpubs.end()) return false;
			xpub = it.second;
			return true;
		}

		/** Cache an xpub derived at an index
		*
		* @param[in] key_exp_pos Position of the key expression within the descriptor
		* @param[in] der_index Derivation index of the xpub
		* @param[in] xpub The ExtPubKey to cache
		*/
		public void CacheDerivedExtPubKey(uint key_exp_pos, uint der_index, ExtPubKey xpub)
		{
			var xpubs = m_derived_xpubs[key_exp_pos];
			xpubs[der_index] = xpub;
		}

		/** Retrieve a cached xpub derived at an index
		*
		* @param[in] key_exp_pos Position of the key expression within the descriptor
		* @param[in] der_index Derivation index of the xpub
		* @param[in] xpub The ExtPubKey to get from cache
		*/
		public bool GetCachedDerivedExtPubKey(uint key_exp_pos, uint der_index, ExtPubKey xpub)
		{
			var key_exp_it = m_derived_xpubs.find(key_exp_pos);
			if (key_exp_it == m_derived_xpubs.end()) return false;
			var der_it = key_exp_it.second.find(der_index);
			if (der_it == key_exp_it.second.end()) return false;
			xpub = der_it.second;
			return true;
		}

		/** Retrieve all cached parent xpubs */
		public ExtPubKeyMap GetCachedParentExtPubKeys()
		{
			return m_parent_xpubs;
		}

		/** Retrieve all cached derived xpubs */
		public IDictionary<uint32_t, ExtPubKeyMap> GetCachedDerivedExtPubKeys()
		{
			return m_derived_xpubs;
		}
	};
                } 