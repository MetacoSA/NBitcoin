using System.Collections.Generic;

namespace NBitcoin.Scripting
{
    /** A parsed wpkh(P) descriptor. */
    class WPKHDescriptor :DescriptorImpl
	{
		protected List<Script> MakeScripts(List<PubKey> keys, Script script, out FlatSigningProvider output) 
		{
			KeyID id = keys[0].GetID();
			out.pubkeys.emplace(id, keys[0]);
			return Vector(GetScriptForDestination(WitnessV0KeyHash(id)));
		}
	
		public WPKHDescriptor(std::unique_ptr<PubkeyProvider> prov) : base(Vector(prov), null, "wpkh") {}
		publicOptional<OutputType> GetOutputType()  { return OutputType::BECH32; }
		publicbool IsSingleType(){ return true; }
	}
                } 