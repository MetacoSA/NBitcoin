namespace NBitcoin.Scripting
{
    /** An object representing a parsed constant public key in a descriptor. */
    public class ConstPubkeyProvider : PubkeyProvider
	{
		private PubKey m_pubkey;

		public ConstPubkeyProvider(uint exp_index, PubKey pubkey) 
			: base(exp_index)
		{ 
			m_pubkey = pubkey;
		}

		public bool GetPubKey(int pos, SigningProvider arg, PubKey key, KeyOriginInfo info, DescriptorCache read_cache = null, DescriptorCache write_cache = null) 
		{
			key = m_pubkey;
			info.path.clear();
			KeyID keyid = m_pubkey.GetID();
			std::copy(keyid.begin(), keyid.begin() + sizeof(info.fingerprint), info.fingerprint);
			return true;
		}
		public bool IsRange()  { return false; }
		public uint GetSize()  { return m_pubkey.size(); }
		public string ToString()  { return HexStr(m_pubkey); }
		public bool ToPrivateString(SigningProvider arg, string ret) 
		{
			Key key;
			if (!arg.GetKey(m_pubkey.GetID(), key)) return false;
			ret = EncodeSecret(key);
			return true;
		}
		public bool GetPrivKey(int pos, SigningProvider arg, Key key) 
		{
			return arg.GetKey(m_pubkey.GetID(), key);
		}
	};
                } 