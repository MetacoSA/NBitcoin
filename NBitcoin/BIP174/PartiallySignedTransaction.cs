#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using Map = System.Collections.Generic.SortedDictionary<byte[], byte[]>;
using NBitcoin.BuilderExtensions;
using System.Diagnostics.CodeAnalysis;
using NBitcoin.BIP370;
using NBitcoin.Protocol;

namespace NBitcoin
{
	public enum PSBTVersion
	{
		PSBTv0 = 0,
		PSBTv2 = 2
	}
	static class PSBTConstants
	{
		public static byte[] PSBT_GLOBAL_ALL { get; }
		public static byte[] PSBT_IN_ALL { get; }
		public static byte[] PSBT_OUT_ALL { get; }
		static PSBTConstants()
		{
			PSBT_GLOBAL_ALL = new byte[] { PSBT_GLOBAL_VERSION, PSBT_GLOBAL_UNSIGNED_TX, PSBT_GLOBAL_XPUB };
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

		public const byte PSBT_GLOBAL_VERSION = 0xFB;
		public const byte PSBT_GLOBAL_UNSIGNED_TX = 0x00;
		public const byte PSBT_GLOBAL_XPUB = 0x01;

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
		public const byte PSBT_OUT_TAP_INTERNAL_KEY = 0x05;
		public const byte PSBT_IN_TAP_KEY_SIG = 0x13;
		public const byte PSBT_IN_TAP_INTERNAL_KEY = 0x17;
		public const byte PSBT_IN_TAP_BIP32_DERIVATION = 0x16;
		public const byte PSBT_OUT_TAP_BIP32_DERIVATION = 0x07;
		public const byte PSBT_IN_TAP_MERKLE_ROOT = 0x18;

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
		/// Use custom builder extensions to customize finalization
		/// </summary>
		public IEnumerable<BuilderExtension>? CustomBuilderExtensions { get; set; }

		/// <summary>
		/// Try to do anything that is possible to deduce PSBT information from input information
		/// </summary>
		public bool IsSmart { get; set; } = true;
		public bool SkipVerifyScript { get; set; } = false;
		public SigningOptions SigningOptions { get; set; } = new SigningOptions();
		public ScriptVerify ScriptVerify { get; internal set; } = ScriptVerify.Standard;
		/// <summary>
		/// Some opereration may strip non_witness_utxo if deemed safe. This is to prevent the PSBT from growing too large.
		/// Set this to false if you want to disable this behavior.
		/// </summary>
		public bool AutomaticUTXOTrimming { get; set; } = true;
		/// <summary>
		/// Default to false, if true, allows fee calculation without having <see cref="PSBTInput.NonWitnessUtxo"/> for non segwit inputs.
		/// </summary>
		public bool AllowUntrustedFeeCalculation { get; set; }
		public PSBTSettings Clone()
		{
			return new PSBTSettings()
			{
				SigningOptions = SigningOptions.Clone(),
				CustomBuilderExtensions = CustomBuilderExtensions?.ToArray(),
				IsSmart = IsSmart,
				ScriptVerify = ScriptVerify,
				SkipVerifyScript = SkipVerifyScript,
				AutomaticUTXOTrimming = AutomaticUTXOTrimming,
				AllowUntrustedFeeCalculation = AllowUntrustedFeeCalculation
			};
		}
	}


	public abstract class PSBT : IEquatable<PSBT>
	{
		public PSBTVersion Version { get; private set; }
		// Magic bytes
		readonly static byte[] PSBT_MAGIC_BYTES = Encoders.ASCII.DecodeData("psbt\xff");

		public SortedDictionary<BitcoinExtPubKey, RootedKeyPath> GlobalXPubs { get; } = new SortedDictionary<BitcoinExtPubKey, RootedKeyPath>(BitcoinExtPubKeyComparer.Instance);
		internal class BitcoinExtPubKeyComparer : IComparer<BitcoinExtPubKey>
		{
			BitcoinExtPubKeyComparer()
			{

			}
			public static BitcoinExtPubKeyComparer Instance { get; } = new BitcoinExtPubKeyComparer();
			public int Compare(BitcoinExtPubKey? x, BitcoinExtPubKey? y)
			{
				if (x is null && y is null)
					return 0;
				if (x is null)
					return -1;
				if (y is null)
					return 1;
				return BytesComparer.Instance.Compare(x.ExtPubKey.ToBytes(), y.ExtPubKey.ToBytes());
			}
		}

		public PSBTInputList Inputs { get; protected set; } = new();
		public PSBTOutputList Outputs { get; protected set;} = new();

		public Map Unknown { get; protected set; } = new Map();

		/// <summary>
		/// Parse PSBT from a hex or base64 string.
		/// </summary>
		/// <param name="hexOrBase64"></param>
		/// <param name="network"></param>
		/// <returns>A <see cref="PSBT0"/> or <see cref="PSBT2"/> instance.</returns>
		/// <exception cref="FormatException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		public static PSBT Parse(string hexOrBase64, Network network)
		{
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			if (hexOrBase64 == null)
				throw new ArgumentNullException(nameof(hexOrBase64));
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			byte[] raw;
			if (HexEncoder.IsWellFormed(hexOrBase64))
				raw = Encoders.Hex.DecodeData(hexOrBase64);
			else
				raw = Encoders.Base64.DecodeData(hexOrBase64);

			return Load(raw, network);
		}
		public static bool TryParse(string hexOrBase64, Network network, [MaybeNullWhen(false)] out PSBT psbt)
		{
			if (hexOrBase64 == null)
				throw new ArgumentNullException(nameof(hexOrBase64));
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			try
			{
				psbt = Parse(hexOrBase64, network);
				return true;
			}
			catch
			{
				psbt = null;
				return false;
			}
		}

		/// <summary>
		/// Load PSBT from raw bytes.
		/// </summary>
		/// <param name="rawBytes"></param>
		/// <param name="network"></param>
		/// <returns>A <see cref="PSBT0"/> or <see cref="PSBT2"/> instance.</returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="FormatException"></exception>
		public static PSBT Load(byte[] rawBytes, Network network)
		{
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			var stream = new BitcoinStream(rawBytes);
			stream.ConsensusFactory = network.Consensus.ConsensusFactory;
			var magicBytes = stream.Inner.ReadBytes(PSBT_MAGIC_BYTES.Length);
			if (!magicBytes.SequenceEqual(PSBT_MAGIC_BYTES))
				throw new FormatException("Invalid PSBT magic bytes");


			var maps = Maps.Load(stream);
			return Load(maps, network);
		}

		private static PSBT Load(Maps maps, Network network)
		{
			if (maps.Global.TryRemove<int>(PSBTConstants.PSBT_GLOBAL_VERSION, out var psbtVersion) && psbtVersion == 0)
				throw new FormatException("PSBTv0 should not include PSBT_GLOBAL_VERSION");
			return psbtVersion switch
			{
				0 => new PSBT0(maps, network),
				2 => new PSBT2(maps, network),
				_ => throw new FormatException("Invalid PSBT version")
			};
		}

		internal ConsensusFactory GetConsensusFactory()
		{
			return Network.Consensus.ConsensusFactory;
		}

		public Network Network { get; }

		internal PSBT(Maps maps, Network network, PSBTVersion version)
		{
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			Version = version;
			Network = network;

			byte[]? xpubBytes = null;
			foreach (var kv in maps.Global.RemoveAll<byte[]>([PSBTConstants.PSBT_GLOBAL_XPUB]))
			{
				xpubBytes ??= Network.GetVersionBytes(Base58Type.EXT_PUBLIC_KEY, false);
				if (xpubBytes is null)
					throw new FormatException("Invalid PSBT. No xpub version bytes");
				var (xpub, rootedKeyPath) = ParseXpub(xpubBytes, kv.Key, kv.Value);
				GlobalXPubs.Add(xpub.GetWif(Network), rootedKeyPath);
			}
		}

		internal static (ExtPubKey, RootedKeyPath) ParseXpub(byte[] xpubBytes, byte[] k, byte[] v)
		{
			if (xpubBytes is null)
				throw new FormatException("Invalid PSBT. No xpub version bytes");
			var expectedLength = 1 + xpubBytes.Length + 74;
			if (k.Length != expectedLength)
				throw new FormatException("Malformed global xpub.");
			if (!k.Skip(1).Take(xpubBytes.Length).SequenceEqual(xpubBytes))
			{
				throw new FormatException("Malformed global xpub.");
			}
			var xpub = new ExtPubKey(k, 1 + xpubBytes.Length, 74);

			KeyPath path = KeyPath.FromBytes(v.Skip(4).ToArray());
			var rootedKeyPath = new RootedKeyPath(new HDFingerprint(v.Take(4).ToArray()), path);
			return (xpub, rootedKeyPath);
		}

		public PSBT AddCoins(params ICoin?[] coins)
		{
			if (coins == null)
				return this;
			foreach (var coin in coins)
			{
				if (coin is null)
					continue;
				var indexedInput = this.Inputs.FindIndexedInput(coin.Outpoint);
				if (indexedInput == null)
					continue;
				indexedInput.UpdateFromCoin(coin);
			}
			foreach (var coin in coins)
			{
				if (coin is null)
					continue;
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
				if (txsById.TryGetValue(input.PrevOut.Hash, out var tx))
				{
					if (input.PrevOut.N >= tx.Outputs.Count)
						continue;
					var output = tx.Outputs[input.PrevOut.N];
					input.NonWitnessUtxo = tx;
					if (input is PSBT0.PSBT0Input input0)
						input0.non_witness_utxo_check = input.PrevOut.Hash;
					if (Network.Consensus.NeverNeedPreviousTxForSigning ||
					input.GetCoin()?.IsMalleable is false)
						input.WitnessUtxo = output;
					if (Settings.AutomaticUTXOTrimming)
						input.TrySlimUTXO();
				}
			}
			return this;
		}

		/// <summary>
		/// If an other PSBT has a specific field and this does not have it, then inject that field to this.
		/// otherwise leave it as it is.
		///
		/// If you need to call this on transactions with different global transaction, use <see cref="PSBT.UpdateFrom(PSBT)"/> instead.
		/// </summary>
		/// <param name="other">Another PSBT to takes information from</param>
		/// <exception cref="System.ArgumentException">Can not Combine PSBT with different global tx.</exception>
		/// <returns>This instance</returns>
		public PSBT Combine(PSBT other)
		{
			if (other == null)
			{
				throw new ArgumentNullException(nameof(other));
			}

			if (other.GetGlobalTransaction(true).GetHash() != this.GetGlobalTransaction(true).GetHash())
				throw new ArgumentException(paramName: nameof(other), message: "Can not Combine PSBT with different global tx.");

			foreach (var xpub in other.GlobalXPubs)
				this.GlobalXPubs.TryAdd(xpub.Key, xpub.Value);

			for (int i = 0; i < Inputs.Count; i++)
				this.Inputs[i].UpdateFrom(other.Inputs[i]);

			for (int i = 0; i < Outputs.Count; i++)
				this.Outputs[i].UpdateFrom(other.Outputs[i]);

			foreach (var uk in other.Unknown)
				this.Unknown.TryAdd(uk.Key, uk.Value);

			return this;
		}

		/// <summary>
		/// If an other PSBT has a specific field and this does not have it, then inject that field to this.
		/// otherwise leave it as it is.
		///
		/// Contrary to <see cref="PSBT.Combine(PSBT)"/>, it can be called on PSBT with a different global transaction.
		/// </summary>
		/// <param name="other">Another PSBT to takes information from</param>
		/// <returns>This instance</returns>
		public virtual PSBT UpdateFrom(PSBT other)
		{
			if (other == null)
			{
				throw new ArgumentNullException(nameof(other));
			}

			foreach (var xpub in other.GlobalXPubs)
				this.GlobalXPubs.TryAdd(xpub.Key, xpub.Value);

			foreach (var otherInput in other.Inputs)
				this.Inputs.FindIndexedInput(otherInput.PrevOut)?.UpdateFrom(otherInput);


			foreach (var otherOutput in other.Outputs)
				foreach (var thisOutput in this.Outputs.Where(o => o.ScriptPubKey == otherOutput.ScriptPubKey))
					thisOutput.UpdateFrom(otherOutput);

			foreach (var uk in other.Unknown)
				this.Unknown.TryAdd(uk.Key, uk.Value);

			return this;
		}

		/// <summary>
		/// Join two PSBT into one CoinJoin PSBT.
		/// This is an immutable method.
		/// TODO: May need assertion for sighash type?
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public abstract PSBT CoinJoin(PSBT other);

		public PSBT Finalize()
		{
			if (!TryFinalize(out var errors))
				throw new PSBTException(errors);
			return this;
		}

		public bool TryFinalize([MaybeNullWhen(true)] out IList<PSBTError> errors)
		{
			var signingOptions = GetSigningOptions(null);
			var localErrors = new List<PSBTError>();
			foreach (var input in Inputs)
			{
				if (!input.TryFinalizeInput(signingOptions, out var e))
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

		internal SigningOptions GetSigningOptions(SigningOptions? signingOptions)
		{
			signingOptions ??= Settings.SigningOptions;
			if (signingOptions.PrecomputedTransactionData is null)
			{
				signingOptions = signingOptions.Clone();
				signingOptions.PrecomputedTransactionData = PrecomputeTransactionData();
			}
			return signingOptions;
		}

		public bool IsReadyToSign()
		{
			return IsReadyToSign(out _);
		}
		public bool IsReadyToSign([MaybeNullWhen(true)] out PSBTError[] errors)
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
		public PSBT SignAll(ScriptPubKeyType scriptPubKeyType, IHDKey accountKey, RootedKeyPath accountKeyPath)
		{
			if (accountKey == null)
				throw new ArgumentNullException(nameof(accountKey));
			return SignAll(new HDKeyScriptPubKey(accountKey, scriptPubKeyType), accountKey, accountKeyPath);
		}

		/// <summary>
		/// Sign all inputs which derive <paramref name="accountKey"/> of type <paramref name="scriptPubKeyType"/>.
		/// </summary>
		/// <param name="scriptPubKeyType">The way to derive addresses from the accountKey</param>
		/// <param name="accountKey">The account key with which to sign</param>
		/// <param name="sigHash">The SigHash</param>
		/// <returns>This PSBT</returns>
		public PSBT SignAll(ScriptPubKeyType scriptPubKeyType, IHDKey accountKey)
		{
			if (accountKey == null)
				throw new ArgumentNullException(nameof(accountKey));
			return SignAll(new HDKeyScriptPubKey(accountKey, scriptPubKeyType), accountKey);
		}

		/// <summary>
		/// Sign all inputs which derive addresses from <paramref name="accountHDScriptPubKey"/> and that need to be signed by <paramref name="accountKey"/>.
		/// </summary>
		/// <param name="accountHDScriptPubKey">The address generator</param>
		/// <param name="accountKey">The account key with which to sign</param>
		/// <param name="sigHash">The SigHash</param>
		/// <returns>This PSBT</returns>
		public PSBT SignAll(IHDScriptPubKey accountHDScriptPubKey, IHDKey accountKey)
		{
			return SignAll(accountHDScriptPubKey, accountKey, null);
		}

		/// <summary>
		/// Sign all inputs which derive addresses from <paramref name="accountHDScriptPubKey"/> and that need to be signed by <paramref name="accountKey"/>.
		/// </summary>
		/// <param name="accountHDScriptPubKey">The address generator</param>
		/// <param name="accountKey">The account key with which to sign</param>
		/// <param name="accountKeyPath">The account key path (eg. [masterFP]/49'/0'/0')</param>
		/// <returns>This PSBT</returns>
		public PSBT SignAll(IHDScriptPubKey? accountHDScriptPubKey, IHDKey accountKey, RootedKeyPath? accountKeyPath)
		{
			if (accountKey == null)
				throw new ArgumentNullException(nameof(accountKey));
			accountHDScriptPubKey = accountHDScriptPubKey?.AsHDKeyCache();
			accountKey = accountKey.AsHDKeyCache();
			Money total = Money.Zero;

			var signingOptions = GetSigningOptions(null);

			foreach (var o in Inputs.CoinsFor(accountHDScriptPubKey, accountKey, accountKeyPath))
			{
				o.TrySign(accountHDScriptPubKey, accountKey, accountKeyPath, signingOptions);
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
			fee = this.GetGlobalTransaction(true).GetFee(GetAllCoins(true).ToArray());
			return fee != null;
		}

		/// <summary>
		/// Returns the fee of the transaction being signed
		/// </summary>
		/// <returns>The fees</returns>
		/// <exception cref="System.InvalidOperationException">Not enough information to know about the fee</exception>
		public Money GetFee()
		{
			if (!TryGetFee(out var fee))
				throw new InvalidOperationException("Not enough information to know about the fee");
			return fee;
		}

		/// <summary>
		/// Returns the fee rate of the transaction. If the PSBT is finalized, then the exact rate is returned, else an estimation is made.
		/// </summary>
		/// <param name="estimatedFeeRate"></param>
		/// <returns>True if could get the estimated fee rate</returns>
		public bool TryGetEstimatedFeeRate([MaybeNullWhen(false)] out FeeRate estimatedFeeRate)
		{
			if (IsAllFinalized())
			{
				estimatedFeeRate = ExtractTransaction().GetFeeRate(GetAllCoins(true).ToArray());
				return estimatedFeeRate != null;
			}
			if (!TryGetFee(out var fee))
			{
				estimatedFeeRate = null;
				return false;
			}
			var transactionBuilder = CreateTransactionBuilder();
			transactionBuilder.AddCoins(GetAllCoins(false));
			try
			{
				var vsize = transactionBuilder.EstimateSize(this.GetGlobalTransaction(true), true);
				estimatedFeeRate = new FeeRate(fee, vsize);
				return true;
			}
			catch
			{
				estimatedFeeRate = null;
				return false;
			}
		}

		/// <summary>
		/// Returns the virtual transaction size of the transaction. If the PSBT is finalized, then the exact virtual size.
		/// </summary>
		/// <param name="vsize">The calculated virtual size</param>
		/// <returns>True if could get the virtual size could get estimated</returns>
		public bool TryGetVirtualSize(out int vsize)
		{
			if (IsAllFinalized())
			{
				vsize = ExtractTransaction().GetVirtualSize();
				return true;
			}
			var transactionBuilder = CreateTransactionBuilder();
			transactionBuilder.AddCoins(GetAllCoins(false));
			try
			{
				vsize = transactionBuilder.EstimateSize(this.GetGlobalTransaction(true), true);
				return true;
			}
			catch
			{
				vsize = -1;
				return false;
			}
		}

		/// <summary>
		/// Returns the fee rate of the transaction. If the PSBT is finalized, then the exact rate is returned, else an estimation is made.
		/// </summary>
		/// <returns>The estimated fee</returns>
		/// <exception cref="System.InvalidOperationException">Not enough information to know about the fee rate</exception>
		public FeeRate GetEstimatedFeeRate()
		{
			if (!TryGetEstimatedFeeRate(out var feeRate))
				throw new InvalidOperationException("Not enough information to know about the fee rate");
			return feeRate;
		}

		public PSBT SignWithKeys(params ISecret[] keys)
		{
			return SignWithKeys(keys.Select(k => k.PrivateKey).ToArray());
		}

		public PSBT SignWithKeys(params Key[] keys)
		{
			var errors = CheckSanity();
			var hasError = new HashSet<uint>(errors.Select(e => e.InputIndex));
			var signingOptions = GetSigningOptions(null);
			foreach (var key in keys)
			{
				foreach (var input in this.Inputs.Where(i => !hasError.Contains(i.Index)))
				{
					input.Sign(key, signingOptions);
				}
			}
			return this;
		}

		/// <summary>
		/// Returns a data structure precomputing some hash values that are needed for all inputs to be signed in the transaction.
		/// </summary>
		/// <returns>The PrecomputedTransactionData</returns>
		/// <exception cref="NBitcoin.PSBTException">Throw if the PSBT is missing some previous outputs.</exception>
		public PrecomputedTransactionData PrecomputeTransactionData()
		{
			var outputs = GetSpentTxOuts(out var errors);
			var tx = GetGlobalTransaction(true);
			if (errors != null)
				return tx.PrecomputeTransactionData();
			return tx.PrecomputeTransactionData(outputs);
		}

		public TransactionValidator CreateTransactionValidator()
		{
			var outputs = GetSpentTxOuts(out var errors);
			if (errors != null)
				throw new PSBTException(errors);
			return this.GetGlobalTransaction(true).CreateValidator(outputs);
		}
		internal bool TryCreateTransactionValidator([MaybeNullWhen(false)] out TransactionValidator validator, [MaybeNullWhen(true)] out IList<PSBTError> errors)
		{
			var outputs = GetSpentTxOuts(out errors);
			if (errors != null)
			{
				validator = null;
				return false;
			}
			validator = GetGlobalTransaction(true).CreateValidator(outputs);
			return true;
		}

		private TxOut[] GetSpentTxOuts(out IList<PSBTError>? errors)
		{
			errors = null;
			TxOut[] spentOutputs = new TxOut[Inputs.Count];
			foreach (var input in Inputs)
			{
				if (input.GetTxOut() is TxOut txOut)
					spentOutputs[input.Index] = txOut;
				else
				{
					errors ??= new List<PSBTError>();
					errors.Add(new PSBTError((uint)input.Index, "Some inputs are missing witness_utxo or non_witness_utxo"));
				}
			}
			return spentOutputs;
		}

		internal TransactionBuilder CreateTransactionBuilder()
		{
			var transactionBuilder = Network.CreateTransactionBuilder();
			if (Settings.CustomBuilderExtensions != null)
			{
				transactionBuilder.Extensions.Clear();
				transactionBuilder.Extensions.AddRange(Settings.CustomBuilderExtensions);
			}
			transactionBuilder.SetSigningOptions(Settings.SigningOptions.Clone());
			return transactionBuilder;
		}

		private IEnumerable<ICoin> GetAllCoins(bool forFee)
		{
			foreach (var i in this.Inputs)
			{
				var c = i.GetSignableCoin() ?? i.GetCoin();
				if (c is null)
					continue;
				if (forFee
					&& !Network.Consensus.NeverNeedPreviousTxForSigning
					&& !Settings.AllowUntrustedFeeCalculation)
				{
					if (c.IsMalleable && i.NonWitnessUtxo is null)
						continue;
				}
				yield return c;
			}
		}
		/// <summary>
		/// Extract the fully signed transaction from the PSBT
		/// </summary>
		/// <returns>The fully signed transaction</returns>
		/// <exception cref="System.InvalidOperationException">PSBTInputs are not all finalized</exception>
		public Transaction ExtractTransaction()
		{
			if (!this.CanExtractTransaction())
				throw new InvalidOperationException("PSBTInputs are not all finalized!");
			return ForceExtractTransaction();
		}
		internal Transaction ForceExtractTransaction()
		{
			var copy = GetGlobalTransaction();
			for (var i = 0; i < copy.Inputs.Count; i++)
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


		/// <summary>
		/// Get the expected hash once the transaction is fully signed
		/// </summary>
		/// <param name="hash">The hash once fully signed</param>
		/// <returns>True if we can know the expected hash. False if we can't (unsigned non-segwit).</returns>
		public bool TryGetFinalizedHash([MaybeNullWhen(false)] out uint256 hash)
		{
			var tx = GetGlobalTransaction();
			for (int i = 0; i < Inputs.Count; i++)
			{
				var utxo = Inputs[i].GetTxOut();
				if (Inputs[i].IsFinalized())
				{
					tx.Inputs[i].ScriptSig = Inputs[i].FinalScriptSig ?? Script.Empty;
					tx.Inputs[i].WitScript = Inputs[i].FinalScriptWitness ?? Script.Empty;
					if (tx.Inputs[i].ScriptSig == Script.Empty
						&& (utxo is null || utxo.ScriptPubKey.IsScriptType(ScriptType.P2SH)))
					{
						hash = null;
						return false;
					}
				}
				else if (utxo is null ||
						!Network.Consensus.SupportSegwit)
				{
					hash = null;
					return false;
				}
				else if (utxo.ScriptPubKey.IsScriptType(ScriptType.P2SH) &&
					Inputs[i].RedeemScript is Script p2shRedeem &&
					(p2shRedeem.IsScriptType(ScriptType.P2WSH) ||
					 p2shRedeem.IsScriptType(ScriptType.P2WPKH)))
				{
					tx.Inputs[i].ScriptSig = PayToScriptHashTemplate.Instance.GenerateScriptSig(null as byte[][], p2shRedeem);
				}
				else if (utxo.ScriptPubKey.IsMalleable)
				{
					hash = null;
					return false;
				}
			}
			hash = tx.GetHash();
			return true;
		}

		#region IBitcoinSerializable Members

		protected static uint DefaultKeyLen = 1;

		protected virtual void ParseGlobals(Map map)
		{

		}

		internal virtual void FillMap(Map map)
		{
			byte[]? xpubVersionBytes = null;
			foreach (var xpub in GlobalXPubs)
			{
				if (xpub.Key.Network != Network)
					throw new InvalidOperationException("Invalid key inside the global xpub collection");
				var path = xpub.Value.KeyPath.ToBytes();
				var pathInfo = xpub.Value.MasterFingerprint.ToBytes().Concat(path);

				xpubVersionBytes ??= Network.GetVersionBytes(Base58Type.EXT_PUBLIC_KEY, false)!;
				byte[] key = [PSBTConstants.PSBT_GLOBAL_XPUB, .. xpubVersionBytes, .. xpub.Key.ExtPubKey.ToBytes()];
				var value = pathInfo;
				map.Add(key, value);
			}
			foreach (var kv in Unknown)
				map.Add(kv.Key, kv.Value);
		}
		internal void FillMaps(Maps maps)
		{
			var globalMap = maps.NewMap();
			FillMap(globalMap);
			// Write inputs
			foreach (var psbtin in Inputs)
			{
				psbtin.FillMap(maps.NewMap());
			}
			// Write outputs
			foreach (var psbtout in Outputs)
			{
				psbtout.FillMap(maps.NewMap());
			}
		}

		#endregion

		public override string ToString()
		{
			var strWriter = new StringWriter();
			var jsonWriter = new JsonTextWriter(strWriter);
			jsonWriter.Formatting = Formatting.Indented;
			jsonWriter.WriteStartObject();
			jsonWriter.WritePropertyValue("version", Version);
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
			this.WriteCore(jsonWriter);
			if (GlobalXPubs.Count != 0)
			{
				jsonWriter.WritePropertyName("xpubs");
				jsonWriter.WriteStartArray();
				foreach (var xpub in GlobalXPubs)
				{
					jsonWriter.WriteStartObject();
					jsonWriter.WritePropertyValue("key", xpub.Key.ToString());
					jsonWriter.WritePropertyValue("value", xpub.Value.ToString());
					jsonWriter.WriteEndObject();
				}
				jsonWriter.WriteEndArray();
			}
			if (Unknown.Count != 0)
			{
				jsonWriter.WritePropertyName("unknown");
				jsonWriter.WriteStartObject();
				foreach (var el in Unknown)
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

		protected virtual void WriteCore(JsonTextWriter jsonWriter)
		{
		}

		public byte[] ToBytes()
		{
			MemoryStream ms = new MemoryStream();
			// magic bytes
			ms.Write(PSBT_MAGIC_BYTES, 0, PSBT_MAGIC_BYTES.Length);
			var maps = new Maps();
			FillMaps(maps);
			maps.ToBytes(ms);
			return ms.ToArrayEfficient();
		}

		/// <summary>
		/// Clone this PSBT
		/// </summary>
		/// <returns>A cloned PSBT</returns>
		public PSBT Clone()
		{
			var maps = new Maps();
			FillMaps(maps);
			var clone = PSBT.Load(maps, Network);
			clone.Settings = Settings.Clone();
			return clone;
		}

		public string ToBase64() => Encoders.Base64.EncodeData(this.ToBytes());
		public string ToHex() => Encoders.Hex.EncodeData(this.ToBytes());

		public override bool Equals(object? obj)
		{
			var item = obj as PSBT;
			if (item == null)
				return false;
			return item.Equals(this);
		}

		public bool Equals(PSBT? b)
		{
			if (b is null)
				return false;
			return this.ToBytes().SequenceEqual(b.ToBytes());
		}
		public override int GetHashCode() => Utils.GetHashCode(this.ToBytes());

		public static PSBT FromTransaction(Transaction transaction, Network network) => FromTransaction(transaction, network, PSBTVersion.PSBTv0);
		public static PSBT FromTransaction(Transaction transaction, Network network, PSBTVersion version)
		{
			if (transaction == null)
				throw new ArgumentNullException(nameof(transaction));
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			foreach (var input in transaction.Inputs)
			{
				if (!Script.IsNullOrEmpty(input.ScriptSig))
					throw new ArgumentException("The transaction should not have any scriptSig set. You can use Transaction.RemoveSignatures() to remove them.", nameof(transaction));
				if (!WitScript.IsNullOrEmpty(input.WitScript))
					throw new ArgumentException("The transaction should not have any witScript set. You can use Transaction.RemoveSignatures() to remove them.", nameof(transaction));
			}
			return version switch
			{
				PSBTVersion.PSBTv0 => new PSBT0(transaction, network),
				PSBTVersion.PSBTv2 => new PSBT2(transaction, network),
				_ => throw new NotSupportedException("Unsupported PSBT version")
			};
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
					if (o is PSBTInput ii && ii.IsFinalized())
						continue;
					var txout = o.GetTxOut();
					if (txout == null)
						continue;
					if (txout.ScriptPubKey == p2sh)
					{
						o.RedeemScript = redeem;
					}
					else if (txout.ScriptPubKey == p2wsh)
					{
						o.WitnessScript = redeem;
						if (o is PSBTInput i && Settings.AutomaticUTXOTrimming)
							i.TrySlimUTXO();
					}
					else if (txout.ScriptPubKey == p2shp2wsh)
					{
						o.WitnessScript = redeem;
						o.RedeemScript = redeem.WitHash.ScriptPubKey;
						if (o is PSBTInput i && Settings.AutomaticUTXOTrimming)
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
		public Money GetBalance(ScriptPubKeyType scriptPubKeyType, IHDKey accountKey, RootedKeyPath? accountKeyPath = null)
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
		public Money GetBalance(IHDScriptPubKey accountHDScriptPubKey, IHDKey accountKey, RootedKeyPath? accountKeyPath = null)
		{
			if (accountHDScriptPubKey == null)
				throw new ArgumentNullException(nameof(accountHDScriptPubKey));
			Money total = Money.Zero;
			foreach (var o in CoinsFor(accountHDScriptPubKey, accountKey, accountKeyPath))
			{
				var amount = o.GetTxOut()?.Value;
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
		public IEnumerable<PSBTCoin> CoinsFor(IHDScriptPubKey? accountHDScriptPubKey, IHDKey accountKey, RootedKeyPath? accountKeyPath = null)
		{
			if (accountKey == null)
				throw new ArgumentNullException(nameof(accountKey));
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
		public IEnumerable<PSBTHDKeyMatch> HDKeysFor(IHDScriptPubKey? accountHDScriptPubKey, IHDKey accountKey, RootedKeyPath? accountKeyPath = null)
		{
			if (accountKey == null)
				throw new ArgumentNullException(nameof(accountKey));
			accountKey = accountKey.AsHDKeyCache();
			return Inputs.HDKeysFor(accountHDScriptPubKey, accountKey, accountKeyPath).OfType<PSBTHDKeyMatch>().Concat(Outputs.HDKeysFor(accountHDScriptPubKey, accountKey, accountKeyPath));
		}

		/// <summary>
		/// Filter the keys which contains the <paramref name="accountKey"/> and <paramref name="accountKeyPath"/>.
		/// </summary>
		/// <param name="accountKey">The account key that will be used to sign (ie. 49'/0'/0')</param>
		/// <param name="accountKeyPath">The account key path</param>
		/// <returns>HD Keys matching master root key</returns>
		public IEnumerable<PSBTHDKeyMatch> HDKeysFor(IHDKey accountKey, RootedKeyPath? accountKeyPath = null)
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
		public PSBT AddKeyPath(IHDKey masterKey, params Tuple<KeyPath, Script?>[] paths)
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
		/// Add keypath information to this PSBT, if the PSBT all finalized this operation is a no-op
		/// </summary>
		/// <param name="pubkey">The public key which need to sign</param>
		/// <param name="rootedKeyPath">The keypath to this public key</param>
		/// <param name="scriptPubKey">A specific scriptPubKey this pubkey is involved with</param>
		/// <returns>This PSBT</returns>
		public PSBT AddKeyPath(PubKey pubkey, RootedKeyPath rootedKeyPath, Script? scriptPubKey)
		{
			if (pubkey == null)
				throw new ArgumentNullException(nameof(pubkey));
			if (rootedKeyPath == null)
				throw new ArgumentNullException(nameof(rootedKeyPath));
			if (IsAllFinalized())
				return this;
			var txBuilder = CreateTransactionBuilder();
			foreach (var o in this.Inputs.OfType<PSBTCoin>().Concat(this.Outputs))
			{
				if (o is PSBTInput i && i.IsFinalized())
					continue;
				var txout = o.GetTxOut();
				if (txout == null)
					continue;
				
				if ((scriptPubKey is not null && txout.ScriptPubKey == scriptPubKey) ||
					(GetScriptCode(o, pubkey) is Script s && txBuilder.IsCompatibleKeyFromScriptCode(pubkey, s)) ||
					  txBuilder.IsCompatibleKeyFromScriptCode(pubkey, txout.ScriptPubKey))
				{
					o.AddKeyPath(pubkey, rootedKeyPath);
				}
			}
			return this;
		}

		private Script? GetScriptCode(PSBTCoin coin, PubKey pubKey)
		{
			var input = coin switch
			{
				PSBTInput i => i,
				PSBTOutput o => new PSBT2Input(OutPoint.Zero, this, 0)
				{
					WitnessScript = o.WitnessScript,
					RedeemScript = o.RedeemScript,
					WitnessUtxo = o.GetTxOut()
				},
				_ => null
			};
			return (input?.GetSignableCoin() ?? input?.GetCoin()?.TryToScriptCoin(pubKey))?.GetScriptCode();
		}
		
		/// <summary>
		/// Rebase the keypaths.
		/// If a PSBT updater only know the child HD public key but not the root one, another updater knowing the parent master key it is based on
		/// can rebase the paths. If the PSBT is all finalized this operation is a no-op
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
			if (IsAllFinalized())
				return this;
			accountKey = accountKey.AsHDKeyCache();
			foreach (var o in HDKeysFor(accountKey).GroupBy(c => c.Coin))
			{
				if (o.Key is PSBTInput i && i.IsFinalized())
					continue;
				foreach (var keyPath in o)
				{
					if (keyPath.RootedKeyPath.MasterFingerprint != newRoot.MasterFingerprint)
					{
						if (keyPath.PubKey is PubKey ecdsa)
						{
							o.Key.HDKeyPaths.Remove(ecdsa);
							o.Key.HDKeyPaths.Add(ecdsa, newRoot.Derive(keyPath.RootedKeyPath.KeyPath));
						}
						else if (keyPath.PubKey is TaprootPubKey taproot)
						{
							var kp = o.Key.HDTaprootKeyPaths[taproot];
							o.Key.HDTaprootKeyPaths.Remove(taproot);
							o.Key.HDTaprootKeyPaths.Add(taproot, new TaprootKeyPath(newRoot.Derive(keyPath.RootedKeyPath.KeyPath), kp.LeafHashes));
						}
					}
				}
			}
			foreach (var xpub in GlobalXPubs.ToList())
			{
				if (xpub.Key.ExtPubKey.PubKey.Equals(accountKey.GetPublicKey()))
				{
					if (xpub.Value.MasterFingerprint != newRoot.MasterFingerprint)
					{
						GlobalXPubs.Remove(xpub.Key);
						GlobalXPubs.Add(xpub.Key, newRoot.Derive(xpub.Value.KeyPath));
					}
				}
			}
			return this;
		}
		public Transaction GetGlobalTransaction() => GetGlobalTransaction(false);
		internal abstract Transaction GetGlobalTransaction(bool @unsafe);

		/// <summary>
		/// If this instance is <see cref="PSBT0"/>, returns it.
		/// Else, converts to a <see cref="PSBT0"/> instance.
		/// </summary>
		/// <returns>This instance, or a conversion</returns>
		public PSBT0 ToPSBTv0()
		{
			if (this is PSBT0 p)
				return p;
			var maps = new Maps();
			FillMaps(maps);
			var global = this.GetGlobalTransaction(true);
			int i = 0;
			foreach (var b in PSBT2Constants.PSBT_V0_GLOBAL_EXCLUSIONSET)
				maps[i].Remove([b]);
			maps[i].Add(PSBTConstants.PSBT_GLOBAL_UNSIGNED_TX, global.ToBytes());
			i++;
			for (int o = 0; o < this.Inputs.Count; o++)
			{
				foreach (var b in PSBT2Constants.PSBT_V0_INPUT_EXCLUSIONSET)
					maps[i].Remove([b]);
				i++;
			}
			for (int o = 0; o < this.Outputs.Count; o++)
			{
				foreach (var b in PSBT2Constants.PSBT_V0_OUTPUT_EXCLUSIONSET)
					maps[i].Remove([b]);
				i++;
			}
			return new PSBT0(maps, Network);
		}
		/// <summary>
		/// If this instance is <see cref="PSBT2"/>, returns it.
		/// Else, converts to a <see cref="PSBT2"/> instance.
		/// </summary>
		/// <returns>This instance, or a conversion</returns>
		public PSBT2 ToPSBTv2()
		{
			if (this is PSBT2 p)
				return p;
			var maps = new Maps();
			FillMaps(maps);
			var tx = GetGlobalTransaction(true);
			maps[0].Remove([PSBTConstants.PSBT_GLOBAL_UNSIGNED_TX]);
			maps[0].Add(PSBT2Constants.PSBT_GLOBAL_TX_VERSION, tx.Version);
			maps[0].Add(PSBT2Constants.PSBT_GLOBAL_INPUT_COUNT, new VarInt((uint)tx.Inputs.Count));
			maps[0].Add(PSBT2Constants.PSBT_GLOBAL_OUTPUT_COUNT, new VarInt((uint)tx.Outputs.Count));
			int i = 1;
			foreach (var txin in tx.Inputs)
			{
				PSBT2Input.FillMap(maps[i++], txin);
			}
			foreach (var txout in tx.Outputs)
			{
				PSBT2Output.FillMap(maps[i++], txout);
			}
			return new PSBT2(maps, Network);
		}
	}
}
#nullable disable
