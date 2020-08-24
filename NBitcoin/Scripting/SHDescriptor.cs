using System.Collections.Generic;

namespace NBitcoin.Scripting
{
    /** A parsed sh(...) descriptor. */
    public class SHDescriptor : DescriptorImpl
	{
		protected List<Script> MakeScripts(List<PubKey> keys, Script script script, out FlatSigningProvider output)  { return Vector(GetScriptForDestination(ScriptHash(script))); }
	
		public SHDescriptor(std::unique_ptr<DescriptorImpl> desc) : base(null, desc, "sh") {}

		public Optional<OutputType> GetOutputType() 
		{
			assert(m_subdescriptor_arg);
			if (m_subdescriptor_arg.GetOutputType() == OutputType::BECH32) return OutputType::P2SH_SEGWIT;
			return OutputType::LEGACY;
		}
		public bool IsSingleType(){ return true; }
	};
                } 