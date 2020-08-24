using System.Collections.Generic;

namespace NBitcoin.Scripting
{
    /** A parsed pk(P) descriptor. */
    public class PKDescriptor : DescriptorImpl
	{
		protected List<Script> MakeScripts(List<PubKey> keys, Script script, FlatSigningProvider signingProvider)  { return Vector(GetScriptForRawPubKey(keys[0])); }

		public PKDescriptor(std::unique_ptr<PubkeyProvider> prov) : base(Vector(prov), null, "pk") {}
		public bool IsSingleType(){ return true; }
	};
                } 