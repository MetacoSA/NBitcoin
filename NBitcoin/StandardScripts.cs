using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{

	public static class StandardScripts
	{
		static readonly ScriptTemplate[] _StandardTemplates = new ScriptTemplate[] 
		{
			new PayToPubkeyHashScriptTemplate(), 
			new PayToPubkeyScriptTemplate() 
		};
		public static Script PayToAddress(BitcoinAddress address)
		{
			return PayToPubkeyHash(address.ID);
		}

		private static Script PayToPubkeyHash(KeyId pubkeyHash)
		{
			return new PayToPubkeyHashScriptTemplate().GenerateOutputScript(pubkeyHash);
		}

		public static Script PayToPubkey(PubKey pubkey)
		{
			return new PayToPubkeyScriptTemplate().GenerateOutputScript(pubkey);
		}



		public static bool IsStandardTransaction(Transaction tx)
		{
			return AreInputsStandard(tx) && AreOutputsStandard(tx);
		}
		public static bool AreInputsStandard(Transaction tx)
		{
			return tx.VIn.All(vin => _StandardTemplates.Any(template => template.IsInputConform(vin.ScriptSig)));
		}
		public static bool AreOutputsStandard(Transaction tx)
		{
			return tx.VOut.All(vout => _StandardTemplates.Any(template => template.IsOutputConform(vout.ScriptPubKey)));
		}

		public static ScriptTemplate GetTemplateFromScriptPubKey(Script script)
		{
			return _StandardTemplates.FirstOrDefault(t => t.IsOutputConform(script));
		}
	}
}
