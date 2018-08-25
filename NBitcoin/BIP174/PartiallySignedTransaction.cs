using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin.DataEncoders;

namespace NBitcoin.BIP174
{
	static class PSBTConstants
	{
		// Note: These constants are in reverse byte order because serialization uses LSB
		// Global types
		public const byte PSBT_GLOBAL_UNSIGNED_TX = 0x00;

		// Input types
		public static byte PSBT_IN_NON_WITNESS_UTXO = 0x00;
		public static byte PSBT_IN_WITNESS_UTXO = 0x01;
		public static byte PSBT_IN_PARTIAL_SIG = 0x02;
		public static byte PSBT_IN_SIGHASH = 0x03;
		public static byte PSBT_IN_REDEEMSCRIPT = 0x04;
		public static byte PSBT_IN_WITNESSSCRIPT = 0x05;
		public static byte PSBT_IN_BIP32_DERIVATION = 0x06;
		public static byte PSBT_IN_SCRIPTSIG = 0x07;
		public static byte PSBT_IN_SCRIPTWITNESS = 0x08;

		// Output types
		public static byte PSBT_OUT_REDEEMSCRIPT = 0x00;
		public static byte PSBT_OUT_WITNESSSCRIPT = 0x01;
		public static byte PSBT_OUT_BIP32_DERIVATION = 0x02;

		// The separator is 0x00. Reading this in means that the unserializer can interpret it
		// as a 0 length key which indicates that this is the separator. The separator has no value.
		public static byte PSBT_SEPARATOR = 0x00;
	}

	public class PSBTInput : IBitcoinSerializable
	{
		private Transaction non_witness_utxo;
		private TxOut witness_utxo;
		private Script redeem_script;
		private Script witness_script;
		private Script final_script_sig;
		private WitScript final_script_witness;

		private Dictionary<PubKey, uint[]> hd_keypaths;
		private Dictionary<KeyId, Tuple<PubKey, byte[]>> partial_sigs;
		private Dictionary<byte[], byte[]> unknown;
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

		public Dictionary<PubKey, uint[]> HDKeyPaths
		{
			get
			{
				return hd_keypaths;
			}
		}

		public Dictionary<KeyId, Tuple<PubKey, byte[]>> PartialSigs
		{
			get
			{
				return partial_sigs;
			}
		}

		public Dictionary<byte[], byte[]> Unknown
		{
			get
			{
				return unknown;
			}
		}

		public PSBTInput()
		{
			hd_keypaths = new Dictionary<PubKey, uint[]>();
			partial_sigs= new Dictionary<KeyId, Tuple<PubKey, byte[]>>();
			unknown = new Dictionary<byte[], byte[]>();
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
		}

		private void Serialize(BitcoinStream stream)
		{
			// Write the utxo
			// If there is a non-witness utxo, then don't add the witness one.
			if (non_witness_utxo != null)
			{
				stream.ReadWrite(ref PSBTConstants.PSBT_IN_NON_WITNESS_UTXO);
				stream.ReadWrite(ref non_witness_utxo);
			} 
			else if (witness_utxo.Value != new Money(-1))
			{
				stream.ReadWrite(ref PSBTConstants.PSBT_IN_WITNESS_UTXO);
				stream.ReadWrite(ref witness_utxo);
			}

			if (final_script_sig == Script.Empty && final_script_witness == WitScript.Empty)
			{
				// Write any partial signatures
				foreach (var sig_pair in partial_sigs)
				{
					var sp = sig_pair.Value.Item2;
					stream.ReadWrite(ref PSBTConstants.PSBT_IN_PARTIAL_SIG);
					stream.ReadWrite(ref sp);
				}

				// Write the sighash type
				if (sighash_type > 0)
				{
					stream.ReadWrite(ref PSBTConstants.PSBT_IN_SIGHASH);
					stream.ReadWrite(ref sighash_type);
				}

				// Write the redeem script
				if (redeem_script != Script.Empty)
				{
					stream.ReadWrite(ref PSBTConstants.PSBT_IN_REDEEMSCRIPT);
					stream.ReadWrite(ref redeem_script);
				}

				// Write the witness script
				if (witness_script != Script.Empty)
				{
					stream.ReadWrite(ref PSBTConstants.PSBT_IN_WITNESSSCRIPT);
					stream.ReadWrite(ref witness_script);
				}

				// Write any hd keypaths
				//SerializeHDKeypaths(s, hd_keypaths, PSBT_IN_BIP32_DERIVATION);
			}

			// Write script sig
			if (final_script_sig != Script.Empty)
			{
				stream.ReadWrite(ref PSBTConstants.PSBT_IN_SCRIPTSIG);
				stream.ReadWrite(ref final_script_sig);
			}

			// write script witness
			if (final_script_witness != WitScript.Empty)
			{
				stream.ReadWrite(ref PSBTConstants.PSBT_IN_SCRIPTWITNESS);
				var stack = final_script_witness.ToBytes();
				stream.ReadWriteAsVarString(ref stack);
			}

			// Write unknown things
			foreach (var entry in unknown)
			{
				var k = entry.Key;
				var v = entry.Value;
				stream.ReadWrite(ref k);
				stream.ReadWrite(ref v);
			}

			stream.ReadWrite(ref PSBTConstants.PSBT_SEPARATOR);
		}

		#endregion
	}

	public class PSBTOutput : IBitcoinSerializable
	{
		private Script redeem_script;
		private Script witness_script;
		private Dictionary<PubKey, uint[]> hd_keypaths;
		private Dictionary<byte[], byte[]> unknown;

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

		public Dictionary<PubKey, uint[]> HDKeyPaths
		{
			get
			{
				return hd_keypaths;
			}
		}

		public Dictionary<byte[], byte[]> Unknown
		{
			get
			{
				return unknown;
			}
		}

		public PSBTOutput() 
		{
			hd_keypaths = new Dictionary<PubKey, uint[]>();
			unknown = new Dictionary<byte[], byte[]>();
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
		}

		#endregion
	}

	public class PartiallySignedTransaction : IBitcoinSerializable
	{
		// Magic bytes
		static byte[] PSBT_MAGIC_BYTES = Encoders.ASCII.DecodeData("psbt\xff");

		private Transaction tx;
		private List<PSBTInput> inputs;
		private List<PSBTOutput> outputs;

		private Dictionary<byte[], byte[]> unknown;

		public static PartiallySignedTransaction Parse(string hex)
		{
			var raw = Encoders.Hex.DecodeData(hex);
			var stream = new BitcoinStream(raw);
			var ret = new PartiallySignedTransaction();
			ret.Deserialize(stream);
			return ret;
		}

		public PartiallySignedTransaction() 
		{
			inputs = new List<PSBTInput>();
			outputs= new List<PSBTOutput>();
			unknown = new Dictionary<byte[], byte[]>();
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
		}

		private void Serialize(BitcoinStream stream)
		{
			// magic bytes
			stream.ReadWrite(ref PSBT_MAGIC_BYTES);

			// unsigned tx flag
			stream.ReadWrite(PSBTConstants.PSBT_GLOBAL_UNSIGNED_TX);

			// Write serialized tx to a stream
			stream.TransactionOptions &= TransactionOptions.None;
			stream.ReadWrite(tx);

			// Write the unknown things
			foreach(var kv in unknown)
			{
				var k = kv.Key;
				var v = kv.Value;
				stream.ReadWrite(ref k);
				stream.ReadWrite(ref v);
			}

			// Separator
			stream.ReadWrite(ref PSBTConstants.PSBT_SEPARATOR);

			// Write inputs
			foreach (var input in inputs)
			{
				var inp = input;
				stream.ReadWrite(ref inp);
			}

			// Write outputs
			foreach (var output in outputs)
			{
				var outp = output;
				stream.ReadWrite(ref outp);
			}
		}

		private void Deserialize(BitcoinStream stream)
		{
			var magicBytes = stream.Inner.ReadBytes(PSBT_MAGIC_BYTES.Length);
			if(!magicBytes.SequenceEqual(PSBT_MAGIC_BYTES))
			{
				throw new FormatException("Invalid PSBT magic bytes");
			}

			while(true)
			{
				byte b = 0;
				stream.ReadWrite(ref b);

				switch(b)
				{
					case PSBTConstants.PSBT_GLOBAL_UNSIGNED_TX:
						if(tx != null)
							throw new FormatException("Duplicate Key, unsigned tx already provided");
						
						//if()

						stream.ReadWrite(ref tx);
						foreach(var txin in tx.Inputs)
						{
							if(txin.ScriptSig != Script.Empty || txin.WitScript != WitScript.Empty)
								throw new FormatException("Duplicate Key, key for unknown value already provided");
						}
						break;

				}
			}
		}
		#endregion

		
		public override bool Equals(object obj)
		{
			var item = obj as PartiallySignedTransaction;
			if(item == null)
				return false;
			return item == this;
		}

		public static bool operator == (PartiallySignedTransaction a, PartiallySignedTransaction b)
		{
			var ain = a.tx.Inputs;
			var bin = b.tx.Inputs;
			var aout = a.tx.Outputs;
			var bout = b.tx.Outputs;

			if (ain.Count != bin.Count || aout.Count != bout.Count)
				return false;

			for (var i = 0; i < ain.Count; ++i)
			{
				if (ain[i].PrevOut != bin[i].PrevOut || ain[i].Sequence != bin[i].Sequence)
				{
					return false;
				}
			}

			// Check the outputs
			for (var i = 0; i < aout.Count; ++i)
			{
				if (aout[i] != bout[i])
				{
					return false;
				}
			}
			return true;
		}

		public static bool operator != (PartiallySignedTransaction a, PartiallySignedTransaction b)
		{
			return !(a == b);
		}
	}
}
