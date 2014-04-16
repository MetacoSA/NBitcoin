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
			new PayToPubkeyScriptTemplate(),
			new PayToScriptHashScriptTemplate(),
			new PayToMultiSigScriptTemplate()
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

		public static bool IsStandardTransaction(Transaction tx, TxOutRepository mapInputs)
		{
			return AreInputsStandard(tx, mapInputs) && AreOutputsStandard(tx);
		}

		public static bool AreOutputsStandard(Transaction tx)
		{
			return tx.VOut.All(vout => IsStandardScriptPubKey(vout.ScriptPubKey));
		}

		public static ScriptTemplate GetTemplateFromScriptPubKey(Script script)
		{
			return _StandardTemplates.FirstOrDefault(t => t.CheckScripPubKey(script));
		}

		public static bool IsStandardScriptPubKey(Script scriptPubKey)
		{
			return _StandardTemplates.Any(template => template.CheckScripPubKey(scriptPubKey));
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
		//
		public static bool AreInputsStandard(Transaction tx, TxOutRepository mapInputs)
		{
			if(tx.IsCoinBase)
				return true; // Coinbases don't use vin normally

			for(int i = 0 ; i < tx.VIn.Length ; i++)
			{
				TxOut prev = mapInputs.GetOutputFor(tx.VIn[i]);
				if(prev == null)
					return false;
				if(!IsStandardScriptSig(tx.VIn[i].ScriptSig, prev.ScriptPubKey))
					return false;
			}

			return true;
		}
	}
}
