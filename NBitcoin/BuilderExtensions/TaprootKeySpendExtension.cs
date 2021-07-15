#nullable enable
#if HAS_SPAN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.BuilderExtensions
{
	public class TaprootBIP86Extension : BuilderExtension
	{
		public override bool CanCombineScriptSig(Script scriptPubKey, Script a, Script b)
		{
			return false;
		}

		public override bool CanDeduceScriptPubKey(Script scriptSig)
		{
			return false;
		}

		public override bool CanEstimateScriptSigSize(Script scriptPubKey)
		{
			return true;
		}

		public override bool CanGenerateScriptSig(Script scriptPubKey)
		{
			return true;
		}

		public override Script CombineScriptSig(Script scriptPubKey, Script a, Script b)
		{
			throw new NotSupportedException();
		}

		public override Script DeduceScriptPubKey(Script scriptSig)
		{
			throw new NotSupportedException();
		}

		public override int EstimateScriptSigSize(Script scriptPubKey)
		{
			// Push op + 65 bytes for sig (most likely 64, but we don't know the sighash here)
			return 66;
		}

		public override Script? GenerateScriptSig(Script scriptPubKey, IKeyRepository keyRepo, ISigner signer)
		{
			var pk = keyRepo.FindKey(scriptPubKey) as TaprootFullPubKey;
			if (pk is null)
				return null;
			var signature = signer.Sign(pk) as TaprootSignature;
			if (signature is null)
				return null;
			return PayToTaprootTemplate.Instance.GenerateScriptSig(signature);
		}

		public override bool IsCompatibleKey(IPubKey publicKey, Script scriptPubKey)
		{
			return publicKey is TaprootPubKey pk && pk.ScriptPubKey == scriptPubKey;
		}
	}
}
#endif
