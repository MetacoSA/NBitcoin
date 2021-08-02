using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.BuilderExtensions
{
	public class P2PKBuilderExtension : BuilderExtension
	{

		public override bool CanDeduceScriptPubKey(Script scriptSig)
		{
			return false;
		}

		public override bool CanEstimateScriptSigSize(ICoin coin)
		{
			return CanSign(coin.GetScriptCode());
		}

		public override void Sign(InputSigningContext inputSigningContext, IKeyRepository keyRepository, ISigner signer)
		{
			var scriptCode = inputSigningContext.Coin.GetScriptCode();
			var key = keyRepository.FindKey(scriptCode) as PubKey;
			if (key == null)
				return;
			var sig = signer.Sign(key) as TransactionSignature;
			if (sig is null)
				return;
			inputSigningContext.Input.PartialSigs.TryAdd(key, sig);
		}

		private static bool CanSign(Script scriptPubKey)
		{
			return PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey) != null;
		}

		public override Script DeduceScriptPubKey(Script scriptSig)
		{
			throw new NotImplementedException();
		}

		public override int EstimateScriptSigSize(ICoin coin)
		{
			return PayToPubkeyTemplate.Instance.GenerateScriptSig(DummySignature).Length;
		}

		public override bool IsCompatibleKey(IPubKey publicKey, Script scriptPubKey)
		{
			return publicKey is PubKey pk && pk.ScriptPubKey == scriptPubKey;
		}

		public override void Finalize(InputSigningContext inputSigningContext)
		{
			var txIn = inputSigningContext.Input;
			var scriptPubKey = inputSigningContext.Coin.GetScriptCode();
			var pk = PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey);
			if (!txIn.PartialSigs.TryGetValue(pk, out var sig))
				return;
			txIn.FinalScriptSig = PayToPubkeyTemplate.Instance.GenerateScriptSig(sig);
		}
		public override bool Match(ICoin coin, PSBTInput input)
		{
			return CanSign(coin.GetScriptCode());
		}

		public override void ExtractExistingSignatures(InputSigningContext inputSigningContext)
		{
			var scriptPubKey = inputSigningContext.Coin.GetScriptCode();
			var pk = PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey);
			var scriptSigData = inputSigningContext.OriginalTxIn.ScriptSig.ToOps().Select(o => o.PushData);
			var witScriptData = inputSigningContext.OriginalTxIn.WitScript.Pushes;
			foreach (var data in scriptSigData.Concat(witScriptData))
			{
				if (data is null)
					continue;
				try
				{
					var sig = new TransactionSignature(data);
					inputSigningContext.Input.PartialSigs.TryAdd(pk, sig);
					break;
				}
				catch
				{

				}
			}
		}
	}
}
