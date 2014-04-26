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
			new PayToMultiSigScriptTemplate(),
			new TxNullDataScriptTemplate()
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
			if(tx.Version > Transaction.CURRENT_VERSION || tx.Version < 1)
			{
				return false;
			}

			//// Treat non-final transactions as non-standard to prevent a specific type
			//// of double-spend attack, as well as DoS attacks. (if the transaction
			//// can't be mined, the attacker isn't expending resources broadcasting it)
			//// Basically we don't want to propagate transactions that can't included in
			//// the next block.
			////
			//// However, IsFinalTx() is confusing... Without arguments, it uses
			//// chainActive.Height() to evaluate nLockTime; when a block is accepted, chainActive.Height()
			//// is set to the value of nHeight in the block. However, when IsFinalTx()
			//// is called within CBlock::AcceptBlock(), the height of the block *being*
			//// evaluated is what is used. Thus if we want to know if a transaction can
			//// be part of the *next* block, we need to call IsFinalTx() with one more
			//// than chainActive.Height().
			////
			//// Timestamps on the other hand don't get any special treatment, because we
			//// can't know what timestamp the next block will have, and there aren't
			//// timestamp applications where it matters.
			//if (!IsFinalTx(tx, chainActive.Height() + 1)) {
			//	reason = "non-final";
			//	return false;
			//}

			// Extremely large transactions with lots of inputs can cost the network
			// almost as much to process as they cost the sender in fees, because
			// computing signature hashes is O(ninputs*txsize). Limiting transactions
			// to MAX_STANDARD_TX_SIZE mitigates CPU exhaustion attacks.
			int sz = tx.GetSerializedSize();
			if(sz >= Transaction.MAX_STANDARD_TX_SIZE)
				return false;


			foreach(TxIn txin in tx.VIn)
			{
				// Biggest 'standard' txin is a 3-signature 3-of-3 CHECKMULTISIG
				// pay-to-script-hash, which is 3 ~80-byte signatures, 3
				// ~65-byte public keys, plus a few script ops.
				if(txin.ScriptSig.Length > 500)
				{
					return false;
				}
				if(!txin.ScriptSig.IsPushOnly)
				{
					return false;
				}
				if(!txin.ScriptSig.HasCanonicalPushes)
				{
					return false;
				}
			}

			uint nDataOut = 0;
			foreach(TxOut txout in tx.VOut)
			{
				var template = StandardScripts.GetTemplateFromScriptPubKey(txout.ScriptPubKey);
				if(template == null)
					return false;

				if(template.Type == TxOutType.TX_NULL_DATA)
					nDataOut++;
				else if(txout.IsDust)
					return false;
			}
			// only one OP_RETURN txout is permitted
			if(nDataOut > 1)
			{
				return false;
			}

			return true;
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

			for(int i = 0 ; i < tx.VIn.Count ; i++)
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
