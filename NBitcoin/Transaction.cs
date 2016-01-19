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
	public class OutPoint : IBitcoinSerializable
	{
		public bool IsNull
		{
			get
			{
				return (hash == 0 && n == uint.MaxValue);
			}
		}
		private uint256 hash;
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
			if(str == null)
				throw new ArgumentNullException("str");
			var splitted = str.Split('-');
			if(splitted.Length != 2)
				return false;

			uint256 hash;
			if(!uint256.TryParse(splitted[0], out hash))
				return false;

			uint index;
			if(!uint.TryParse(splitted[1], out index))
				return false;
			result = new OutPoint(hash, index);
			return true;
		}

		public static OutPoint Parse(string str)
		{
			OutPoint result;
			if(TryParse(str, out result))
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
			hash = 0;
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
			if(Object.ReferenceEquals(a, null))
			{
				return Object.ReferenceEquals(b, null);
			}
			if(Object.ReferenceEquals(b, null))
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
			if(object.ReferenceEquals(null, item))
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
		OutPoint prevout = new OutPoint();
		Script scriptSig = Script.Empty;
		uint nSequence = uint.MaxValue;
		public const uint NO_SEQUENCE = uint.MaxValue;

		public uint Sequence
		{
			get
			{
				return nSequence;
			}
			set
			{
				nSequence = value;
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

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
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
			if(n == 0)
				return 0;
			int e = 0;
			while(((n % 10) == 0) && e < 9)
			{
				n /= 10;
				e++;
			}
			if(e < 9)
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
			if(x == 0)
				return 0;
			x--;
			// x = 10*(9*n + d - 1) + e
			int e = (int)(x % 10);
			x /= 10;
			ulong n = 0;
			if(e < 9)
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
			while(e != 0)
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
			if(stream.Serializing)
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
			if(!stream.Serializing)
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

		private KeyId GetKeyId()
		{
			if(_Script.Length == 25 && _Script[0] == (byte)OpcodeType.OP_DUP && _Script[1] == (byte)OpcodeType.OP_HASH160
								&& _Script[2] == 20 && _Script[23] == (byte)OpcodeType.OP_EQUALVERIFY
								&& _Script[24] == (byte)OpcodeType.OP_CHECKSIG)
			{
				return new KeyId(_Script.SafeSubarray(3, 20));
			}
			return null;
		}

		private ScriptId GetScriptId()
		{
			if(_Script.Length == 23 && _Script[0] == (byte)OpcodeType.OP_HASH160 && _Script[1] == 20
								&& _Script[22] == (byte)OpcodeType.OP_EQUAL)
			{
				return new ScriptId(_Script.SafeSubarray(2, 20));
			}
			return null;
		}

		private PubKey GetPubKey()
		{
			return PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(new Script(_Script));
		}

		byte[] Compress()
		{
			byte[] result = null;
			KeyId keyID = GetKeyId();
			if(keyID != null)
			{
				result = new byte[21];
				result[0] = 0x00;
				Array.Copy(keyID.ToBytes(), 0, result, 1, 20);
				return result;
			}
			ScriptId scriptID = GetScriptId();
			if(scriptID != null)
			{
				result = new byte[21];
				result[0] = 0x01;
				Array.Copy(scriptID.ToBytes(), 0, result, 1, 20);
				return result;
			}
			PubKey pubkey = GetPubKey();
			if(pubkey != null)
			{
				result = new byte[33];
				var pubBytes = pubkey.ToBytes();
				Array.Copy(pubBytes, 1, result, 1, 32);
				if(pubBytes[0] == 0x02 || pubBytes[0] == 0x03)
				{
					result[0] = pubBytes[0];
					return result;
				}
				else if(pubBytes[0] == 0x04)
				{
					result[0] = (byte)(0x04 | (pubBytes[64] & 0x01));
					return result;
				}
			}
			return null;
		}

		Script Decompress(uint nSize, byte[] data)
		{
			switch(nSize)
			{
				case 0x00:
					return new Script(OpcodeType.OP_DUP, OpcodeType.OP_HASH160, Op.GetPushOp(data.SafeSubarray(0, 20)), OpcodeType.OP_EQUALVERIFY, OpcodeType.OP_CHECKSIG);
				case 0x01:
					return new Script(OpcodeType.OP_HASH160, Op.GetPushOp(data.SafeSubarray(0, 20)), OpcodeType.OP_EQUAL);
				case 0x02:
				case 0x03:
					return new Script(Op.GetPushOp(new byte[] { (byte)nSize }.Concat(data.SafeSubarray(0, 32)).ToArray()), OpcodeType.OP_CHECKSIG);
				case 0x04:
				case 0x05:
					byte[] vch = new byte[33];
					vch[0] = (byte)(nSize - 2);
					Array.Copy(data, 0, vch, 1, 32);
					PubKey pubkey = new PubKey(vch);
					pubkey = pubkey.Decompress();
					return new Script(Op.GetPushOp(pubkey.ToBytes()), OpcodeType.OP_CHECKSIG);
			}
			return null;
		}





		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if(stream.Serializing)
			{
				var compr = Compress();
				if(compr != null)
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
				if(nSize < nSpecialScripts)
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
			if(nSize == 0 || nSize == 1)
				return 20;
			if(nSize == 2 || nSize == 3 || nSize == 4 || nSize == 5)
				return 32;
			return 0;
		}



		#endregion
	}

	public class TxOut : IBitcoinSerializable, IDestination
	{
		Script publicKey = Script.Empty;
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

		private long value = -1;
		Money _MoneyValue;
		public bool IsNull
		{
			get
			{
				return value == -1;
			}
		}



		public TxOut()
		{

		}

		public TxOut(Money value, IDestination destination)
		{
			Value = value;
			if(destination != null)
				ScriptPubKey = destination.ScriptPubKey;
		}

		public TxOut(Money value, Script scriptPubKey)
		{
			Value = value;
			ScriptPubKey = scriptPubKey;
		}

		public Money Value
		{
			get
			{
				if(_MoneyValue == null)
					_MoneyValue = new Money(value);
				return _MoneyValue;
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException("value");
				_MoneyValue = value;
				this.value = (long)_MoneyValue.Satoshi;
			}
		}


		public bool IsDust(FeeRate minRelayTxFee)
		{
			return (Value < GetDustThreshold(minRelayTxFee));
		}

		public Money GetDustThreshold(FeeRate minRelayTxFee)
		{
			if(minRelayTxFee == null)
				throw new ArgumentNullException("minRelayTxFee");
			int nSize = this.GetSerializedSize() + 148;
			return 3 * minRelayTxFee.GetFee(nSize);
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref value);
			stream.ReadWrite(ref publicKey);
			_MoneyValue = null; //Might been updated
		}

		#endregion

		public bool IsTo(IDestination destination)
		{
			return ScriptPubKey == destination.ScriptPubKey;
		}

		internal void SetNull()
		{
			value = -1;
		}

		public static TxOut Parse(string hex)
		{
			var ret = new TxOut();
			ret.FromBytes(Encoders.Hex.DecodeData(hex));
			return ret;
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
				return Transaction.Witness[(int)Index];
			}
			set
			{
				Transaction.Witness[(int)Index] = value;
			}
		}
		public Transaction Transaction
		{
			get;
			set;
		}

		public bool VerifyScript(Script scriptPubKey, ScriptVerify scriptVerify = ScriptVerify.Standard)
		{
			ScriptError unused;
			return VerifyScript(scriptPubKey, scriptVerify, out unused);
		}
		public bool VerifyScript(Script scriptPubKey, out ScriptError error)
		{
			return Script.VerifyScript(scriptPubKey, Transaction, (int)Index, null, out error);
		}
		public bool VerifyScript(Script scriptPubKey, ScriptVerify scriptVerify, out ScriptError error)
		{
			return Script.VerifyScript(scriptPubKey, Transaction, (int)Index, null, scriptVerify, SigHash.Undefined, out error);
		}
		public uint256 GetSignatureHash(Script scriptPubKey, SigHash sigHash = SigHash.All)
		{
			return scriptPubKey.SignatureHash(Transaction, (int)Index, sigHash);
		}
		public TransactionSignature Sign(ISecret secret, Script scriptPubKey, SigHash sigHash = SigHash.All)
		{
			return Sign(secret.PrivateKey, scriptPubKey, sigHash);
		}
		public TransactionSignature Sign(Key key, Script scriptPubKey, SigHash sigHash = SigHash.All)
		{
			var hash = GetSignatureHash(scriptPubKey, sigHash);
			return key.Sign(hash, sigHash);
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
			return AsIndexedOutputs().Select(i => i.ToCoin());
		}

		public IEnumerable<IndexedTxOut> AsSpendableIndexedOutputs()
		{
			return AsIndexedOutputs()
					.Where(r => !r.TxOut.ScriptPubKey.IsUnspendable);
		}
	}

	public enum RawFormat
	{
		Satoshi,
		BlockExplorer,
	}

	public class WitScript
	{
		byte[][] _Pushes;
		public WitScript(string script)
		{
			var parts = script.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			_Pushes = new byte[parts.Length][];
			for(int i = 0 ; i < parts.Length ; i++)
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
			if(@unsafe)
				_Pushes = script;
			else
			{
				_Pushes = script.ToArray();
				for(int i = 0 ; i < _Pushes.Length ; i++)
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
			foreach(var op in ops)
			{
				if(op.PushData == null)
					throw new ArgumentException("Non push operation unsupported in WitScript", "ops");
				pushes.Add(op.PushData);
			}
			_Pushes = pushes.ToArray();
		}

		public WitScript(byte[] script)
		{
			if(script == null)
				throw new ArgumentNullException("script");
			var ms = new MemoryStream(script);
			BitcoinStream stream = new BitcoinStream(ms, false);
			ReadCore(stream);
		}
		WitScript()
		{

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
			for(int i = 0 ; i < (int)pushCount ; i++)
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

		public byte[] ToBytes()
		{
			var ms = new MemoryStream();
			BitcoinStream stream = new BitcoinStream(ms, true);
			uint pushCount = (uint)_Pushes.Length;
			stream.ReadWriteAsVarInt(ref pushCount);
			foreach(var push in Pushes)
			{
				var localpush = push;
				stream.ReadWriteAsVarString(ref localpush);
			}
			return ms.ToArrayEfficient();
		}

		public override string ToString()
		{
			return String.Join(" ", _Pushes.Select(p => Encoders.Hex.EncodeData(p)).ToArray());
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
	}

	public enum TransactionOptions : uint
	{
		None = 0x00000000,
		Witness = 0x40000000,
		All = Witness
	}
	public class Witness : IEnumerable<WitScript>
	{
		List<WitScript> _Scripts = new List<WitScript>();
		internal void SetNull()
		{
			_Scripts.Clear();
		}

		internal void Size(int size)
		{
			_Scripts.Resize(size);
			for(int i = 0 ; i < size ; i++)
			{
				_Scripts[i] = _Scripts[i] == null ? WitScript.Empty : _Scripts[i];
			}
		}

		internal void ReadWrite(BitcoinStream stream)
		{
			for(int i = 0 ; i < _Scripts.Count ; i++)
			{
				if(stream.Serializing)
				{
					var bytes = _Scripts[i].ToBytes();
					stream.ReadWrite(ref bytes);
				}
				else
				{
					_Scripts[i] = WitScript.Load(stream);
				}
			}
		}

		internal bool IsNull()
		{
			return _Scripts.All(s => s == WitScript.Empty);
		}

		public int Count
		{
			get
			{
				return _Scripts.Count;
			}
		}

		public WitScript this[int txinIndex]
		{
			get
			{
				if(txinIndex >= _Scripts.Count)
					return WitScript.Empty;
				return _Scripts[txinIndex];
			}
			set
			{
				EnsureSize(txinIndex + 1);
				_Scripts[txinIndex] = value;
			}
		}

		private void EnsureSize(int size)
		{
			if(size > _Scripts.Count)
				Size(size);
		}

		public WitScript TryGet(int index)
		{
			if(index >= _Scripts.Count)
				return null;
			return _Scripts[index];
		}

		#region IEnumerable<WitnessScript> Members

		public IEnumerator<WitScript> GetEnumerator()
		{
			return _Scripts.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}

	//https://en.bitcoin.it/wiki/Transactions
	//https://en.bitcoin.it/wiki/Protocol_specification
	public class Transaction : IBitcoinSerializable
	{
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

		private Witness wit = new Witness();
		public Witness Witness
		{
			get
			{
				return wit;
			}
		}

		//Since it is impossible to serialize a transaction with 0 input without problems during deserialization with wit activated (we fit a flag in the version to workaround it)
		const uint NoInputTx = (1 << 27);

		#region IBitcoinSerializable Members

		public virtual void ReadWrite(BitcoinStream stream)
		{
			var witSupported = (((uint)stream.TransactionOptions & (uint)TransactionOptions.Witness) != 0);			

			byte flags = 0;
			if(!stream.Serializing)
			{
				stream.ReadWrite(ref nVersion);
				var wasInvalid = (nVersion & NoInputTx) != 0;
				if(wasInvalid)
					nVersion = nVersion & ~NoInputTx;
				/* Try to read the vin. In case the dummy is there, this will be read as an empty vector. */
				stream.ReadWrite<TxInList, TxIn>(ref vin);
				if(vin.Count == 0 && witSupported && !wasInvalid)
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
				}
				else
				{
					/* We read a non-empty vin. Assume a normal vout follows. */
					stream.ReadWrite<TxOutList, TxOut>(ref vout);
					vout.Transaction = this;
				}
				wit.SetNull();
				if(((flags & 1) != 0) && witSupported)
				{
					/* The witness flag is present, and we support witnesses. */
					flags ^= 1;
					//const_cast<CTxWitness*>(&tx.wit)->vtxinwit.resize(tx.vin.size());
					wit.Size(vin.Count);
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
				var version = vin.Count == 0 && vout.Count > 0 ? nVersion | NoInputTx : nVersion;
				stream.ReadWrite(ref version);

				if(witSupported)
				{
					/* Check whether witnesses need to be serialized. */
					if(!wit.IsNull())
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
					wit.Size(vin.Count);
					wit.ReadWrite(stream);
				}
			}
			stream.ReadWriteStruct(ref nLockTime);
		}

		#endregion

		public uint256 GetHash()
		{
			MemoryStream ms = new MemoryStream();
			this.ReadWrite(new BitcoinStream(ms, true)
			{
				TransactionOptions = TransactionOptions.None
			});
			return Hashes.Hash256(ms.ToArrayEfficient());
		}
		public uint256 GetWitHash()
		{
			return Hashes.Hash256(this.ToBytes());
		}
		public uint256 GetSignatureHash(Script scriptPubKey, int nIn, SigHash sigHash = SigHash.All)
		{
			return Inputs.AsIndexedInputs().ToArray()[nIn].GetSignatureHash(scriptPubKey, sigHash);
		}
		public TransactionSignature SignInput(ISecret secret, Script scriptPubKey, int nIn, SigHash sigHash = SigHash.All)
		{
			return SignInput(secret.PrivateKey, scriptPubKey, nIn, sigHash);
		}
		public TransactionSignature SignInput(Key key, Script scriptPubKey, int nIn, SigHash sigHash = SigHash.All)
		{
			return Inputs.AsIndexedInputs().ToArray()[nIn].Sign(key, scriptPubKey, sigHash);
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
			TransactionBuilder builder = new TransactionBuilder();
			builder.AddKeys(key);
			for(int i = 0 ; i < Inputs.Count ; i++)
			{
				var txin = Inputs[i];
				if(Script.IsNullOrEmpty(txin.ScriptSig))
					throw new InvalidOperationException("ScriptSigs should be filled with either previous scriptPubKeys or redeem script (for P2SH)");
				if(assumeP2SH)
				{
					var p2shSig = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(txin.ScriptSig);
					if(p2shSig == null)
					{
						builder.AddCoins(new ScriptCoin(txin.PrevOut, new TxOut()
						{
							ScriptPubKey = txin.ScriptSig.PaymentScript,
						}, txin.ScriptSig));
					}
					else
					{
						builder.AddCoins(new ScriptCoin(txin.PrevOut, new TxOut()
						{
							ScriptPubKey = p2shSig.RedeemScript.PaymentScript
						}, p2shSig.RedeemScript));
					}
				}
				else
				{
					builder.AddCoins(new Coin(txin.PrevOut, new TxOut()
					{
						ScriptPubKey = txin.ScriptSig
					}));
				}

			}
			builder.SignTransactionInPlace(this);
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

		public Transaction RemoveOption(TransactionOptions options)
		{
			var instance = new Transaction();
			var ms = new MemoryStream();
			var bms = new BitcoinStream(ms, true);
			bms.TransactionOptions = TransactionOptions.All & ~options;
			this.ReadWrite(bms);
			return instance;
		}
	}

}
