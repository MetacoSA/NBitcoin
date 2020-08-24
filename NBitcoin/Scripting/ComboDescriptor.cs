using System.Collections.Generic;

namespace NBitcoin.Scripting
{
    /** A parsed combo(P) descriptor. */
    class ComboDescriptor :DescriptorImpl
	{
		protected List<Script> MakeScripts(List<PubKey> keys, Script script, out FlatSigningProvider output) 
		{
			List<Script> ret;
			KeyID id = keys[0].GetID();
			output.pubkeys.emplace(id, keys[0]);
			ret.emplace_back(GetScriptForRawPubKey(keys[0])); // P2PK
			ret.emplace_back(GetScriptForDestination(PKHash(id))); // P2PKH
			if (keys[0].IsCompressed()) {
				Script p2wpkh = GetScriptForDestination(WitnessV0KeyHash(id));
				output.scripts.emplace(ScriptID(p2wpkh), p2wpkh);
				ret.emplace_back(p2wpkh);
				ret.emplace_back(GetScriptForDestination(ScriptHash(p2wpkh))); // P2SH-P2WPKH
			}
			return ret;
		}
	
		public ComboDescriptor(std::unique_ptr<PubkeyProvider> prov) : base(Vector(prov), null, "combo") {}
		public bool IsSingleType(){ return false; }
	}
                } 