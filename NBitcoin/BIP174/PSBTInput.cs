using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using NBitcoin.DataEncoders;
using PartialSigKVMap = System.Collections.Generic.SortedDictionary<NBitcoin.PubKey, NBitcoin.TransactionSignature>;

namespace NBitcoin
{
	public class PSBTInput : PSBTCoin
	{
		// Those fields are not saved, but can be used as hint to solve more info for the PSBT
		internal Script originalScriptSig = Script.Empty;
		internal WitScript originalWitScript = Script.Empty;
		internal TxOut orphanTxOut = null; // When this input is not segwit, but we don't have the previous tx

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
						non_witness_utxo = this.GetConsensusFactory().CreateTransaction();
						non_witness_utxo.FromBytes(v);
						break;
					case PSBTConstants.PSBT_IN_WITNESS_UTXO:
						if (k.Length != 1)
							throw new FormatException("Invalid PSBTInput. Contains illegal value in key for WitnessUTXO");
						if (witness_utxo != null)
							throw new FormatException("Invalid PSBTInput. Duplicate witness_utxo");
						if (this.GetConsensusFactory().TryCreateNew<TxOut>(out var txout))
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
						var pubkey = new PubKey(k.Skip(1).ToArray());
						if (partial_sigs.ContainsKey(pubkey))
							throw new FormatException("Invalid PSBTInput. Duplicate key for partial_sigs");
						partial_sigs.Add(pubkey, new TransactionSignature(v));
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
					case PSBTConstants.PSBT_IN_BIP32_DERIVATION:
						var pubkey2 = new PubKey(k.Skip(1).ToArray());
						if (hd_keypaths.ContainsKey(pubkey2))
							throw new FormatException("Invalid PSBTInput. Duplicate key for hd_keypaths");
						var masterFingerPrint = new HDFingerprint(v.Take(4).ToArray());
						KeyPath path = KeyPath.FromBytes(v.Skip(4).ToArray());
						hd_keypaths.Add(pubkey2, new RootedKeyPath(masterFingerPrint, path));
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

		internal TxIn TxIn { get; }

		public OutPoint PrevOut => TxIn.PrevOut;

		public uint Index { get; }
		internal Transaction Transaction => Parent.tx;

		private Transaction non_witness_utxo;
		private TxOut witness_utxo;
		private Script final_script_sig;
		private WitScript final_script_witness;
		private PartialSigKVMap partial_sigs = new PartialSigKVMap(PubKeyComparer.Instance);

		SigHash? sighash_type;

		public Transaction NonWitnessUtxo
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

		public TxOut WitnessUtxo
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

		public Script FinalScriptSig
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

		public WitScript FinalScriptWitness
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



		public PartialSigKVMap PartialSigs
		{
			get
			{
				return partial_sigs;
			}
		}

		public override void AddKeyPath(PubKey key, RootedKeyPath rootedKeyPath)
		{
			base.AddKeyPath(key, rootedKeyPath);
			TrySlimUTXO();
		}


		public void UpdateFromCoin(ICoin coin)
		{
			if (coin == null)
				throw new ArgumentNullException(nameof(coin));
			if (IsFinalized())
				throw new InvalidOperationException("Impossible to modify the PSBTInput if it has been finalized");
			if (coin.Outpoint != PrevOut)
				throw new ArgumentException("This coin does not match the input", nameof(coin));
			if (IsFinalized())
				return;
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

			if (coin.GetHashVersion() == HashVersion.Witness || witness_script != null)
			{
				witness_utxo = coin.TxOut;
				non_witness_utxo = null;
			}
			else
			{
				orphanTxOut = coin.TxOut;
				witness_utxo = null;
			}
		}

		internal void Combine(PSBTInput other)
		{
			if (this.IsFinalized())
				return;

			foreach (var uk in other.unknown)
				unknown.TryAdd(uk.Key, uk.Value);


			if (other.final_script_sig != null)
				final_script_sig = other.final_script_sig;

			if (other.final_script_witness != null)
				final_script_witness = other.final_script_witness;
			if (IsFinalized())
			{
				ClearForFinalize();
				return;
			}

			if (non_witness_utxo == null && other.non_witness_utxo != null)
				non_witness_utxo = other.non_witness_utxo;

			if (witness_utxo == null && other.witness_utxo != null)
				non_witness_utxo = other.non_witness_utxo;

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
		/// This will not clear utxos since tx extractor might want to check the validity
		/// </summary>
		internal void ClearForFinalize()
		{
			this.redeem_script = null;
			this.witness_script = null;
			this.partial_sigs.Clear();
			this.hd_keypaths.Clear();
			this.sighash_type = null;
		}

		public override Coin GetSignableCoin(out string error)
		{
			if (witness_utxo == null && non_witness_utxo == null)
			{
				error = "Neither witness_utxo nor non_witness_output is set";
				return null;
			}
			return base.GetSignableCoin(out error);
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

			if (witness_utxo != null && non_witness_utxo != null)
				errors.Add(new PSBTError(Index, "witness utxo and non witness utxo simultaneously present"));

			if (witness_script != null && witness_utxo == null)
				errors.Add(new PSBTError(Index, "witness script present but no witness utxo"));

			if (final_script_witness != null && witness_utxo == null)
				errors.Add(new PSBTError(Index, "final witness script present but no witness utxo"));

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

		public void TrySign(IHDScriptPubKey accountHDScriptPubKey, IHDKey accountKey, RootedKeyPath accountKeyPath, SigHash sigHash = SigHash.All)
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
					Sign(k.PrivateKey, sigHash);
				else
					throw new ArgumentException(paramName: nameof(accountKey), message: "This should be a private key");
			}
		}

		public void TrySign(IHDScriptPubKey accountHDScriptPubKey, IHDKey accountKey, SigHash sigHash = SigHash.All)
		{
			TrySign(accountHDScriptPubKey, accountKey, null, sigHash);
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
			bs.ConsensusFactory = Parent.tx.GetConsensusFactory();
			this.Serialize(bs);
			return ms.ToArrayEfficient();
		}

		public virtual ConsensusFactory GetConsensusFactory() => Bitcoin.Instance.Mainnet.Consensus.ConsensusFactory;

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
				var formatter = new RPC.BlockExplorerFormatter();
				formatter.WriteTransaction2(jsonWriter, NonWitnessUtxo);
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
				case SigHash.Undefined:
					return "UNDEFINED";
				default:
					return sighashType.ToString();
			}
		}
		public TxOut GetTxOut()
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
		public bool TryFinalizeInput(out IList<PSBTError> errors)
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
			TransactionBuilder transactionBuilder = Parent.CreateTransactionBuilder();
			transactionBuilder.AddCoins(coin);
			foreach (var sig in PartialSigs)
			{
				transactionBuilder.AddKnownSignature(sig.Key, sig.Value, coin.Outpoint);
			}
			Transaction signed = null;
			try
			{
				var signedTx = Parent.Settings.IsSmart ? Parent.GetOriginalTransaction() : Transaction.Clone();
				signed = transactionBuilder.SignTransaction(signedTx, SigHash.All);
			}
			catch (Exception ex)
			{
				errors = new List<PSBTError>() { new PSBTError(Index, $"Error while finalizing the input \"{getSignableCoinError ?? ex.Message}\"") };
				return false;
			}
			var indexedInput = signed.Inputs.FindIndexedInput(coin.Outpoint);
			if (!indexedInput.VerifyScript(coin, out var error))
			{
				errors = new List<PSBTError>() { new PSBTError(Index, $"The finalized input script does not properly validate \"{error}\"") };
				return false;
			}

			FinalScriptSig = indexedInput.ScriptSig is Script oo && oo != Script.Empty ? oo : null;
			FinalScriptWitness = indexedInput.WitScript is WitScript o && o != WitScript.Empty ? o : null;
			if (transactionBuilder.FindSignableCoin(indexedInput) is ScriptCoin scriptCoin)
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

		public void FinalizeInput()
		{
			if (!TryFinalizeInput(out var errors))
				throw new PSBTException(errors);
		}

		public TransactionSignature Sign(Key key)
		{
			return Sign(key, SigHash.All);
		}
		public TransactionSignature Sign(Key key, SigHash sigHash)
		{
			CheckCompatibleSigHash(sigHash);
			if (PartialSigs.ContainsKey(key.PubKey))
			{
				var signature = PartialSigs[key.PubKey];
				if (sigHash != signature.SigHash)
					throw new InvalidOperationException("A signature with a different sighash is already in the partial sigs");
				return signature;
			}
			AssertSanity();
			var coin = GetSignableCoin();
			if (coin == null)
				return null;

			var builder = Parent.CreateTransactionBuilder();
			builder.AddCoins(coin);
			builder.AddKeys(key);
			if (builder.TrySignInput(Transaction, Index, sigHash, out var signature2))
			{
				this.PartialSigs.TryAdd(key.PubKey, signature2);
			}
			return signature2;
		}

		private void CheckCompatibleSigHash(SigHash sigHash)
		{
			if (SighashType is SigHash s && s != sigHash)
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

			if (coin.GetHashVersion() == HashVersion.Witness)
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

		public override Coin GetCoin()
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

		protected override PSBTHDKeyMatch CreateHDKeyMatch(IHDKey accountKey, KeyPath addressKeyPath, KeyValuePair<PubKey, RootedKeyPath> kv)
		{
			return new PSBTHDKeyMatch<PSBTInput>(this, accountKey, addressKeyPath, kv);
		}
	}

	public class PSBTInputList : PSBTCoinList<PSBTInput>
	{
		Dictionary<OutPoint, PSBTInput> _InputsByOutpoint = new Dictionary<OutPoint, PSBTInput>();


		internal void Add(PSBTInput input)
		{
			_Inner.Add(input);
			_InputsByOutpoint.Add(input.TxIn.PrevOut, input);
		}

		public PSBTInput FindIndexedInput(OutPoint prevOut)
		{
			if (prevOut == null)
				throw new ArgumentNullException(nameof(prevOut));
			_InputsByOutpoint.TryGetValue(prevOut, out var result);
			return result;
		}
	}

}
