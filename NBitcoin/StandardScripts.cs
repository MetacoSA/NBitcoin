using NBitcoin.Policy;
using System;
using System.Linq;

namespace NBitcoin
{

	public static class StandardScripts
	{
		static readonly ScriptTemplate[] _StandardTemplates = new ScriptTemplate[]
		{
			PayToPubkeyHashTemplate.Instance,
			PayToPubkeyTemplate.Instance,
			PayToScriptHashTemplate.Instance,
			PayToMultiSigTemplate.Instance,
			TxNullDataTemplate.Instance,
			PayToWitTemplate.Instance,
			PayToTaprootTemplate.Instance,
		};

		public static bool IsStandardTransaction(Transaction tx)
		{
			return new StandardTransactionPolicy().Check(tx, null).Length == 0;
		}

		public static bool AreOutputsStandard(Transaction tx)
		{
			return tx.Outputs.All(vout => IsStandardScriptPubKey(vout.ScriptPubKey));
		}

		public static ScriptTemplate GetTemplateFromScriptPubKey(Script script)
		{
			return _StandardTemplates.FirstOrDefault(t => t.CheckScriptPubKey(script));
		}

		public static bool IsStandardScriptPubKey(Script scriptPubKey)
		{
			return _StandardTemplates.Any(template => template.CheckScriptPubKey(scriptPubKey));
		}
		private static bool IsStandardScriptSig(Script scriptSig, Script scriptPubKey)
		{
			var template = GetTemplateFromScriptPubKey(scriptPubKey);
			if (template == null)
				return false;

			return template.CheckScriptSig(scriptSig, scriptPubKey);
		}
	}
}
