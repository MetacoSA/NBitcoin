using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.BuilderExtensions
{
	public class P2PKBuilderExtension : BuilderExtension
	{
		public override bool CanGenerateScriptSig(Script scriptPubkey)
		{
			return PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubkey) != null;
		}

		public override Script GenerateScriptSig(Script scriptPubKey, IKeyRepository keyRepo, ISigner signer)
		{
			var key = keyRepo.FindKey(scriptPubKey);
			if(key == null)
				return null;
			var sig = signer.Sign(key);
			return PayToPubkeyTemplate.Instance.GenerateScriptSig(sig);
		}

		public override Script DeduceScriptPubKey(Script scriptSig)
		{
			throw new NotImplementedException();
		}

		public override bool CanDeduceScriptPubKey(Script scriptSig)
		{
			return false;
		}

		public override bool CanEstimateScriptSigSize(Script scriptPubkey)
		{
			return PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubkey) != null;
		}

		public override int EstimateScriptSigSize(Script scriptPubKey)
		{
			return PayToPubkeyTemplate.Instance.GenerateScriptSig(DummySignature).Length;
		}

		public override bool CanCombineScriptSig(Script scriptPubKey, Script a, Script b)
		{
			return false;
		}

		public override Script CombineScriptSig(Script scriptPubKey, Script a, Script b)
		{
			throw new NotImplementedException();
		}
	}
}
