using System.Collections.Generic;

namespace NBitcoin.Scripting
{
    /** A parsed wsh(...) descriptor. */
    public class WSHDescriptor : DescriptorImpl
	{
		protected List<Script> MakeScripts(List<PubKey> keys, Script script, out FlatSigningProvider output)  { return Vector(GetScriptForDestination(WitnessV0ScriptHash(script))); }

		public WSHDescriptor(std::unique_ptr<DescriptorImpl> desc) : base(null, desc, "wsh") {}
		public Optional<OutputType> GetOutputType()  { return OutputType::BECH32; }
		public bool IsSingleType() { return true; }
	}
                } 