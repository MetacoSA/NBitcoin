using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;

namespace NBitcoin.BIP174
{
	using HDKeyPathKVMap = Dictionary<PubKey, byte[]>;
	using PartialSigKVMap = Dictionary<KeyId, Tuple<PubKey, ECDSASignature>>;
	using UnknownKVMap = Dictionary<byte[], byte[]>;
	static class PSBTConstants
	{
		public static byte[] PSBT_GLOBAL_ALL { get; }
		public static byte[] PSBT_IN_ALL { get; }
		public static byte[] PSBT_OUT_ALL { get; }
		static PSBTConstants()
		{
			PSBT_GLOBAL_ALL = new byte[] { PSBT_GLOBAL_UNSIGNED_TX };
			PSBT_IN_ALL = new byte[]
				{
					PSBT_IN_NON_WITNESS_UTXO,
					PSBT_IN_WITNESS_UTXO,
					PSBT_IN_PARTIAL_SIG,
					PSBT_IN_SIGHASH,
					PSBT_IN_REDEEMSCRIPT,
					PSBT_IN_WITNESSSCRIPT,
					PSBT_IN_SCRIPTSIG,
					PSBT_IN_SCRIPTWITNESS,
					PSBT_IN_BIP32_DERIVATION
				};
			PSBT_OUT_ALL = new byte[] {
				PSBT_OUT_REDEEMSCRIPT,
				PSBT_OUT_WITNESSSCRIPT,
				PSBT_OUT_BIP32_DERIVATION
			};
		}
		// Note: These constants are in reverse byte order because serialization uses LSB
		// Global types
		public const byte PSBT_GLOBAL_UNSIGNED_TX = 0x00;

		// Input types
		public const byte PSBT_IN_NON_WITNESS_UTXO = 0x00;
		public const byte PSBT_IN_WITNESS_UTXO = 0x01;
		public const byte PSBT_IN_PARTIAL_SIG = 0x02;
		public const byte PSBT_IN_SIGHASH = 0x03;
		public const byte PSBT_IN_REDEEMSCRIPT = 0x04;
		public const byte PSBT_IN_WITNESSSCRIPT = 0x05;
		public const byte PSBT_IN_BIP32_DERIVATION = 0x06;
		public const byte PSBT_IN_SCRIPTSIG = 0x07;
		public const byte PSBT_IN_SCRIPTWITNESS = 0x08;

		// Output types
		public const byte PSBT_OUT_REDEEMSCRIPT = 0x00;
		public const byte PSBT_OUT_WITNESSSCRIPT = 0x01;
		public const byte PSBT_OUT_BIP32_DERIVATION = 0x02;

		// The separator is 0x00. Reading this in means that the unserializer can interpret it
		// as a 0 length key which indicates that this is the separator. The separator has no value.
		public const byte PSBT_SEPARATOR = 0x00;
	}

	public class PSBTInput : IBitcoinSerializable
	{
		private Transaction non_witness_utxo;
		private TxOut witness_utxo;
		private Script redeem_script;
		private Script witness_script;
		private Script final_script_sig;
		private WitScript final_script_witness;

		private HDKeyPathKVMap hd_keypaths;
		private PartialSigKVMap partial_sigs;
		private UnknownKVMap unknown;
		int sighash_type = 0;

		// Signatures which does not know which pubkey corresponds to him.
		private List<ECDSASignature> OrphanPartialSigs = new List<ECDSASignature>();
		private List<PubKey> OrphanPubKeys = new List<PubKey>();
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

		public Script RedeemScript
		{
			get
			{
				return redeem_script;
			}
			set
			{
				redeem_script = value;
			}
		}

		public Script WitnessScript
		{
			get
			{
				return witness_script;
			}
			set
			{
				witness_script = value;
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

		public HDKeyPathKVMap HDKeyPaths
		{
			get
			{
				return hd_keypaths;
			}
		}

		public PartialSigKVMap PartialSigs
		{
			get
			{
				return partial_sigs;
			}
		}

		public UnknownKVMap Unknown
		{
			get
			{
				return unknown;
			}
		}

		public PSBTInput()
		{
			SetUp();
		}

		private void SetUp()
		{
			hd_keypaths = new HDKeyPathKVMap();
			partial_sigs = new PartialSigKVMap();
			unknown = new UnknownKVMap();
		}

		public PSBTInput(TxIn txin)
		{
			if (txin == null)
			{
				throw new ArgumentNullException(nameof(txin));
			}
			SetUp();

			DeconstructScripts(txin);
		}

		private void DeconstructScripts(TxIn txin)
		{
			var ScriptSig = txin.ScriptSig;
			var witScript = txin.WitScript;

			// p2pkh
			var P2PKHIngredients = PayToPubkeyHashTemplate.Instance.ExtractScriptSigParameters(ScriptSig);
			if (P2PKHIngredients != null)
			{
				if (P2PKHIngredients.TransactionSignature != null) // already finalized
				{
					final_script_sig = ScriptSig;
				}
				// do not store anything if it is not finalized.
				return;
			}

			// p2sh
			var P2SHIngredients = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(ScriptSig);
			if (P2SHIngredients != null)
			{
				redeem_script = P2SHIngredients.RedeemScript;

				TransactionSignature[] txSigs = P2SHIngredients.GetMultisigSignatures();
				if (txSigs != null)
				{
					var sigHashes = txSigs.Select(sig => sig.SigHash);
					if (sigHashes.Any(i => i != sigHashes.First()))
						throw new InvalidDataException("All signatures in input must have a same sighash type.");
					sighash_type = txSigs.Select(sig => (int)sig.SigHash).First();
					this.OrphanPartialSigs = txSigs.Select(txSig => txSig.Signature).ToList();

				}

				OrphanPubKeys.AddRange(ExtractPubKeyFromScript(redeem_script));

				// Do not return here. Since it may be p2sh-nested-witness.
			}

			// p2wpkh
			var P2WPKHIngredients = PayToWitPubKeyHashTemplate.Instance.ExtractWitScriptParameters(witScript);
			if (P2WPKHIngredients != null)
			{
				if (P2WPKHIngredients.TransactionSignature != null) // already finalized
				{
					if (redeem_script != null) // p2sh-p2wpkh
					{
						final_script_sig = redeem_script.Clone();
						redeem_script = null;
					}
					final_script_witness = witScript;
				}
				// do not store anything if it is not finalized.
				return;
			}

			// p2wsh
			witness_script = PayToWitScriptHashTemplate.Instance.ExtractWitScriptParameters(witScript);
			if (witness_script != null)
			{
				// This may be slow. But the performance does not really matter for PSBT in many case.
				foreach (byte[] item in witScript.Pushes)
				{
					if (TransactionSignature.IsValid(item))
					{
						OrphanPartialSigs.Add(new TransactionSignature(item).Signature);
					}
				}
				OrphanPubKeys.AddRange(ExtractPubKeyFromScript(witness_script));
				return;
			}
		}

		private List<PubKey> ExtractPubKeyFromScript(Script script) =>
			script.ToOps().Where(op => op.PushData != null && PubKey.Check(op.PushData, true)).Select(op => new PubKey(op.PushData)).ToList();

		internal void AddCoin(ICoin coin)
		{
			if (coin is ScriptCoin)
			{
				var sCoin = (coin as ScriptCoin);
				if (sCoin.RedeemType == RedeemType.P2SH) // p2sh, p2sh-p2wpkh
				{
					redeem_script = sCoin.Redeem;
				}
				else // p2wsh, p2sh-p2wsh
				{
					witness_script = sCoin.Redeem;
					if (sCoin.IsP2SH)
						redeem_script = witness_script.WitHash.ScriptPubKey;
				}
			}

			// Why we need this check? because we should never add witness_utxo to non_segwit input.
			if (
				PayToWitTemplate.Instance.CheckScriptPubKey(coin.TxOut.ScriptPubKey) ||
				(PayToScriptHashTemplate.Instance.CheckScriptPubKey(coin.TxOut.ScriptPubKey) && redeem_script != null && PayToWitTemplate.Instance.CheckScriptPubKey(redeem_script)))
			{
				witness_utxo = coin.TxOut;
			}
		}

		/// <summary>
		/// When first this is instantiated from TxIn, There are no ways to know which sig corresponds to
		/// which pubkey so it will set sigs to OrphanPartialSigs. PSBT will call this method when it is sure
		/// That this PSBTInput has witness_utxo, and thus we can match the sig and pubkey.
		/// </summary>
		/// <param name="globalTx"></param>
		internal void MoveOrphansToPartialSigs(Transaction globalTx)
		{ }

		private bool IsRelatedKey(PubKey pk, Script ScriptPubKey) =>
			OrphanPubKeys.Any(k => k.Equals(pk)) || // key was in script or ...
				pk.Hash.ScriptPubKey.Equals(ScriptPubKey) || // matches as p2pkh
				pk.WitHash.ScriptPubKey.Equals(ScriptPubKey) || // as p2wpkh
				pk.WitHash.ScriptPubKey.Equals(redeem_script); // as p2sh-p2wpkh


		// Wny not just use `Transaction.Sign()` ? Because we must pass sighash_type somehow.
		// TODO: Using Transaction Builder here is ugly. It must be somewhere a method just for creating signature.
		private void SignTx(ref Transaction tx, Key key, ICoin coin)
		{
			TransactionBuilder builder = this.GetConsensusFactory().CreateTransactionBuilder();
			builder.AddKeys(key)
				.AddCoins(coin)
				.SignTransactionInPlace(tx, sighash_type != 0 ? (SigHash)sighash_type : SigHash.All);
		}
		internal bool Sign(int index, Transaction tx, Key[] keys)
		{
			var txin = tx.Inputs[index];
			CheckSanityForSigner(txin);
			var outpoint = txin.PrevOut;
			var prevout = this.GetOutput(outpoint);
			if (prevout == null) // no way we can sign without utxo.
				return false;
			var coin = new Coin(outpoint, prevout);

			// non_witness_utxo is necessary for non_segwit case.
			if (
				PayToPubkeyHashTemplate.Instance.CheckScriptPubKey(prevout.ScriptPubKey) || // p2pkh
				(PayToScriptHashTemplate.Instance.CheckScriptPubKey(prevout.ScriptPubKey) &&
					!PayToWitTemplate.Instance.CheckScriptPubKey(redeem_script)) // bare p2sh
			)
			{
				if (non_witness_utxo == null)
					return false;
			}

			bool result = false;
			var dummyTx = tx.Clone();
			foreach (var key in keys)
			{
				var nextScript = prevout.ScriptPubKey;
				if (!IsRelatedKey(key.PubKey, nextScript))
					continue;

				if (partial_sigs.ContainsKey(key.PubKey.Hash))
					continue; // already holds signature.

				// 1. p2pkh
				if (PayToPubkeyHashTemplate.Instance.CheckScriptPubKey(nextScript))
				{
					SignTx(ref dummyTx, key, coin);
				}

				// 2. p2sh
				else if (nextScript.IsPayToScriptHash)
				{
					if (witness_script == null)
					{
						var scriptCoin = new ScriptCoin(coin, redeem_script);
						SignTx(ref dummyTx, key, scriptCoin);
					}
					nextScript = redeem_script;
				}

				// 3. p2wsh
				if (PayToWitScriptHashTemplate.Instance.CheckScriptPubKey(nextScript))
				{
					var scriptCoin = new ScriptCoin(coin, witness_script);
					SignTx(ref dummyTx, key, scriptCoin);
				}
				// 4. p2wpkh
				else if (PayToWitPubKeyHashTemplate.Instance.CheckScriptPubKey(nextScript))
				{
					// var scriptCoin = new ScriptCoin(coin, key.PubKey.WitHash.ScriptPubKey);
					SignTx(ref dummyTx, key, coin);
				}

				var sig = GetRawSigFromTxIn(dummyTx.Inputs[index]);
				if (sig != null)
				{
					result = true;
					partial_sigs.Add(key.PubKey.Hash, Tuple.Create(key.PubKey, sig));
				}
			}
			return result;
		}

		private ECDSASignature GetRawSigFromTxIn(TxIn txin)
		{
			var item1 = txin.ScriptSig
				.Clone()
				.ToOps()
				.Where(op => op.PushData != null)
				.Select(op => op.PushData);

			foreach (var i in item1)
			{
				if (TransactionSignature.IsValid(i))
					return new TransactionSignature(i).Signature;
			}

			var item2 = txin.WitScript.Pushes;
			foreach (var i in item2)
			{
				if (TransactionSignature.IsValid(i))
					return new TransactionSignature(i).Signature;
			}

			return null;
		}

		public bool IsFinalized() => final_script_sig != null || final_script_witness != null;

		public void Finalize(Transaction tx, int index)
		{
			if (IsFinalized())
				return;

			var prevout = GetOutput(tx.Inputs[index].PrevOut);
			if (prevout == null)
				throw new InvalidOperationException("Can not finalize PSBTInput without utxo");

			var nextScript = prevout.ScriptPubKey;
			// 1. p2pkh
			if (PayToPubkeyHashTemplate.Instance.CheckScriptPubKey(nextScript))
			{
				var sigPair = partial_sigs.First();
				var txSig = new TransactionSignature(sigPair.Value.Item2, sighash_type == 0 ? SigHash.All : (SigHash)sighash_type);
				final_script_sig = PayToPubkeyHashTemplate.Instance.GenerateScriptSig(txSig, sigPair.Value.Item1);
			}

			// 2. p2sh
			else if (nextScript.IsPayToScriptHash)
			{
				// bare p2sh
				if (witness_script == null && !PayToWitTemplate.Instance.CheckScriptPubKey(redeem_script))
				{
					var sigPushes = GetSigPushes(redeem_script);
					var ss = PayToScriptHashTemplate.Instance.GenerateScriptSig(sigPushes, redeem_script);
					final_script_sig = ss;
				}
				// Why not create `final_script_sig` here? because if the following code throws an error, it will be left out dirty.
				nextScript = redeem_script;
			}

			// 3. p2wpkh
			if (PayToWitPubKeyHashTemplate.Instance.CheckScriptPubKey(nextScript))
			{
				var sigPair = partial_sigs.First();
				var txSig = new TransactionSignature(sigPair.Value.Item2, sighash_type == 0 ? SigHash.All : (SigHash)sighash_type);
				final_script_witness = PayToWitPubKeyHashTemplate.Instance.GenerateWitScript(txSig, sigPair.Value.Item1);

				if (prevout.ScriptPubKey.IsPayToScriptHash)
					final_script_sig = new Script(Op.GetPushOp(redeem_script.ToBytes()));
			}

			// 4. p2wsh
			else if (PayToWitScriptHashTemplate.Instance.CheckScriptPubKey(nextScript))
			{
				var sigPushes = GetSigPushes(witness_script);
				final_script_witness = PayToWitScriptHashTemplate.Instance.GenerateWitScript(sigPushes, witness_script);

				if (prevout.ScriptPubKey.IsPayToScriptHash)
					final_script_sig = new Script(Op.GetPushOp(redeem_script.ToBytes()));
			}
			ClearForFinalize();
		}
		public bool TryFinalize(Transaction tx, int index)
		{
			try
			{
				Finalize(tx, index);
			}
			catch
			{
				return false;
			}
			return true;
		}

		private Op[] GetSigPushes(Script redeem)
		{
			var sigPushes = partial_sigs.Values
				.Select(v => new TransactionSignature(v.Item2, sighash_type == 0 ? SigHash.All : (SigHash)sighash_type))
				.Select(txSig => Op.GetPushOp(txSig.ToBytes()))
				.ToArray();

			// check sig is more than m in case of p2multisig.
			var multiSigParam = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(redeem);
			if (multiSigParam != null && sigPushes.Length < multiSigParam.SignatureCount)
				throw new InvalidOperationException("Not enough signatures to finalize.");

			return sigPushes;
		}

		/// <summary>
		/// This will not clear utxos since tx extractor might want to check the validity
		/// </summary>
		private void ClearForFinalize()
		{
			this.redeem_script = null;
			this.witness_script = null;
			this.partial_sigs.Clear();
			this.hd_keypaths.Clear();
			this.sighash_type = 0;
		}

		public void CheckSanity()
		{
			var result = IsSane();
			var isSane = result.Item1;
			var reason = result.Item2;
			if (!isSane)
				throw new FormatException(reason);
		}

		private Tuple<bool, string> IsSane()
		{
			if (this.IsFinalized())
				if (partial_sigs.Count != 0 || hd_keypaths.Count != 0 || sighash_type != 0 || redeem_script != null || witness_script != null)
					return Tuple.Create(false, "PSBT Input is dirty. It has been finalized but properties are not cleared");
			return Tuple.Create(true, "");
		}

		internal void CheckSanityForSigner(TxIn txin)
		{
			var result = IsSaneForSigner(txin);
			var isSane = result.Item1;
			var reason = result.Item2;
			if (!isSane)
				throw new FormatException(reason);
		}

		private Tuple<bool, string> IsSaneForSigner(TxIn txin)
		{
			// Tests for signer.
			var prevout = GetOutput(txin.PrevOut);
			if (prevout != null)
			{
				if (
					PayToPubkeyHashTemplate.Instance.CheckScriptPubKey(prevout.ScriptPubKey) || // p2pkh
					(PayToScriptHashTemplate.Instance.CheckScriptPubKey(prevout.ScriptPubKey) &&
						!PayToWitTemplate.Instance.CheckScriptPubKey(RedeemScript)) // bare p2sh
				)
				{
					if (NonWitnessUtxo == null)
						return Tuple.Create(false, "malformed PSBTInput for Signer! witness_utxo for non_witness_output");
				}

				var nextScript = prevout.ScriptPubKey;
				if (redeem_script != null)
				{
					if (redeem_script.Hash != PayToScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(prevout.ScriptPubKey))
						return Tuple.Create(false, "malformed PSBTInput for Signer! redeem_script hash does not match to actual hash scriptPubKey");

					nextScript = redeem_script;
				}

				if (witness_script != null)
				{
					var scriptId = PayToWitTemplate.Instance.ExtractScriptPubKeyParameters(nextScript);
					if (scriptId == null || scriptId != witness_script.WitHash)
						return Tuple.Create(false, "malformed PSBTInput for Signer! witness_script hash does not match to actual scriptPubKey");
				}
			}

			return Tuple.Create(true, "");
		}

		internal TxOut GetOutput(OutPoint prevout)
		{
			if (witness_utxo != null)
				return witness_utxo;
			if (non_witness_utxo != null && prevout != null)
				return non_witness_utxo.Outputs[prevout.N];
			return null;
		}

		#region IBitcoinSerializable Members
		public void ReadWrite(BitcoinStream stream)
		{
			if (stream.Serializing)
			{
				Serialize(stream);
			}
			else
			{
				Deserialize(stream);
			}
		}

		private static uint defaultKeyLen = 1;

		public void Serialize(BitcoinStream stream)
		{
			CheckSanity();
			// Write the utxo
			// If there is a non-witness utxo, then don't add the witness one.
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
			else if (non_witness_utxo != null)
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
				uint valueLength = 1;
				stream.ReadWriteAsVarInt(ref valueLength);
				var tmp = (byte)sighash_type;
				stream.ReadWrite(ref tmp);
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
				var key = new byte[] { PSBTConstants.PSBT_IN_PARTIAL_SIG }.Concat(sig_pair.Value.Item1.ToBytes());
				stream.ReadWriteAsVarString(ref key);
				var sig = sig_pair.Value.Item2.ToDER();
				stream.ReadWriteAsVarString(ref sig);
			}

			// Write any hd keypaths
			foreach (var pathPair in hd_keypaths)
			{
				var key = new byte[] { PSBTConstants.PSBT_IN_BIP32_DERIVATION }.Concat(pathPair.Key.ToBytes());
				stream.ReadWriteAsVarString(ref key);
				var path = pathPair.Value;
				stream.ReadWriteAsVarString(ref path);
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

		public void Deserialize(BitcoinStream stream)
		{
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
						if (partial_sigs.ContainsKey(pubkey.Hash))
							throw new FormatException("Invalid PSBTInput. Duplicate key for partial_sigs");
						partial_sigs.Add(pubkey.Hash, Tuple.Create(pubkey, ECDSASignature.FromDER(v)));
						break;
					case PSBTConstants.PSBT_IN_SIGHASH:
						if (k.Length != 1)
							throw new FormatException("Invalid PSBTInput. Contains illegal value in key for SigHash type");
						if (sighash_type != 0)
							throw new FormatException("Invalid PSBTInput. Duplicate key for sighash_type");
						sighash_type = (int)v[0];
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
						hd_keypaths.Add(pubkey2, v);
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
			CheckSanity();
		}
		#endregion

		public override bool Equals(object obj)
		{
			var item = obj as PSBTInput;
			if (item == null)
				return false;
			return item.Equals(this);
		}

		public bool Equals(PSBTInput other) => other != null && this.ToBytes().SequenceEqual(other.ToBytes());

		public override int GetHashCode() => Utils.GetHashCode(this.ToBytes());

		public virtual ConsensusFactory GetConsensusFactory() => Bitcoin.Instance.Mainnet.Consensus.ConsensusFactory;

		public override string ToString()
		{
			return "Not Implemented";
			// return JsonConvert.SerializeObject(this);
		}
	}

	public class PSBTInputList : UnsignedList<PSBTInput>
	{
		public PSBTInputList() { }
		public PSBTInputList(Transaction globalTx) : base(globalTx) { }

		public PSBTInput CreateNewPSBTInput()
		{
			PSBTInput psbtin;
			if (!Transaction.GetConsensusFactory().TryCreateNew(out psbtin))
				psbtin = new PSBTInput();

			return psbtin;
		}
		public new PSBTInput Add(PSBTInput item)
		{
			base.Add(item);
			return item;
		}
	}

	public class PSBTOutput : IBitcoinSerializable
	{
		private Script redeem_script;
		private Script witness_script;
		private HDKeyPathKVMap hd_keypaths;
		private UnknownKVMap unknown;

		private static uint defaultKeyLen = 1;

		public Script RedeemScript
		{
			get
			{
				return redeem_script;
			}
			set
			{
				redeem_script = value;
			}
		}

		public Script WitnessScript
		{
			get
			{
				return witness_script;
			}
			set
			{
				witness_script = value;
			}
		}

		public HDKeyPathKVMap HDKeyPaths
		{
			get
			{
				return hd_keypaths;
			}
		}

		public UnknownKVMap Unknown
		{
			get
			{
				return unknown;
			}
		}

		public PSBTOutput()
		{
			hd_keypaths = new HDKeyPathKVMap();
			unknown = new UnknownKVMap();
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if (stream.Serializing)
			{
				Serialize(stream);
			}
			else
			{
				Deserialize(stream);
			}
		}

		public void Serialize(BitcoinStream stream)
		{
			if (redeem_script != null)
			{
				stream.ReadWriteAsVarInt(ref defaultKeyLen);
				stream.ReadWrite(PSBTConstants.PSBT_OUT_REDEEMSCRIPT);
				var value = redeem_script.ToBytes();
				stream.ReadWriteAsVarString(ref value);
			}

			if (witness_script != null)
			{
				stream.ReadWriteAsVarInt(ref defaultKeyLen);
				stream.ReadWrite(PSBTConstants.PSBT_OUT_WITNESSSCRIPT);
				var value = witness_script.ToBytes();
				stream.ReadWriteAsVarString(ref value);
			}

			foreach (var pathPair in hd_keypaths)
			{
				var key = new byte[] { PSBTConstants.PSBT_OUT_BIP32_DERIVATION }.Concat(pathPair.Key.ToBytes());
				stream.ReadWriteAsVarString(ref key);
				var path = pathPair.Value;
				stream.ReadWriteAsVarString(ref path);
			}

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

		public void Deserialize(BitcoinStream stream)
		{
			byte[] k = new byte[0];
			byte[] v = new byte[0];
			try
			{
				stream.ReadWriteAsVarString(ref k);
			}
			catch (EndOfStreamException e)
			{
				throw new FormatException("Invalid PSBTOutput. Could not read key", e);
			}
			while (k.Length != 0)
			{
				try
				{
					stream.ReadWriteAsVarString(ref v);
				}
				catch (EndOfStreamException e)
				{
					throw new FormatException("Invalid PSBTOutput. Could not read value", e);
				}
				switch (k.First())
				{
					case PSBTConstants.PSBT_OUT_REDEEMSCRIPT:
						if (k.Length != 1)
							throw new FormatException("Invalid PSBTOutput. Contains illegal value in key for redeem script");
						if (redeem_script != null)
							throw new FormatException("Invalid PSBTOutput, duplicate key for redeem_script");
						redeem_script = Script.FromBytesUnsafe(v);
						break;
					case PSBTConstants.PSBT_OUT_WITNESSSCRIPT:
						if (k.Length != 1)
							throw new FormatException("Invalid PSBTOutput. Contains illegal value in key for witness script");
						if (witness_script != null)
							throw new FormatException("Invalid PSBTOutput, duplicate key for redeem_script");
						witness_script = Script.FromBytesUnsafe(v);
						break;
					case PSBTConstants.PSBT_OUT_BIP32_DERIVATION:
						var pubkey2 = new PubKey(k.Skip(1).ToArray());
						if (hd_keypaths.ContainsKey(pubkey2))
							throw new FormatException("Invalid PSBTOutput, duplicate key for hd_keypaths");
						hd_keypaths.Add(pubkey2, v);
						break;
					default:
						if (unknown.ContainsKey(k))
							throw new FormatException("Invalid PSBTInput, duplicate key for unknown value");
						unknown.Add(k, v);
						break;
				}
				stream.ReadWriteAsVarString(ref k);
			}
		}
		#endregion

		public override string ToString()
		{
			return "Not Implemented";
			// return JsonConvert.SerializeObject(this);
		}

		public override bool Equals(object obj)
		{
			var item = obj as PSBTOutput;
			if (item == null)
				return false;
			return item.Equals(this);
		}
		public bool Equals(PSBTOutput b) =>
			b != null && this.ToBytes().SequenceEqual(b.ToBytes());

		public override int GetHashCode() => Utils.GetHashCode(this.ToBytes());
	}

	public class PSBTOutputList : UnsignedList<PSBTOutput>
	{
		public PSBTOutputList() { }
		public PSBTOutputList(Transaction globalTx) : base(globalTx) { }
		public new PSBTOutput Add(PSBTOutput item)
		{
			base.Add(item);
			return item;
		}
	}

	public class PSBT : IBitcoinSerializable, IEquatable<PSBT>
	{
		// Magic bytes
		static byte[] PSBT_MAGIC_BYTES = Encoders.ASCII.DecodeData("psbt\xff");

		protected Transaction tx;
		public PSBTInputList inputs;
		public PSBTOutputList outputs;


		protected Dictionary<byte[], byte[]> unknown;

		public static PSBT Parse(string base64)
		{
			var raw = Encoders.Base64.DecodeData(base64);
			var stream = new BitcoinStream(raw);
			var ret = new PSBT();
			ret.Deserialize(stream);
			return ret;
		}

		public PSBT(Transaction globalTx, IEnumerable<Transaction> prevTXs)
		{
			tx = globalTx ?? throw new ArgumentNullException(nameof(globalTx));
			if (prevTXs == null)
			{
				throw new ArgumentNullException(nameof(prevTXs));
			}

			Initialize();

			// extract relevant coins from prevTXs
			var items = from txin in tx.Inputs
									from prevtx in prevTXs
									where txin.PrevOut.Hash == prevtx.GetHash()
									select Tuple.Create(new Coin(txin.PrevOut, prevtx.Outputs[txin.PrevOut.N]), prevtx);
			SetUpInputWithCoins(items.Select(i => i.Item1));

			// Set NonWitnessUTXO 
			// This is O(n^2) ... but who cares?
			foreach (var item in items)
			{
				for (var i = 0; i < tx.Inputs.Count; i++)
				{
					var txin = tx.Inputs[i];
					if (item.Item1.Outpoint == txin.PrevOut)
						this.inputs[i].NonWitnessUtxo = item.Item2;
				}
			}

			SetUpOutput();
		}
		/// <summary>
		/// NOTE: This won't preserve signatures in case of multisig.
		/// If you want to, give coins or previous TXs as a second argument.
		/// </summary>
		/// <param name="globalTx"></param>
		public PSBT(Transaction globalTx)
		{
			tx = globalTx ?? throw new ArgumentNullException(nameof(globalTx));
			Initialize();
			SetUpInput();
			SetUpOutput();
		}

		public PSBT(Transaction globalTx, IEnumerable<ICoin> coins)
		{
			tx = globalTx ?? throw new ArgumentNullException(nameof(globalTx));
			if (coins == null)
			{
				throw new ArgumentNullException(nameof(coins));
			}
			Initialize();
			SetUpInputWithCoins(coins);
			SetUpOutput();
		}

		private void SetUpInputWithCoins(IEnumerable<ICoin> coins)
		{
			for (var i = 0; i < tx.Inputs.Count; i++)
			{
				var txin = tx.Inputs[i];
				var coin = coins.FirstOrDefault(c => c.Outpoint == txin.PrevOut);
				if (coin != null)
				{
					this.inputs.Add(new PSBTInput(txin));
					this.inputs[i].AddCoin(coin);
					this.inputs[i].MoveOrphansToPartialSigs(tx);
					this.inputs[i].TryFinalize(tx, i);
				}
				else
				{
					this.inputs.Add(new PSBTInput(tx.Inputs[i]));
				}
			}
		}

		private void SetUpInput()
		{
			for (var i = 0; i < tx.Inputs.Count; i++)
				this.inputs.Add(new PSBTInput(tx.Inputs[i]));
		}

		private void SetUpOutput()
		{
			for (var i = 0; i < tx.Outputs.Count; i++)
				this.outputs.Add(new PSBTOutput());
		}


		public PSBT()
		{
			Initialize();
		}
		private void Initialize()
		{
			if (tx == null)
			{
				tx = GetConsensusFactory().CreateTransaction();
			}
			inputs = new PSBTInputList(tx);
			outputs = new PSBTOutputList(tx);
			unknown = new UnknownKVMap();
		}

		public static PSBT FromTransaction(Transaction tx) => new PSBT(tx);

		public PSBT AddCoins(params ICoin[] coins)
		{
			for (var i = 0; i < tx.Inputs.Count; i++)
			{
				var txin = tx.Inputs[i];
				var coin = coins.FirstOrDefault(c => c.Outpoint == txin.PrevOut);
				if (coin != null)
				{
					this.inputs[i].AddCoin(coin);
					this.inputs[i].CheckSanityForSigner(txin);
					this.inputs[i].MoveOrphansToPartialSigs(tx);
				}
			}

			return this;
		}

		public PSBT AddTransactions(params Transaction[] txs)
		{
			for (var i = 0; i < tx.Inputs.Count; i++)
			{
				var txin = tx.Inputs[i];
				var nonWitnessUtxo = txs.FirstOrDefault(t => t.GetHash() == txin.PrevOut.Hash);
				if (nonWitnessUtxo != null)
				{
					this.inputs[i].NonWitnessUtxo = nonWitnessUtxo;
					this.inputs[i].MoveOrphansToPartialSigs(tx);
				}
			}

			return this;
		}

		public PSBT TryFinalize(out bool result)
		{
			result = true;
			for (var i = 0; i < inputs.Count; i++)
			{
				var psbtin = inputs[i];
				result &= psbtin.TryFinalize(tx, i);
			}

			return this;
		}

		public PSBT Finalize()
		{
			for (var i = 0; i < inputs.Count; i++)
			{
				var psbtin = inputs[i];
				psbtin.TryFinalize(tx, i);
			}
			return this;
		}

		public PSBT TrySignAll(params Key[] keys)
		{
			CheckSanity();
			for (var i = 0; i < inputs.Count; i++)
			{
				Sign(i, keys);
			}

			return this;
		}
		public PSBT Sign(int index, Key[] keys) => Sign(index, keys, out bool _);

		public PSBT Sign(int index, Key[] keys, out bool success)
		{
			success = this.inputs[index].Sign(index, tx, keys);
			return this;
		}

		public Transaction ExtractTX()
		{
			if (!this.CanExtractTX())
				throw new InvalidOperationException("PSBTInputs are not all finalized!");

			for (var i = 0; i < tx.Inputs.Count; i++)
			{
				tx.Inputs[i].ScriptSig = inputs[i].FinalScriptSig ?? Script.Empty;
				tx.Inputs[i].WitScript = inputs[i].FinalScriptWitness ?? WitScript.Empty;
			}

			return tx;
		}
		public bool CanExtractTX() => IsAllFinalized();

		public bool IsAllFinalized() => this.inputs.All(i => i.IsFinalized());

		public PSBT AddPathTo(PubKey key, KeyPath path, int index, bool ToInput = true)
		{
			// TODO: Convert path object to storable type.
			if (ToInput)
			{
				// inputs[index].HDKeyPaths.Add(key, path);
			}
			else
			{
				// outputs[index].HDKeyPaths.Add(key, path);
			}

			return this;
		}

		public void CheckSanity()
		{
			var result = IsSane();
			var isSane = result.Item1;
			var reason = result.Item2;
			if (!isSane)
				throw new FormatException(reason);
		}

		private Tuple<bool, string> IsSane()
		{
			for (var i = 0; i < this.tx.Inputs.Count(); i++)
			{
				var psbtin = this.inputs[i];
				var txin = tx.Inputs[i];
				if (psbtin.WitnessUtxo != null && psbtin.NonWitnessUtxo != null)
				{
					var prevOutIndex = txin.PrevOut.N;
					if (!psbtin.NonWitnessUtxo.Outputs[prevOutIndex].Equals(psbtin.NonWitnessUtxo))
						return Tuple.Create(false, "malformed PSBT! witness_utxo and non_witness_utxo is different");
				}

				if (psbtin.NonWitnessUtxo != null)
				{
					var prevOutTxId = psbtin.NonWitnessUtxo.GetHash();
					if (txin.PrevOut.Hash != prevOutTxId)
						return Tuple.Create(false, "malformed PSBT! wrong non_witness_utxo.");
				}

			}

			return Tuple.Create(true, "");
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if (stream.Serializing)
			{
				Serialize(stream);
			}
			else
			{
				Deserialize(stream);
			}
		}

		private static uint defaultKeyLen = 1;
		public void Serialize(BitcoinStream stream)
		{
			CheckSanity();
			// magic bytes
			stream.ReadWrite(ref PSBT_MAGIC_BYTES);

			// unsigned tx flag
			stream.ReadWriteAsVarInt(ref defaultKeyLen);
			stream.ReadWrite(PSBTConstants.PSBT_GLOBAL_UNSIGNED_TX);

			// Write serialized tx to a stream
			stream.TransactionOptions &= TransactionOptions.None;
			uint txLength = (uint)tx.GetSerializedSize(TransactionOptions.None);
			stream.ReadWriteAsVarInt(ref txLength);
			stream.ReadWrite(tx);

			// Write the unknown things
			foreach (var kv in unknown)
			{
				byte[] k = kv.Key;
				byte[] v = kv.Value;
				stream.ReadWriteAsVarString(ref k);
				stream.ReadWriteAsVarString(ref v);
			}

			// Separator
			var sep = PSBTConstants.PSBT_SEPARATOR;
			stream.ReadWrite(ref sep);
			// Write inputs
			foreach (var psbtin in inputs)
			{
				stream.ReadWrite(psbtin);
			}
			// Write outputs
			foreach (var psbtout in outputs)
			{
				stream.ReadWrite(psbtout);
			}
		}

		public void Deserialize(BitcoinStream stream)
		{
			var magicBytes = stream.Inner.ReadBytes(PSBT_MAGIC_BYTES.Length);
			if (!magicBytes.SequenceEqual(PSBT_MAGIC_BYTES))
			{
				throw new FormatException("Invalid PSBT magic bytes");
			}

			// It will be reassigned in `ReadWriteAsVarString` so no worry to assign 0 length array here.
			byte[] k = new byte[0];
			byte[] v = new byte[0];
			var txFound = false;
			stream.ReadWriteAsVarString(ref k);
			while (k.Length != 0)
			{
				stream.ReadWriteAsVarString(ref v);
				switch (k.First())
				{
					case PSBTConstants.PSBT_GLOBAL_UNSIGNED_TX:
						if (k.Length != 1)
							throw new FormatException("Invalid PSBT. Contains illegal value in key global tx");
						var checkResult = tx.Check();
						if (checkResult != TransactionCheckResult.NoInput)
							throw new FormatException("Duplicate Key, unsigned tx already provided");

						tx.FromBytes(v);
						if (tx.Inputs.Any(txin => txin.ScriptSig != Script.Empty || txin.WitScript != WitScript.Empty))
							throw new FormatException("Malformed global tx. It should not contain any scriptsig or witness by itself");
						txFound = true;
						break;
					default:
						if (unknown.ContainsKey(k))
							throw new FormatException("Invalid PSBTInput, duplicate key for unknown value");
						unknown.Add(k, v);
						break;
				}
				stream.ReadWriteAsVarString(ref k);
			}
			if (!txFound)
				throw new FormatException("Invalid PSBT. No global TX");

			for (var i = 0; i < tx.Inputs.Count(); i++)
			{
				var psbtin = new PSBTInput();
				psbtin.ReadWrite(stream);
				inputs.Add(psbtin);
			}

			for (var i = 0; i < tx.Outputs.Count(); i++)
			{
				var psbtout = new PSBTOutput();
				psbtout.ReadWrite(stream);
				outputs.Add(psbtout);
			}

			CheckSanity();
		}
		#endregion

		public override string ToString()
		{
			return "Not Implemented";
			// return JsonConvert.SerializeObject(this);
		}

		public virtual ConsensusFactory GetConsensusFactory() => Bitcoin.Instance.Mainnet.Consensus.ConsensusFactory;

		public bool HasEqualTx(PSBT other) =>
			Object.ReferenceEquals(this.tx, other.tx) || this.tx.ToBytes().SequenceEqual(other.tx.ToBytes());

		public override bool Equals(object obj)
		{
			var item = obj as PSBT;
			if (item == null)
				return false;
			return item.Equals(this);
		}

		public bool Equals(PSBT b)
		{
			if (!this.HasEqualTx(b))
				return false;

			var ains = this.inputs;
			var bins = b.inputs;
			var aouts = this.outputs;
			var bouts = b.outputs;

			if (ains.Count() != bins.Count() || aouts.Count() != bouts.Count())
				return false;

			bool isInputAllSame = ains.Zip(bins, (PSBTInput ain, PSBTInput bin) => ain.Equals(bin)).All(res => res);
			if (!isInputAllSame)
				return false;
			bool isOutputAllSame = aouts.Zip(bouts, (PSBTOutput aout, PSBTOutput bout) => aout.Equals(bout)).All(res => res);
			if (!isOutputAllSame)
				return false;

			return true;
		}
		public override int GetHashCode() => Utils.GetHashCode(this.ToBytes());
	}
}
