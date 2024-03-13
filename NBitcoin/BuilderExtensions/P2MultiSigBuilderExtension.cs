#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.BuilderExtensions
{
	public class P2MultiSigBuilderExtension : BuilderExtension
	{
		public override bool CanDeduceScriptPubKey(Script scriptSig)
		{
			return false;
		}

		public override bool CanEstimateScriptSigSize(ICoin coin)
		{
			return CanSign(coin.GetScriptCode());
		}

		private static bool CanSign(Script scriptPubKey)
		{
			return PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey) != null;
		}

		public override Script DeduceScriptPubKey(Script scriptSig)
		{
			throw new NotImplementedException();
		}

		public override int EstimateScriptSigSize(ICoin coin)
		{
			var scriptPubKey = coin.GetScriptCode();
			var p2mk = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey)!;
			return PayToMultiSigTemplate.Instance.GenerateScriptSig(Enumerable.Range(0, p2mk.SignatureCount).Select(o => DummySignature).ToArray()).Length;
		}

		public override void Sign(InputSigningContext inputSigningContext, IKeyRepository keyRepository, ISigner signer)
		{
			var scriptCode = inputSigningContext.Coin.GetScriptCode();
			var multiSigParams = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(scriptCode)!;
			TransactionSignature?[] signatures = new TransactionSignature[multiSigParams.PubKeys.Length];
			var keys = multiSigParams.PubKeys;
			int sigcount = 0;
			for (int i = 0; i < keys.Length && sigcount < multiSigParams.SignatureCount; i++)
			{
				var sig = signer.Sign(keys[i]) as TransactionSignature;
				signatures[i] = sig;
				if (sig != null)
					sigcount++;
			}
			for (int i = 0; i < keys.Length; i++)
			{
				var sig = signatures[i];
				var key = keys[i];
				if (key is PubKey && sig is TransactionSignature s && s != TransactionSignature.Empty)
				{
					inputSigningContext.Input.PartialSigs.TryAdd(key, sig);
				}
			}
		}

		public override bool IsCompatibleKey(IPubKey publicKey, Script scriptPubKey)
		{
			if (!(publicKey is PubKey pk))
				return false;
			var multiSigParams = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey);
			if (multiSigParams == null)
				return false;
			return multiSigParams.PubKeys.Any(p => p == pk);
		}

		public override void Finalize(InputSigningContext inputSigningContext)
		{
			var txIn = inputSigningContext.Input;
			var scriptPubKey = inputSigningContext.Coin.GetScriptCode();
			var multiSigParams = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey)!;
			if (multiSigParams.SignatureCount > txIn.PartialSigs.Count)
				return;
			List<TransactionSignature> sigs = new List<TransactionSignature>();
			int sigcount = 0;
			foreach (var pk in multiSigParams.PubKeys)
			{
				if (sigcount == multiSigParams.SignatureCount)
					break;
				if (txIn.PartialSigs.TryGetValue(pk, out var s))
				{
					sigcount++;
					sigs.Add(s);
				}
			}
			txIn.FinalScriptSig = PayToMultiSigTemplate.Instance.GenerateScriptSig(sigs.ToArray());
		}

		public override bool Match(ICoin coin, PSBTInput input)
		{
			return CanSign(coin.GetScriptCode());
		}

		public override void ExtractExistingSignatures(InputSigningContext inputSigningContext)
		{
			if (inputSigningContext.OriginalTxIn is null || inputSigningContext.TransactionContext.Transaction is null)
				return;
			var scriptSig = inputSigningContext.OriginalTxIn.ScriptSig;
			var witScript = inputSigningContext.OriginalTxIn.WitScript;
			var multiSigParams = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(inputSigningContext.Coin.GetScriptCode())!;
			var txIn = inputSigningContext.Input;
			var scriptPubKey = inputSigningContext.Coin.GetScriptCode();

			bool hasRedeem;
			List<byte[]> sigs;
			if (inputSigningContext.Coin is ScriptCoin scriptCoin)
			{
				hasRedeem = true;
				if (scriptCoin.RedeemType == RedeemType.P2SH)
				{
					if (Script.IsNullOrEmpty(inputSigningContext.OriginalTxIn.ScriptSig))
						return;
					sigs = scriptSig.ToOps().Select(s => s.PushData).ToList();
				}
				else // if (scriptCoin.RedeemType == RedeemType.WitnessV0)
				{
					if (WitScript.IsNullOrEmpty(inputSigningContext.OriginalTxIn.WitScript))
						return;
					sigs = witScript.Pushes.ToList();
				}
			}
			else
			{
				hasRedeem = false;
				sigs = scriptSig.ToOps().Select(s => s.PushData).ToList();
			}
			// At least leading 0, pk count and the redeem
			if (sigs.Count < 2 + (hasRedeem ? 1 : 0))
				return;
			if (!(sigs[0]?.Length is 0))
				return;
			sigs.RemoveAt(0); // Remove leading 0
			if (hasRedeem)
				sigs.RemoveAt(sigs.Count - 1); // Remove the redeem


			int pkIndex = 0;
			for (int i = 0; i < sigs.Count && pkIndex < multiSigParams.PubKeys.Length; i++)
			{
				var sig = sigs[i];
				var pk = multiSigParams.PubKeys[pkIndex];
				if (sig.Length is 0)
				{
					pkIndex++;
				}
				else
				{
					try
					{
						var txsig = new TransactionSignature(sig);
						var hash = inputSigningContext.TransactionContext
													.Transaction
													.Inputs.FindIndexedInput(inputSigningContext.Coin.Outpoint)
													.GetSignatureHash(inputSigningContext.Coin, txsig.SigHash, inputSigningContext.TransactionContext.SigningOptions.PrecomputedTransactionData);
						while (!pk.Verify(hash, txsig.Signature))
						{
							pkIndex++;
							if (pkIndex >= multiSigParams.PubKeys.Length)
								goto end;
							pk = multiSigParams.PubKeys[pkIndex];
						}
						txIn.PartialSigs.TryAdd(pk, txsig);
						pkIndex++;
					}
					catch { }
				}
			}
			end:;
		}

		public override void MergePartialSignatures(InputSigningContext inputSigningContext)
		{
			if (inputSigningContext.OriginalTxIn is null || inputSigningContext.TransactionContext.Transaction is null)
				return;
			var scriptSig = inputSigningContext.OriginalTxIn.ScriptSig;
			var witScript = inputSigningContext.OriginalTxIn.WitScript;
			var multiSigParams = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(inputSigningContext.Coin.GetScriptCode())!;
			var txIn = inputSigningContext.Input;
			var scriptPubKey = inputSigningContext.Coin.GetScriptCode();

			var sigs = new TransactionSignature?[multiSigParams.PubKeys.Length];
			int sigCount = 0;
			for (int i = 0; i < multiSigParams.PubKeys.Length; i++)
			{
				if (txIn.PartialSigs.TryGetValue(multiSigParams.PubKeys[i], out sigs[i]))
					sigCount++;
			}
			if (sigCount >= multiSigParams.SignatureCount)
				return; // We have all signatures already, no need merging here, finalize will take care of it.
			List<Op> ops = new List<Op>();
			ops.Add(OpcodeType.OP_0);
			for (int i = 0; i < multiSigParams.PubKeys.Length; i++)
			{
				if (sigs[i] is TransactionSignature sig)
					ops.Add(Op.GetPushOp(sig.ToBytes()));
				else
					ops.Add(OpcodeType.OP_0);
			}

			if (txIn.WitnessScript is Script s)
			{
				ops.Add(Op.GetPushOp(s.ToBytes()));
				inputSigningContext.OriginalTxIn.WitScript = new WitScript(ops.ToArray());
				if (txIn.RedeemScript is Script p2sh)
				{
					inputSigningContext.OriginalTxIn.ScriptSig = new Script(Op.GetPushOp(p2sh.ToBytes()));
				}
			}
			else if (txIn.RedeemScript is Script s2)
			{
				ops.Add(Op.GetPushOp(s2.ToBytes()));
				inputSigningContext.OriginalTxIn.ScriptSig = new Script(ops.ToArray());
			}
			else
			{
				inputSigningContext.OriginalTxIn.ScriptSig = new Script(ops.ToArray());
			}
		}
	}
}
