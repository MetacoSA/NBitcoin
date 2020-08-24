using System.Collections.Generic;

namespace NBitcoin.Scripting
{
    /** A parsed pkh(P) descriptor. */
    class PKHDescriptor :DescriptorImpl
	{
		protected List<Script> MakeScripts(List<PubKey> keys, Script*, FlatSigningProvider output) 
		{
			KeyID id = keys[0].GetID();
			out.pubkeys.emplace(id, keys[0]);
			return Vector(GetScriptForDestination(PKHash(id)));
		}
	
		public PKHDescriptor(std::unique_ptr<PubkeyProvider> prov) : base(Vector(prov), null, "pkh") {}
		public Optional<OutputType> GetOutputType()  { return OutputType::LEGACY; }
		public bool IsSingleType(){ return true; }
	};
                } 