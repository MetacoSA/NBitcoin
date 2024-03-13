#nullable enable
#if HAS_SPAN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.BuilderExtensions
{
	public class TaprootKeySpendExtension : BuilderExtension
	{

		public override bool CanDeduceScriptPubKey(Script scriptSig)
		{
			return false;
		}

		public override bool CanEstimateScriptSigSize(ICoin coin)
		{
			return CanSign(coin.GetScriptCode());
		}

		
		bool CanSign(Script executedScript)
		{
			return PayToTaprootTemplate.Instance.CheckScriptPubKey(executedScript);
		}

		public override Script DeduceScriptPubKey(Script scriptSig)
		{
			throw new NotSupportedException();
		}

		public override int EstimateScriptSigSize(ICoin coin)
		{
			return 66;
		}

		public override void Sign(InputSigningContext inputSigningContext, IKeyRepository keyRepository, ISigner signer)
		{
			var pk = keyRepository.FindKey(inputSigningContext.Coin.TxOut.ScriptPubKey) as TaprootFullPubKey;
			if (pk is null)
				return;
			var signature = signer.Sign(pk) as TaprootSignature;
			if (signature is null)
				return;
			inputSigningContext.Input.TaprootInternalKey = pk.InternalKey;
			inputSigningContext.Input.TaprootKeySignature = signature;
			inputSigningContext.Input.TaprootMerkleRoot = pk.MerkleRoot;
		}

		public override bool IsCompatibleKey(IPubKey publicKey, Script scriptPubKey)
		{
			return publicKey is TaprootPubKey pk && pk.ScriptPubKey == scriptPubKey;
		}

		public override void Finalize(InputSigningContext inputSigningContext)
		{
			var txIn = inputSigningContext.Input;
			if (txIn.TaprootInternalKey is TaprootInternalPubKey &&
				txIn.TaprootKeySignature is TaprootSignature)
			{
				txIn.FinalScriptWitness = PayToTaprootTemplate.Instance.GenerateWitScript(txIn.TaprootKeySignature);
			}
		}
		public override bool Match(ICoin coin, PSBTInput input)
		{
			return coin.TxOut.ScriptPubKey.IsScriptType(ScriptType.Taproot)
				&& PayToTaprootTemplate.Instance.CheckScriptPubKey(coin.TxOut.ScriptPubKey);
		}
	}
}
#endif
