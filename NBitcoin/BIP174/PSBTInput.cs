#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using NBitcoin.DataEncoders;
using PartialSigKVMap = System.Collections.Generic.SortedDictionary<NBitcoin.PubKey, NBitcoin.TransactionSignature>;
using System.Diagnostics.CodeAnalysis;
using NBitcoin.Crypto;
using System.Text;

namespace NBitcoin
{
	public class PSBTInput : PSBTCoin
	{
		// Those fields are not saved, but can be used as hint to solve more info for the PSBT
		internal Script originalScriptSig = Script.Empty;
		internal WitScript originalWitScript = WitScript.Empty;
		internal TxOut? orphanTxOut = null; // When this input is not segwit, but we don't have the previous tx

		internal PSBTInput(PSBT parent, uint index, TxIn input) : base(parent)
		{
			TxIn = input;
			Index = index;
			originalScriptSig = TxIn.ScriptSig ?? Script.Empty;
			originalWitScript = TxIn.WitScript ?? WitScript.Empty;
		}

		internal PSBTInput(BitcoinStream stream, PSBT parent, uint index, TxIn input) : base(parent)
		{
			TxIn = input;
			Index = index;
			originalScriptSig = TxIn.ScriptSig ?? Script.Empty;
			originalWitScript = TxIn.WitScript ?? WitScript.Empty;
			byte[] k = new byte[0];
			byte[] v = new byte[0];
			try
			{
				stream.ReadWriteAsVarString(ref k);
			}
			catch (EndOfStreamException e)
			{
				throw new FormatException("Invalid PSBTInput. Failed to Parse key.", e);
			}
			while (k.Length != 0)
			{
				try
				{
					stream.ReadWriteAsVarString(ref v);
				}
				catch (EndOfStreamException e)
				{
					throw new FormatException("Invalid PSBTInput. Failed to parse key.", e);
				}
				switch (k.First())
				{
					case PSBTConstants.PSBT_IN_NON_WITNESS_UTXO:
						if (k.Length != 1)
							throw new FormatException("Invalid PSBTInput. Contains illegal value in key for NonWitnessUTXO");
						if (non_witness_utxo != null)
							throw new FormatException("Invalid PSBTInput. Duplicate non_witness_utxo");
						non_witness_utxo = Parent.GetConsensusFactory().CreateTransaction();
						non_witness_utxo.FromBytes(v);
						break;
					case PSBTConstants.PSBT_IN_WITNESS_UTXO:
						if (k.Length != 1)
							throw new FormatException("Invalid PSBTInput. Contains illegal value in key for WitnessUTXO");
						if (witness_utxo != null)
							throw new FormatException("Invalid PSBTInput. Duplicate witness_utxo");
						if (Parent.GetConsensusFactory().TryCreateNew<TxOut>(out var txout))
						{
							witness_utxo = txout;
						}
						else
						{
							witness_utxo = new TxOut();
						}
						witness_utxo.FromBytes(v);
						break;
					case PSBTConstants.PSBT_IN_PARTIAL_SIG:
						var pkbytes = k.Skip(1).ToArray();
						if (pkbytes.Length == 33)
						{
							var pubkey = new PubKey(pkbytes);
							if (partial_sigs.ContainsKey(pubkey))
								throw new FormatException("Invalid PSBTInput. Duplicate key for partial_sigs");
							partial_sigs.Add(pubkey, new TransactionSignature(v));
						}
						else
							throw new FormatException("Unexpected public key size in the PSBT");
						break;
					case PSBTConstants.PSBT_IN_SIGHASH:
						if (k.Length != 1)
							throw new FormatException("Invalid PSBTInput. Contains illegal value in key for SigHash type");
						if (!(sighash_type is null))
							throw new FormatException("Invalid PSBTInput. Duplicate key for sighash_type");
						if (v.Length != 4)
							throw new FormatException("Invalid PSBTInput. SigHash Type is not 4 byte");
						var value = Utils.ToUInt32(v, 0, true);
						if (!Enum.IsDefined(typeof(SigHash), value))
							throw new FormatException($"Invalid PSBTInput Unknown SigHash Type {value}");
						sighash_type = (SigHash)value;
						break;
					case PSBTConstants.PSBT_IN_REDEEMSCRIPT:
						if (k.Length != 1)
							throw new FormatException("Invalid PSBTInput. Contains illegal value in key for redeem script");
						if (redeem_script != null)
							throw new FormatException("Invalid PSBTInput. Duplicate key for redeem_script");
						redeem_script = Script.FromBytesUnsafe(v);
						break;
					case PSBTConstants.PSBT_IN_WITNESSSCRIPT:
						if (k.Length != 1)
							throw new FormatException("Invalid PSBTInput. Contains illegal value in key for witness script");
						if (witness_script != null)
							throw new FormatException("Invalid PSBTInput. Duplicate key for redeem_script");
						witness_script = Script.FromBytesUnsafe(v);
						break;
					case PSBTConstants.PSBT_IN_TAP_KEY_SIG:
						if (k.Length != 1)
							throw new FormatException("Invalid PSBTInput. Unexpected key length for PSBT_IN_TAP_KEY_SIG");
						if (!TaprootSignature.TryParse(v, out var sig))
							throw new FormatException("Invalid PSBTInput. Contains invalid TaprootSignature");
						TaprootKeySignature = sig;
						break;
					case PSBTConstants.PSBT_IN_TAP_INTERNAL_KEY:
						if (k.Length != 1)
							throw new FormatException("Invalid PSBTInput. Unexpected key length for PSBT_IN_TAP_INTERNAL_KEY");
						if (!TaprootInternalPubKey.TryCreate(v, out var tpk))
							throw new FormatException("Invalid PSBTInput. Contains invalid internal taproot pubkey");
						TaprootInternalKey = tpk;
						break;
					case PSBTConstants.PSBT_IN_BIP32_DERIVATION:
						var pubkey2 = new PubKey(k.Skip(1).ToArray());
						if (hd_keypaths.ContainsKey(pubkey2))
							throw new FormatException("Invalid PSBTInput. Duplicate key for hd_keypaths");
						var masterFingerPrint = new HDFingerprint(v.Take(4).ToArray());
						KeyPath path = KeyPath.FromBytes(v.Skip(4).ToArray());
						hd_keypaths.Add(pubkey2, new RootedKeyPath(masterFingerPrint, path));
						break;
					case PSBTConstants.PSBT_IN_TAP_BIP32_DERIVATION:
						var pubkey3 = new TaprootPubKey(k.Skip(1).ToArray());
						if (hd_taprootkeypaths.ContainsKey(pubkey3))
							throw new FormatException("Invalid PSBTOutput, duplicate key for PSBT_IN_TAP_BIP32_DERIVATION");
						var bs = new BitcoinStream(v);
						List<uint256> hashes = null!;
						bs.ReadWrite(ref hashes);
						var pos = (int)bs.Inner.Position;
						KeyPath path2 = KeyPath.FromBytes(v.Skip(pos + 4).ToArray());
						hd_taprootkeypaths.Add(pubkey3,
							new TaprootKeyPath(
								new RootedKeyPath(new HDFingerprint(v.Skip(pos).Take(4).ToArray()), path2),
								hashes.ToArray()));
						break;
					case PSBTConstants.PSBT_IN_TAP_MERKLE_ROOT:
						if (k.Length != 1)
							throw new FormatException("Invalid PSBTInput. Unexpected key length for PSBT_IN_TAP_MERKLE_ROOT");
						if (v.Length != 32)
							throw new FormatException("Invalid PSBTInput. Unexpected value length for PSBT_IN_TAP_MERKLE_ROOT");
						TaprootMerkleRoot = new uint256(v);
						break;
					case PSBTConstants.PSBT_IN_SCRIPTSIG:
						if (k.Length != 1)
							throw new FormatException("Invalid PSBTInput. Contains illegal value in key for final scriptsig");
						if (final_script_sig != null)
							throw new FormatException("Invalid PSBTInput. Duplicate key for final_script_sig");
						final_script_sig = Script.FromBytesUnsafe(v);
						break;
					case PSBTConstants.PSBT_IN_SCRIPTWITNESS:
						if (k.Length != 1)
							throw new FormatException("Invalid PSBTInput. Contains illegal value in key for final script witness");
						if (final_script_witness != null)
							throw new FormatException("Invalid PSBTInput. Duplicate key for final_script_witness");
						final_script_witness = new WitScript(v);
						break;
					default:
						if (unknown.ContainsKey(k))
							throw new FormatException("Invalid PSBTInput. Duplicate key for unknown value");
						unknown.Add(k, v);
						break;
				}
				stream.ReadWriteAsVarString(ref k);
			}
		}

		/// <summary>
		/// Changes the nSequence field of the corresponding TxIn.
		/// You should not call this method if any PSBTInput in the same PSBT has a signature.
		/// Because the siagnature usually commits to the old nSequence value.
		/// </summary>
		/// <exception cref="InvalidOperationException">When at least one signature exists in any other inputs in the PSBT</exception>
		public void SetSequence(ushort sequence)
		{
			for (int i = 0; i < this.Parent.Inputs.Count; i++)
			{
				var txIn = this.Parent.Inputs[i];
				if (txIn.partial_sigs.Count > 0 || txIn.final_script_sig != null || txIn.final_script_witness != null)
				{
					throw new InvalidOperationException($"You should not change the transaction's input nSequence after signing. In case of particular type of SIGHASH, this will make your signature invalid. PSBTInput in index {i} had signature");
				}
			}
			Transaction.Inputs[this.Index].Sequence = sequence;
		}

		internal TxIn TxIn { get; }
		internal IndexedTxIn GetIndexedInput() => new IndexedTxIn() { Transaction = Transaction, Index = Index, TxIn = TxIn, PrevOut = PrevOut, ScriptSig = TxIn.ScriptSig, WitScript = TxIn.WitScript };

		public OutPoint PrevOut => TxIn.PrevOut;

		public uint Index { get; }
		internal Transaction Transaction => Parent.tx;

		private Transaction? non_witness_utxo;
		private TxOut? witness_utxo;
		private Script? final_script_sig;
		private WitScript? final_script_witness;
		private PartialSigKVMap partial_sigs = new PartialSigKVMap(PubKeyComparer.Instance);

		SigHash? sighash_type;

		public Transaction? NonWitnessUtxo
		{
			get
			{
				return non_witness_utxo;
			}
			set
			{
				non_witness_utxo = value;
			}
		}

		public TxOut? WitnessUtxo
		{
			get
			{
				return witness_utxo;
			}
			set
			{
				witness_utxo = value;
			}
		}

		public SigHash? SighashType
		{
			get
			{
				return sighash_type;
			}
			set
			{
				sighash_type = value;
			}
		}

		public Script? FinalScriptSig
		{
			get
			{
				return final_script_sig;
			}
			set
			{
				final_script_sig = value;
			}
		}

		public WitScript? FinalScriptWitness
		{
			get
			{
				return final_script_witness;
			}
			set
			{
				final_script_witness = value;
			}
		}

		public TaprootSignature? TaprootKeySignature { get; set; }
		public uint256? TaprootMerkleRoot { get; set; }

		public PartialSigKVMap PartialSigs
		{
			get
			{
				return partial_sigs;
			}
		}

		public void UpdateFromCoin(ICoin coin)
		{
			if (coin == null)
				throw new ArgumentNullException(nameof(coin));
			if (coin.Outpoint != PrevOut)
				throw new ArgumentException("This coin does not match the input", nameof(coin));
			
			if (coin is ScriptCoin scriptCoin)
			{
				if (scriptCoin.RedeemType == RedeemType.P2SH)
				{
					redeem_script = scriptCoin.Redeem;
				}
				else if (scriptCoin.RedeemType == RedeemType.WitnessV0)
				{
					witness_script = scriptCoin.Redeem;
					if (scriptCoin.IsP2SH)
						redeem_script = witness_script.WitHash.ScriptPubKey;
				}
			}
			else
			{
				if (coin.TxOut.ScriptPubKey.IsScriptType(ScriptType.P2SH) && redeem_script == null)
				{
					// Let's try to be smart by finding the redeemScript in the global tx
					if (Parent.Settings.IsSmart && redeem_script == null)
					{
						var redeemScript = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(originalScriptSig, coin.TxOut.ScriptPubKey)?.RedeemScript;
						if (redeemScript != null)
						{
							redeem_script = redeemScript;
						}
					}
				}

				if (witness_script == null)
				{
					// Let's try to be smart by finding the witness script in the global tx
					if (Parent.Settings.IsSmart && witness_script == null)
					{
						var witScriptId = PayToWitScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(coin.TxOut.ScriptPubKey);
						if (witScriptId == null && redeem_script != null)
							witScriptId = PayToWitScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(redeem_script);
						if (witScriptId != null)
						{
							var redeemScript = PayToWitScriptHashTemplate.Instance.ExtractWitScriptParameters(originalWitScript, witScriptId);
							if (redeemScript != null)
							{
								witness_script = redeemScript;
							}
						}
					}
				}
			}
			if (Parent.Network.Consensus.NeverNeedPreviousTxForSigning ||
				!coin.IsMalleable || witness_script != null)
			{
				witness_utxo = coin.TxOut;
				non_witness_utxo = null;
			}
			else
			{
				orphanTxOut = coin.TxOut;
				witness_utxo = null;
			}
			if (IsFinalized())
				ClearForFinalize();
		}

		/// <summary>
		/// Import informations contained by <paramref name="other"/> into this instance.
		/// </summary>
		/// <param name="other"></param>
		public void UpdateFrom(PSBTInput other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			foreach (var uk in other.unknown)
				unknown.TryAdd(uk.Key, uk.Value);


			if (other.final_script_sig != null)
				final_script_sig = other.final_script_sig;

			if (other.final_script_witness != null)
				final_script_witness = other.final_script_witness;

			if (non_witness_utxo == null && other.non_witness_utxo != null)
				non_witness_utxo = other.non_witness_utxo;

			if (witness_utxo == null && other.witness_utxo != null)
				witness_utxo = other.witness_utxo;

			if (sighash_type == 0 && other.sighash_type > 0)
				sighash_type = other.sighash_type;

			if (redeem_script == null && other.redeem_script != null)
				redeem_script = other.redeem_script;

			if (witness_script == null && other.witness_script != null)
				witness_script = other.witness_script;

			foreach (var sig in other.partial_sigs)
				partial_sigs.TryAdd(sig.Key, sig.Value);

			foreach (var keyPath in other.hd_keypaths)
				hd_keypaths.TryAdd(keyPath.Key, keyPath.Value);

			foreach (var keyPath in other.HDTaprootKeyPaths)
				HDTaprootKeyPaths.TryAdd(keyPath.Key, keyPath.Value);

			TaprootInternalKey ??= other.TaprootInternalKey;
			TaprootKeySignature ??= other.TaprootKeySignature;
			TaprootMerkleRoot ??= other.TaprootMerkleRoot;
			if (IsFinalized())
				ClearForFinalize();
		}

		public uint256 GetSignatureHash(TaprootSigHash sigHash, PrecomputedTransactionData precomputedTransactionData)
		{
			if (GetSignableCoin() is ICoin coin)
				return this.Transaction.Inputs.FindIndexedInput((int)Index)
									   .GetSignatureHashTaproot(coin, sigHash, precomputedTransactionData);
			throw new InvalidOperationException("WitnessUtxo, NonWitnessUtxo, WitnessScript or redeemScript is required to get the signature hash");
		}
		public uint256 GetSignatureHash(SigHash sigHash, PrecomputedTransactionData? precomputedTransactionData)
		{
			if (GetSignableCoin() is ICoin coin)
				return this.Transaction.Inputs.FindIndexedInput((int)Index)
									   .GetSignatureHash(coin, sigHash, precomputedTransactionData);
			throw new InvalidOperationException("WitnessUtxo, NonWitnessUtxo, WitnessScript or redeemScript is required to get the signature hash");
		}

		public bool IsFinalized() => final_script_sig != null || final_script_witness != null;

		/// <summary>
		/// conovert partial sigs to suitable form for ScriptSig (or Witness).
		/// This will preserve the ordering of redeem script even if it did not follow bip67.
		/// </summary>
		/// <param name="redeem"></param>
		/// <returns></returns>
		private Op[] GetPushItems(Script redeem)
		{
			if (PayToMultiSigTemplate.Instance.CheckScriptPubKey(redeem))
			{
				var sigPushes = new List<Op> { OpcodeType.OP_0 };
				foreach (var pk in redeem.GetAllPubKeys())
				{
					if (!partial_sigs.TryGetValue(pk, out var sigPair))
						continue;
					sigPushes.Add(Op.GetPushOp(sigPair.ToBytes()));
				}
				// check sig is more than m in case of p2multisig.
				var multiSigParam = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(redeem);
				var numSigs = sigPushes.Count - 1;
				if (multiSigParam != null && numSigs < multiSigParam.SignatureCount)
					throw new InvalidOperationException("Not enough signatures to finalize.");
				return sigPushes.ToArray();
			}
			throw new InvalidOperationException("PSBT does not know how to finalize this type of script!");
		}

		/// <summary>
		/// Delete superflous information from a finalized input.
		/// This will not clear utxos since tx extractor might want to check the validity
		/// </summary>
		/// <exception cref="System.InvalidOperationException">The input need to be finalized</exception>
		public void ClearForFinalize()
		{
			if (!IsFinalized())
				throw new InvalidOperationException("The input need to be finalized");
			this.redeem_script = null;
			this.witness_script = null;
			this.partial_sigs.Clear();
			this.hd_keypaths.Clear();
			this.sighash_type = null;
			this.TaprootKeySignature = null;
			this.TaprootInternalKey = null;
		}

		/// <summary>
		/// Represent this input as a coin that can be used for signing operations.
		/// Returns null if <see cref="WitnessUtxo"/>, <see cref="NonWitnessUtxo"/> are not set
		/// or if <see cref="PSBTCoin.WitnessScript"/> or <see cref="PSBTCoin.RedeemScript"/> are missing but needed.
		/// </summary>
		/// <returns>The input as a signable coin</returns>
		public new Coin? GetSignableCoin()
		{
			return base.GetSignableCoin();
		}

		/// <summary>
		/// Represent this input as a coin that can be used for signing operations.
		/// Returns null if <see cref="WitnessUtxo"/>, <see cref="NonWitnessUtxo"/> are not set
		/// or if <see cref="PSBTCoin.WitnessScript"/> or <see cref="PSBTCoin.RedeemScript"/> are missing but needed.
		/// </summary>
		/// <param name="error">If it is not possible to retrieve the signable coin, a human readable reason.</param>
		/// <returns>The input as a signable coin</returns>
		public override Coin? GetSignableCoin(out string? error)
		{
			if (witness_utxo is null && non_witness_utxo is null)
			{
				error = "Neither witness_utxo nor non_witness_output is set";
				return null;
			}
			return base.GetSignableCoin(out error);
		}

		internal override Script? GetRedeemScript()
		{
			var redeemScript = base.GetRedeemScript();
			if (redeemScript != null)
				return redeem_script;
			if (FinalScriptSig is null)
				return null;
			var coin = GetCoin();
			if (coin is null)
				return null;
			var scriptId = PayToScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(coin.ScriptPubKey);
			if (scriptId is null)
				return null;
			return PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(FinalScriptSig, scriptId)?.RedeemScript;
		}

		internal override Script? GetWitnessScript()
		{
			var witnessScript = base.GetWitnessScript();
			if (witnessScript != null)
				return witness_script;
			if (FinalScriptWitness is null)
				return null;
			var coin = GetCoin();
			if (coin is null)
				return null;
			var witScriptId = PayToWitScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(coin.ScriptPubKey);
			if (witScriptId != null)
			{
				return PayToWitScriptHashTemplate.Instance.ExtractWitScriptParameters(FinalScriptWitness, witScriptId);
			}
			// Maybe wrapped P2SH
			if (FinalScriptSig is null)
				return null;
			var scriptId = PayToScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(coin.ScriptPubKey);
			if (scriptId is null)
				return null;
			var p2shRedeem = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(FinalScriptSig, scriptId)
				?.RedeemScript;
			if (p2shRedeem is null)
				return null;
			witScriptId = PayToWitScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(p2shRedeem);
			if (witScriptId is null)
				return null;
			return PayToWitScriptHashTemplate.Instance.ExtractWitScriptParameters(FinalScriptWitness, witScriptId);
		}

		public IList<PSBTError> CheckSanity()
		{
			List<PSBTError> errors = new List<PSBTError>();
			if (this.IsFinalized())
			{
				if (partial_sigs.Count != 0)
					errors.Add(new PSBTError(Index, "Input finalized, but partial sigs are not empty"));
				if (hd_keypaths.Count != 0)
					errors.Add(new PSBTError(Index, "Input finalized, but hd keypaths are not empty"));
				if (!(sighash_type is null))
					errors.Add(new PSBTError(Index, "Input finalized, but sighash type is not null"));
				if (redeem_script != null)
					errors.Add(new PSBTError(Index, "Input finalized, but redeem script is not null"));
				if (witness_script != null)
					errors.Add(new PSBTError(Index, "Input finalized, but witness script is not null"));
			}

			if (witness_script != null && witness_utxo is null && non_witness_utxo is null)
				errors.Add(new PSBTError(Index, "witness script present but not witness_utxo or non_witness_utxo"));

			if (final_script_witness != null && witness_utxo is null && non_witness_utxo is null)
				errors.Add(new PSBTError(Index, "final witness script present but not witness_utxo or non_witness_utxo"));

			if (NonWitnessUtxo != null)
			{
				var prevOutTxId = NonWitnessUtxo.GetHash();
				bool validOutpoint = true;
				if (TxIn.PrevOut.Hash != prevOutTxId)
				{
					errors.Add(new PSBTError(Index, "non_witness_utxo does not match the transaction id referenced by the global transaction sign"));
					validOutpoint = false;
				}
				if (TxIn.PrevOut.N >= NonWitnessUtxo.Outputs.Count)
				{
					errors.Add(new PSBTError(Index, "Global transaction referencing an out of bound output in non_witness_utxo"));
					validOutpoint = false;
				}
				if (redeem_script != null && validOutpoint)
				{
					if (redeem_script.Hash.ScriptPubKey != NonWitnessUtxo.Outputs[TxIn.PrevOut.N].ScriptPubKey)
						errors.Add(new PSBTError(Index, "The redeem_script is not coherent with the scriptPubKey of the non_witness_utxo"));
				}
			}

			if (witness_utxo != null)
			{
				if (redeem_script != null)
				{
					if (redeem_script.Hash.ScriptPubKey != witness_utxo.ScriptPubKey)
						errors.Add(new PSBTError(Index, "The redeem_script is not coherent with the scriptPubKey of the witness_utxo"));
					if (witness_script != null &&
						redeem_script != null &&
						PayToWitScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(redeem_script) != witness_script.WitHash)
						errors.Add(new PSBTError(Index, "witnessScript with witness UTXO does not match the redeemScript"));
				}
			}

			if (witness_utxo?.ScriptPubKey is Script s)
			{
				if (!s.IsScriptType(ScriptType.P2SH) && !s.IsScriptType(ScriptType.Witness))
					errors.Add(new PSBTError(Index, "A Witness UTXO is provided for a non-witness input"));
				if (s.IsScriptType(ScriptType.P2SH) && redeem_script is Script r && !r.IsScriptType(ScriptType.Witness))
					errors.Add(new PSBTError(Index, "A Witness UTXO is provided for a non-witness input"));
			}
			return errors;
		}

		public void TrySign(IHDScriptPubKey accountHDScriptPubKey, IHDKey accountKey, RootedKeyPath? accountKeyPath)
		{
			TrySign(accountHDScriptPubKey, accountKey, accountKeyPath, null);
		}
		internal void TrySign(IHDScriptPubKey accountHDScriptPubKey, IHDKey accountKey, RootedKeyPath? accountKeyPath, SigningOptions? signingOptions)
		{
			if (accountKey == null)
				throw new ArgumentNullException(nameof(accountKey));
			if (accountHDScriptPubKey == null)
				throw new ArgumentNullException(nameof(accountHDScriptPubKey));
			if (IsFinalized())
				return;
			var cache = accountKey.AsHDKeyCache();
			accountHDScriptPubKey = accountHDScriptPubKey.AsHDKeyCache();
			foreach (var hdk in this.HDKeysFor(accountHDScriptPubKey, cache, accountKeyPath))
			{
				if (((HDKeyCache)cache.Derive(hdk.AddressKeyPath)).Inner is ISecret k)
					Sign(k.PrivateKey, signingOptions);
				else
					throw new ArgumentException(paramName: nameof(accountKey), message: "This should be a private key");
			}
		}
		public void AssertSanity()
		{
			var errors = CheckSanity();
			if (errors.Count != 0)
				throw new PSBTException(errors);
		}

		#region IBitcoinSerializable Members

		private static uint defaultKeyLen = 1;

		public void Serialize(BitcoinStream stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			// Write the utxo
			// If there is a non-witness utxo, then don't serialize the witness one.
			if (witness_utxo != null)
			{
				// key
				stream.ReadWriteAsVarInt(ref defaultKeyLen);
				var key = PSBTConstants.PSBT_IN_WITNESS_UTXO;
				stream.ReadWrite(ref key);

				// value
				var data = witness_utxo.ToBytes();
				stream.ReadWriteAsVarString(ref data);
			}

			if (non_witness_utxo != null)
			{
				// key
				stream.ReadWriteAsVarInt(ref defaultKeyLen);
				var key = PSBTConstants.PSBT_IN_NON_WITNESS_UTXO;
				stream.ReadWrite(ref key);
				// value
				byte[] data = non_witness_utxo.ToBytes();
				stream.ReadWriteAsVarString(ref data);
			}

			// Write the sighash type
			if (sighash_type > 0)
			{
				stream.ReadWriteAsVarInt(ref defaultKeyLen);
				var key = PSBTConstants.PSBT_IN_SIGHASH;
				stream.ReadWrite(ref key);
				var tmp = Utils.ToBytes((uint)sighash_type, true);
				stream.ReadWriteAsVarString(ref tmp);
			}

			// Write the redeem script
			if (redeem_script != null)
			{
				stream.ReadWriteAsVarInt(ref defaultKeyLen);
				var key = PSBTConstants.PSBT_IN_REDEEMSCRIPT;
				stream.ReadWrite(ref key);
				var value = redeem_script.ToBytes();
				stream.ReadWriteAsVarString(ref value);
			}

			// Write the witness script
			if (witness_script != null)
			{
				stream.ReadWriteAsVarInt(ref defaultKeyLen);
				var key = PSBTConstants.PSBT_IN_WITNESSSCRIPT;
				stream.ReadWrite(ref key);
				var value = witness_script.ToBytes();
				stream.ReadWriteAsVarString(ref value);
			}

			if (this.TaprootMerkleRoot is uint256 merkleRoot)
			{
				stream.ReadWriteAsVarInt(ref defaultKeyLen);
				var key = PSBTConstants.PSBT_IN_TAP_MERKLE_ROOT;
				stream.ReadWrite(ref key);
				var value = merkleRoot.ToBytes();
				stream.ReadWriteAsVarString(ref value);
			}

			if (this.TaprootInternalKey is TaprootInternalPubKey tp)
			{
				stream.ReadWriteAsVarInt(ref defaultKeyLen);
				var key = PSBTConstants.PSBT_IN_TAP_INTERNAL_KEY;
				stream.ReadWrite(ref key);
				var b = tp.ToBytes();
				stream.ReadWriteAsVarString(ref b);
			}

			if (this.TaprootKeySignature is TaprootSignature tsig)
			{
				stream.ReadWriteAsVarInt(ref defaultKeyLen);
				var key = PSBTConstants.PSBT_IN_TAP_KEY_SIG;
				stream.ReadWrite(ref key);
				var b = tsig.ToBytes();
				stream.ReadWriteAsVarString(ref b);
			}

			// Write any partial signatures
			foreach (var sig_pair in partial_sigs)
			{
				var key = new byte[] { PSBTConstants.PSBT_IN_PARTIAL_SIG }.Concat(sig_pair.Key.ToBytes());
				stream.ReadWriteAsVarString(ref key);
				var sig = sig_pair.Value.ToBytes();
				stream.ReadWriteAsVarString(ref sig);
			}

			// Write any hd keypaths
			foreach (var pathPair in hd_keypaths)
			{
				var key = new byte[] { PSBTConstants.PSBT_IN_BIP32_DERIVATION }.Concat(pathPair.Key.ToBytes());
				stream.ReadWriteAsVarString(ref key);
				var masterFingerPrint = pathPair.Value.MasterFingerprint;
				var path = pathPair.Value.KeyPath.ToBytes();
				var pathInfo = masterFingerPrint.ToBytes().Concat(path);
				stream.ReadWriteAsVarString(ref pathInfo);
			}
			foreach (var pathPair in hd_taprootkeypaths)
			{
				var key = new byte[] { PSBTConstants.PSBT_IN_TAP_BIP32_DERIVATION }.Concat(pathPair.Key.ToBytes());
				stream.ReadWriteAsVarString(ref key);
				uint leafCount = (uint)pathPair.Value.LeafHashes.Length;
				BitcoinStream bs = new BitcoinStream(new MemoryStream(), true);
				bs.ReadWriteAsVarInt(ref leafCount);
				foreach (var hash in pathPair.Value.LeafHashes)
				{
					bs.ReadWrite(hash);
				}
				var b = pathPair.Value.RootedKeyPath.MasterFingerprint.ToBytes();
				bs.ReadWrite(ref b);
				b = pathPair.Value.RootedKeyPath.KeyPath.ToBytes();
				bs.ReadWrite(ref b);
				b = ((MemoryStream)bs.Inner).ToArrayEfficient();
				stream.ReadWriteAsVarString(ref b);
			}

			// Write script sig
			if (final_script_sig != null)
			{
				stream.ReadWriteAsVarInt(ref defaultKeyLen);
				var key = PSBTConstants.PSBT_IN_SCRIPTSIG;
				stream.ReadWrite(ref key);
				byte[] value = final_script_sig.ToBytes();
				stream.ReadWriteAsVarString(ref value);
			}

			// write script witness
			if (final_script_witness != null)
			{
				stream.ReadWriteAsVarInt(ref defaultKeyLen);
				var key = PSBTConstants.PSBT_IN_SCRIPTWITNESS;
				stream.ReadWrite(ref key);
				var stack = final_script_witness.ToBytes();
				stream.ReadWriteAsVarString(ref stack);
			}

			// Write unknown things
			foreach (var entry in unknown)
			{
				var k = entry.Key;
				var v = entry.Value;
				stream.ReadWriteAsVarString(ref k);
				stream.ReadWriteAsVarString(ref v);
			}

			var sep = PSBTConstants.PSBT_SEPARATOR;
			stream.ReadWrite(ref sep);
		}

		#endregion

		public byte[] ToBytes()
		{
			MemoryStream ms = new MemoryStream();
			var bs = new BitcoinStream(ms, true);
			bs.ConsensusFactory = Parent.GetConsensusFactory();
			this.Serialize(bs);
			return ms.ToArrayEfficient();
		}

		internal void Write(JsonTextWriter jsonWriter)
		{
			jsonWriter.WriteStartObject();
			jsonWriter.WritePropertyValue("index", Index);
			if (unknown.Count != 0)
			{
				jsonWriter.WritePropertyName("unknown");
				jsonWriter.WriteStartObject();
				foreach (var el in unknown)
				{
					jsonWriter.WritePropertyValue(Encoders.Hex.EncodeData(el.Key), Encoders.Hex.EncodeData(el.Value));
				}
				jsonWriter.WriteEndObject();
			}

			if (this.TaprootInternalKey is TaprootInternalPubKey tpk)
			{
				jsonWriter.WritePropertyValue("taproot_internal_key", tpk.ToString());
			}
			if (this.TaprootMerkleRoot is uint256 r)
			{
				jsonWriter.WritePropertyValue("taproot_merkle_root", r.ToString());
			}
			if (this.TaprootKeySignature is TaprootSignature tsig)
			{
				jsonWriter.WritePropertyValue("taproot_key_signature", tsig.ToString());
			}
			jsonWriter.WritePropertyName("partial_signatures");
			jsonWriter.WriteStartObject();
			foreach (var sig in partial_sigs)
			{
				jsonWriter.WritePropertyValue(sig.Key.ToString(), Encoders.Hex.EncodeData(sig.Value.ToBytes()));
			}
			jsonWriter.WriteEndObject();
			if (SighashType is SigHash s)
				jsonWriter.WritePropertyValue("sighash", GetName(s));
			if (this.FinalScriptSig != null)
			{
				jsonWriter.WritePropertyValue("final_script_sig", FinalScriptSig.ToString());
			}
			if (this.FinalScriptWitness != null)
			{
				jsonWriter.WritePropertyValue("final_script_witness", FinalScriptWitness.ToString());
			}

			if (this.redeem_script != null)
			{
				jsonWriter.WritePropertyValue("redeem_script", redeem_script.ToString());
			}
			if (this.witness_script != null)
			{
				jsonWriter.WritePropertyValue("witness_script", witness_script.ToString());
			}

			if (this.NonWitnessUtxo != null)
			{
				jsonWriter.WritePropertyName("non_witness_utxo");
				jsonWriter.WriteStartObject();
				RPC.BlockExplorerFormatter.WriteTransaction(jsonWriter, NonWitnessUtxo);
				jsonWriter.WriteEndObject();
			}
			if (this.WitnessUtxo != null)
			{
				jsonWriter.WritePropertyName("witness_utxo");
				jsonWriter.WriteStartObject();
				jsonWriter.WritePropertyValue("value", WitnessUtxo.Value.ToString(false, false));
				jsonWriter.WritePropertyValue("scriptPubKey", WitnessUtxo.ScriptPubKey.ToString());
				jsonWriter.WriteEndObject();
			}
			jsonWriter.WriteBIP32Derivations(this.hd_keypaths);
			jsonWriter.WriteBIP32Derivations(this.hd_taprootkeypaths);
			jsonWriter.WriteEndObject();
		}

		private string GetName(SigHash sighashType)
		{
			switch (sighashType)
			{
				case SigHash.All:
					return "ALL";
				case SigHash.None:
					return "NONE";
				case SigHash.Single:
					return "SINGLE";
				case SigHash.All | SigHash.AnyoneCanPay:
					return "ALL|ANYONECANPAY";
				case SigHash.None | SigHash.AnyoneCanPay:
					return "NONE|ANYONECANPAY";
				case SigHash.Single | SigHash.AnyoneCanPay:
					return "SINGLE|ANYONECANPAY";
				default:
					return sighashType.ToString();
			}
		}
		public TxOut? GetTxOut()
		{
			if (WitnessUtxo != null)
				return WitnessUtxo;
			if (NonWitnessUtxo != null)
			{
				if (TxIn.PrevOut.N >= NonWitnessUtxo.Outputs.Count)
					return null;
				return NonWitnessUtxo.Outputs[TxIn.PrevOut.N];
			}
			if (orphanTxOut != null)
				return orphanTxOut;
			return null;
		}

		public bool TryFinalizeInput([MaybeNullWhen(true)] out IList<PSBTError> errors)
		{
			return TryFinalizeInput(null, out errors);
		}
		internal bool TryFinalizeInput(SigningOptions? signingOptions, [MaybeNullWhen(true)] out IList<PSBTError> errors)
		{
			errors = null;
			if (IsFinalized())
				return true;
			var isSane = this.CheckSanity();
			if (isSane.Count != 0)
			{
				errors = isSane;
				return false;
			}
			if (witness_utxo == null && non_witness_utxo == null)
			{
				errors = new List<PSBTError>() { new PSBTError(Index, "Neither witness_utxo nor non_witness_output is set") };
				return false;
			}
			var coin = this.GetSignableCoin(out var getSignableCoinError) ?? this.GetCoin(); // GetCoin can't be null at this stage.
			if (coin is null)
				throw new InvalidOperationException("Bug in NBitcoin during TryFinalizeInput: Please report it");

			signingOptions ??= Parent.Settings.SigningOptions;

			TransactionBuilder transactionBuilder = Parent.CreateTransactionBuilder();
			signingOptions = Parent.GetSigningOptions(signingOptions);
			if (!IsTaprootReady(signingOptions, coin))
			{
				errors = new List<PSBTError>();
				errors.Add(new PSBTError(Index, "When finalizing a taproot input, you need to make sure that all inputs of the PSBT contains witness_utxo or non_witness_utxo"));
				return false;
			}
			transactionBuilder.SetSigningOptions(signingOptions);
			transactionBuilder.AddCoins(coin);
			foreach (var sig in PartialSigs)
			{
				transactionBuilder.AddKnownSignature(sig.Key, sig.Value, coin.Outpoint);
			}
#if HAS_SPAN
			if (TaprootInternalKey is TaprootInternalPubKey tpk && TaprootKeySignature is TaprootSignature ts)
			{
				var k = TaprootFullPubKey.Create(tpk, TaprootMerkleRoot);
				transactionBuilder.AddKnownSignature(k, ts, coin.Outpoint);
			}
#endif
			var finalizedInput = this;
			var previousFinalScriptSig = this.FinalScriptSig;
			var previousFinalScriptWit = this.FinalScriptWitness;

			void Rollback()
			{
				this.FinalScriptSig = previousFinalScriptSig;
				this.FinalScriptWitness = previousFinalScriptWit;
			}
			try
			{
				transactionBuilder.FinalizePSBTInput(this);
				if (!finalizedInput.IsFinalized() && Parent.Settings.IsSmart)
				{
					transactionBuilder.ExtractSignatures(finalizedInput.PSBT, Parent.GetOriginalTransaction());
					transactionBuilder.FinalizePSBTInput(finalizedInput);
				}
			}
			catch (Exception ex)
			{
				Rollback();
				errors = new List<PSBTError>() { new PSBTError(Index, $"Error while finalizing the input \"{getSignableCoinError ?? ex.Message}\"") };
				return false;
			}
			if (!finalizedInput.IsFinalized())
			{
				Rollback();
				errors = new List<PSBTError>() { new PSBTError(Index, $"Impossible to finalize the input") };
				return false;
			}
			if (!Parent.Settings.SkipVerifyScript)
			{
				if (!finalizedInput.VerifyScript(Parent.Settings.ScriptVerify, signingOptions.PrecomputedTransactionData, out var err2))
				{
					Rollback();
					errors = new List<PSBTError>() { new PSBTError(Index, $"The finalized input script does not properly validate \"{err2}\"") };
					return false;
				}
			}

			if (coin is ScriptCoin scriptCoin)
			{
				if (scriptCoin.IsP2SH)
					RedeemScript = scriptCoin.GetP2SHRedeem();
				if (scriptCoin.RedeemType == RedeemType.WitnessV0)
					WitnessScript = scriptCoin.Redeem;
			}
			ClearForFinalize();
			errors = null;
			return true;
		}

		internal static bool IsTaprootReady(SigningOptions signingOptions, Coin coin)
		{
			return !coin.ScriptPubKey.IsScriptType(ScriptType.Taproot) || (signingOptions.PrecomputedTransactionData is TaprootReadyPrecomputedTransactionData);
		}

		public bool VerifyScript(ScriptVerify scriptVerify, PrecomputedTransactionData? precomputedTransactionData, out ScriptError err)
		{
			var eval = new ScriptEvaluationContext
			{
				ScriptVerify = scriptVerify
			};
			if (Transaction is IHasForkId)
				eval.ScriptVerify |= NBitcoin.ScriptVerify.ForkId;
			var txout = GetTxOut();
			if (txout is null)
			{
				err = ScriptError.UnknownError;
				return false;
			}
			var checker = new TransactionChecker(Transaction, (int)Index, GetTxOut(), precomputedTransactionData);
			var result = eval.VerifyScript(this.FinalScriptSig, this.FinalScriptWitness, txout.ScriptPubKey, checker);
			err = eval.Error;
			return result;
		}

		public void FinalizeInput()
		{
			if (!TryFinalizeInput(out var errors))
				throw new PSBTException(errors);
		}

		public void Sign(Key key)
		{
			Sign(key, null);
		}
		public void Sign(KeyPair keyPair)
		{
			Sign(keyPair, null);
		}
		internal void Sign(KeyPair keyPair, SigningOptions? signingOptions)
		{
			if (keyPair == null)
				throw new ArgumentNullException(nameof(keyPair));
			if (this.IsFinalized())
				return;
			signingOptions = Parent.GetSigningOptions(signingOptions);
			if (keyPair.PubKey is PubKey ecdsapk && PartialSigs.TryGetValue(ecdsapk, out var existingSig))
			{
				CheckCompatibleSigHash(signingOptions.SigHash);
				var signature = PartialSigs[ecdsapk];
				var signatureSigHash = existingSig.SigHash;
				if (Transaction is IHasForkId)
					signatureSigHash = (SigHash)((uint)existingSig.SigHash & ~(0x40u));
				if (!SameSigHash(signatureSigHash, signingOptions.SigHash))
					throw new InvalidOperationException("A signature with a different sighash is already in the partial sigs");
				return;
			}

			AssertSanity();
			var coin = GetSignableCoin();
			if (coin == null)
				return;

			signingOptions = Parent.GetSigningOptions(signingOptions);
			if (!IsTaprootReady(signingOptions, coin))
				return;
			var builder = Parent.CreateTransactionBuilder();
			builder.AddKeys(keyPair);
			builder.SetSigningOptions(signingOptions);
			builder.SignPSBTInput(this);
		}
		internal void Sign(Key key, SigningOptions? signingOptions)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			var coin = GetSignableCoin();
			if (coin == null)
				return;
			if (coin.ScriptPubKey.IsScriptType(ScriptType.Taproot))
			{
#if HAS_SPAN
				Sign(key.CreateTaprootKeyPair(TaprootMerkleRoot), signingOptions);
				return;
#else
				throw new NotSupportedException("Impossible to sign taproot input on .NET Framework");
#endif
			}
			else
			{
				Sign(key.CreateKeyPair(), signingOptions);
				return;
			}
		}

		bool SameSigHash(SigHash a, SigHash b)
		{
			if (a == b)
				return true;
			if (Transaction is not IHasForkId)
				return false;
			a = (SigHash)((uint)a & ~(0x40u));
			b = (SigHash)((uint)a & ~(0x40u));
			return a == b;
		}
		private void CheckCompatibleSigHash(SigHash sigHash)
		{
			if (SighashType is SigHash s && !SameSigHash(s,sigHash))
				throw new InvalidOperationException($"The input assert the use of sighash {GetName(s)}");
		}

		/// <summary>
		/// Check if this satisfies criteria for witness. if it does, delete non_witness_utxo
		/// This is useful for following reasons.
		/// 1. It will make a data smaller which is an obviously good thing.
		/// 2. Future HW Wallet may not support non segwit tx and thus won't recognize non_witness_utxo
		/// 3. To pass test in BIP174
		/// </summary>
		public bool TrySlimUTXO()
		{
			if (NonWitnessUtxo == null)
				return false;
			var coin = GetSignableCoin();
			if (coin == null)
				return false;

			if (Parent.Network.Consensus.NeverNeedPreviousTxForSigning ||
				!coin.IsMalleable)
			{
				if (WitnessUtxo == null)
				{
					if (TxIn.PrevOut.N < NonWitnessUtxo.Outputs.Count)
						WitnessUtxo = NonWitnessUtxo.Outputs[TxIn.PrevOut.N];
				}
				NonWitnessUtxo = null;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Represent this input as a coin.
		/// Returns null if <see cref="WitnessUtxo"/> or <see cref="NonWitnessUtxo"/> is not set.
		/// </summary>
		/// <returns>The input as a coin</returns>
		public override Coin? GetCoin()
		{
			var txout = GetTxOut();
			if (txout == null)
				return null;
			return new Coin(TxIn.PrevOut, txout);
		}

		public override string ToString()
		{
			var strWriter = new StringWriter();
			var jsonWriter = new JsonTextWriter(strWriter);
			jsonWriter.Formatting = Formatting.Indented;
			Write(jsonWriter);
			jsonWriter.Flush();
			return strWriter.ToString();
		}

		protected override PSBTHDKeyMatch CreateHDKeyMatch(IHDKey accountKey, KeyPath addressKeyPath, KeyValuePair<IPubKey, RootedKeyPath> kv)
		{
			return new PSBTHDKeyMatch<PSBTInput>(this, accountKey, addressKeyPath, kv);
		}
	}

	public class PSBTInputList : PSBTCoinList<PSBTInput>
	{
		Dictionary<OutPoint, PSBTInput> _InputsByOutpoint = new Dictionary<OutPoint, PSBTInput>();


		internal void Add(PSBTInput input)
		{
			if (!_InputsByOutpoint.TryAdd(input.TxIn.PrevOut, input))
				throw new InvalidOperationException("Two inputs are spending the same output in the same transaction");
			_Inner.Add(input);
		}

		public PSBTInput? FindIndexedInput(OutPoint prevOut)
		{
			if (prevOut == null)
				throw new ArgumentNullException(nameof(prevOut));
			_InputsByOutpoint.TryGetValue(prevOut, out var result);
			return result;
		}
	}

}
#nullable disable
