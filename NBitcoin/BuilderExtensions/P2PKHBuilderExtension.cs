﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.BuilderExtensions
{
	public class P2PKHBuilderExtension : BuilderExtension
	{
		public override bool CanGenerateScriptSig(Script scriptPubkey)
		{
			return PayToPubkeyHashTemplate.Instance.CheckScriptPubKey(scriptPubkey);
		}

		public override Script GenerateScriptSig(Script scriptPubKey, IKeyRepository keyRepo, ISigner signer)
		{
			var parameters = PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey);
			var key = keyRepo.FindKey(parameters.ScriptPubKey);
			if(key == null)
				return null;
			var sig = signer.Sign(key);
			return PayToPubkeyHashTemplate.Instance.GenerateScriptSig(sig, key.PubKey);
		}

		public override Script DeduceScriptPubKey(Script scriptSig)
		{
			var p2pkh = PayToPubkeyHashTemplate.Instance.ExtractScriptSigParameters(scriptSig);
			return p2pkh.PublicKey.Hash.ScriptPubKey;
		}

		public override bool CanDeduceScriptPubKey(Script scriptSig)
		{
			var para = PayToPubkeyHashTemplate.Instance.ExtractScriptSigParameters(scriptSig);
			return para != null && para.PublicKey != null;
		}

		public override bool CanEstimateScriptSigSize(Script scriptPubkey)
		{
			return PayToPubkeyHashTemplate.Instance.CheckScriptPubKey(scriptPubkey);
		}

		public override int EstimateScriptSigSize(Script scriptPubkey)
		{
			return PayToPubkeyHashTemplate.Instance.GenerateScriptSig(DummySignature, DummyPubKey).Length;
		}

		public override bool CanCombineScriptSig(Script scriptPubKey, Script a, Script b)
		{
			return PayToPubkeyHashTemplate.Instance.CheckScriptPubKey(scriptPubKey);
		}

		public override Script CombineScriptSig(Script scriptPubKey, Script a, Script b)
		{
			var aSig = PayToPubkeyHashTemplate.Instance.ExtractScriptSigParameters(a);
			var bSig = PayToPubkeyHashTemplate.Instance.ExtractScriptSigParameters(b);
			if(aSig == null)
				return b;
			if(bSig == null)
				return a;
			var merged = new PayToPubkeyHashScriptSigParameters();
			merged.PublicKey = aSig.PublicKey ?? bSig.PublicKey;
			merged.TransactionSignature = aSig.TransactionSignature ?? bSig.TransactionSignature;
			return PayToPubkeyHashTemplate.Instance.GenerateScriptSig(merged);
		}
	}
}
