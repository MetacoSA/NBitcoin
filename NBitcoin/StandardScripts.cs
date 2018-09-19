using NBitcoin.BitcoinCore;
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
			PayToWitTemplate.Instance
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
			if(template == null)
				return false;

			return template.CheckScriptSig(scriptSig, scriptPubKey);
		}

		//
		// Check transaction inputs, and make sure any
		// pay-to-script-hash transactions are evaluating IsStandard scripts
		//
		// Why bother? To avoid denial-of-service attacks; an attacker
		// can submit a standard HASH... OP_EQUAL transaction,
		// which will get accepted into blocks. The redemption
		// script can be anything; an attacker could use a very
		// expensive-to-check-upon-redemption script like:
		//   DUP CHECKSIG DROP ... repeated 100 times... OP_1
		[Obsolete]
		public static bool AreInputsStandard(Transaction tx, CoinsView coinsView)
		{
			if(tx.IsCoinBase)
				return true; // Coinbases don't use vin normally

			foreach(var input in tx.Inputs)
			{
				TxOut prev = coinsView.GetOutputFor(input);
				if(prev == null)
					return false;
				if(!IsStandardScriptSig(input.ScriptSig, prev.ScriptPubKey))
					return false;
			}

			return true;
		}
	}
}
