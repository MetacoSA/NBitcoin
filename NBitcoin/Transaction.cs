using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using NBitcoin.Logging;

namespace NBitcoin
{
	public class OutPoint : IBitcoinSerializable
	{
		public bool IsNull
		{
			get
			{
				return (hash == uint256.Zero && n == uint.MaxValue);
			}
		}
		private uint256 hash = uint256.Zero;
		private uint n;


		public uint256 Hash
		{
			get
			{
				return hash;
			}
			set
			{
				hash = value;
			}
		}
		public uint N
		{
			get
			{
				return n;
			}
			set
			{
				n = value;
			}
		}

		public static bool TryParse(string str, out OutPoint result)
		{
			result = null;
			if (str == null)
				throw new ArgumentNullException("str");
			var splitted = str.Split('-');
			if (splitted.Length != 2)
				return false;

			uint256 hash;
			if (!uint256.TryParse(splitted[0], out hash))
				return false;

			uint index;
			if (!uint.TryParse(splitted[1], out index))
				return false;
			result = new OutPoint(hash, index);
			return true;
		}

		public static OutPoint Parse(string str)
		{
			OutPoint result;
			if (TryParse(str, out result))
				return result;
			throw new FormatException("The format of the outpoint is incorrect");
		}

		public OutPoint()
		{
			SetNull();
		}
		public OutPoint(uint256 hashIn, uint nIn)
		{
			hash = hashIn;
			n = nIn;
		}
		public OutPoint(uint256 hashIn, int nIn)
		{
			hash = hashIn;
			this.n = nIn == -1 ? n = uint.MaxValue : (uint)nIn;
		}

		public OutPoint(Transaction tx, uint i)
			: this(tx.GetHash(), i)
		{
		}

		public OutPoint(Transaction tx, int i)
			: this(tx.GetHash(), i)
		{
		}

		public OutPoint(OutPoint outpoint)
		{
			this.FromBytes(outpoint.ToBytes());
		}
		//IMPLEMENT_SERIALIZE( READWRITE(FLATDATA(*this)); )

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref hash);
			stream.ReadWrite(ref n);
		}


		void SetNull()
		{
			hash = uint256.Zero;
			n = uint.MaxValue;
		}

		public static bool operator <(OutPoint a, OutPoint b)
		{
			return (a.hash < b.hash || (a.hash == b.hash && a.n < b.n));
		}
		public static bool operator >(OutPoint a, OutPoint b)
		{
			return (a.hash > b.hash || (a.hash == b.hash && a.n > b.n));
		}

		public static bool operator ==(OutPoint a, OutPoint b)
		{
			if (Object.ReferenceEquals(a, null))
			{
				return Object.ReferenceEquals(b, null);
			}
			if (Object.ReferenceEquals(b, null))
			{
				return false;
			}
			return (a.hash == b.hash && a.n == b.n);
		}

		public static bool operator !=(OutPoint a, OutPoint b)
		{
			return !(a == b);
		}
		public override bool Equals(object obj)
		{
			OutPoint item = obj as OutPoint;
			if (object.ReferenceEquals(null, item))
				return false;
			return item == this;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return 17 + hash.GetHashCode() * 31 + n.GetHashCode() * 31 * 31;
			}
		}

		public override string ToString()
		{
			return Hash + "-" + N;
		}
	}


	public class TxIn : IBitcoinSerializable
	{
		public TxIn()
		{

		}
		public TxIn(Script scriptSig)
		{
			this.scriptSig = scriptSig;
		}
		public TxIn(OutPoint prevout, Script scriptSig)
		{
			this.prevout = prevout;
			this.scriptSig = scriptSig;
		}
		public TxIn(OutPoint prevout)
		{
			this.prevout = prevout;
		}
		protected OutPoint prevout = new OutPoint();
		protected Script scriptSig = Script.Empty;
		protected uint nSequence = uint.MaxValue;

		public Sequence Sequence
		{
			get
			{
				return nSequence;
			}
			set
			{
				nSequence = value.Value;
			}
		}
		public OutPoint PrevOut
		{
			get
			{
				return prevout;
			}
			set
			{
				prevout = value;
			}
		}


		public Script ScriptSig
		{
			get
			{
				return scriptSig;
			}
			set
			{
				scriptSig = value;
			}
		}


		/// <summary>
		/// Try to get the expected scriptPubKey of this TxIn based on its scriptSig and witScript.
		/// </summary>
		/// <returns>Null if could not infer the scriptPubKey, else, the expected scriptPubKey</returns>
		public IDestination GetSigner()
		{
			return scriptSig.GetSigner() ?? witScript.GetSigner();
		}

		WitScript witScript = WitScript.Empty;

		/// <summary>
		/// The witness script (Witness script is not serialized and deserialized at the TxIn level, but at the Transaction level)
		/// </summary>
		public WitScript WitScript
		{
			get
			{
				return witScript;
			}
			set
			{
				witScript = value;
			}
		}

		#region IBitcoinSerializable Members

		public virtual void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref prevout);
			stream.ReadWrite(ref scriptSig);
			stream.ReadWrite(ref nSequence);
		}

		#endregion

		public bool IsFrom(PubKey pubKey)
		{
			var result = PayToPubkeyHashTemplate.Instance.ExtractScriptSigParameters(ScriptSig);
			return result != null && result.PublicKey == pubKey;
		}

		public bool IsFinal
		{
			get
			{
				return (nSequence == uint.MaxValue);
			}
		}
		
		public virtual ConsensusFactory GetConsensusFactory()
		{
			return Bitcoin.Instance.Mainnet.Consensus.ConsensusFactory;
		}
		public virtual TxIn Clone()
		{
			var consensusFactory = GetConsensusFactory();
			if (!consensusFactory.TryCreateNew<TxIn>(out var txin))
				txin = new TxIn();
			txin.ReadWrite(new BitcoinStream(this.ToBytes()) { ConsensusFactory = consensusFactory });
			txin.WitScript = (witScript ?? WitScript.Empty).Clone();
			return txin;
		}

		public static TxIn CreateCoinbase(int height)
		{
			var txin = new TxIn();
			txin.ScriptSig = new Script(Op.GetPushOp(height)) + OpcodeType.OP_0;
			return txin;
		}
	}

	public class TxOutCompressor : IBitcoinSerializable
	{
		// Amount compression:
		// * If the amount is 0, output 0
		// * first, divide the amount (in base units) by the largest power of 10 possible; call the exponent e (e is max 9)
		// * if e<9, the last digit of the resulting number cannot be 0; store it as d, and drop it (divide by 10)
		//   * call the result n
		//   * output 1 + 10*(9*n + d - 1) + e
		// * if e==9, we only know the resulting number is not zero, so output 1 + 10*(n - 1) + 9
		// (this is decodable, as d is in [1-9] and e is in [0-9])

		ulong CompressAmount(ulong n)
		{
			if (n == 0)
				return 0;
			int e = 0;
			while (((n % 10) == 0) && e < 9)
			{
				n /= 10;
				e++;
			}
			if (e < 9)
			{
				int d = (int)(n % 10);
				n /= 10;
				return 1 + (n * 9 + (ulong)(d - 1)) * 10 + (ulong)e;
			}
			else
			{
				return 1 + (n - 1) * 10 + 9;
			}
		}

		ulong DecompressAmount(ulong x)
		{
			// x = 0  OR  x = 1+10*(9*n + d - 1) + e  OR  x = 1+10*(n - 1) + 9
			if (x == 0)
				return 0;
			x--;
			// x = 10*(9*n + d - 1) + e
			int e = (int)(x % 10);
			x /= 10;
			ulong n = 0;
			if (e < 9)
			{
				// x = 9*n + d - 1
				int d = (int)((x % 9) + 1);
				x /= 9;
				// x = n
				n = (x * 10 + (ulong)d);
			}
			else
			{
				n = x + 1;
			}
			while (e != 0)
			{
				n *= 10;
				e--;
			}
			return n;
		}


		private TxOut _TxOut = new TxOut();
		public TxOut TxOut
		{
			get
			{
				return _TxOut;
			}
		}
		public TxOutCompressor()
		{

		}
		public TxOutCompressor(TxOut txOut)
		{
			_TxOut = txOut;
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if (stream.Serializing)
			{
				ulong val = CompressAmount((ulong)_TxOut.Value.Satoshi);
				stream.ReadWriteAsCompactVarInt(ref val);
			}
			else
			{
				ulong val = 0;
				stream.ReadWriteAsCompactVarInt(ref val);
				_TxOut.Value = new Money(DecompressAmount(val));
			}
			ScriptCompressor cscript = new ScriptCompressor(_TxOut.ScriptPubKey);
			stream.ReadWrite(ref cscript);
			if (!stream.Serializing)
				_TxOut.ScriptPubKey = new Script(cscript.ScriptBytes);
		}

		#endregion
	}

	public class ScriptCompressor : IBitcoinSerializable
	{
		// make this static for now (there are only 6 special scripts defined)
		// this can potentially be extended together with a new nVersion for
		// transactions, in which case this value becomes dependent on nVersion
		// and nHeight of the enclosing transaction.
		const uint nSpecialScripts = 6;
		byte[] _Script;
		public byte[] ScriptBytes
		{
			get
			{
				return _Script;
			}
		}
		public ScriptCompressor(Script script)
		{
			_Script = script.ToBytes(true);
		}
		public ScriptCompressor()
		{

		}

		public Script GetScript()
		{
			return new Script(_Script);
		}

		byte[] Compress()
		{
			byte[] result = null;
			var script = Script.FromBytesUnsafe(_Script);
			KeyId keyID = PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(script);
			if (keyID != null)
			{
				result = new byte[21];
				result[0] = 0x00;
				Array.Copy(keyID.ToBytes(true), 0, result, 1, 20);
				return result;
			}
			ScriptId scriptID = PayToScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(script);
			if (scriptID != null)
			{
				result = new byte[21];
				result[0] = 0x01;
				Array.Copy(scriptID.ToBytes(true), 0, result, 1, 20);
				return result;
			}
			PubKey pubkey = PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(script, true);
			if (pubkey != null)
			{
				result = new byte[33];
				var pubBytes = pubkey.ToBytes(true);
				Array.Copy(pubBytes, 1, result, 1, 32);
				if (pubBytes[0] == 0x02 || pubBytes[0] == 0x03)
				{
					result[0] = pubBytes[0];
					return result;
				}
				else if (pubBytes[0] == 0x04)
				{
					result[0] = (byte)(0x04 | (pubBytes[64] & 0x01));
					return result;
				}
			}
			return null;
		}

		Script Decompress(uint nSize, byte[] data)
		{
			switch (nSize)
			{
				case 0x00:
					return PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(new KeyId(data.SafeSubarray(0, 20)));
				case 0x01:
					return PayToScriptHashTemplate.Instance.GenerateScriptPubKey(new ScriptId(data.SafeSubarray(0, 20)));
				case 0x02:
				case 0x03:
					var keyPart = data.SafeSubarray(0, 32);
					var keyBytes = new byte[33];
					keyBytes[0] = (byte)nSize;
					Array.Copy(keyPart, 0, keyBytes, 1, 32);
					return PayToPubkeyTemplate.Instance.GenerateScriptPubKey(keyBytes);
				case 0x04:
				case 0x05:
					byte[] vch = new byte[33];
					vch[0] = (byte)(nSize - 2);
					Array.Copy(data, 0, vch, 1, 32);
					PubKey pubkey = new PubKey(vch, true);
					pubkey = pubkey.Decompress();
					return PayToPubkeyTemplate.Instance.GenerateScriptPubKey(pubkey);
			}
			return null;
		}





		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if (stream.Serializing)
			{
				var compr = Compress();
				if (compr != null)
				{
					stream.ReadWrite(ref compr);
					return;
				}
				uint nSize = (uint)_Script.Length + nSpecialScripts;
				stream.ReadWriteAsCompactVarInt(ref nSize);
				stream.ReadWrite(ref _Script);
			}
			else
			{
				uint nSize = 0;
				stream.ReadWriteAsCompactVarInt(ref nSize);
				if (nSize < nSpecialScripts)
				{
					byte[] vch = new byte[GetSpecialSize(nSize)];
					stream.ReadWrite(ref vch);
					_Script = Decompress(nSize, vch).ToBytes();
					return;
				}
				nSize -= nSpecialScripts;
				_Script = new byte[nSize];
				stream.ReadWrite(ref _Script);
			}
		}

		private int GetSpecialSize(uint nSize)
		{
			if (nSize == 0 || nSize == 1)
				return 20;
			if (nSize == 2 || nSize == 3 || nSize == 4 || nSize == 5)
				return 32;
			return 0;
		}



		#endregion
	}

	public class TxOut : IBitcoinSerializable, IDestination
	{
		protected Script publicKey = Script.Empty;
		public Script ScriptPubKey
		{
			get
			{
				return this.publicKey;
			}
			set
			{
				this.publicKey = value;
			}
		}

		public TxOut()
		{

		}

		public TxOut(Money value, IDestination destination)
		{
			Value = value;
			if (destination != null)
				ScriptPubKey = destination.ScriptPubKey;
		}

		public TxOut(Money value, Script scriptPubKey)
		{
			Value = value;
			ScriptPubKey = scriptPubKey;
		}

		internal readonly static Money NullMoney = new Money(-1);
		Money _Value = NullMoney;
		public virtual Money Value
		{
			get
			{
				return _Value;
			}
			set
			{
				_Value = value;
			}
		}


		public bool IsDust(FeeRate minRelayTxFee)
		{
			return (Value < GetDustThreshold(minRelayTxFee));
		}

		public Money GetDustThreshold(FeeRate minRelayTxFee)
		{
			if (minRelayTxFee == null)
				throw new ArgumentNullException("minRelayTxFee");
			int nSize = this.GetSerializedSize() + 148;
			return 3 * minRelayTxFee.GetFee(nSize);
		}

		#region IBitcoinSerializable Members

		public virtual void ReadWrite(BitcoinStream stream)
		{
			long value = Value.Satoshi;
			stream.ReadWrite(ref value);
			if (!stream.Serializing)
				_Value = new Money(value);
			stream.ReadWrite(ref publicKey);
		}

		#endregion

		public bool IsTo(IDestination destination)
		{
			return ScriptPubKey == destination.ScriptPubKey;
		}

		public static TxOut Parse(string hex)
		{
			var ret = new TxOut();
			ret.FromBytes(Encoders.Hex.DecodeData(hex));
			return ret;
		}

		public virtual TxOut Clone()
		{
			var consensusFactory = GetConsensusFactory();
			if (!consensusFactory.TryCreateNew<TxOut>(out var txout))
				txout = new TxOut();
			txout.ReadWrite(new BitcoinStream(this.ToBytes()) { ConsensusFactory = consensusFactory });
			return txout;
		}

		public virtual ConsensusFactory GetConsensusFactory()
		{
			return Bitcoin.Instance.Mainnet.Consensus.ConsensusFactory;
		}
	}

	public class IndexedTxIn
	{
		public TxIn TxIn
		{
			get;
			set;
		}

		/// <summary>
		/// The index of this TxIn in its transaction
		/// </summary>
		public uint Index
		{
			get;
			set;
		}

		public OutPoint PrevOut
		{
			get
			{
				return TxIn.PrevOut;
			}
			set
			{
				TxIn.PrevOut = value;
			}
		}

		public Script ScriptSig
		{
			get
			{
				return TxIn.ScriptSig;
			}
			set
			{
				TxIn.ScriptSig = value;
			}
		}


		public WitScript WitScript
		{
			get
			{
				return TxIn.WitScript;
			}
			set
			{
				TxIn.WitScript = value;
			}
		}
		public Transaction Transaction
		{
			get;
			set;
		}

		[Obsolete("Use VerifyScript(TxOut spentOutput, ScriptVerify scriptVerify = ScriptVerify.Standard) instead")]
		public bool VerifyScript(Script scriptPubKey, ScriptVerify scriptVerify = ScriptVerify.Standard)
		{
			ScriptError unused;
			return VerifyScript(scriptPubKey, scriptVerify, out unused);
		}
		public bool VerifyScript(TxOut spentOutput, ScriptVerify scriptVerify = ScriptVerify.Standard)
		{
			ScriptError unused;
			return VerifyScript(spentOutput, scriptVerify, out unused);
		}
		[Obsolete("Use VerifyScript(TxOut spentOutput, out ScriptError error) instead")]
		public bool VerifyScript(Script scriptPubKey, out ScriptError error)
		{
			TxOut txOut = Transaction.Outputs.CreateNewTxOut(null, scriptPubKey);
			return Script.VerifyScript(Transaction, (int)Index, txOut, out error);
		}
		public bool VerifyScript(TxOut spentOutput, out ScriptError error)
		{
			return Script.VerifyScript(Transaction, (int)Index, spentOutput, out error);
		}
		[Obsolete("Use VerifyScript(TxOut spentOutput, ScriptVerify scriptVerify, out ScriptError error) instead")]
		public bool VerifyScript(Script scriptPubKey, ScriptVerify scriptVerify, out ScriptError error)
		{
			TxOut txOut = Transaction.Outputs.CreateNewTxOut(null, scriptPubKey);
			return Script.VerifyScript(Transaction, (int)Index, txOut, scriptVerify, SigHash.Undefined, out error);
		}

		[Obsolete("Use VerifyScript(TxOut spentOutput, ScriptVerify scriptVerify, out ScriptError error) instead")]
		public bool VerifyScript(Script scriptPubKey, Money value, ScriptVerify scriptVerify, out ScriptError error)
		{
			TxOut txOut = Transaction.Outputs.CreateNewTxOut(value, scriptPubKey);
			return Script.VerifyScript(Transaction, (int)Index, txOut, scriptVerify, SigHash.Undefined, out error);
		}

		public bool VerifyScript(TxOut spentOutput, ScriptVerify scriptVerify, out ScriptError error)
		{
			return Script.VerifyScript(Transaction, (int)Index, spentOutput, scriptVerify, SigHash.Undefined, out error);
		}

		public bool VerifyScript(ICoin coin, ScriptVerify scriptVerify = ScriptVerify.Standard)
		{
			ScriptError error;
			return VerifyScript(coin, scriptVerify, out error);
		}

		public bool VerifyScript(ICoin coin, ScriptVerify scriptVerify, out ScriptError error)
		{
			return Script.VerifyScript(Transaction, (int)Index, coin.TxOut, scriptVerify, SigHash.Undefined, out error);
		}
		public bool VerifyScript(ICoin coin, out ScriptError error)
		{
			return VerifyScript(coin, ScriptVerify.Standard, out error);
		}

		public TransactionSignature Sign(Key key, ICoin coin, SigHash sigHash)
		{
			var hash = GetSignatureHash(coin, sigHash);
			return key.Sign(hash, sigHash);
		}

		public uint256 GetSignatureHash(ICoin coin, SigHash sigHash = SigHash.All)
		{
			return Transaction.GetSignatureHash(coin.GetScriptCode(), (int)Index, sigHash, coin.TxOut, coin.GetHashVersion());
		}

	}
	public class TxInList : UnsignedList<TxIn>
	{
		public TxInList()
		{

		}
		public TxInList(Transaction parent)
			: base(parent)
		{

		}
		public TxIn this[OutPoint outpoint]
		{
			get
			{
				return this[outpoint.N];
			}
			set
			{
				this[outpoint.N] = value;
			}
		}

		/// <summary>
		/// Returns the IndexedTxIn whose PrevOut is equal to <paramref name="outpoint"/> or null.
		/// </summary>
		/// <param name="outpoint">The outpoint being searched for</param>
		/// <returns>The IndexedTxIn which PrevOut is equal to <paramref name="outpoint"/> or null if not found</returns>
		public IndexedTxIn FindIndexedInput(OutPoint outpoint)
		{
			if (outpoint == null)
				throw new ArgumentNullException(nameof(outpoint));
			for (int i = 0; i < this.Count; i++)
			{
				var txin = this[i];
				if (outpoint == txin.PrevOut)
				{
					return new IndexedTxIn()
					{
						TxIn = txin,
						Index = (uint)i,
						Transaction = Transaction
					};
				}
			}
			return null;
		}

		public TxIn CreateNewTxIn(OutPoint outpoint = null, Script scriptSig = null, WitScript witScript = null, Sequence? sequence = null)
		{
			TxIn txIn;
			if (!Transaction.GetConsensusFactory().TryCreateNew<TxIn>(out txIn))
				txIn = new TxIn();
			if (outpoint != null)
				txIn.PrevOut = outpoint;
			if (scriptSig != null)
				txIn.ScriptSig = scriptSig;
			if (witScript != null)
				txIn.WitScript = witScript;
			if (sequence.HasValue)
				txIn.Sequence = sequence.Value;
			return txIn;
		}

		public TxIn Add(OutPoint outpoint = null, Script scriptSig = null, WitScript witScript = null, Sequence? sequence = null)
		{
			var txIn = CreateNewTxIn(outpoint, scriptSig, witScript, sequence);
			return Add(txIn);
		}

		public new TxIn Add(TxIn txIn)
		{
			base.Add(txIn);
			return txIn;
		}

		public IEnumerable<IndexedTxIn> AsIndexedInputs()
		{
			// We want i as the index of txIn in Intputs[], not index in enumerable after where filter
			return this.Select((r, i) => new IndexedTxIn()
			{
				TxIn = r,
				Index = (uint)i,
				Transaction = Transaction
			});
		}

		public TxIn Add(Transaction prevTx, int outIndex)
		{
			if (outIndex >= prevTx.Outputs.Count)
				throw new InvalidOperationException("Output " + outIndex + " is not present in the prevTx");
			var @in = CreateNewTxIn();
			@in.PrevOut.Hash = prevTx.GetHash();
			@in.PrevOut.N = (uint)outIndex;
			return this.Add(@in);
		}
	}

	public class IndexedTxOut
	{
		public TxOut TxOut
		{
			get;
			set;
		}
		public uint N
		{
			get;
			set;
		}

		public Transaction Transaction
		{
			get;
			set;
		}
		public Coin ToCoin()
		{
			return new Coin(this);
		}
	}

	public class TxOutList : UnsignedList<TxOut>
	{
		public TxOutList()
		{

		}
		public TxOutList(Transaction parent)
			: base(parent)
		{

		}
		public IEnumerable<TxOut> To(IDestination destination)
		{
			return this.Where(r => r.IsTo(destination));
		}
		public IEnumerable<TxOut> To(Script scriptPubKey)
		{
			return this.Where(r => r.ScriptPubKey == scriptPubKey);
		}

		public IEnumerable<IndexedTxOut> AsIndexedOutputs()
		{
			// We want i as the index of txOut in Outputs[], not index in enumerable after where filter
			return this.Select((r, i) => new IndexedTxOut()
			{
				TxOut = r,
				N = (uint)i,
				Transaction = Transaction
			});
		}

		public IEnumerable<Coin> AsCoins()
		{
			var txId = Transaction.GetHash();
			for (int i = 0; i < Count; i++)
			{
				yield return new Coin(new OutPoint(txId, i), this[i]);
			}
		}
		public TxOut CreateNewTxOut()
		{
			return CreateNewTxOut(null, null as Script);
		}
		public TxOut CreateNewTxOut(Money money = null, Script scriptPubKey = null)
		{
			if (!Transaction.GetConsensusFactory().TryCreateNew<TxOut>(out var txout))
				txout = new TxOut();
			if (money != null)
				txout.Value = money;
			if (scriptPubKey != null)
				txout.ScriptPubKey = scriptPubKey;
			return txout;
		}

		public TxOut CreateNewTxOut(Money money = null, IDestination destination = null)
		{
			return CreateNewTxOut(money, destination?.ScriptPubKey);
		}

		public TxOut Add(Money money = null, Script scriptPubKey = null)
		{
			var txOut = CreateNewTxOut(money, scriptPubKey);
			return Add(txOut);
		}

		public TxOut Add(Money money = null, IDestination destination = null)
		{
			return Add(money, destination?.ScriptPubKey);
		}

		public new TxOut Add(TxOut txOut)
		{
			base.Add(txOut);
			return txOut;
		}
	}

	public enum RawFormat
	{
		Satoshi,
		BlockExplorer,
	}

	public class WitScript : IEquatable<WitScript>
	{
		byte[][] _Pushes;
		public WitScript(string script)
		{
			var parts = script.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			_Pushes = new byte[parts.Length][];
			for (int i = 0; i < parts.Length; i++)
			{
				_Pushes[i] = Encoders.Hex.DecodeData(parts[i]);
			}
		}

		/// <summary>
		/// Create a new WitnessScript
		/// </summary>
		/// <param name="script">Scripts</param>
		/// <param name="unsafe">If false, make a copy of the input script array</param>
		public WitScript(byte[][] script, bool @unsafe = false)
		{
			if (@unsafe)
				_Pushes = script;
			else
			{
				_Pushes = script.ToArray();
				for (int i = 0; i < _Pushes.Length; i++)
					_Pushes[i] = script[i].ToArray();
			}
		}

		/// <summary>
		/// Create a new WitnessScript
		/// </summary>
		/// <param name="script">Scripts</param>
		public WitScript(IEnumerable<byte[]> script, bool @unsafe = false)
			: this(script.ToArray(), @unsafe)
		{

		}

		public WitScript(params Op[] ops)
		{
			List<byte[]> pushes = new List<byte[]>();
			foreach (var op in ops)
			{
				if (op.PushData == null)
					throw new ArgumentException("Non push operation unsupported in WitScript", "ops");
				pushes.Add(op.PushData);
			}
			_Pushes = pushes.ToArray();
		}

		public WitScript(byte[] script)
		{
			if (script == null)
				throw new ArgumentNullException("script");
			var ms = new MemoryStream(script);
			BitcoinStream stream = new BitcoinStream(ms, false);
			ReadCore(stream);
		}
		WitScript()
		{

		}

		public WitScript(Script scriptSig)
		{
			List<byte[]> pushes = new List<byte[]>();
			foreach (var op in scriptSig.ToOps())
			{
				if (op.PushData == null)
					throw new ArgumentException("A WitScript can only contains push operations", "script");
				pushes.Add(op.PushData);
			}
			_Pushes = pushes.ToArray();
		}


		public static WitScript Load(BitcoinStream stream)
		{
			WitScript script = new WitScript();
			script.ReadCore(stream);
			return script;
		}
		void ReadCore(BitcoinStream stream)
		{
			List<byte[]> pushes = new List<byte[]>();
			uint pushCount = 0;
			stream.ReadWriteAsVarInt(ref pushCount);
			for (int i = 0; i < (int)pushCount; i++)
			{
				byte[] push = ReadPush(stream);
				pushes.Add(push);
			}
			_Pushes = pushes.ToArray();
		}
		private static byte[] ReadPush(BitcoinStream stream)
		{
			byte[] push = null;
			stream.ReadWriteAsVarString(ref push);
			return push;
		}

		public byte[] this[int index]
		{
			get
			{
				return _Pushes[index];
			}
		}

		public IEnumerable<byte[]> Pushes
		{
			get
			{
				return _Pushes;
			}
		}

		static WitScript _Empty = new WitScript(new byte[0][], true);

		public static WitScript Empty
		{
			get
			{
				return _Empty;
			}
		}

		public override bool Equals(object obj)
		{
			WitScript item = obj as WitScript;
			if (item == null)
				return false;
			return Equals(item);
		}

		public bool Equals(WitScript item)
		{
			if (_Pushes.Length != item._Pushes.Length)
				return false;
			for (int i = 0; i < _Pushes.Length; i++)
			{
				if (!Utils.ArrayEqual(_Pushes[i], item._Pushes[i]))
					return false;
			}
			return true;
		}
		public static bool operator ==(WitScript a, WitScript b)
		{
			if (System.Object.ReferenceEquals(a, b))
				return true;
			if (((object)a == null) || ((object)b == null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(WitScript a, WitScript b)
		{
			return !(a == b);
		}
		public static WitScript operator +(WitScript a, WitScript b)
		{
			if (a == null)
				return b;
			if (b == null)
				return a;
			return new WitScript(a._Pushes.Concat(b._Pushes).ToArray());
		}
		public static implicit operator Script(WitScript witScript)
		{
			if (witScript == null)
				return null;
			return witScript.ToScript();
		}
		public override int GetHashCode()
		{
			return Utils.GetHashCode(ToBytes());
		}

		public byte[] ToBytes()
		{
			var ms = new MemoryStream();
			BitcoinStream stream = new BitcoinStream(ms, true);
			uint pushCount = (uint)_Pushes.Length;
			stream.ReadWriteAsVarInt(ref pushCount);
			foreach (var push in Pushes)
			{
				var localpush = push;
				stream.ReadWriteAsVarString(ref localpush);
			}
			return ms.ToArrayEfficient();
		}

		public override string ToString()
		{
			return ToScript().ToString();
		}

		public Script ToScript()
		{
			return new Script(_Pushes.Select(p => Op.GetPushOp(p)).ToArray());
		}

		public int PushCount
		{
			get
			{
				return _Pushes.Length;
			}
		}

		public byte[] GetUnsafePush(int i)
		{
			return _Pushes[i];
		}

		public WitScript Clone()
		{
			return new WitScript(ToBytes());
		}

		public TxDestination GetSigner()
		{
			var pubKey = PayToWitPubKeyHashTemplate.Instance.ExtractWitScriptParameters(this);
			if (pubKey != null)
			{
				return pubKey.PublicKey.WitHash;
			}
			var p2sh = PayToWitScriptHashTemplate.Instance.ExtractWitScriptParameters(this);
			return p2sh != null ? p2sh.WitHash : null;
		}
	}

	[Flags]
	public enum TransactionOptions : uint
	{
		None = 0x00000000,
		Witness = 0x40000000,
		All = Witness
	}
	class Witness
	{
		TxInList _Inputs;
		public Witness(TxInList inputs)
		{
			_Inputs = inputs;
		}

		internal bool IsNull()
		{
			return _Inputs.All(i => i.WitScript.PushCount == 0);
		}

		internal void ReadWrite(BitcoinStream stream)
		{
			for (int i = 0; i < _Inputs.Count; i++)
			{
				if (stream.Serializing)
				{
					var bytes = (_Inputs[i].WitScript ?? WitScript.Empty).ToBytes();
					stream.ReadWrite(ref bytes);
				}
				else
				{
					_Inputs[i].WitScript = WitScript.Load(stream);
				}
			}

			if (IsNull())
				throw new FormatException("Superfluous witness record");
		}
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

		protected uint nVersion = 1;

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
		protected TxInList vin;
		protected TxOutList vout;
		protected LockTime nLockTime;


		[Obsolete("You should better use Transaction.Create(Network network)")]
		public Transaction()
		{
			vin = new TxInList(this);
			vout = new TxOutList(this);
		}

		public static Transaction Create(Network network)
		{
			return network.Consensus.ConsensusFactory.CreateTransaction();
		}

		[Obsolete("You should instantiate Transaction from ConsensusFactory.CreateTransaction")]
		public Transaction(string hex, uint? version = null)
			: this()
		{
			this.FromBytes(Encoders.Hex.DecodeData(hex), version);
		}

		[Obsolete("You should instantiate Transaction from ConsensusFactory.CreateTransaction")]
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
		protected const uint NoDummyInput = (1 << 27);

		#region IBitcoinSerializable Members

		public virtual void ReadWrite(BitcoinStream stream)
		{
			var witSupported = (((uint)stream.TransactionOptions & (uint)TransactionOptions.Witness) != 0) &&
								stream.ProtocolCapabilities.SupportWitness;

			byte flags = 0;
			if (!stream.Serializing)
			{
				stream.ReadWrite(ref nVersion);
				/* Try to read the vin. In case the dummy is there, this will be read as an empty vector. */
				stream.ReadWrite<TxInList, TxIn>(ref vin);

				var hasNoDummy = (nVersion & NoDummyInput) != 0 && vin.Count == 0;
				if (witSupported && hasNoDummy)
					nVersion = nVersion & ~NoDummyInput;

				if (vin.Count == 0 && witSupported && !hasNoDummy)
				{
					/* We read a dummy or an empty vin. */
					stream.ReadWrite(ref flags);
					if (flags != 0)
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
				if (((flags & 1) != 0) && witSupported)
				{
					/* The witness flag is present, and we support witnesses. */
					flags ^= 1;
					Witness wit = new Witness(Inputs);
					wit.ReadWrite(stream);
				}
				if (flags != 0)
				{
					/* Unknown flag in the serialization */
					throw new FormatException("Unknown transaction optional data");
				}
			}
			else
			{
				var version = (witSupported && (vin.Count == 0 && vout.Count > 0)) ? nVersion | NoDummyInput : nVersion;
				stream.ReadWrite(ref version);

				if (witSupported)
				{
					/* Check whether witnesses need to be serialized. */
					if (HasWitness)
					{
						flags |= 1;
					}
				}
				if (flags != 0)
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
				if ((flags & 1) != 0)
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
			uint256 h = null;
			var hashes = _Hashes;
			if (hashes != null)
			{
				h = hashes[0];
			}
			if (h != null)
				return h;

			using (var hs = CreateHashStream())
			{
				var stream = new BitcoinStream(hs, true)
				{
					TransactionOptions = TransactionOptions.None,
					ConsensusFactory = GetConsensusFactory(),
				};
				stream.SerializationTypeScope(SerializationType.Hash);
				this.ReadWrite(stream);
				h = hs.GetHash();
			}

			hashes = _Hashes;
			if (hashes != null)
			{
				hashes[0] = h;
			}
			return h;
		}

		protected virtual HashStreamBase CreateHashStream()
		{
			return new HashStream();
		}

		protected virtual HashStreamBase CreateSignatureHashStream()
		{
			return new HashStream();
		}

		[Obsolete("Call PrecomputeHash(true, true) instead")]
		public void CacheHashes()
		{
			PrecomputeHash(true, true);
		}

		/// <summary>
		/// Precompute the transaction hash and witness hash so that later calls to GetHash() and GetWitHash() will returns the precomputed hash
		/// </summary>
		/// <param name="invalidateExisting">If true, the previous precomputed hash is thrown away, else it is reused</param>
		/// <param name="lazily">If true, the hash will be calculated and cached at the first call to GetHash(), else it will be immediately</param>
		public void PrecomputeHash(bool invalidateExisting, bool lazily)
		{
			_Hashes = invalidateExisting ? new uint256[2] : _Hashes ?? new uint256[2];
			if (!lazily && _Hashes[0] == null)
				_Hashes[0] = GetHash();
			if (!lazily && _Hashes[1] == null)
				_Hashes[1] = GetWitHash();
		}

		public Transaction Clone(bool cloneCache)
		{
			var clone = BitcoinSerializableExtensions.Clone(this);
			if (cloneCache)
				clone._Hashes = _Hashes.ToArray();
			return clone;
		}

		uint256[] _Hashes = null;

		public uint256 GetWitHash()
		{
			if (!HasWitness)
				return GetHash();

			uint256 h = null;
			var hashes = _Hashes;
			if (hashes != null)
			{
				h = hashes[1];
			}
			if (h != null)
				return h;

			using (HashStream hs = new HashStream())
			{
				this.ReadWrite(new BitcoinStream(hs, true)
				{
					TransactionOptions = TransactionOptions.Witness
				});
				h = hs.GetHash();
			}

			hashes = _Hashes;
			if (hashes != null)
			{
				hashes[1] = h;
			}
			return h;
		}
		public uint256 GetSignatureHash(ICoin coin, SigHash sigHash = SigHash.All)
		{
			return GetIndexedInput(coin).GetSignatureHash(coin, sigHash);
		}
		public TransactionSignature SignInput(ISecret secret, ICoin coin, SigHash sigHash = SigHash.All)
		{
			return SignInput(secret.PrivateKey, coin, sigHash);
		}
		public TransactionSignature SignInput(Key key, ICoin coin, SigHash sigHash = SigHash.All)
		{
			return GetIndexedInput(coin).Sign(key, coin, sigHash);
		}

		private IndexedTxIn GetIndexedInput(ICoin coin)
		{
			return Inputs.FindIndexedInput(coin.Outpoint) ?? throw new ArgumentException("The coin is not being spent by this transaction", nameof(coin));
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

		[Obsolete("Use Transaction.Outputs.Add(Money money = null, IDestination destination = null) instead")]
		public TxOut AddOutput(Money money, IDestination destination)
		{
			return AddOutput(money, destination.ScriptPubKey);
		}

		[Obsolete("Use Transaction.Outputs.Add(Money money = null, Script scriptPubKey = null) instead")]
		public TxOut AddOutput(Money money, Script scriptPubKey)
		{
			return AddOutput(CreateOutput(money, scriptPubKey));
		}

		[Obsolete("Use Transaction.Outputs.CreateNewTxOut(Money money = null, Script scriptPubKey = null) instead")]
		public TxOut CreateOutput(Money money, Script scriptPubKey)
		{
			return Outputs.CreateNewTxOut(money, scriptPubKey);
		}

		[Obsolete("Use Transaction.Outputs.Add(Money money = null, Script scriptPubKey = null) instead")]
		public TxOut AddOutput(TxOut @out)
		{
			this.vout.Add(@out);
			return @out;
		}

		[Obsolete("Use Transaction.Inputs.Add(OutPoint outpoint = null, Script scriptSig = null, Sequence? sequence = null) instead")]
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

		[Obsolete("Use Transaction.Inputs.Add(prevTx, int outIndex) instead")]
		public TxIn AddInput(Transaction prevTx, int outIndex)
		{
			return Inputs.Add(prevTx, outIndex);
		}


		/// <summary>
		/// Sign a specific coin with the given secret
		/// </summary>
		/// <param name="secrets">Secrets</param>
		/// <param name="coins">Coins to sign</param>
		public void Sign(ISecret[] secrets, ICoin[] coins)
		{
			Sign(secrets.Select(s => s.PrivateKey).ToArray(), coins);
		}

		/// <summary>
		/// Sign a specific coin with the given secret
		/// </summary>
		/// <param name="keys">Private keys</param>
		/// <param name="coins">Coins to sign</param>
		public void Sign(Key[] keys, ICoin[] coins)
		{
			TransactionBuilder builder = this.GetConsensusFactory().CreateTransactionBuilder();
			builder.AddKeys(keys);
			builder.AddCoins(coins);
			builder.SignTransactionInPlace(this);
		}

		/// <summary>
		/// Sign a specific coin with the given secret
		/// </summary>
		/// <param name="secret">Secret</param>
		/// <param name="coins">Coins to sign</param>
		public void Sign(ISecret secret, ICoin[] coins)
		{
			Sign(new[] { secret }, coins);
		}

		/// <summary>
		/// Sign a specific coin with the given secret
		/// </summary>
		/// <param name="secrets">Secrets</param>
		/// <param name="coins">Coins to sign</param>
		public void Sign(ISecret[] secrets, ICoin coin)
		{
			Sign(secrets, new[] { coin });
		}

		/// <summary>
		/// Sign a specific coin with the given secret
		/// </summary>
		/// <param name="secret">Secret</param>
		/// <param name="coin">Coins to sign</param>
		public void Sign(ISecret secret, ICoin coin)
		{
			Sign(new[] { secret }, new[] { coin });
		}

		/// <summary>
		/// Sign a specific coin with the given secret
		/// </summary>
		/// <param name="key">Private key</param>
		/// <param name="coins">Coins to sign</param>
		public void Sign(Key key, ICoin[] coins)
		{
			Sign(new[] { key }, coins);
		}

		/// <summary>
		/// Sign a specific coin with the given secret
		/// </summary>
		/// <param name="key">Private key</param>
		/// <param name="coin">Coin to sign</param>
		public void Sign(Key key, ICoin coin)
		{
			Sign(new[] { key }, new[] { coin });
		}

		/// <summary>
		/// Sign a specific coin with the given secret
		/// </summary>
		/// <param name="keys">Private keys</param>
		/// <param name="coin">Coin to sign</param>
		public void Sign(Key[] keys, ICoin coin)
		{
			Sign(keys, new[] { coin });
		}

		/// <summary>
		/// Sign the transaction with a private key
		/// <para>ScriptSigs should be filled with previous ScriptPubKeys</para>
		/// <para>For more complex scenario, use TransactionBuilder</para>
		/// </summary>
		/// <param name="secret"></param>
		[Obsolete("Use Sign(ISecret,ICoin[]) instead)")]
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
		[Obsolete("Use Sign(Key,ICoin[]) instead)")]
		public void Sign(Key key, bool assumeP2SH)
		{
			List<Coin> coins = new List<Coin>();
			for (int i = 0; i < Inputs.Count; i++)
			{
				var txin = Inputs[i];
				if (Script.IsNullOrEmpty(txin.ScriptSig))
					throw new InvalidOperationException("ScriptSigs should be filled with either previous scriptPubKeys or redeem script (for P2SH)");
				if (assumeP2SH)
				{
					var p2shSig = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(txin.ScriptSig);
					if (p2shSig == null)
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

#if !NOJSONNET
		[Obsolete("Do not parse JSON")]
		public static Transaction Parse(string tx, RawFormat format, Network network = null)
		{
			return GetFormatter(format, network).ParseJson(tx);
		}
#endif

		public static Transaction Parse(string hex, Network network)
		{
			var tx = network.Consensus.ConsensusFactory.CreateTransaction();
			var data = Encoders.Hex.DecodeData(hex);
			var stream = new BitcoinStream(data);
			stream.ConsensusFactory = network.Consensus.ConsensusFactory;
			tx.ReadWrite(stream);
			return tx;
		}


		[Obsolete("Use Transaction.Parse(string hex, Network network)")]
		public static Transaction Parse(string hex)
		{
			return new Transaction(Encoders.Hex.DecodeData(hex));
		}

		public string ToHex()
		{
			return Encoders.Hex.EncodeData(this.ToBytes());
		}
#if !NOJSONNET
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
			switch (rawFormat)
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
			if (formatter == null)
				throw new ArgumentNullException("formatter");
			return formatter.ToString(this);
		}
#endif
		/// <summary>
		/// Calculate the fee of the transaction
		/// </summary>
		/// <param name="spentCoins">Coins being spent</param>
		/// <returns>Fee or null if some spent coins are missing or if spentCoins is null</returns>
		public virtual Money GetFee(ICoin[] spentCoins)
		{
			if (IsCoinBase)
				return Money.Zero;
			spentCoins = spentCoins ?? new ICoin[0];

			Money fees = -TotalOut;
			foreach (var input in this.Inputs)
			{
				var coin = spentCoins.FirstOrDefault(s => s.Outpoint == input.PrevOut);
				if (coin == null)
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
			if (fee == null)
				return null;
			return new FeeRate(fee, this.GetVirtualSize());
		}

		public bool IsFinal(ChainedBlock block)
		{
			if (block == null)
				return IsFinal(Utils.UnixTimeToDateTime(0), 0);
			if (block.Header == null)
				throw new InvalidOperationException("ChainedBlock.Header must be available");
			return IsFinal(block.Header.BlockTime, block.Height);
		}
		public bool IsFinal(DateTimeOffset blockTime, int blockHeight)
		{
			var nBlockTime = Utils.DateTimeToUnixTime(blockTime);
			if (nLockTime == 0)
				return true;
			if ((long)nLockTime < ((long)nLockTime < LockTime.LOCKTIME_THRESHOLD ? (long)blockHeight : nBlockTime))
				return true;
			foreach (var txin in Inputs)
				if (!txin.IsFinal)
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
			if (prevHeights.Length != Inputs.Count)
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
			if (!fEnforceBIP68)
			{
				return new SequenceLock(nMinHeight, nMinTime);
			}

			for (var txinIndex = 0; txinIndex < Inputs.Count; txinIndex++)
			{
				TxIn txin = Inputs[txinIndex];

				// Sequence numbers with the most significant bit set are not
				// treated as relative lock-times, nor are they given any
				// consensus-enforced meaning at this point.
				if ((txin.Sequence & Sequence.SEQUENCE_LOCKTIME_DISABLE_FLAG) != 0)
				{
					// The height of this input is not relevant for sequence locks
					prevHeights[txinIndex] = 0;
					continue;
				}

				int nCoinHeight = prevHeights[txinIndex];

				if ((txin.Sequence & Sequence.SEQUENCE_LOCKTIME_TYPE_FLAG) != 0)
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
			if (options == TransactionOptions.Witness && HasWitness)
				return this;
			if (options == TransactionOptions.None && !HasWitness)
				return this;
			var instance = GetConsensusFactory().CreateTransaction();
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

		public virtual bool HasWitness
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
			if (Inputs.Count == 0)
				return TransactionCheckResult.NoInput;
			if (Outputs.Count == 0)
				return TransactionCheckResult.NoOutput;
			// Size limits
			if (this.GetSerializedSize() > MAX_BLOCK_SIZE)
				return TransactionCheckResult.TransactionTooLarge;

			// Check for negative or overflow output values
			long nValueOut = 0;
			foreach (var txout in Outputs)
			{
				if (txout.Value < 0)
					return TransactionCheckResult.NegativeOutput;
				if (txout.Value > MAX_MONEY)
					return TransactionCheckResult.OutputTooLarge;
				nValueOut += txout.Value;
				if (!((nValueOut >= 0 && nValueOut <= (long)MAX_MONEY)))
					return TransactionCheckResult.OutputTotalTooLarge;
			}

			// Check for duplicate inputs
			var vInOutPoints = new HashSet<OutPoint>();
			foreach (var txin in Inputs)
			{
				if (vInOutPoints.Contains(txin.PrevOut))
					return TransactionCheckResult.DuplicateInputs;
				vInOutPoints.Add(txin.PrevOut);
			}

			if (IsCoinBase)
			{
				if (Inputs[0].ScriptSig.Length < 2 || Inputs[0].ScriptSig.Length > 100)
					return TransactionCheckResult.CoinbaseScriptTooLarge;
			}
			else
			{
				foreach (var txin in Inputs)
					if (txin.PrevOut.IsNull)
						return TransactionCheckResult.NullInputPrevOut;
			}

			return TransactionCheckResult.Success;
		}

		[Obsolete("Use Transaction.GetSignatureHash(Script scriptCode, int nIn, SigHash nHashType, TxOut spentOutput, HashVersion sigversion, PrecomputedTransactionData precomputedTransactionData) instead")]
		public uint256 GetSignatureHash(Script scriptCode, int nIn, SigHash nHashType, Money amount, HashVersion sigversion, PrecomputedTransactionData precomputedTransactionData)
		{
			TxOut txOut = this.Outputs.CreateNewTxOut();
			txOut.Value = amount;
			return GetSignatureHash(scriptCode, nIn, nHashType, txOut, sigversion, precomputedTransactionData);
		}
		public virtual uint256 GetSignatureHash(Script scriptCode, int nIn, SigHash nHashType, TxOut spentOutput, HashVersion sigversion, PrecomputedTransactionData precomputedTransactionData)
		{
			if (sigversion == HashVersion.Witness)
			{
				if (spentOutput?.Value == null || spentOutput.Value == TxOut.NullMoney)
					throw new ArgumentException("The output being signed with the amount must be provided", nameof(spentOutput));
				uint256 hashPrevouts = uint256.Zero;
				uint256 hashSequence = uint256.Zero;
				uint256 hashOutputs = uint256.Zero;

				if ((nHashType & SigHash.AnyoneCanPay) == 0)
				{
					hashPrevouts = precomputedTransactionData == null ?
								   GetHashPrevouts() : precomputedTransactionData.HashPrevouts;
				}

				if ((nHashType & SigHash.AnyoneCanPay) == 0 && ((uint)nHashType & 0x1f) != (uint)SigHash.Single && ((uint)nHashType & 0x1f) != (uint)SigHash.None)
				{
					hashSequence = precomputedTransactionData == null ?
								   GetHashSequence() : precomputedTransactionData.HashSequence;
				}

				if (((uint)nHashType & 0x1f) != (uint)SigHash.Single && ((uint)nHashType & 0x1f) != (uint)SigHash.None)
				{
					hashOutputs = precomputedTransactionData == null ?
									GetHashOutputs() : precomputedTransactionData.HashOutputs;
				}
				else if (((uint)nHashType & 0x1f) == (uint)SigHash.Single && nIn < this.Outputs.Count)
				{
					BitcoinStream ss = CreateHashWriter(sigversion);
					ss.ReadWrite(this.Outputs[nIn]);
					hashOutputs = GetHash(ss);
				}

				BitcoinStream sss = CreateHashWriter(sigversion);
				// Version
				sss.ReadWrite(this.Version);
				// Input prevouts/nSequence (none/all, depending on flags)
				sss.ReadWrite(hashPrevouts);
				sss.ReadWrite(hashSequence);
				// The input being signed (replacing the scriptSig with scriptCode + amount)
				// The prevout may already be contained in hashPrevout, and the nSequence
				// may already be contain in hashSequence.
				sss.ReadWrite(Inputs[nIn].PrevOut);
				sss.ReadWrite(scriptCode);
				sss.ReadWrite(spentOutput.Value.Satoshi);
				sss.ReadWrite((uint)Inputs[nIn].Sequence);
				// Outputs (none/one/all, depending on flags)
				sss.ReadWrite(hashOutputs);
				// Locktime
				sss.ReadWriteStruct(LockTime);
				// Sighash type
				sss.ReadWrite((uint)nHashType);

				return GetHash(sss);
			}

			bool fAnyoneCanPay = (nHashType & SigHash.AnyoneCanPay) != 0;
			bool fHashSingle = ((byte)nHashType & 0x1f) == (byte)SigHash.Single;
			bool fHashNone = ((byte)nHashType & 0x1f) == (byte)SigHash.None;

			if (nIn >= Inputs.Count)
			{
				return uint256.One;
			}
			if (fHashSingle)
			{
				if (nIn >= Outputs.Count)
				{
					return uint256.One;
				}
			}

			var stream = CreateHashWriter(sigversion);
			stream.ReadWrite(Version);
			uint nInputs = (uint)(fAnyoneCanPay ? 1 : Inputs.Count);
			stream.ReadWriteAsVarInt(ref nInputs);
			for (int nInput = 0; nInput < nInputs; nInput++)
			{
				if (fAnyoneCanPay)
					nInput = nIn;
				stream.ReadWrite(Inputs[nInput].PrevOut);
				if (nInput != nIn)
				{
					stream.ReadWrite(Script.Empty);
				}
				else
				{
					WriteScriptCode(stream, scriptCode);
				}

				if (nInput != nIn && (fHashSingle || fHashNone))
					stream.ReadWrite((uint)0);
				else
					stream.ReadWrite((uint)Inputs[nInput].Sequence);
			}

			uint nOutputs = (uint)(fHashNone ? 0 : (fHashSingle ? nIn + 1 : Outputs.Count));
			stream.ReadWriteAsVarInt(ref nOutputs);
			for (int nOutput = 0; nOutput < nOutputs; nOutput++)
			{
				if (fHashSingle && nOutput != nIn)
				{
					this.Outputs.CreateNewTxOut().ReadWrite(stream);
				}
				else
				{
					Outputs[nOutput].ReadWrite(stream);
				}
			}

			stream.ReadWriteStruct(LockTime);
			stream.ReadWrite((uint)nHashType);
			return GetHash(stream);
		}

		private static void WriteScriptCode(BitcoinStream stream, Script scriptCode)
		{
			int nCodeSeparators = 0;
			var reader = scriptCode.CreateReader();
			OpcodeType opcode;
			while (reader.TryReadOpCode(out opcode))
			{
				if (opcode == OpcodeType.OP_CODESEPARATOR)
					nCodeSeparators++;
			}

			uint n = (uint)(scriptCode.Length - nCodeSeparators);
			stream.ReadWriteAsVarInt(ref n);

			reader = scriptCode.CreateReader();
			int itBegin = 0;
			while (reader.TryReadOpCode(out opcode))
			{
				if (opcode == OpcodeType.OP_CODESEPARATOR)
				{
					stream.Inner.Write(scriptCode.ToBytes(true), itBegin, (int)(reader.Inner.Position - itBegin - 1));
					itBegin = (int)reader.Inner.Position;
				}
			}

			if (itBegin != scriptCode.Length)
				stream.Inner.Write(scriptCode.ToBytes(true), itBegin, (int)(reader.Inner.Position - itBegin));
		}

		[Obsolete("Use Transaction.GetSignatureHash(Script scriptCode, int nIn, SigHash nHashType, TxOut spentOutput= null, HashVersion sigversion = HashVersion.Original) instead")]
		public uint256 GetSignatureHash(Script scriptCode, int nIn, SigHash nHashType, Money amount, HashVersion sigversion = HashVersion.Original)
		{
			return this.GetSignatureHash(scriptCode, nIn, nHashType, amount, sigversion, null);
		}

		public uint256 GetSignatureHash(Script scriptCode, int nIn, SigHash nHashType, TxOut spentOutput= null, HashVersion sigversion = HashVersion.Original)
		{
			return this.GetSignatureHash(scriptCode, nIn, nHashType, spentOutput, sigversion, null);
		}

		private static uint256 GetHash(BitcoinStream stream)
		{
			var preimage = ((HashStreamBase)stream.Inner).GetHash();
			stream.Inner.Dispose();
			return preimage;
		}

		internal virtual uint256 GetHashOutputs()
		{
			uint256 hashOutputs;
			BitcoinStream ss = CreateHashWriter(HashVersion.Witness);
			foreach (var txout in Outputs)
			{
				txout.ReadWrite(ss);
			}
			hashOutputs = GetHash(ss);
			return hashOutputs;
		}

		internal virtual uint256 GetHashSequence()
		{
			uint256 hashSequence;
			BitcoinStream ss = CreateHashWriter(HashVersion.Witness);
			foreach (var input in Inputs)
			{
				ss.ReadWrite((uint)input.Sequence);
			}
			hashSequence = GetHash(ss);
			return hashSequence;
		}

		internal virtual uint256 GetHashPrevouts()
		{
			uint256 hashPrevouts;
			BitcoinStream ss = CreateHashWriter(HashVersion.Witness);
			foreach (var input in Inputs)
			{
				ss.ReadWrite(input.PrevOut);
			}
			hashPrevouts = GetHash(ss);
			return hashPrevouts;
		}

		protected BitcoinStream CreateHashWriter(HashVersion version)
		{
			var hs = CreateSignatureHashStream();
			BitcoinStream stream = new BitcoinStream(hs, true);
			stream.Type = SerializationType.Hash;
			stream.TransactionOptions = version == HashVersion.Original ? TransactionOptions.None : TransactionOptions.Witness;
			return stream;
		}

		public virtual ConsensusFactory GetConsensusFactory()
		{
			return Bitcoin.Instance.Mainnet.Consensus.ConsensusFactory;
		}

		public Transaction Clone()
		{
			var instance = GetConsensusFactory().CreateTransaction();
			instance.ReadWrite(new BitcoinStream(this.ToBytes()) { ConsensusFactory = GetConsensusFactory() });
			return instance;
		}

		public void FromBytes(byte[] bytes)
		{
			this.ReadWrite(new BitcoinStream(bytes) { ConsensusFactory = GetConsensusFactory() });
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
