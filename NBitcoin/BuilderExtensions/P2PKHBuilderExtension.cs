using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.BuilderExtensions
{
	public class P2PKHBuilderExtension : BuilderExtension
	{
		public override bool CanDeduceScriptPubKey(Script scriptSig)
		{
			var para = PayToPubkeyHashTemplate.Instance.ExtractScriptSigParameters(scriptSig);
			return para != null && para.PublicKey != null;
		}

		public override bool CanEstimateScriptSigSize(ICoin coin)
		{
			return CanSign(coin.GetScriptCode());
		}

		private static bool CanSign(Script scriptPubKey)
		{
			return PayToPubkeyHashTemplate.Instance.CheckScriptPubKey(scriptPubKey);
		}


		public override Script DeduceScriptPubKey(Script scriptSig)
		{
			var p2pkh = PayToPubkeyHashTemplate.Instance.ExtractScriptSigParameters(scriptSig);
			return p2pkh.PublicKey.Hash.ScriptPubKey;
		}

		public override int EstimateScriptSigSize(ICoin coin)
		{
			return 107;
		}

		public override bool IsCompatibleKey(IPubKey publicKey, Script scriptPubKey)
		{
			return publicKey is PubKey pk && pk.Hash.ScriptPubKey == scriptPubKey;
		}

		public override void Sign(InputSigningContext inputSigningContext, IKeyRepository keyRepository, ISigner signer)
		{
			var executedScript = inputSigningContext.Coin.GetScriptCode();
			var parameters = PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(executedScript);
			var key = keyRepository.FindKey(parameters.ScriptPubKey) as PubKey;
			if (key == null)
				return;
			var sig = signer.Sign(key) as TransactionSignature;
			if (sig is null)
				return;
			inputSigningContext.Input.PartialSigs.TryAdd(key, sig);
		}

		public override void Finalize(InputSigningContext inputSigningContext)
		{
			var txIn = inputSigningContext.Input;
			if (txIn.PartialSigs.Count is 0)
				return;
			var sig = txIn.PartialSigs.First();
			txIn.FinalScriptSig = PayToPubkeyHashTemplate.Instance.GenerateScriptSig(sig.Value, sig.Key);
		}
		public override bool Match(ICoin coin, PSBTInput input)
		{
			return CanSign(coin.GetScriptCode());
		}

		public override void ExtractExistingSignatures(InputSigningContext inputSigningContext)
		{
			var scriptPubKey = inputSigningContext.Coin.GetScriptCode();
			var scriptSigData = inputSigningContext.OriginalTxIn.ScriptSig.ToOps().Select(o => o.PushData);
			var witScriptData = inputSigningContext.OriginalTxIn.WitScript.Pushes;
			PubKey pk = null;
			TransactionSignature sig = null;
			foreach (var data in scriptSigData.Concat(witScriptData))
			{
				if (data is null)
					continue;
				if (data.Length == 65 || data.Length == 33)
				{
					try
					{
						pk = new PubKey(data);
					}
					catch { }
				}
				else
				{
					try
					{
						sig = new TransactionSignature(data);
					}
					catch
					{

					}
				}
				if (sig is TransactionSignature && pk is PubKey)
				{
					inputSigningContext.Input.PartialSigs.TryAdd(pk, sig);
				}
			}
		}
	}
}
