using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using UnKnownKVMap = System.Collections.Generic.SortedDictionary<byte[], byte[]>;
using NBitcoin.BuilderExtensions;

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


	public class PSBTSettings
	{
		/// <summary>
		/// Test vector in the bip174 specify to use a signer which follows RFC 6979.
		/// So we must sign without [LowR value assured way](https://github.com/MetacoSA/NBitcoin/pull/510)
		/// This should be turned false only in the test.
		/// ref: https://github.com/bitcoin/bitcoin/pull/13666
		/// </summary>
		public bool UseLowR { get; set; } = true;

		/// <summary>
		/// Use custom builder extensions to customize finalization
		/// </summary>
		public IEnumerable<BuilderExtension> CustomBuilderExtensions { get; set; }

		/// <summary>
		/// Try to do anything that is possible to deduce PSBT information from input information
		/// </summary>
		public bool IsSmart { get; set; } = true;

		public PSBTSettings Clone()
		{
			return new PSBTSettings()
			{
				UseLowR = UseLowR,
				CustomBuilderExtensions = CustomBuilderExtensions?.ToArray(),
				IsSmart = IsSmart
			};
		}
	}

	public class PSBT : IEquatable<PSBT>
	{
		// Magic bytes
		readonly static byte[] PSBT_MAGIC_BYTES = Encoders.ASCII.DecodeData("psbt\xff");

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
		public static PSBT Load(byte[] rawBytes, Network network)
		{
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			return Load(rawBytes, network.Consensus.ConsensusFactory);
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
			foreach (var input in tx.Inputs)
			{
				input.ScriptSig = Script.Empty;
				input.WitScript = WitScript.Empty;
			}
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
				indexedInput.UpdateFromCoin(coin);
			}
			foreach (var coin in coins)
			{
				foreach (var output in this.Outputs)
				{
					if (output.ScriptPubKey == coin.TxOut.ScriptPubKey)
					{
						output.UpdateFromCoin(coin);
					}
				}
			}
			return this;
		}

		public PSBT AddCoins(params Transaction[] transactions)
		{
			if (transactions == null)
				throw new ArgumentNullException(nameof(transactions));
			return AddTransactions(transactions).AddCoins(transactions.SelectMany(t => t.Outputs.AsCoins()).ToArray());
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
					if (output.ScriptPubKey.IsScriptType(ScriptType.Witness) || input.RedeemScript?.IsScriptType(ScriptType.Witness) is true)
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

			if (other.tx.GetHash() != this.tx.GetHash())
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

		public PSBT Finalize()
		{
			if (!TryFinalize(out var errors))
				throw new PSBTException(errors);
			return this;
		}

		public bool TryFinalize(out IList<PSBTError> errors)
		{
			var localErrors = new List<PSBTError>();
			foreach (var input in Inputs)
			{
				if (!input.TryFinalizeInput(out var e))
				{
					localErrors.AddRange(e);
				}
			}
			if (localErrors.Count != 0)
			{
				errors = localErrors;
				return false;
			}
			errors = null;
			return true;
		}

		public bool IsReadyToSign()
		{
			return IsReadyToSign(out _);
		}
		public bool IsReadyToSign(out PSBTError[] errors)
		{
			var errorList = new List<PSBTError>();
			foreach (var input in Inputs)
			{
				var localErrors = input.CheckSanity();
				if (localErrors.Count != 0)
				{
					errorList.AddRange(localErrors);
				}
				else
				{
					if (input.GetSignableCoin(out var err) == null)
						errorList.Add(new PSBTError(input.Index, err));
				}
			}
			if (errorList.Count != 0)
			{
				errors = errorList.ToArray();
				return false;
			}
			errors = null;
			return true;
		}

		public PSBTSettings Settings { get; set; } = new PSBTSettings();


		/// <summary>
		/// Sign all inputs which derive <paramref name="accountKey"/> of type <paramref name="scriptPubKeyType"/>.
		/// </summary>
		/// <param name="scriptPubKeyType">The way to derive addresses from the accountKey</param>
		/// <param name="accountKey">The account key with which to sign</param>
		/// <param name="accountKeyPath">The account key path (eg. [masterFP]/49'/0'/0')</param>
		/// <param name="sigHash">The SigHash</param>
		/// <returns>This PSBT</returns>
		public PSBT SignAll(ScriptPubKeyType scriptPubKeyType, IHDKey accountKey, RootedKeyPath accountKeyPath, SigHash sigHash = SigHash.All)
		{
			if (accountKey == null)
				throw new ArgumentNullException(nameof(accountKey));
			return SignAll(new HDKeyScriptPubKey(accountKey, scriptPubKeyType), accountKey, accountKeyPath, sigHash);
		}

		/// <summary>
		/// Sign all inputs which derive <paramref name="accountKey"/> of type <paramref name="scriptPubKeyType"/>.
		/// </summary>
		/// <param name="scriptPubKeyType">The way to derive addresses from the accountKey</param>
		/// <param name="accountKey">The account key with which to sign</param>
		/// <param name="sigHash">The SigHash</param>
		/// <returns>This PSBT</returns>
		public PSBT SignAll(ScriptPubKeyType scriptPubKeyType, IHDKey accountKey, SigHash sigHash = SigHash.All)
		{
			if (accountKey == null)
				throw new ArgumentNullException(nameof(accountKey));
			return SignAll(new HDKeyScriptPubKey(accountKey, scriptPubKeyType), accountKey, sigHash);
		}

		/// <summary>
		/// Sign all inputs which derive addresses from <paramref name="accountHDScriptPubKey"/> and that need to be signed by <paramref name="accountKey"/>.
		/// </summary>
		/// <param name="accountHDScriptPubKey">The address generator</param>
		/// <param name="accountKey">The account key with which to sign</param>
		/// <param name="sigHash">The SigHash</param>
		/// <returns>This PSBT</returns>
		public PSBT SignAll(IHDScriptPubKey accountHDScriptPubKey, IHDKey accountKey, SigHash sigHash = SigHash.All)
		{
			return SignAll(accountHDScriptPubKey, accountKey, null, sigHash);
		}

		/// <summary>
		/// Sign all inputs which derive addresses from <paramref name="accountHDScriptPubKey"/> and that need to be signed by <paramref name="accountKey"/>.
		/// </summary>
		/// <param name="accountHDScriptPubKey">The address generator</param>
		/// <param name="accountKey">The account key with which to sign</param>
		/// <param name="accountKeyPath">The account key path (eg. [masterFP]/49'/0'/0')</param>
		/// <param name="sigHash">The SigHash</param>
		/// <returns>This PSBT</returns>
		public PSBT SignAll(IHDScriptPubKey accountHDScriptPubKey, IHDKey accountKey, RootedKeyPath accountKeyPath, SigHash sigHash = SigHash.All)
		{
			if (accountKey == null)
				throw new ArgumentNullException(nameof(accountKey));
			if (accountHDScriptPubKey == null)
				throw new ArgumentNullException(nameof(accountHDScriptPubKey));
			accountHDScriptPubKey = accountHDScriptPubKey.AsHDKeyCache();
			accountKey = accountKey.AsHDKeyCache();
			Money total = Money.Zero;
			foreach (var o in Inputs.CoinsFor(accountHDScriptPubKey, accountKey, accountKeyPath))
			{
				o.TrySign(accountHDScriptPubKey, accountKey, accountKeyPath, sigHash);
			}
			return this;
		}

		/// <summary>
		/// Returns the fee of the transaction being signed
		/// </summary>
		/// <param name="fee"></param>
		/// <returns></returns>
		public bool TryGetFee(out Money fee)
		{
			fee = tx.GetFee(GetAllCoins().ToArray());
			return fee != null;
		}

		/// <summary>
		/// Returns the fee rate of the transaction. If the PSBT is finalized, then the exact rate is returned, else an estimation is made.
		/// </summary>
		/// <param name="estimatedFeeRate"></param>
		/// <returns>True if could get the estimated fee rate</returns>
		public bool TryGetEstimatedFeeRate(out FeeRate estimatedFeeRate)
		{
			if (IsAllFinalized())
			{
				estimatedFeeRate = ExtractTransaction().GetFeeRate(GetAllCoins().ToArray());
				return estimatedFeeRate != null;
			}
			if (!TryGetFee(out var fee))
			{
				estimatedFeeRate = null;
				return false;
			}
			var transactionBuilder = CreateTransactionBuilder();
			transactionBuilder.AddCoins(GetAllCoins());
			try
			{
				var vsize = transactionBuilder.EstimateSize(this.tx, true);
				estimatedFeeRate = new FeeRate(fee, vsize);
				return true;
			}
			catch
			{
				estimatedFeeRate = null;
				return false;
			}
		}

		public PSBT SignWithKeys(params Key[] keys)
		{
			return SignWithKeys(SigHash.All, keys);
		}
		public PSBT SignWithKeys(params ISecret[] keys)
		{
			return SignWithKeys(SigHash.All, keys.Select(k => k.PrivateKey).ToArray());
		}

		public PSBT SignWithKeys(SigHash sigHash, params Key[] keys)
		{
			AssertSanity();
			foreach (var key in keys)
			{
				foreach (var input in this.Inputs)
				{
					input.Sign(key, sigHash);
				}
			}
			return this;
		}

		internal TransactionBuilder CreateTransactionBuilder()
		{
			var transactionBuilder = tx.GetConsensusFactory().CreateTransactionBuilder();
			if (Settings.CustomBuilderExtensions != null)
			{
				transactionBuilder.Extensions.Clear();
				transactionBuilder.Extensions.AddRange(Settings.CustomBuilderExtensions);
			}
			transactionBuilder.UseLowR = Settings.UseLowR;
			return transactionBuilder;
		}

		private IEnumerable<ICoin> GetAllCoins()
		{
			return this.Inputs.Select(i => i.GetSignableCoin() ?? i.GetCoin()).Where(c => c != null).ToArray();
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
			// magic bytes
			stream.Inner.Write(PSBT_MAGIC_BYTES, 0, PSBT_MAGIC_BYTES.Length);

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

		#endregion

		public override string ToString()
		{
			var strWriter = new StringWriter();
			var jsonWriter = new JsonTextWriter(strWriter);
			jsonWriter.Formatting = Formatting.Indented;
			jsonWriter.WriteStartObject();
			if (TryGetFee(out var fee))
			{
				jsonWriter.WritePropertyValue("fee", $"{fee} BTC");
			}
			else
			{
				jsonWriter.WritePropertyName("fee");
				jsonWriter.WriteToken(JsonToken.Null);
			}
			if (TryGetEstimatedFeeRate(out var feeRate))
			{
				jsonWriter.WritePropertyValue("feeRate", $"{feeRate}");
			}
			else
			{
				jsonWriter.WritePropertyName("feeRate");
				jsonWriter.WriteToken(JsonToken.Null);
			}
			jsonWriter.WritePropertyName("tx");
			jsonWriter.WriteStartObject();
			var formatter = new RPC.BlockExplorerFormatter();
			formatter.WriteTransaction2(jsonWriter, tx);
			jsonWriter.WriteEndObject();
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

			jsonWriter.WritePropertyName("inputs");
			jsonWriter.WriteStartArray();
			foreach (var input in this.Inputs)
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

		/// <summary>
		/// Clone this PSBT
		/// </summary>
		/// <returns>A cloned PSBT</returns>
		public PSBT Clone()
		{
			return Clone(true);
		}

		/// <summary>
		/// Clone this PSBT
		/// </summary>
		/// <param name="keepOriginalTransactionInformation">Whether the original scriptSig and witScript or inputs is saved</param>
		/// <returns>A cloned PSBT</returns>
		public PSBT Clone(bool keepOriginalTransactionInformation)
		{
			var bytes = ToBytes();
			var psbt = PSBT.Load(bytes, tx.GetConsensusFactory());
			if (keepOriginalTransactionInformation)
			{
				for (int i = 0; i < Inputs.Count; i++)
				{
					psbt.Inputs[i].originalScriptSig = this.Inputs[i].originalScriptSig;
					psbt.Inputs[i].originalWitScript = this.Inputs[i].originalWitScript;
					psbt.Inputs[i].orphanTxOut = this.Inputs[i].orphanTxOut;
				}
			}
			psbt.Settings = Settings.Clone();
			return psbt;
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
				foreach (var o in this.Inputs.OfType<PSBTCoin>().Concat(this.Outputs))
				{
					var txout = o.GetCoin()?.TxOut;
					if (txout == null)
						continue;
					if (txout.ScriptPubKey == p2sh)
					{
						o.RedeemScript = redeem;
					}
					else if (txout.ScriptPubKey == p2wsh)
					{
						o.WitnessScript = redeem;
						if (o is PSBTInput i)
							i.TrySlimUTXO();
					}
					else if (txout.ScriptPubKey == p2shp2wsh)
					{
						o.WitnessScript = redeem;
						o.RedeemScript = redeem.WitHash.ScriptPubKey;
						if (o is PSBTInput i)
							i.TrySlimUTXO();
					}
				}
			}
			return this;
		}


		/// <summary>
		/// Get the balance change if you were signing this transaction.
		/// </summary>
		/// <param name="accountHDScriptPubKey">The hdScriptPubKey used to generate addresses</param>
		/// <param name="accountKey">The account key that will be used to sign (ie. 49'/0'/0')</param>
		/// <param name="accountKeyPath">The account key path</param>
		/// <returns>The balance change</returns>
		public Money GetBalance(ScriptPubKeyType scriptPubKeyType, IHDKey accountKey, RootedKeyPath accountKeyPath = null)
		{
			if (accountKey == null)
				throw new ArgumentNullException(nameof(accountKey));
			return GetBalance(new HDKeyScriptPubKey(accountKey, scriptPubKeyType), accountKey, accountKeyPath);
		}


		/// <summary>
		/// Get the balance change if you were signing this transaction.
		/// </summary>
		/// <param name="accountHDScriptPubKey">The hdScriptPubKey used to generate addresses</param>
		/// <param name="accountKey">The account key that will be used to sign (ie. 49'/0'/0')</param>
		/// <param name="accountKeyPath">The account key path</param>
		/// <returns>The balance change</returns>
		public Money GetBalance(IHDScriptPubKey accountHDScriptPubKey, IHDKey accountKey, RootedKeyPath accountKeyPath = null)
		{
			if (accountHDScriptPubKey == null)
				throw new ArgumentNullException(nameof(accountHDScriptPubKey));
			Money total = Money.Zero;
			foreach (var o in CoinsFor(accountHDScriptPubKey, accountKey, accountKeyPath))
			{
				var amount = o.GetCoin()?.Amount;
				if (amount == null)
					continue;
				total += o is PSBTInput ? -amount : amount;
			}
			return total;
		}

		/// <summary>
		/// Filter the coins which contains the <paramref name="accountKey"/> and <paramref name="accountKeyPath"/> in the HDKeys and derive
		/// the same scriptPubKeys as <paramref name="accountHDScriptPubKey"/>.
		/// </summary>
		/// <param name="accountHDScriptPubKey">The hdScriptPubKey used to generate addresses</param>
		/// <param name="accountKey">The account key that will be used to sign (ie. 49'/0'/0')</param>
		/// <param name="accountKeyPath">The account key path</param>
		/// <returns>Inputs with HD keys matching masterFingerprint and account key</returns>
		public IEnumerable<PSBTCoin> CoinsFor(IHDScriptPubKey accountHDScriptPubKey, IHDKey accountKey, RootedKeyPath accountKeyPath = null)
		{
			if (accountKey == null)
				throw new ArgumentNullException(nameof(accountKey));
			if (accountHDScriptPubKey == null)
				throw new ArgumentNullException(nameof(accountHDScriptPubKey));
			accountHDScriptPubKey = accountHDScriptPubKey.AsHDKeyCache();
			accountKey = accountKey.AsHDKeyCache();
			return Inputs.CoinsFor(accountHDScriptPubKey, accountKey, accountKeyPath).OfType<PSBTCoin>().Concat(Outputs.CoinsFor(accountHDScriptPubKey, accountKey, accountKeyPath).OfType<PSBTCoin>());
		}

		/// <summary>
		/// Filter the keys which contains the <paramref name="accountKey"/> and <paramref name="accountKeyPath"/> in the HDKeys and whose input/output
		/// the same scriptPubKeys as <paramref name="accountHDScriptPubKey"/>.
		/// </summary>
		/// <param name="accountHDScriptPubKey">The hdScriptPubKey used to generate addresses</param>
		/// <param name="accountKey">The account key that will be used to sign (ie. 49'/0'/0')</param>
		/// <param name="accountKeyPath">The account key path</param>
		/// <returns>HD Keys matching master root key</returns>
		public IEnumerable<PSBTHDKeyMatch> HDKeysFor(IHDScriptPubKey accountHDScriptPubKey, IHDKey accountKey, RootedKeyPath accountKeyPath = null)
		{
			if (accountKey == null)
				throw new ArgumentNullException(nameof(accountKey));
			if (accountHDScriptPubKey == null)
				throw new ArgumentNullException(nameof(accountHDScriptPubKey));
			accountHDScriptPubKey = accountHDScriptPubKey.AsHDKeyCache();
			accountKey = accountKey.AsHDKeyCache();
			return Inputs.HDKeysFor(accountHDScriptPubKey, accountKey, accountKeyPath).OfType<PSBTHDKeyMatch>().Concat(Outputs.HDKeysFor(accountHDScriptPubKey, accountKey, accountKeyPath));
		}

		/// <summary>
		/// Filter the keys which contains the <paramref name="accountKey"/> and <paramref name="accountKeyPath"/>.
		/// </summary>
		/// <param name="accountKey">The account key that will be used to sign (ie. 49'/0'/0')</param>
		/// <param name="accountKeyPath">The account key path</param>
		/// <returns>HD Keys matching master root key</returns>
		public IEnumerable<PSBTHDKeyMatch> HDKeysFor(IHDKey accountKey, RootedKeyPath accountKeyPath = null)
		{
			if (accountKey == null)
				throw new ArgumentNullException(nameof(accountKey));
			accountKey = accountKey.AsHDKeyCache();
			return Inputs.HDKeysFor(accountKey, accountKeyPath).OfType<PSBTHDKeyMatch>().Concat(Outputs.HDKeysFor(accountKey, accountKeyPath));
		}

		/// <summary>
		/// Add keypath information to this PSBT for each input or output involving it
		/// </summary>
		/// <param name="masterKey">The master key of the keypaths</param>
		/// <param name="paths">The path of the public keys</param>
		/// <returns>This PSBT</returns>
		public PSBT AddKeyPath(IHDKey masterKey, params KeyPath[] paths)
		{
			return AddKeyPath(masterKey, paths.Select(p => Tuple.Create(p, null as Script)).ToArray());
		}

		/// <summary>
		/// Add keypath information to this PSBT for each input or output involving it
		/// </summary>
		/// <param name="masterKey">The master key of the keypaths</param>
		/// <param name="paths">The path of the public keys with their expected scriptPubKey</param>
		/// <returns>This PSBT</returns>
		public PSBT AddKeyPath(IHDKey masterKey, params Tuple<KeyPath, Script>[] paths)
		{
			if (masterKey == null)
				throw new ArgumentNullException(nameof(masterKey));
			if (paths == null)
				throw new ArgumentNullException(nameof(paths));

			masterKey = masterKey.AsHDKeyCache();
			var masterKeyFP = masterKey.GetPublicKey().GetHDFingerPrint();
			foreach (var path in paths)
			{
				var key = masterKey.Derive(path.Item1);
				AddKeyPath(key.GetPublicKey(), new RootedKeyPath(masterKeyFP, path.Item1), path.Item2);
			}
			return this;
		}

		/// <summary>
		/// Add keypath information to this PSBT for each input or output involving it
		/// </summary>
		/// <param name="pubkey">The public key which need to sign</param>
		/// <param name="rootedKeyPath">The keypath to this public key</param>
		/// <returns>This PSBT</returns>
		public PSBT AddKeyPath(PubKey pubkey, RootedKeyPath rootedKeyPath)
		{
			return AddKeyPath(pubkey, rootedKeyPath, null);
		}

		/// <summary>
		/// Add keypath information to this PSBT
		/// </summary>
		/// <param name="pubkey">The public key which need to sign</param>
		/// <param name="rootedKeyPath">The keypath to this public key</param>
		/// <param name="scriptPubKey">A specific scriptPubKey this pubkey is involved with</param>
		/// <returns>This PSBT</returns>
		public PSBT AddKeyPath(PubKey pubkey, RootedKeyPath rootedKeyPath, Script scriptPubKey)
		{
			if (pubkey == null)
				throw new ArgumentNullException(nameof(pubkey));
			if (rootedKeyPath == null)
				throw new ArgumentNullException(nameof(rootedKeyPath));

			var txBuilder = CreateTransactionBuilder();
			foreach (var o in this.Inputs.OfType<PSBTCoin>().Concat(this.Outputs))
			{
				var coin = o.GetCoin();
				if (coin == null)
					continue;
				if ((scriptPubKey != null && coin.ScriptPubKey == scriptPubKey) ||
					((o.GetSignableCoin() ?? coin.TryToScriptCoin(pubkey)) is Coin c && txBuilder.IsCompatibleKeyFromScriptCode(pubkey, c.GetScriptCode())) ||
					  txBuilder.IsCompatibleKeyFromScriptCode(pubkey, coin.ScriptPubKey))
				{
					o.AddKeyPath(pubkey, rootedKeyPath);
				}
			}
			return this;
		}

		/// <summary>
		/// Rebase the keypaths.
		/// If a PSBT updater only know the child HD public key but not the root one, another updater knowing the parent master key it is based on
		/// can rebase the paths.
		/// </summary>
		/// <param name="accountKey">The current account key</param>
		/// <param name="newRoot">The KeyPath with the fingerprint of the new root key</param>
		/// <returns>This PSBT</returns>
		public PSBT RebaseKeyPaths(IHDKey accountKey, RootedKeyPath newRoot)
		{
			if (accountKey == null)
				throw new ArgumentNullException(nameof(accountKey));
			if (newRoot == null)
				throw new ArgumentNullException(nameof(newRoot));
			accountKey = accountKey.AsHDKeyCache();
			var accountKeyFP = accountKey.GetPublicKey().GetHDFingerPrint();
			foreach (var o in HDKeysFor(accountKey).GroupBy(c => c.Coin))
			{
				foreach (var keyPath in o)
				{
					o.Key.HDKeyPaths.Remove(keyPath.PubKey);
					o.Key.HDKeyPaths.Add(keyPath.PubKey, newRoot.Derive(keyPath.RootedKeyPath.KeyPath));
				}
			}
			return this;
		}

		public Transaction GetOriginalTransaction()
		{
			var clone = tx.Clone();
			for (int i = 0; i < Inputs.Count; i++)
			{
				clone.Inputs[i].ScriptSig = Inputs[i].originalScriptSig;
				clone.Inputs[i].WitScript = Inputs[i].originalWitScript;
			}
			return clone;
		}

		public Transaction GetGlobalTransaction()
		{
			return tx.Clone();
		}
	}
}
