using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public enum RawFormat
	{
		Satoshi,
		BlockExplorer,
	}

	[Flags]
	public enum TransactionOptions : uint
	{
		None = 0x00000000,
		Witness = 0x40000000,
		All = Witness
	}

	//https://en.bitcoin.it/wiki/Transactions
	//https://en.bitcoin.it/wiki/Protocol_specification
	public class Transaction : IBitcoinSerializable
	{
		public bool RBF
		{
			get
			{
				return Inputs.Any(i => i.Sequence < 0xffffffff - 1);
			}
		}

		uint nVersion = 1;

		public uint Version
		{
			get
			{
				return nVersion;
			}
			set
			{
				nVersion = value;
			}
		}
		TxInList vin;
		TxOutList vout;
		LockTime nLockTime;

		public Transaction()
		{
			vin = new TxInList(this);
			vout = new TxOutList(this);
		}

		public Transaction(string hex, ProtocolVersion version = ProtocolVersion.PROTOCOL_VERSION)
			: this()
		{
			this.FromBytes(Encoders.Hex.DecodeData(hex), version);
		}

		public Transaction(byte[] bytes)
			: this()
		{
			this.FromBytes(bytes);
		}

		public Money TotalOut
		{
			get
			{
				return Outputs.Sum(v => v.Value);
			}
		}

		public LockTime LockTime
		{
			get
			{
				return nLockTime;
			}
			set
			{
				nLockTime = value;
			}
		}

		public TxInList Inputs
		{
			get
			{
				return vin;
			}
		}
		public TxOutList Outputs
		{
			get
			{
				return vout;
			}
		}

		//Since it is impossible to serialize a transaction with 0 input without problems during deserialization with wit activated, we fit a flag in the version to workaround it
		const uint NoDummyInput = (1 << 27);

		#region IBitcoinSerializable Members

		public virtual void ReadWrite(BitcoinStream stream)
		{
			var witSupported = (((uint)stream.TransactionOptions & (uint)TransactionOptions.Witness) != 0) &&
								stream.ProtocolVersion >= ProtocolVersion.WITNESS_VERSION;

			byte flags = 0;
			if(!stream.Serializing)
			{
				stream.ReadWrite(ref nVersion);
				/* Try to read the vin. In case the dummy is there, this will be read as an empty vector. */
				stream.ReadWrite<TxInList, TxIn>(ref vin);

				var hasNoDummy = (nVersion & NoDummyInput) != 0 && vin.Count == 0;
				if(hasNoDummy)
					nVersion = nVersion & ~NoDummyInput;

				if(vin.Count == 0 && witSupported && !hasNoDummy)
				{
					/* We read a dummy or an empty vin. */
					stream.ReadWrite(ref flags);
					if(flags != 0)
					{
						/* Assume we read a dummy and a flag. */
						stream.ReadWrite<TxInList, TxIn>(ref vin);
						vin.Transaction = this;
						stream.ReadWrite<TxOutList, TxOut>(ref vout);
						vout.Transaction = this;
					}
					else
					{
						/* Assume read a transaction without output. */
						vout = new TxOutList();
						vout.Transaction = this;
					}
				}
				else
				{
					/* We read a non-empty vin. Assume a normal vout follows. */
					stream.ReadWrite<TxOutList, TxOut>(ref vout);
					vout.Transaction = this;
				}
				if(((flags & 1) != 0) && witSupported)
				{
					/* The witness flag is present, and we support witnesses. */
					flags ^= 1;
					Witness wit = new Witness(Inputs);
					wit.ReadWrite(stream);
				}
				if(flags != 0)
				{
					/* Unknown flag in the serialization */
					throw new FormatException("Unknown transaction optional data");
				}
			}
			else
			{
				var version = vin.Count == 0 && vout.Count > 0 ? nVersion | NoDummyInput : nVersion;
				stream.ReadWrite(ref version);

				if(witSupported)
				{
					/* Check whether witnesses need to be serialized. */
					if(HasWitness)
					{
						flags |= 1;
					}
				}
				if(flags != 0)
				{
					/* Use extended format in case witnesses are to be serialized. */
					TxInList vinDummy = new TxInList();
					stream.ReadWrite<TxInList, TxIn>(ref vinDummy);
					stream.ReadWrite(ref flags);
				}
				stream.ReadWrite<TxInList, TxIn>(ref vin);
				vin.Transaction = this;
				stream.ReadWrite<TxOutList, TxOut>(ref vout);
				vout.Transaction = this;
				if((flags & 1) != 0)
				{
					Witness wit = new Witness(this.Inputs);
					wit.ReadWrite(stream);
				}
			}
			stream.ReadWriteStruct(ref nLockTime);
		}

		#endregion

		public uint256 GetHash()
		{
			if(_Hashes != null && _Hashes[0] != null)
			{
				return _Hashes[0];
			}
			MemoryStream ms = new MemoryStream();
			this.ReadWrite(new BitcoinStream(ms, true)
			{
				TransactionOptions = TransactionOptions.None
			});
			var h = Hashes.Hash256(ms.ToArrayEfficient());
			if(_Hashes != null)
			{
				_Hashes[0] = h;
			}
			return h;
		}

		/// <summary>
		/// If called, GetHash and GetWitHash become cached, only use if you believe the instance will not be modified after calculation. Calling it a second type invalidate the cache.
		/// </summary>
		public void CacheHashes()
		{
			_Hashes = new uint256[2];
		}

		public Transaction Clone(bool cloneCache)
		{
			var clone = BitcoinSerializableExtensions.Clone(this);
			if(cloneCache)
				clone._Hashes = _Hashes.ToArray();
			return clone;
		}

		uint256[] _Hashes = null;

		public uint256 GetWitHash()
		{
			if(!HasWitness)
				return GetHash();
			if(_Hashes != null && _Hashes[1] != null)
			{
				return _Hashes[1];
			}
			MemoryStream ms = new MemoryStream();
			this.ReadWrite(new BitcoinStream(ms, true)
			{
				TransactionOptions = TransactionOptions.Witness
			});
			var h = Hashes.Hash256(ms.ToArrayEfficient());
			if(_Hashes != null)
			{
				_Hashes[1] = h;
			}
			return h;
		}
		public uint256 GetSignatureHash(ICoin coin, SigHash sigHash = SigHash.All)
		{
			return Inputs.AsIndexedInputs().ToArray()[GetIndex(coin)].GetSignatureHash(coin, sigHash);
		}
		public TransactionSignature SignInput(ISecret secret, ICoin coin, SigHash sigHash = SigHash.All)
		{
			return SignInput(secret.PrivateKey, coin, sigHash);
		}
		public TransactionSignature SignInput(Key key, ICoin coin, SigHash sigHash = SigHash.All)
		{
			return Inputs.AsIndexedInputs().ToArray()[GetIndex(coin)].Sign(key, coin, sigHash);
		}

		private int GetIndex(ICoin coin)
		{
			for(int i = 0; i < Inputs.Count; i++)
			{
				if(Inputs[i].PrevOut == coin.Outpoint)
					return i;
			}
			throw new ArgumentException("The coin is not being spent by this transaction", "coin");
		}

		public bool IsCoinBase
		{
			get
			{
				return (Inputs.Count == 1 && Inputs[0].PrevOut.IsNull);
			}
		}

		public static uint CURRENT_VERSION = 2;
		public static uint MAX_STANDARD_TX_SIZE = 100000;

		public TxOut AddOutput(Money money, IDestination destination)
		{
			return AddOutput(new TxOut(money, destination));
		}
		public TxOut AddOutput(Money money, Script scriptPubKey)
		{
			return AddOutput(new TxOut(money, scriptPubKey));
		}
		public TxOut AddOutput(TxOut @out)
		{
			this.vout.Add(@out);
			return @out;
		}
		public TxIn AddInput(TxIn @in)
		{
			this.vin.Add(@in);
			return @in;
		}

		internal static readonly int WITNESS_SCALE_FACTOR = 4;
		/// <summary>
		/// Size of the transaction discounting the witness (Used for fee calculation)
		/// </summary>
		/// <returns>Transaction size</returns>
		public int GetVirtualSize()
		{
			var totalSize = this.GetSerializedSize(TransactionOptions.Witness);
			var strippedSize = this.GetSerializedSize(TransactionOptions.None);
			// This implements the weight = (stripped_size * 4) + witness_size formula,
			// using only serialization with and without witness data. As witness_size
			// is equal to total_size - stripped_size, this formula is identical to:
			// weight = (stripped_size * 3) + total_size.
			var weight = strippedSize * (WITNESS_SCALE_FACTOR - 1) + totalSize;
			return (weight + WITNESS_SCALE_FACTOR - 1) / WITNESS_SCALE_FACTOR;
		}

		public TxIn AddInput(Transaction prevTx, int outIndex)
		{
			if(outIndex >= prevTx.Outputs.Count)
				throw new InvalidOperationException("Output " + outIndex + " is not present in the prevTx");
			var @in = new TxIn();
			@in.PrevOut.Hash = prevTx.GetHash();
			@in.PrevOut.N = (uint)outIndex;
			AddInput(@in);
			return @in;
		}


		/// <summary>
		/// Sign a specific coin with the given secret
		/// </summary>
		/// <param name="secrets">Secrets</param>
		/// <param name="coins">Coins to sign</param>
		public void Sign(ISecret[] secrets, params ICoin[] coins)
		{
			Sign(secrets.Select(s => s.PrivateKey).ToArray(), coins);
		}

		/// <summary>
		/// Sign a specific coin with the given secret
		/// </summary>
		/// <param name="key">Private keys</param>
		/// <param name="coins">Coins to sign</param>
		public void Sign(Key[] keys, params ICoin[] coins)
		{
			TransactionBuilder builder = new TransactionBuilder();
			builder.AddKeys(keys);
			builder.AddCoins(coins);
			builder.SignTransactionInPlace(this);
		}
		/// <summary>
		/// Sign a specific coin with the given secret
		/// </summary>
		/// <param name="secret">Secret</param>
		/// <param name="coins">Coins to sign</param>
		public void Sign(ISecret secret, params ICoin[] coins)
		{
			Sign(new[] { secret }, coins);
		}

		/// <summary>
		/// Sign a specific coin with the given secret
		/// </summary>
		/// <param name="key">Private key</param>
		/// <param name="coins">Coins to sign</param>
		public void Sign(Key key, params ICoin[] coins)
		{
			Sign(new[] { key }, coins);
		}

		/// <summary>
		/// Sign the transaction with a private key
		/// <para>ScriptSigs should be filled with previous ScriptPubKeys</para>
		/// <para>For more complex scenario, use TransactionBuilder</para>
		/// </summary>
		/// <param name="secret"></param>
		public void Sign(ISecret secret, bool assumeP2SH)
		{
			Sign(secret.PrivateKey, assumeP2SH);
		}

		/// <summary>
		/// Sign the transaction with a private key
		/// <para>ScriptSigs should be filled with either previous scriptPubKeys or redeem script (for P2SH)</para>
		/// <para>For more complex scenario, use TransactionBuilder</para>
		/// </summary>
		/// <param name="secret"></param>
		public void Sign(Key key, bool assumeP2SH)
		{
			List<Coin> coins = new List<Coin>();
			for(int i = 0; i < Inputs.Count; i++)
			{
				var txin = Inputs[i];
				if(Script.IsNullOrEmpty(txin.ScriptSig))
					throw new InvalidOperationException("ScriptSigs should be filled with either previous scriptPubKeys or redeem script (for P2SH)");
				if(assumeP2SH)
				{
					var p2shSig = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(txin.ScriptSig);
					if(p2shSig == null)
					{
						coins.Add(new ScriptCoin(txin.PrevOut, new TxOut()
						{
							ScriptPubKey = txin.ScriptSig.PaymentScript,
						}, txin.ScriptSig));
					}
					else
					{
						coins.Add(new ScriptCoin(txin.PrevOut, new TxOut()
						{
							ScriptPubKey = p2shSig.RedeemScript.PaymentScript
						}, p2shSig.RedeemScript));
					}
				}
				else
				{
					coins.Add(new Coin(txin.PrevOut, new TxOut()
					{
						ScriptPubKey = txin.ScriptSig
					}));
				}

			}
			Sign(key, coins.ToArray());
		}

		public TxPayload CreatePayload()
		{
			return new TxPayload(this.Clone());
		}


		public static Transaction Parse(string tx, RawFormat format, Network network = null)
		{
			return GetFormatter(format, network).ParseJson(tx);
		}

		public static Transaction Parse(string hex)
		{
			return new Transaction(Encoders.Hex.DecodeData(hex));
		}

		public string ToHex()
		{
			return Encoders.Hex.EncodeData(this.ToBytes());
		}

		public override string ToString()
		{
			return ToString(RawFormat.BlockExplorer);
		}

		public string ToString(RawFormat rawFormat, Network network = null)
		{
			var formatter = GetFormatter(rawFormat, network);
			return ToString(formatter);
		}

		static private RawFormatter GetFormatter(RawFormat rawFormat, Network network)
		{
			RawFormatter formatter = null;
			switch(rawFormat)
			{
				case RawFormat.Satoshi:
					formatter = new SatoshiFormatter();
					break;
				case RawFormat.BlockExplorer:
					formatter = new BlockExplorerFormatter();
					break;
				default:
					throw new NotSupportedException(rawFormat.ToString());
			}
			formatter.Network = network ?? formatter.Network;
			return formatter;
		}

		internal string ToString(RawFormatter formatter)
		{
			if(formatter == null)
				throw new ArgumentNullException("formatter");
			return formatter.ToString(this);
		}

		/// <summary>
		/// Calculate the fee of the transaction
		/// </summary>
		/// <param name="spentCoins">Coins being spent</param>
		/// <returns>Fee or null if some spent coins are missing or if spentCoins is null</returns>
		public Money GetFee(ICoin[] spentCoins)
		{
			if(IsCoinBase)
				return Money.Zero;
			spentCoins = spentCoins ?? new ICoin[0];

			Money fees = -TotalOut;
			foreach(var input in this.Inputs)
			{
				var coin = spentCoins.FirstOrDefault(s => s.Outpoint == input.PrevOut);
				if(coin == null)
					return null;
				fees += coin.TxOut.Value;
			}
			return fees;
		}

		/// <summary>
		/// Calculate the fee rate of the transaction
		/// </summary>
		/// <param name="spentCoins">Coins being spent</param>
		/// <returns>Fee or null if some spent coins are missing or if spentCoins is null</returns>
		public FeeRate GetFeeRate(ICoin[] spentCoins)
		{
			var fee = GetFee(spentCoins);
			if(fee == null)
				return null;
			return new FeeRate(fee, this.GetSerializedSize());
		}

		public bool IsFinal(ChainedBlock block)
		{
			if(block == null)
				return IsFinal(Utils.UnixTimeToDateTime(0), 0);
			return IsFinal(block.Header.BlockTime, block.Height);
		}
		public bool IsFinal(DateTimeOffset blockTime, int blockHeight)
		{
			var nBlockTime = Utils.DateTimeToUnixTime(blockTime);
			if(nLockTime == 0)
				return true;
			if((long)nLockTime < ((long)nLockTime < LockTime.LOCKTIME_THRESHOLD ? (long)blockHeight : nBlockTime))
				return true;
			foreach(var txin in Inputs)
				if(!txin.IsFinal)
					return false;
			return true;
		}

		[Flags]
		public enum LockTimeFlags : int
		{
			None = 0,
			/// <summary>
			/// Interpret sequence numbers as relative lock-time constraints.
			/// </summary>
			VerifySequence = (1 << 0),

			/// <summary>
			///  Use GetMedianTimePast() instead of nTime for end point timestamp.
			/// </summary>
			MedianTimePast = (1 << 1),
		}


		/// <summary>
		/// Calculates the block height and time which the transaction must be later than
		/// in order to be considered final in the context of BIP 68.  It also removes
		/// from the vector of input heights any entries which did not correspond to sequence
		/// locked inputs as they do not affect the calculation.
		/// </summary>		
		/// <param name="prevHeights">Previous Height</param>
		/// <param name="block">The block being evaluated</param>
		/// <param name="flags">If VerifySequence is not set, returns always true SequenceLock</param>
		/// <returns>Sequence lock of minimum SequenceLock to satisfy</returns>
		public bool CheckSequenceLocks(int[] prevHeights, ChainedBlock block, LockTimeFlags flags = LockTimeFlags.VerifySequence)
		{
			return CalculateSequenceLocks(prevHeights, block, flags).Evaluate(block);
		}

		/// <summary>
		/// Calculates the block height and time which the transaction must be later than
		/// in order to be considered final in the context of BIP 68.  It also removes
		/// from the vector of input heights any entries which did not correspond to sequence
		/// locked inputs as they do not affect the calculation.
		/// </summary>		
		/// <param name="prevHeights">Previous Height</param>
		/// <param name="block">The block being evaluated</param>
		/// <param name="flags">If VerifySequence is not set, returns always true SequenceLock</param>
		/// <returns>Sequence lock of minimum SequenceLock to satisfy</returns>
		public SequenceLock CalculateSequenceLocks(int[] prevHeights, ChainedBlock block, LockTimeFlags flags = LockTimeFlags.VerifySequence)
		{
			if(prevHeights.Length != Inputs.Count)
				throw new ArgumentException("The number of element in prevHeights should be equal to the number of inputs", "prevHeights");

			// Will be set to the equivalent height- and time-based nLockTime
			// values that would be necessary to satisfy all relative lock-
			// time constraints given our view of block chain history.
			// The semantics of nLockTime are the last invalid height/time, so
			// use -1 to have the effect of any height or time being valid.
			int nMinHeight = -1;
			long nMinTime = -1;

			// tx.nVersion is signed integer so requires cast to unsigned otherwise
			// we would be doing a signed comparison and half the range of nVersion
			// wouldn't support BIP 68.
			bool fEnforceBIP68 = Version >= 2
							  && (flags & LockTimeFlags.VerifySequence) != 0;

			// Do not enforce sequence numbers as a relative lock time
			// unless we have been instructed to
			if(!fEnforceBIP68)
			{
				return new SequenceLock(nMinHeight, nMinTime);
			}

			for(var txinIndex = 0; txinIndex < Inputs.Count; txinIndex++)
			{
				TxIn txin = Inputs[txinIndex];

				// Sequence numbers with the most significant bit set are not
				// treated as relative lock-times, nor are they given any
				// consensus-enforced meaning at this point.
				if((txin.Sequence & Sequence.SEQUENCE_LOCKTIME_DISABLE_FLAG) != 0)
				{
					// The height of this input is not relevant for sequence locks
					prevHeights[txinIndex] = 0;
					continue;
				}

				int nCoinHeight = prevHeights[txinIndex];

				if((txin.Sequence & Sequence.SEQUENCE_LOCKTIME_TYPE_FLAG) != 0)
				{
					long nCoinTime = (long)Utils.DateTimeToUnixTimeLong(block.GetAncestor(Math.Max(nCoinHeight - 1, 0)).GetMedianTimePast());

					// Time-based relative lock-times are measured from the
					// smallest allowed timestamp of the block containing the
					// txout being spent, which is the median time past of the
					// block prior.
					nMinTime = Math.Max(nMinTime, nCoinTime + (long)((txin.Sequence & Sequence.SEQUENCE_LOCKTIME_MASK) << Sequence.SEQUENCE_LOCKTIME_GRANULARITY) - 1);
				}
				else
				{
					// We subtract 1 from relative lock-times because a lock-
					// time of 0 has the semantics of "same block," so a lock-
					// time of 1 should mean "next block," but nLockTime has
					// the semantics of "last invalid block height."
					nMinHeight = Math.Max(nMinHeight, nCoinHeight + (int)(txin.Sequence & Sequence.SEQUENCE_LOCKTIME_MASK) - 1);
				}
			}

			return new SequenceLock(nMinHeight, nMinTime);
		}


		private DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b)
		{
			return a > b ? a : b;
		}

		/// <summary>
		/// Create a transaction with the specified option only. (useful for stripping data from a transaction)
		/// </summary>
		/// <param name="options">Options to keep</param>
		/// <returns>A new transaction with only the options wanted</returns>
		public Transaction WithOptions(TransactionOptions options)
		{
			var instance = new Transaction();
			var ms = new MemoryStream();
			var bms = new BitcoinStream(ms, true);
			bms.TransactionOptions = options;
			this.ReadWrite(bms);
			ms.Position = 0;
			bms = new BitcoinStream(ms, false);
			bms.TransactionOptions = options;
			instance.ReadWrite(bms);
			return instance;
		}

		public bool HasWitness
		{
			get
			{
				return Inputs.Any(i => i.WitScript != WitScript.Empty && i.WitScript != null);
			}
		}

		private static readonly uint MAX_BLOCK_SIZE = 1000000;
		private static readonly ulong MAX_MONEY = 21000000ul * Money.COIN;

		/// <summary>
		/// Context free transaction check
		/// </summary>
		/// <returns>The error or success of the check</returns>
		public TransactionCheckResult Check()
		{
			// Basic checks that don't depend on any context
			if(Inputs.Count == 0)
				return TransactionCheckResult.NoInput;
			if(Outputs.Count == 0)
				return TransactionCheckResult.NoOutput;
			// Size limits
			if(this.GetSerializedSize() > MAX_BLOCK_SIZE)
				return TransactionCheckResult.TransactionTooLarge;

			// Check for negative or overflow output values
			long nValueOut = 0;
			foreach(var txout in Outputs)
			{
				if(txout.Value < 0)
					return TransactionCheckResult.NegativeOutput;
				if(txout.Value > MAX_MONEY)
					return TransactionCheckResult.OutputTooLarge;
				nValueOut += txout.Value;
				if(!((nValueOut >= 0 && nValueOut <= (long)MAX_MONEY)))
					return TransactionCheckResult.OutputTotalTooLarge;
			}

			// Check for duplicate inputs
			var vInOutPoints = new HashSet<OutPoint>();
			foreach(var txin in Inputs)
			{
				if(vInOutPoints.Contains(txin.PrevOut))
					return TransactionCheckResult.DuplicateInputs;
				vInOutPoints.Add(txin.PrevOut);
			}

			if(IsCoinBase)
			{
				if(Inputs[0].ScriptSig.Length < 2 || Inputs[0].ScriptSig.Length > 100)
					return TransactionCheckResult.CoinbaseScriptTooLarge;
			}
			else
			{
				foreach(var txin in Inputs)
					if(txin.PrevOut.IsNull)
						return TransactionCheckResult.NullInputPrevOut;
			}

			return TransactionCheckResult.Success;
		}
	}

	public enum TransactionCheckResult
	{
		Success,
		NoInput,
		NoOutput,
		NegativeOutput,
		OutputTooLarge,
		OutputTotalTooLarge,
		TransactionTooLarge,
		DuplicateInputs,
		NullInputPrevOut,
		CoinbaseScriptTooLarge,
	}
}
