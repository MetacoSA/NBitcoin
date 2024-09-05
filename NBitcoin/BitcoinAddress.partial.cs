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
		static readonly ScriptVerify BIP322ScriptVerify = ScriptVerify.Const_ScriptCode
								   | ScriptVerify.LowS
								   | ScriptVerify.StrictEnc
								   | ScriptVerify.NullFail
								   | ScriptVerify.MinimalData
								   | ScriptVerify.CleanStack
								   | ScriptVerify.P2SH
								   | ScriptVerify.Witness
								   | ScriptVerify.Taproot
								   | ScriptVerify.MinimalIf;
		public bool VerifyBIP322(string message, BIP322.BIP322Signature signature, Coin[]? fundProofOutputs = null)
		{
			var messageBytes = Encoding.UTF8.GetBytes(message);
			if (signature is BIP322.BIP322Signature.Simple { WitnessScript: var script })
			{
				if (script.PushCount < 2 && !ScriptPubKey.IsScriptType(ScriptType.Taproot))
				{
					return false;
				}
				var psbtToSign = Key.CreateToSignPSBT(Network, Key.CreateMessageHash(message, HashType.BIP322), ScriptPubKey);
				psbtToSign.Inputs[0].FinalScriptWitness = script;
				if (this is BitcoinScriptAddress)
				{
					var withScriptParams = PayToWitPubKeyHashTemplate.Instance.ExtractWitScriptParameters(script);
					if (withScriptParams is null)
						return false;
					psbtToSign.Inputs[0].FinalScriptSig = PayToScriptHashTemplate.Instance.GenerateScriptSig(new Op[0], withScriptParams.Hash.ScriptPubKey);
				}
				return psbtToSign.Inputs[0].VerifyScript(BIP322ScriptVerify, psbtToSign.PrecomputeTransactionData(), out _);
			}
			else if (signature is BIP322.BIP322Signature.Legacy { CompactSignature: var sig })
			{
				try
				{
					if (!ScriptPubKey.IsScriptType(ScriptType.P2PKH))
					{
						return false;
					}
					var hash = Key.CreateMessageHash(message, HashType.Legacy);
					var k = sig.RecoverPubKey(hash);
					if (k.GetAddress(ScriptPubKeyType.Legacy, Network) != this)
					{
						return false;
					}
					return ECDSASignature.TryParseFromCompact(sig.Signature, out var ecSig) && k.Verify(hash, ecSig);
				}
				catch
				{
					return false;
				}
			}
			else if (signature is BIP322.BIP322Signature.Full { SignedTransaction: var toSign } full)
			{
				fundProofOutputs ??= Array.Empty<Coin>();
				var toSignPSBT = Key.CreateToSignPSBT(Network, Key.CreateMessageHash(message, HashType.BIP322), ScriptPubKey, additionalInputs: fundProofOutputs);
				toSignPSBT.AddCoins(fundProofOutputs);
				for (int i = 0; i < toSignPSBT.Inputs.Count; i++)
				{
					toSignPSBT.Inputs[i].FinalScriptWitness = toSign.Inputs[i].WitScript;
					toSignPSBT.Inputs[i].FinalScriptSig = toSign.Inputs[i].ScriptSig;
				}
				var txData = toSignPSBT.PrecomputeTransactionData();
				return toSignPSBT.Inputs.Select(i => i.VerifyScript(BIP322ScriptVerify, txData, out _)).All(o => o);
			}
			return false;
		}
	}
}
#endif
