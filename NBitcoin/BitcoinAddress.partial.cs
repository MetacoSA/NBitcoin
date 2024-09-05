#if HAS_SPAN
#nullable enable
using NBitcoin.BIP322;
using NBitcoin.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NBitcoin
{
	public abstract partial class BitcoinAddress
	{
		public bool VerifyBIP322(string message, string signature, Coin[]? fundProofOutputs = null)
		{
			var sig = BIP322Signature.Parse(signature, Network);
			return VerifyBIP322(message, sig, fundProofOutputs);
		}

		public bool VerifyBIP322(string message, BIP322Signature signature, Coin[]? fundProofOutputs = null)
		{
			if (signature is BIP322Signature.Simple { WitnessScript: var script })
			{
				var psbtToSign = CreateBIP322PSBT(message);
				psbtToSign.Inputs[0].FinalScriptWitness = script;
				if (this is BitcoinScriptAddress)
				{
					var withScriptParams = PayToWitPubKeyHashTemplate.Instance.ExtractWitScriptParameters(script);
					if (withScriptParams is null)
						return false;
					psbtToSign.Inputs[0].FinalScriptSig = PayToScriptHashTemplate.Instance.GenerateScriptSig(new Op[0], withScriptParams.Hash.ScriptPubKey);
				}
				return psbtToSign.Inputs[0].VerifyScript(psbtToSign.PrecomputeTransactionData(), out _);
			}
			else if (signature is BIP322Signature.Legacy { CompactSignature: var sig })
			{
				try
				{
					var hash = BIP322Signature.CreateMessageHash(message, true);
					var k = sig.RecoverPubKey(hash);
					if (k.GetAddress(ScriptPubKeyType.Legacy, Network) != this)
						return false;
					return ECDSASignature.TryParseFromCompact(sig.Signature, out var ecSig) && k.Verify(hash, ecSig);
				}
				catch
				{
					return false;
				}
			}
			else if (signature is BIP322Signature.Full { SignedTransaction: var toSign } full)
			{
				fundProofOutputs ??= Array.Empty<Coin>();
				var toSignPSBT = CreateBIP322PSBT(message, fundProofOutputs: fundProofOutputs);
				toSignPSBT.AddCoins(fundProofOutputs);
				for (int i = 0; i < toSignPSBT.Inputs.Count; i++)
				{
					toSignPSBT.Inputs[i].FinalScriptWitness = toSign.Inputs[i].WitScript;
					toSignPSBT.Inputs[i].FinalScriptSig = toSign.Inputs[i].ScriptSig;
				}
				var txData = toSignPSBT.PrecomputeTransactionData();
				return toSignPSBT.Inputs.Select(i => i.VerifyScript(txData, out _)).All(o => o);
			}
			return false;
		}

		/// <summary>
		/// This PSBT represent the to_sign transaction along with the to_spend one as the non_witness_utxo of the first input.
		/// Users can take this PSBT, sign it, then call <see cref="BIP322Signature.FromPSBT(PSBT, SignatureType)"/> to create the signature.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="version"></param>
		/// <param name="lockTime"></param>
		/// <param name="sequence"></param>
		/// <param name="fundProofOutputs"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public PSBT CreateBIP322PSBT(
			string message,
			uint version = 0, uint lockTime = 0, uint sequence = 0, Coin[]? fundProofOutputs = null)
		{
			return BIP322Signature.CreatePSBT(this, message, version, lockTime, sequence, fundProofOutputs);
		}
	}
}
#endif
