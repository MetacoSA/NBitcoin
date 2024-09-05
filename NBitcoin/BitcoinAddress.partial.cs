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
		/// <summary>
		/// This PSBT represent the to_sign transaction along with the to_spend one as the non_witness_utxo of the first input.
		/// Users can take this PSBT, sign it, then call <see cref="NBitcoin.BIP322.BIP322Signature.FromPSBT(PSBT, SignatureType)"/> to create the signature.
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
			var messageHash = Key.CreateBIP322MessageHash(message);

			var toSpend = Network.CreateTransaction();
			toSpend.Version = 0;
			toSpend.LockTime = 0;
			toSpend.Inputs.Add(new TxIn(new OutPoint(uint256.Zero, 0xFFFFFFFF), new Script(OpcodeType.OP_0, Op.GetPushOp(messageHash.ToBytes(false))))
			{
				Sequence = 0,
				WitScript = WitScript.Empty,
			});
			toSpend.Outputs.Add(new TxOut(Money.Zero, ScriptPubKey));
			var toSpendTxId = toSpend.GetHash();
			var toSign = Network.CreateTransaction();
			toSign.Version = version;
			toSign.LockTime = lockTime;
			toSign.Inputs.Add(new TxIn(new OutPoint(toSpendTxId, 0))
			{
				Sequence = sequence
			});
			fundProofOutputs ??= fundProofOutputs ?? Array.Empty<Coin>();

			foreach (var input in fundProofOutputs)
			{
				toSign.Inputs.Add(new TxIn(input.Outpoint, Script.Empty)
				{
					Sequence = sequence,
				});
			}
			toSign.Outputs.Add(new TxOut(Money.Zero, new Script(OpcodeType.OP_RETURN)));
			var psbt = PSBT.FromTransaction(toSign, Network);
			psbt.Settings.AutomaticUTXOTrimming = false;
			psbt.AddTransactions(toSpend);
			psbt.AddCoins(fundProofOutputs);
			return psbt;
		}
		public bool VerifyBIP322(string message, string signature, Coin[]? fundProofOutputs = null)
		{
			var sig = BIP322Signature.Parse(signature, Network);
			return VerifyBIP322(message, sig, fundProofOutputs);
		}
		static readonly ScriptVerify BIP322ScriptVerify = ScriptVerify.ConstScriptCode
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
			if (signature is BIP322.BIP322Signature.Simple { WitnessScript: var script })
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
				return psbtToSign.Inputs[0].VerifyScript(BIP322ScriptVerify, psbtToSign.PrecomputeTransactionData(), out _);
			}
			else if (signature is BIP322.BIP322Signature.Legacy { CompactSignature: var sig })
			{
				try
				{
					var hash = Key.CreateBIP322MessageHash(message, true);
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
			else if (signature is BIP322.BIP322Signature.Full { SignedTransaction: var toSign } full)
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
				return toSignPSBT.Inputs.Select(i => i.VerifyScript(BIP322ScriptVerify, txData, out _)).All(o => o);
			}
			return false;
		}
	}
}
#endif
