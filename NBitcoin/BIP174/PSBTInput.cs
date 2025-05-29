#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using NBitcoin.DataEncoders;
using PartialSigKVMap = System.Collections.Generic.SortedDictionary<NBitcoin.PubKey, NBitcoin.TransactionSignature>;
using System.Diagnostics.CodeAnalysis;
using NBitcoin.BIP370;

namespace NBitcoin
{


	public abstract class PSBTInput : PSBTCoin
	{
		internal PSBTInput(PSBT parent, uint index) : base(parent)
		{
			Index = index;
		}

		internal PSBTInput(Map map, PSBT parent, uint index) : base(parent)
		{
			Index = index;
			if (map.TryRemove<byte[]>(PSBTConstants.PSBT_IN_NON_WITNESS_UTXO, out var b))
			{
				non_witness_utxo = Parent.GetConsensusFactory().CreateTransaction();
				non_witness_utxo.FromBytes(b);
			}
			if (map.TryRemove<byte[]>(PSBTConstants.PSBT_IN_WITNESS_UTXO, out b))
			{
				if (Parent.GetConsensusFactory().TryCreateNew<TxOut>(out var txout))
				{
					witness_utxo = txout;
				}
				else
				{
					witness_utxo = new TxOut();
				}

				witness_utxo.FromBytes(b);
			}

			foreach (var kv in map.RemoveAll<byte[]>(PSBTConstants.PSBT_IN_PARTIAL_SIG))
			{
				var pkbytes = kv.Key.Skip(1).ToArray();
				if (pkbytes.Length == 33)
				{
					var pubkey = new PubKey(pkbytes);
					if (partial_sigs.ContainsKey(pubkey))
						throw new FormatException("Invalid PSBTInput. Duplicate key for partial_sigs");
					partial_sigs.Add(pubkey, new TransactionSignature(kv.Value));
				}
				else
					throw new FormatException("Unexpected public key size in the PSBT");
			}

			if (map.TryRemove<byte[]>(PSBTConstants.PSBT_IN_SIGHASH, out b))
			{
				if (b.Length != 4)
					throw new FormatException("Invalid PSBTInput. SigHash Type is not 4 byte");
				var value = Utils.ToUInt32(b, 0, true);
				if (value is not (1 or 2 or 3 or 0 or 1 | 0x80 or 2 | 0x80 or 3 | 0x80 or 0 | 0x80))
					throw new FormatException($"Invalid PSBTInput Unknown SigHash Type {value}");
				sighash_type = value;
			}
			if (map.TryRemove<byte[]>(PSBTConstants.PSBT_IN_REDEEMSCRIPT, out b))
				redeem_script = Script.FromBytesUnsafe(b);
			if (map.TryRemove<byte[]>(PSBTConstants.PSBT_IN_WITNESSSCRIPT, out b))
				witness_script = Script.FromBytesUnsafe(b);
			if (map.TryRemove<byte[]>(PSBTConstants.PSBT_IN_TAP_KEY_SIG, out b))
			{
				if (!TaprootSignature.TryParse(b, out var sig))
					throw new FormatException("Invalid PSBTInput. Contains invalid TaprootSignature");
				TaprootKeySignature = sig;
			}
			if (map.TryRemove<byte[]>(PSBTConstants.PSBT_IN_TAP_INTERNAL_KEY, out b))
			{
				if (!TaprootInternalPubKey.TryCreate(b, out var tpk))
					throw new FormatException("Invalid PSBTInput. Contains invalid internal taproot pubkey");
				TaprootInternalKey = tpk;
			}
			foreach (var kv in map.RemoveAll<byte[]>(PSBTConstants.PSBT_IN_BIP32_DERIVATION))
			{
				var pubkey2 = new PubKey(kv.Key.Skip(1).ToArray());
				if (hd_keypaths.ContainsKey(pubkey2))
					throw new FormatException("Invalid PSBTInput. Duplicate key for hd_keypaths");
				var masterFingerPrint = new HDFingerprint(kv.Value.Take(4).ToArray());
				KeyPath path = KeyPath.FromBytes(kv.Value.Skip(4).ToArray());
				hd_keypaths.Add(pubkey2, new RootedKeyPath(masterFingerPrint, path));
			}
			foreach (var kv in map.RemoveAll<byte[]>(PSBTConstants.PSBT_IN_TAP_BIP32_DERIVATION))
			{
				var pubkey3 = new TaprootPubKey(kv.Key.Skip(1).ToArray());
				if (hd_taprootkeypaths.ContainsKey(pubkey3))
					throw new FormatException(
						"Invalid PSBTOutput, duplicate key for PSBT_IN_TAP_BIP32_DERIVATION");
				var bs = new BitcoinStream(kv.Value);
				List<uint256> hashes = null!;
				bs.ReadWrite(ref hashes);
				var pos = (int)bs.Inner.Position;
				KeyPath path2 = KeyPath.FromBytes(kv.Value.Skip(pos + 4).ToArray());
				hd_taprootkeypaths.Add(pubkey3,
					new TaprootKeyPath(
						new RootedKeyPath(new HDFingerprint(kv.Value.Skip(pos).Take(4).ToArray()), path2),
						hashes.ToArray()));
			}
			if (map.TryRemove<byte[]>(PSBTConstants.PSBT_IN_TAP_MERKLE_ROOT, out b))
			{
				if (b.Length != 32)
					throw new FormatException(
						"Invalid PSBTInput. Unexpected value length for PSBT_IN_TAP_MERKLE_ROOT");
				TaprootMerkleRoot = new uint256(b);
			}
			if (map.TryRemove<byte[]>(PSBTConstants.PSBT_IN_SCRIPTSIG, out b))
				FinalScriptSig = Script.FromBytesUnsafe(b);
			if (map.TryRemove<byte[]>(PSBTConstants.PSBT_IN_SCRIPTWITNESS, out b))
				FinalScriptWitness = new WitScript(b);
			unknown = map;
		}

		public abstract Sequence Sequence { get; set; }

		internal IndexedTxIn GetIndexedInput() => GetTransaction().Inputs.FindIndexedInput((int)Index);

		public abstract OutPoint PrevOut { get; }

		public uint Index { get; }
		internal Transaction GetTransaction() => Parent.GetGlobalTransaction(true);

		private Transaction? non_witness_utxo;
		// Record the hash of the last time we checked the non_witness_utxo correspond to the outpoint of the input
		// If NonWitnessUTXO change, we unset this.
		// If the Outpoint change, then non_witness_utxo_check != prevout.Hash
		// This is optimization to not have to calculate the hash of non_witness_utxo every time.
		internal uint256? non_witness_utxo_check;
		private TxOut? witness_utxo;
		private PartialSigKVMap partial_sigs = new PartialSigKVMap(PubKeyComparer.Instance);

		uint? sighash_type;

		public Transaction? NonWitnessUtxo
		{
			get
			{
				return non_witness_utxo;
			}
			set
			{
				non_witness_utxo = value;
				non_witness_utxo_check = null;
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
				if (sighash_type is null or 0)
					return null;
				return (SigHash)sighash_type;
			}
			set
			{
				sighash_type = value is null ? null : ((uint)value.Value);
			}
		}

		public TaprootSigHash? TaprootSighashType
		{
			get
			{
				return sighash_type is null ? null : (TaprootSigHash)sighash_type;
			}
			set
			{
				sighash_type = value is null ? null : ((uint)value.Value);
			}
		}

		public Script? FinalScriptSig { get; set; }

		public WitScript? FinalScriptWitness { get; set; }

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
					if (Parent.Settings.IsSmart && redeem_script == null && FinalScriptSig is not null)
					{
						var redeemScript = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(FinalScriptSig, coin.TxOut.ScriptPubKey)?.RedeemScript;
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
						if (witScriptId != null && FinalScriptWitness is not null)
						{
							var redeemScript = PayToWitScriptHashTemplate.Instance.ExtractWitScriptParameters(FinalScriptWitness, witScriptId);
							if (redeemScript != null)
							{
								witness_script = redeemScript;
							}
						}
					}
				}
			}
			witness_utxo = coin.TxOut;
			if (Parent.Settings.AutomaticUTXOTrimming)
				this.TrySlimUTXO();
			if (IsFinalized())
				ClearForFinalize();
		}

		/// <summary>
		/// Import informations contained by <paramref name="other"/> into this instance.
		/// </summary>
		/// <param name="other"></param>
		public virtual void UpdateFrom(PSBTInput other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			foreach (var uk in other.unknown)
				unknown.TryAdd(uk.Key, uk.Value);


			if (other.FinalScriptSig != null)
				FinalScriptSig = other.FinalScriptSig;

			if (other.FinalScriptWitness != null)
				FinalScriptWitness = other.FinalScriptWitness;

			if (non_witness_utxo == null && other.non_witness_utxo != null)
				non_witness_utxo = other.non_witness_utxo;

			if (witness_utxo == null && other.witness_utxo != null)
				witness_utxo = other.witness_utxo;

			if (sighash_type is null && other.sighash_type is not null)
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
				return this.GetTransaction().Inputs.FindIndexedInput((int)Index)
									   .GetSignatureHashTaproot(coin, sigHash, precomputedTransactionData);
			throw new InvalidOperationException("WitnessUtxo, NonWitnessUtxo, WitnessScript or redeemScript is required to get the signature hash");
		}
		public uint256 GetSignatureHash(SigHash sigHash, PrecomputedTransactionData? precomputedTransactionData)
		{
			if (GetSignableCoin() is ICoin coin)
				return this.GetTransaction().Inputs.FindIndexedInput((int)Index)
									   .GetSignatureHash(coin, sigHash, precomputedTransactionData);
			throw new InvalidOperationException("WitnessUtxo, NonWitnessUtxo, WitnessScript or redeemScript is required to get the signature hash");
		}

		public bool IsFinalized() => FinalScriptSig != null || FinalScriptWitness != null;

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

		internal Script? GetRedeemScript()
		{
			if (RedeemScript != null)
				return RedeemScript;
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

		internal Script? GetWitnessScript()
		{
			if (WitnessScript != null)
				return WitnessScript;
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

			if (FinalScriptWitness != null && witness_utxo is null && non_witness_utxo is null)
				errors.Add(new PSBTError(Index, "final witness script present but not witness_utxo or non_witness_utxo"));

			if (NonWitnessUtxo != null)
			{
				var prevOutTxId = NonWitnessUtxo.GetHash();
				bool validOutpoint = true;
				if (PrevOut.Hash != prevOutTxId)
				{
					errors.Add(new PSBTError(Index, "non_witness_utxo does not match the transaction id referenced by the global transaction sign"));
					validOutpoint = false;
				}
				else
					non_witness_utxo_check = prevOutTxId;
				if (PrevOut.N >= NonWitnessUtxo.Outputs.Count)
				{
					errors.Add(new PSBTError(Index, "Global transaction referencing an out of bound output in non_witness_utxo"));
					validOutpoint = false;
				}
				if (redeem_script != null && validOutpoint)
				{
					if (redeem_script.Hash.ScriptPubKey != NonWitnessUtxo.Outputs[PrevOut.N].ScriptPubKey)
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

			if (IsNonWitnessUTXOMissing() is true)
				errors.Add(new PSBTError(Index, "A NonWitnessUtxo is required but hasn't been provided"));
			return errors;
		}

		public bool? IsNonWitnessUTXOMissing()
		{
			if (Parent.Network.Consensus.NeverNeedPreviousTxForSigning || non_witness_utxo is not null)
				return false;
			if (witness_utxo?.ScriptPubKey is not Script s)
				return null;
			var p2sh = s.IsScriptType(ScriptType.P2SH);
			var needNonWitnessUTXO = p2sh && redeem_script is Script r && !r.IsScriptType(ScriptType.Witness);
			needNonWitnessUTXO |= !p2sh && !s.IsScriptType(ScriptType.Witness);
			return needNonWitnessUTXO;
		}

		public void TrySign(IHDScriptPubKey accountHDScriptPubKey, IHDKey accountKey, RootedKeyPath? accountKeyPath)
		{
			TrySign(accountHDScriptPubKey, accountKey, accountKeyPath, null);
		}
		internal void TrySign(IHDScriptPubKey? accountHDScriptPubKey, IHDKey accountKey, RootedKeyPath? accountKeyPath, SigningOptions? signingOptions)
		{
			if (accountKey == null)
				throw new ArgumentNullException(nameof(accountKey));
			if (IsFinalized())
				return;
			var cache = accountKey.AsHDKeyCache();
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

		protected static uint defaultKeyLen = 1;

		internal virtual void FillMap(Map map)
		{
			if (non_witness_utxo != null)
				map.Add([PSBTConstants.PSBT_IN_NON_WITNESS_UTXO], non_witness_utxo.ToBytes());

			if (witness_utxo != null)
				map.Add([PSBTConstants.PSBT_IN_WITNESS_UTXO], witness_utxo.ToBytes());

			if (sighash_type is not null)
				map.Add([PSBTConstants.PSBT_IN_SIGHASH], (uint)sighash_type.Value);

			if (redeem_script != null)
				map.Add([PSBTConstants.PSBT_IN_REDEEMSCRIPT], redeem_script.ToBytes());

			if (witness_script != null)
				map.Add([PSBTConstants.PSBT_IN_WITNESSSCRIPT], witness_script.ToBytes());

			if (this.TaprootMerkleRoot is uint256 merkleRoot)
				map.Add([PSBTConstants.PSBT_IN_TAP_MERKLE_ROOT], TaprootMerkleRoot.ToBytes());
			
			if (this.TaprootInternalKey is TaprootInternalPubKey tp)
				map.Add([PSBTConstants.PSBT_IN_TAP_INTERNAL_KEY], tp.ToBytes());

			if (this.TaprootKeySignature is TaprootSignature tsig)
				map.Add([PSBTConstants.PSBT_IN_TAP_KEY_SIG], tsig.ToBytes());

			// Write any partial signatures
			foreach (var sig_pair in partial_sigs)
			{
				var key = new byte[] { PSBTConstants.PSBT_IN_PARTIAL_SIG }.Concat(sig_pair.Key.ToBytes());
				map.Add(key, sig_pair.Value.ToBytes());
			}

			// Write any hd keypaths
			foreach (var pathPair in hd_keypaths)
			{
				var key = new byte[] { PSBTConstants.PSBT_IN_BIP32_DERIVATION }.Concat(pathPair.Key.ToBytes());
				var masterFingerPrint = pathPair.Value.MasterFingerprint;
				var path = pathPair.Value.KeyPath.ToBytes();
				var pathInfo = masterFingerPrint.ToBytes().Concat(path);
				map.Add(key, pathInfo);
			}
			foreach (var pathPair in hd_taprootkeypaths)
			{
				var key = new byte[] { PSBTConstants.PSBT_IN_TAP_BIP32_DERIVATION }.Concat(pathPair.Key.ToBytes());
				uint leafCount = (uint)pathPair.Value.LeafHashes.Length;
				BitcoinStream bs = new BitcoinStream(new MemoryStream(), true);
				bs.ReadWriteAsVarInt(ref leafCount);
				foreach (var hash in pathPair.Value.LeafHashes)
				{
					bs.ReadWrite(hash);
				}
				var b = pathPair.Value.RootedKeyPath.MasterFingerprint.ToBytes();
				bs.ReadWrite(b);
				b = pathPair.Value.RootedKeyPath.KeyPath.ToBytes();
				bs.ReadWrite(b);
				b = ((MemoryStream)bs.Inner).ToArrayEfficient();
				map.Add(key, b);
			}

			if (FinalScriptSig != null)
				map.Add([PSBTConstants.PSBT_IN_SCRIPTSIG], FinalScriptSig.ToBytes());

			// write script witness
			if (FinalScriptWitness != null)
				map.Add([PSBTConstants.PSBT_IN_SCRIPTWITNESS], FinalScriptWitness.ToBytes());

			// Write unknown things
			foreach (var kv in unknown)
				map.Add(kv.Key, kv.Value);
		}

		#endregion

		public byte[] ToBytes()
		{
			Map m = new();
			this.FillMap(m);
			return m.ToBytes();
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
			if (sighash_type is uint s)
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
			WriteCore(jsonWriter);
			jsonWriter.WriteBIP32Derivations(this.hd_keypaths);
			jsonWriter.WriteBIP32Derivations(this.hd_taprootkeypaths);
			jsonWriter.WriteEndObject();
		}

		protected virtual void WriteCore(JsonTextWriter jsonWriter)
		{
		}

		private string GetName(uint sighashType)
		{
			switch (sighashType)
			{
				case 0:
					return "DEFAULT";
				case 1:
					return "ALL";
				case 2:
					return "NONE";
				case 3:
					return "SINGLE";
				case 0x80:
					return "DEFAULT|ANYONECANPAY";
				case 1 | 0x80:
					return "ALL|ANYONECANPAY";
				case 2 | 0x80:
					return "NONE|ANYONECANPAY";
				case 3 | 0x80:
					return "SINGLE|ANYONECANPAY";
				default:
					return sighashType.ToString();
			}
		}
		public override TxOut? GetTxOut()
		{
			if (NonWitnessUtxo != null)
			{
				if (PrevOut.N >= NonWitnessUtxo.Outputs.Count)
					return null;
				if (non_witness_utxo_check != PrevOut.Hash)
				{
					if (NonWitnessUtxo.GetHash() != PrevOut.Hash)
						return null;
					non_witness_utxo_check = PrevOut.Hash;
				}
				non_witness_utxo_check = PrevOut.Hash;
				return NonWitnessUtxo.Outputs[PrevOut.N];
			}
			if (WitnessUtxo != null)
				return WitnessUtxo;
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
					transactionBuilder.ExtractSignatures(finalizedInput.PSBT, Parent.ForceExtractTransaction());
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

		public bool VerifyScript(PrecomputedTransactionData? precomputedTransactionData, out ScriptError err) => VerifyScript(Parent.Settings.ScriptVerify, precomputedTransactionData, out err);
		public bool VerifyScript(ScriptVerify scriptVerify, PrecomputedTransactionData? precomputedTransactionData, out ScriptError err)
		{
			var eval = new ScriptEvaluationContext
			{
				ScriptVerify = scriptVerify
			};
			if (GetTransaction() is IHasForkId)
				eval.ScriptVerify |= NBitcoin.ScriptVerify.ForkId;
			var txout = GetTxOut();
			if (txout is null)
			{
				err = ScriptError.UnknownError;
				return false;
			}
			var checker = new TransactionChecker(GetTransaction(), (int)Index, GetTxOut(), precomputedTransactionData);
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
#if HAS_SPAN
				var sigHash = keyPair is TaprootKeyPair ? (uint)signingOptions.TaprootSigHash : (uint)signingOptions.SigHash;
#else
				var sigHash = (uint)signingOptions.SigHash;
#endif
				CheckCompatibleSigHash(sigHash);
				var signature = PartialSigs[ecdsapk];

				var existingSigHash = (uint)existingSig.SigHash;
				if (GetTransaction() is IHasForkId)
					existingSigHash = existingSigHash & ~(0x40u);
				if (!SameSigHash(existingSigHash, sigHash))
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

		bool SameSigHash(uint a, uint b)
		{
			if (a == b)
				return true;
			if (GetTransaction() is not IHasForkId)
				return false;
			a = ((uint)a & ~(0x40u));
			b = ((uint)a & ~(0x40u));
			return a == b;
		}
		private void CheckCompatibleSigHash(uint sigHash)
		{
			if (this.sighash_type is uint s && !SameSigHash(s,sigHash))
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
					if (PrevOut.N < NonWitnessUtxo.Outputs.Count)
						WitnessUtxo = NonWitnessUtxo.Outputs[PrevOut.N];
				}
				NonWitnessUtxo = null;
				return true;
			}
			WitnessUtxo = null;
			return false;
		}

		/// <summary>
		/// Represent this input as a coin.
		/// Returns null if <see cref="WitnessUtxo"/> or <see cref="NonWitnessUtxo"/> is not set.
		/// </summary>
		/// <returns>The input as a coin</returns>
		public Coin? GetCoin()
		{
			var txout = GetTxOut();
			if (txout == null)
				return null;
			return new Coin(PrevOut, txout);
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

		public Coin? GetSignableCoin()
		{
			return GetSignableCoin(out _);
		}
		public virtual Coin? GetSignableCoin(out string? error)
		{
			if (witness_utxo is null && non_witness_utxo is null)
			{
				error = "Neither witness_utxo nor non_witness_output is set";
				return null;
			}
			var coin = GetCoin();
			if (coin == null)
			{
				error = "Impossible to know the TxOut this coin refers to";
				return null;
			}
			if (PayToScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(coin.ScriptPubKey) is ScriptId scriptId)
			{
				var redeemScript = GetRedeemScript();
				if (redeemScript == null)
				{
					error = "Spending p2sh output but redeem_script is not set";
					return null;
				}

				if (redeemScript.Hash != scriptId)
				{
					error = "Spending p2sh output but redeem_script is not matching the utxo scriptPubKey";
					return null;
				}

				if (PayToWitTemplate.Instance.ExtractScriptPubKeyParameters2(redeemScript) is WitProgramParameters prog
					&& prog.NeedWitnessRedeemScript())
				{
					var witnessScript = GetWitnessScript();
					if (witnessScript == null)
					{
						error = "Spending p2sh-p2wsh output but witness_script is not set";
						return null;
					}
					if (!prog.VerifyWitnessRedeemScript(witnessScript))
					{
						error = "Spending p2sh-p2wsh output but witness_script does not match redeem_script";
						return null;
					}
					coin = coin.ToScriptCoin(witnessScript);
					error = null;
					return coin;
				}
				else
				{
					coin = coin.ToScriptCoin(redeemScript);
					error = null;
					return coin;
				}
			}
			else
			{
				if (GetRedeemScript() != null)
				{
					error = "Spending non p2sh output but redeem_script is set";
					return null;
				}
				if (PayToWitTemplate.Instance.ExtractScriptPubKeyParameters2(coin.ScriptPubKey) is WitProgramParameters prog
					&& prog.NeedWitnessRedeemScript())
				{
					var witnessScript = GetWitnessScript();
					if (witnessScript == null)
					{
						error = "Spending p2wsh output but witness_script is not set";
						return null;
					}
					if (!prog.VerifyWitnessRedeemScript(witnessScript))
					{
						error = "Spending p2wsh output but witness_script does not match the scriptPubKey";
						return null;
					}
					coin = coin.ToScriptCoin(witnessScript);
					error = null;
					return coin;
				}
				else
				{
					error = null;
					return coin;
				}
			}
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
			if (!_InputsByOutpoint.TryAdd(input.PrevOut, input))
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
