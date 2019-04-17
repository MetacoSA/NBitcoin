using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnKnownKVMap = System.Collections.Generic.SortedDictionary<byte[], byte[]>;
using HDKeyPathKVMap = System.Collections.Generic.SortedDictionary<NBitcoin.PubKey, System.Tuple<NBitcoin.HDFingerprint, NBitcoin.KeyPath>>;
using PartialSigKVMap = System.Collections.Generic.SortedDictionary<NBitcoin.KeyId, System.Tuple<NBitcoin.PubKey, NBitcoin.Crypto.ECDSASignature>>;

namespace NBitcoin
{
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

	
	

	public class PSBT : IEquatable<PSBT>
	{
		// Magic bytes
		static byte[] PSBT_MAGIC_BYTES = Encoders.ASCII.DecodeData("psbt\xff");

		internal Transaction tx;
		public PSBTInputList Inputs { get; }
		public PSBTOutputList Outputs { get; } 

		internal UnKnownKVMap unknown = new UnKnownKVMap(BytesComparer.Instance);
		public static PSBT Parse(string hexOrBase64, Network network)
		{
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			return Parse(hexOrBase64, network.Consensus.ConsensusFactory);
		}

		public static PSBT Parse(string hexOrBase64, ConsensusFactory consensusFactory)
		{
			if (hexOrBase64 == null)
				throw new ArgumentNullException(nameof(hexOrBase64));
			if (consensusFactory == null)
				throw new ArgumentNullException(nameof(consensusFactory));
			byte[] raw;
			if (HexEncoder.IsWellFormed(hexOrBase64))
				raw = Encoders.Hex.DecodeData(hexOrBase64);
			else
				raw = Encoders.Base64.DecodeData(hexOrBase64);

			return Load(raw, consensusFactory);
		}

		public static PSBT Load(byte[] rawBytes, ConsensusFactory consensusFactory)
		{
			if (rawBytes == null)
				throw new ArgumentNullException(nameof(rawBytes));
			var stream = new BitcoinStream(rawBytes);
			stream.ConsensusFactory = consensusFactory;
			var ret = new PSBT(stream);
			return ret;
		}

		private PSBT(Transaction transaction)
		{
			if (transaction == null)
				throw new ArgumentNullException(nameof(transaction));
			tx = transaction.Clone();
			Inputs = new PSBTInputList();
			Outputs = new PSBTOutputList();
			for (var i = 0; i < tx.Inputs.Count; i++)
				this.Inputs.Add(new PSBTInput(this, (uint)i, tx.Inputs[i]));
			for (var i = 0; i < tx.Outputs.Count; i++)
				this.Outputs.Add(new PSBTOutput(this, (uint)i, tx.Outputs[i]));
		}

		internal PSBT(BitcoinStream stream)
		{
			Inputs = new PSBTInputList();
			Outputs = new PSBTOutputList();
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
				switch (k.First())
				{
					case PSBTConstants.PSBT_GLOBAL_UNSIGNED_TX:
						if (k.Length != 1)
							throw new FormatException("Invalid PSBT. Contains illegal value in key global tx");
						if (tx != null)
							throw new FormatException("Duplicate Key, unsigned tx already provided");
						tx = stream.ConsensusFactory.CreateTransaction();
						uint size = 0;
						stream.ReadWriteAsVarInt(ref size);
						var pos = stream.Counter.ReadenBytes;
						tx.ReadWrite(stream);
						if (stream.Counter.ReadenBytes - pos != size)
							throw new FormatException("Malformed global tx. Unexpected size.");
						if (tx.Inputs.Any(txin => txin.ScriptSig != Script.Empty || txin.WitScript != WitScript.Empty))
							throw new FormatException("Malformed global tx. It should not contain any scriptsig or witness by itself");
						txFound = true;
						break;
					default:
						if (unknown.ContainsKey(k))
							throw new FormatException("Invalid PSBTInput, duplicate key for unknown value");
						stream.ReadWriteAsVarString(ref v);
						unknown.Add(k, v);
						break;
				}
				stream.ReadWriteAsVarString(ref k);
			}
			if (!txFound)
				throw new FormatException("Invalid PSBT. No global TX");

			int i = 0;
			while (stream.Inner.CanRead && i < tx.Inputs.Count)
			{
				var psbtin = new PSBTInput(stream, this, (uint)i, tx.Inputs[i]);
				Inputs.Add(psbtin);
				i++;
			}
			if (i != tx.Inputs.Count)
				throw new FormatException("Invalid PSBT. Number of input does not match to the global tx");

			i = 0;
			while (stream.Inner.CanRead && i < tx.Outputs.Count)
			{
				var psbtout = new PSBTOutput(stream, this, (uint)i, tx.Outputs[i]);
				Outputs.Add(psbtout);
				i++;
			}
			if (i != tx.Outputs.Count)
				throw new FormatException("Invalid PSBT. Number of outputs does not match to the global tx");

			AssertSanity();
		}

		public PSBT AddCoins(params ICoin[] coins)
		{
			if (coins == null)
				return this;
			foreach (var coin in coins)
			{
				var indexedInput = this.Inputs.FindIndexedInput(coin.Outpoint);
				if (indexedInput == null)
					continue;
				indexedInput.SetCoin(coin);
			}
			foreach (var coin in coins)
			{
				foreach(var output in this.Outputs)
				{
					if (output.TxOut.ScriptPubKey == coin.TxOut.ScriptPubKey)
					{
						output.SetCoin(coin);
					}
				}
			}
			return this;
		}

		public PSBT AddCoins(params Transaction[] transactions)
		{
			if (transactions == null)
				throw new ArgumentNullException(nameof(transactions));
			return AddCoins(transactions.SelectMany(t => t.Outputs.AsCoins()).ToArray()).AddTransactions(transactions);
		}

		/// <summary>
		/// Add transactions to non segwit outputs
		/// </summary>
		/// <param name="parentTransactions">Parent transactions</param>
		/// <returns>This PSBT</returns>
		public PSBT AddTransactions(params Transaction[] parentTransactions)
		{
			if (parentTransactions == null)
				return this;

			Dictionary<uint256, Transaction> txsById = new Dictionary<uint256, Transaction>();
			foreach (var tx in parentTransactions)
				txsById.TryAdd(tx.GetHash(), tx);
			foreach (var input in Inputs)
			{
				if (input.WitnessUtxo == null && txsById.TryGetValue(input.TxIn.PrevOut.Hash, out var tx))
				{
					if (input.TxIn.PrevOut.N >= tx.Outputs.Count)
						continue;
					var output = tx.Outputs[input.TxIn.PrevOut.N];
					if (output.ScriptPubKey.IsWitness || input.RedeemScript?.IsWitness is true)
					{
						input.WitnessUtxo = output;
						input.NonWitnessUtxo = null;
					}
					else
					{
						input.WitnessUtxo = null;
						input.NonWitnessUtxo = tx;
					}
				}
			}
			return this;
		}

		/// <summary>
		/// If an other PSBT has a specific field and this does not have it, then inject that field to this.
		/// otherwise leave it as it is.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public PSBT Combine(PSBT other)
		{
			if (other == null)
			{
				throw new ArgumentNullException(nameof(other));
			}

			if (other.StrippedTransaction().GetHash() != this.StrippedTransaction().GetHash())
				throw new ArgumentException(paramName: nameof(other), message: "Can not Combine PSBT with different global tx.");

			for (int i = 0; i < Inputs.Count; i++)
				this.Inputs[i].Combine(other.Inputs[i]);

			for (int i = 0; i < Outputs.Count; i++)
				this.Outputs[i].Combine(other.Outputs[i]);

			foreach (var uk in other.unknown)
				this.unknown.TryAdd(uk.Key, uk.Value);

			return this;
		}

		/// <summary>
		/// Join two PSBT into one CoinJoin PSBT.
		/// This is an immutable method.
		/// TODO: May need assertion for sighash type?
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public PSBT CoinJoin(PSBT other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			other.AssertSanity();

			var result = this.Clone();

			for (int i = 0; i < other.Inputs.Count; i++)
			{
				result.tx.Inputs.Add(other.tx.Inputs[i]);
				result.Inputs.Add(other.Inputs[i]);
			}
			for (int i = 0; i < other.Outputs.Count; i++)
			{
				result.tx.Outputs.Add(other.tx.Outputs[i]);
				result.Outputs.Add(other.Outputs[i]);
			}
			return result;
		}

		public PSBT Finalize(TransactionBuilder transactionBuilder = null)
		{
			List<PSBTError> errors = new List<PSBTError>();
			foreach (var input in Inputs)
			{
				if(!input.TryFinalize(transactionBuilder, out var e))
				{
					errors.AddRange(e);
				}
			}
			if (errors.Count != 0)
				throw new PSBTException(errors);
			return this;
		}

		/// <summary>
		/// Test vector in the bip174 specify to use a signer which follows RFC 6979.
		/// So we must sign without [LowR value assured way](https://github.com/MetacoSA/NBitcoin/pull/510)
		/// This should be turned false only in the test.
		/// ref: https://github.com/bitcoin/bitcoin/pull/13666
		/// </summary>
		internal bool UseLowR { get; set; } = true;

		public PSBT SignAll(params Key[] keys)
		{
			return SignAll(SigHash.All, keys);
		}

		public PSBT SignAll(SigHash sigHash, params Key[] keys)
		{
			AssertSanity();
			foreach (var key in keys)
			{
				foreach (var input in this.Inputs)
				{
					input.TrySign(key, sigHash, UseLowR, out _);
				}
			}
			return this;
		}

		public Transaction ExtractTransaction()
		{
			if (!this.CanExtractTransaction())
				throw new InvalidOperationException("PSBTInputs are not all finalized!");

			var copy = tx.Clone();
			for (var i = 0; i < tx.Inputs.Count; i++)
			{
				copy.Inputs[i].ScriptSig = Inputs[i].FinalScriptSig ?? Script.Empty;
				copy.Inputs[i].WitScript = Inputs[i].FinalScriptWitness ?? WitScript.Empty;
			}

			return copy;
		}
		public bool CanExtractTransaction() => IsAllFinalized();

		public bool IsAllFinalized() => this.Inputs.All(i => i.IsFinalized());

		public IList<PSBTError> CheckSanity()
		{
			List<PSBTError> errors = new List<PSBTError>();
			foreach (var input in Inputs)
			{
				errors.AddRange(input.CheckSanity());
			}
			return errors;
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
			AssertSanity();
			// magic bytes
			stream.ReadWrite(ref PSBT_MAGIC_BYTES);

			// unsigned tx flag
			stream.ReadWriteAsVarInt(ref defaultKeyLen);
			stream.ReadWrite(PSBTConstants.PSBT_GLOBAL_UNSIGNED_TX);

			// Write serialized tx to a stream
			stream.TransactionOptions &= TransactionOptions.None;
			Transaction clone = StrippedTransaction();
			uint txLength = (uint)clone.GetSerializedSize(TransactionOptions.None);
			stream.ReadWriteAsVarInt(ref txLength);
			stream.ReadWrite(clone);

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
			foreach (var psbtin in Inputs)
			{
				psbtin.Serialize(stream);
			}
			// Write outputs
			foreach (var psbtout in Outputs)
			{
				psbtout.Serialize(stream);
			}
		}

		private Transaction StrippedTransaction()
		{
			var clone = tx.Clone();
			for (int i = 0; i < clone.Inputs.Count; i++)
			{
				clone.Inputs[i].ScriptSig = Script.Empty;
				clone.Inputs[i].WitScript = WitScript.Empty;
			}

			return clone;
		}

		#endregion

		public override string ToString()
		{
			var strWriter = new StringWriter();
			var jsonWriter = new JsonTextWriter(strWriter);
			jsonWriter.Formatting = Formatting.Indented;
			jsonWriter.WriteStartObject();
			jsonWriter.WritePropertyName("tx");
			jsonWriter.WriteStartObject();
			var formatter = new RPC.BlockExplorerFormatter();
			formatter.WriteTransaction2(jsonWriter, StrippedTransaction());
			jsonWriter.WriteEndObject();

			jsonWriter.WritePropertyName("inputs");
			jsonWriter.WriteStartArray();
			foreach(var input in this.Inputs)
			{
				input.Write(jsonWriter);
			}
			jsonWriter.WriteEndArray();

			jsonWriter.WritePropertyName("outputs");
			jsonWriter.WriteStartArray();
			foreach (var output in this.Outputs)
			{
				output.Write(jsonWriter);
			}
			jsonWriter.WriteEndArray();

			jsonWriter.WriteEndObject();
			jsonWriter.Flush();
			return strWriter.ToString();
		}

		public byte[] ToBytes()
		{
			MemoryStream ms = new MemoryStream();
			var bs = new BitcoinStream(ms, true);
			bs.ConsensusFactory = tx.GetConsensusFactory();
			this.Serialize(bs);
			return ms.ToArrayEfficient();
		}

		public PSBT Clone()
		{
			var bytes = ToBytes();
			return PSBT.Load(bytes, tx.GetConsensusFactory());
		}

		public string ToBase64() => Encoders.Base64.EncodeData(this.ToBytes());
		public string ToHex() => Encoders.Hex.EncodeData(this.ToBytes());

		public override bool Equals(object obj)
		{
			var item = obj as PSBT;
			if (item == null)
				return false;
			return item.Equals(this);
		}

		public bool Equals(PSBT b)
		{
			if (b is null)
				return false;
			return this.ToBytes().SequenceEqual(b.ToBytes());
		}
		public override int GetHashCode() => Utils.GetHashCode(this.ToBytes());

		public static PSBT FromTransaction(Transaction transaction)
		{
			if (transaction == null)
				throw new ArgumentNullException(nameof(transaction));
			return new PSBT(transaction);
		}

		public PSBT AddScripts(params Script[] redeems)
		{
			if (redeems == null)
				throw new ArgumentNullException(nameof(redeems));
			var unused = new OutPoint(uint256.Zero, 0);
			foreach (var redeem in redeems)
			{
				var p2sh = redeem.Hash.ScriptPubKey;
				var p2wsh = redeem.WitHash.ScriptPubKey;
				var p2shp2wsh = redeem.WitHash.ScriptPubKey.Hash.ScriptPubKey;
				foreach (var input in this.Inputs)
				{
					var txout = input.GetTxOut();
					if (txout == null)
						continue;
					if (txout.ScriptPubKey == p2sh)
					{
						input.RedeemScript = redeem;
					}
					else if (txout.ScriptPubKey == p2wsh)
					{
						input.WitnessScript = redeem;
						input.TrySlimOutput();
					}
					else if (txout.ScriptPubKey == p2shp2wsh)
					{
						input.WitnessScript = redeem;
						input.RedeemScript = redeem.WitHash.ScriptPubKey;
						input.TrySlimOutput();
					}
				}

				foreach (var output in this.Outputs)
				{
					var txout = output.TxOut;
					if(txout.ScriptPubKey == p2sh)
					{
						output.RedeemScript = redeem;
					}
					else if (txout.ScriptPubKey == p2wsh)
					{
						output.WitnessScript = redeem;
					}
					else if (txout.ScriptPubKey == p2shp2wsh)
					{
						output.WitnessScript = redeem;
						output.RedeemScript = redeem.WitHash.ScriptPubKey;
					}
				}
			}
			return this;
		}

		public PSBT AddKeyPath(PubKey pubkey, KeyPath path)
		{
			return AddKeyPath(default(HDFingerprint), pubkey, path);
		}

		public PSBT AddKeyPath(HDFingerprint fingerprint, PubKey pubkey, KeyPath path)
		{
			if (pubkey == null)
				throw new ArgumentNullException(nameof(pubkey));
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			foreach(var input in this.Inputs)
			{
				if (input.IsRelatedKey(pubkey))
					input.AddKeyPath(fingerprint, pubkey, path);
			}
			foreach (var ouptut in this.Outputs)
			{
				if (ouptut.IsRelatedKey(pubkey))
					ouptut.AddKeyPath(fingerprint, pubkey, path);
			}
			return this;
		}
	}
}
