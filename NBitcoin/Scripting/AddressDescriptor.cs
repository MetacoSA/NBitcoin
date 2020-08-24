using System.Collections.Generic;

namespace NBitcoin.Scripting
{
    /** A parsed addr(A) descriptor. */
    public class AddressDescriptor : DescriptorImpl
	{
		private IDestination m_destination;

		protected string ToStringExtra()  { return EncodeDestination(m_destination); }
		protected List<Script> MakeScripts(List<PubKey> pubkeys, Script script, FlatSigningProvider signingProvider)  { return Vector(GetScriptForDestination(m_destination)); }
	
		public AddressDescriptor(IDestination destination)
			: base(null, null, "addr")
		{
			m_destination = destination;
		}
		public bool IsSolvable(){ return false; }

		public Optional<OutputType> GetOutputType() 
		{
			switch (m_destination.which()) {
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
	}
                } 