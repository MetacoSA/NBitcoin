using System.Collections.Generic;

namespace NBitcoin.Scripting
{
    /** A parsed raw(H) descriptor. */
    class RawDescriptor : DescriptorImpl
	{
		private Script m_script;
	
		protected string ToStringExtra()  { return HexStr(m_script); }
		protected List<Script> MakeScripts(List<PubKey> pubkeys, Script script, FlatSigningProvider signingProvider)  { return Vector(m_script); }
	
		public RawDescriptor(Script script) : base(null, null, "raw")
		{
			m_script = script;
		}
		public bool IsSolvable(){ return false; }

		public Optional<OutputType> GetOutputType() 
		{
			IDestination dest;
			ExtractDestination(m_script, dest);
			switch (dest.which()) {
				case 1 /* PKHash */:
				case 2 /* ScriptHash */: return OutputType::LEGACY;
				case 3 /* WitnessV0ScriptHash */:
				case 4 /* WitnessV0KeyHash */:
				case 5 /* WitnessUnknown */: return OutputType::BECH32;
				case 0 /* CNoDestination */:
				default: return nullopt;
			}
		}
		public bool IsSingleType(){ return true; }
	};
                } 