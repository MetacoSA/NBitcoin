using System.Collections.Generic;

namespace NBitcoin.Scripting
{
    /** A parsed multi(...) or sortedmulti(...) descriptor */
    class MultisigDescriptor : DescriptorImpl
	{
		private int m_threshold;
		private bool m_sorted;
	
		protected string ToStringExtra()  { return strprintf("%i", m_threshold); }
		protected List<Script> MakeScripts(List<PubKey> keys, Script script, out FlatSigningProvider output)  {
			if (m_sorted) {
				List<PubKey> sorted_keys(keys);
				std::sort(sorted_keys.begin(), sorted_keys.end());
				return Vector(GetScriptForMultisig(m_threshold, sorted_keys));
			}
			return Vector(GetScriptForMultisig(m_threshold, keys));
		}
	
		public MultisigDescriptor(int threshold, List<std::unique_ptr<PubkeyProvider>> providers, bool sorted = false) 
			: base(providers, null, sorted ? "sortedmulti" : "multi")
			{
				m_threshold = threshold;
				m_sorted = sorted;
			} 
		public bool IsSingleType(){ return true; }
	}
                } 