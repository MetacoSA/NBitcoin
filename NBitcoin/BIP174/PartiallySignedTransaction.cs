using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;

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
			hd_keypaths = new HDKeyPathKVMap();
			partial_sigs = new PartialSigKVMap();
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

		private static uint defaultKeyLen = 1;

		public bool IsFinalized() => final_script_sig != null || final_script_witness != null;

		public void CheckSanity()
		{
			var (isSane, reason) = IsSane();
			if (!isSane)
				throw new FormatException(reason);
		}

		private (bool, string) IsSane()
		{
			if (this.IsFinalized())
				if (partial_sigs.Count != 0 || hd_keypaths.Count != 0 || sighash_type != 0 || redeem_script != null || witness_script != null)
					return (false, "PSBT Input is dirty. It has been finalized but properties are not cleared");
			return (true, "");
		}

		public TxOut GetOutput(OutPoint prevout)
		{
			if (witness_utxo != null)
				return witness_utxo;
			if (non_witness_utxo != null && prevout != null)
				return non_witness_utxo.Outputs[prevout.N];
			return null;
		}
		private void Serialize(BitcoinStream stream)
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
				stream.ReadWrite(ref sighash_type);
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
		}

		private void Deserialize(BitcoinStream stream)
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
			return item == this;
		}
		public bool Equals(PSBTInput other) =>
			IsReferencingSamePrevOut(other) &&
			Utils.DictEqual(partial_sigs, other.partial_sigs, IsPartialSigSame) &&
			sighash_type == other.sighash_type &&
			Utils.NullSafeEquals(redeem_script, redeem_script) &&
			Utils.NullSafeEquals(witness_script, witness_script) &&
			Utils.NullSafeEquals<Script>(final_script_sig, other.final_script_sig) &&
			Utils.NullSafeEquals<WitScript>(final_script_witness, other.final_script_witness) &&
			Utils.DictEqual(hd_keypaths, other.hd_keypaths, (x, y) => x.SequenceEqual(y));

		private bool IsPartialSigSame(Tuple<PubKey, ECDSASignature> a, Tuple<PubKey, ECDSASignature> b) =>
			a.Item1.Equals(b.Item1) && a.Item2.ToDER().SequenceEqual(b.Item2.ToDER());

		public static bool operator ==(PSBTInput a, PSBTInput b) => a.Equals(b);
		public static bool operator !=(PSBTInput a, PSBTInput b) => !(a == b);

		public override int GetHashCode() => Utils.GetHashCode(this.ToBytes());

		private bool IsReferencingSamePrevOut(PSBTInput other) =>
			(non_witness_utxo == null && other.non_witness_utxo == null && witness_utxo == null && other.witness_utxo == null) ||
			Utils.NullSafeEquals(non_witness_utxo, other.non_witness_utxo) ||
			Utils.NullSafeEquals(witness_utxo, other.witness_utxo);

		public virtual ConsensusFactory GetConsensusFactory() => Bitcoin.Instance.Mainnet.Consensus.ConsensusFactory;
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

		public override bool Equals(object obj)
		{
			var item = obj as PSBTOutput;
			if (item == null)
				return false;
			return item.Equals(this);
		}
		public bool Equals(PSBTOutput b) =>
			Utils.NullSafeEquals(redeem_script, b.redeem_script) &&
			Utils.NullSafeEquals(witness_script, b.witness_script) &&
			Utils.DictEqual(hd_keypaths, b.hd_keypaths, (x, y) => x.SequenceEqual(y));

		public static bool operator ==(PSBTOutput a, PSBTOutput b) => a.Equals(b);
		public static bool operator !=(PSBTOutput a, PSBTOutput b) => !(a == b);

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
		protected PSBTInputList inputs;
		protected PSBTOutputList outputs;

		protected Dictionary<byte[], byte[]> unknown;

		public static PSBT Parse(string base64)
		{
			var raw = Encoders.Base64.DecodeData(base64);
			var stream = new BitcoinStream(raw);
			var ret = new PSBT();
			ret.Deserialize(stream);
			return ret;
		}

		public PSBT(Transaction globalTx)
		{
			tx = globalTx;
			new PSBT();
		}
		public PSBT()
		{
			if (tx == null)
			{
				tx = GetConsensusFactory().CreateTransaction();
			}
			inputs = new PSBTInputList(tx);
			outputs = new PSBTOutputList(tx);
			unknown = new UnknownKVMap();
		}

		public void CheckSanity()
		{
			var result = IsSane();
			if (!result.Item1)
				throw new FormatException(result.Item2);
		}

		private (bool, string) IsSane()
		{
			for (var i = 0; i < this.tx.Inputs.Count(); i++)
			{
				var psbtin = this.inputs[i];
				if (psbtin.WitnessUtxo != null && psbtin.NonWitnessUtxo != null)
				{
					var prevOutIndex = this.tx.Inputs[i].PrevOut.N;
					if (!psbtin.NonWitnessUtxo.Outputs[prevOutIndex].Equals(psbtin.NonWitnessUtxo))
						return (false, "malformed PSBT! witness_utxo and non_witness_utxo is different");
				}

				if (psbtin.NonWitnessUtxo != null)
				{
					var prevOutTxId = psbtin.NonWitnessUtxo.GetHash();
					if (this.tx.Inputs[i].PrevOut.Hash != prevOutTxId)
						return (false, "malformed PSBT! wrong non_witness_utxo.");
				}
			}

			return (true, "");
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
		private void Serialize(BitcoinStream stream)
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
				stream.ReadWrite(sep);
			}
			// Write outputs
			foreach (var psbtout in outputs)
			{
				stream.ReadWrite(psbtout);
				stream.ReadWrite(sep);
			}
		}

		private void Deserialize(BitcoinStream stream)
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
							throw new FormatException("Malformed global tx. It should not contain any script or witness but it does.");
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

		public virtual ConsensusFactory GetConsensusFactory() => Bitcoin.Instance.Mainnet.Consensus.ConsensusFactory;

		public bool HasEqualTx(PSBT other) => this.tx.Equals(other.tx);

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
		public static bool operator ==(PSBT a, PSBT b) => a.Equals(b);
		public static bool operator !=(PSBT a, PSBT b) => !(a == b);

		public override int GetHashCode() => Utils.GetHashCode(this.ToBytes());
	}
}
