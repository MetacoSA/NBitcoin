namespace NBitcoin.Scripting
{
    public class OriginPubkeyProvider :  PubkeyProvider
	{
		private KeyOriginInfo m_origin;
		private std::unique_ptr<PubkeyProvider> m_provider;

		private string OriginString()
		{
			return HexStr(m_origin.fingerprint) + FormatHDKeypath(m_origin.path);
		}

	
		public OriginPubkeyProvider(uint exp_index, KeyOriginInfo info, std::unique_ptr<PubkeyProvider> provider) 
			: base(exp_index)
	 	{ 
			m_origin = info;
			m_provider = provider;
		}

		public bool GetPubKey(int pos, SigningProvider arg, PubKey key, KeyOriginInfo info, DescriptorCache read_cache = null, DescriptorCache write_cache = null) 
		{
			if (!m_provider.GetPubKey(pos, arg, key, info, read_cache, write_cache)) return false;
			std::copy(std::begin(m_origin.fingerprint), std::end(m_origin.fingerprint), info.fingerprint);
			info.path.insert(info.path.begin(), m_origin.path.begin(), m_origin.path.end());
			return true;
		}
		public bool IsRange()  { return m_provider.IsRange(); }
		public uint GetSize()  { return m_provider.GetSize(); }
		public string ToString()  { return "[" + OriginString() + "]" + m_provider.ToString(); }
		public bool ToPrivateString(SigningProvider arg, string ret) 
		{
			string sub;
			if (!m_provider.ToPrivateString(arg, sub)) return false;
			ret = "[" + OriginString() + "]" + sub;
			return true;
		}
		public bool GetPrivKey(int pos, SigningProvider arg, Key key) 
		{
			return m_provider.GetPrivKey(pos, arg, key);
		}
	}
} 