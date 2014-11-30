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
				return Object.ReferenceEquals(a, null);
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
			return Tuple.Create(hash, n).GetHashCode();
		}

		public override string ToString()
		{
			return N + "-" + Hash;
		}
	}


	public class TxIn : IBitcoinSerializable
	{
		public TxIn()
		{

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
			_Script = script.ToRawScript(true);
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
				return new KeyId(_Script.Skip(3).Take(20).ToArray());
			}
			return null;
		}

		private ScriptId GetScriptId()
		{
			if(_Script.Length == 23 && _Script[0] == (byte)OpcodeType.OP_HASH160 && _Script[1] == 20
								&& _Script[22] == (byte)OpcodeType.OP_EQUAL)
			{
				return new ScriptId(_Script.Skip(2).Take(20).ToArray());
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
					return new Script(OpcodeType.OP_DUP, OpcodeType.OP_HASH160, Op.GetPushOp(data.Take(20).ToArray()), OpcodeType.OP_EQUALVERIFY, OpcodeType.OP_CHECKSIG);
				case 0x01:
					return new Script(OpcodeType.OP_HASH160, Op.GetPushOp(data.Take(20).ToArray()), OpcodeType.OP_EQUAL);
				case 0x02:
				case 0x03:
					return new Script(Op.GetPushOp(new byte[] { (byte)nSize }.Concat(data.Take(32)).ToArray()), OpcodeType.OP_CHECKSIG);
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
					_Script = Decompress(nSize, vch).ToRawScript();
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

	public class TxOut : IBitcoinSerializable
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
		public TxOut(Money value, BitcoinAddress bitcoinAddress)
		{
			if(bitcoinAddress == null)
				throw new ArgumentNullException("bitcoinAddress");
			if(value == null)
				throw new ArgumentNullException("value");
			Value = value;
			SetDestination(bitcoinAddress.ID);
		}

		public TxOut(Money value, KeyId keyId)
		{
			Value = value;
			SetDestination(keyId);
		}
		public TxOut(Money value, ScriptId scriptId)
		{
			Value = value;
			SetDestination(scriptId);
		}

		public TxOut(Money value, Script scriptPubKey)
		{
			Value = value;
			ScriptPubKey = scriptPubKey;
		}
		public TxOut(Money value, PubKey pubkey)
		{
			Value = value;
			ScriptPubKey = PayToPubkeyTemplate.Instance.GenerateScriptPubKey(pubkey);
		}

		private void SetDestination(TxDestination destination)
		{
			ScriptPubKey = destination.CreateScriptPubKey();
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
				if(value.Satoshi > long.MaxValue || value.Satoshi < long.MinValue)
					throw new ArgumentOutOfRangeException("satoshi's value should be between Int64.Max and Int64.Min");
				_MoneyValue = value;
				this.value = (long)_MoneyValue.Satoshi;
			}
		}


		public bool IsDust
		{
			get
			{
				// "Dust" is defined in terms of CTransaction::nMinRelayTxFee,
				// which has units satoshis-per-kilobyte.
				// If you'd pay more than 1/3 in fees
				// to spend something, then we consider it dust.
				// A typical txout is 34 bytes big, and will
				// need a CTxIn of at least 148 bytes to spend,
				// so dust is a txout less than 546 satoshis 
				// with default nMinRelayTxFee.
				return ((value * 1000) / (3 * ((int)this.GetSerializedSize() + 148)) < Transaction.nMinRelayTxFee);
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref value);
			stream.ReadWrite(ref publicKey);
			_MoneyValue = null; //Might been updated
		}

		#endregion

		/// <summary>
		/// Verify belongs to the given address
		/// </summary>
		/// <param name="address">P2PKH or P2SH address</param>
		/// <returns></returns>
		public bool IsTo(BitcoinAddress address)
		{
			if(address == null)
				throw new ArgumentNullException("address");
			return IsTo(address.ID);
		}

		/// <summary>
		/// Verify belongs to the given address
		/// </summary>
		/// <param name="address">P2PKH or P2SH address</param>
		/// <returns></returns>
		public bool IsTo(TxDestination destination)
		{
			return ScriptPubKey.GetDestination() == destination;
		}

		/// <summary>
		/// Verify is a P2PK of the given public key
		/// </summary>
		/// <param name="pubkey"></param>
		/// <returns></returns>
		public bool IsTo(PubKey pubkey)
		{
			var owners = ScriptPubKey.GetDestinationPublicKeys();
			if(owners.Length != 1)
				return false;
			return owners[0] == pubkey;
		}


		internal void SetNull()
		{
			value = -1;
		}
	}

	public class TxInList: UnsignedList<TxIn>
	{
		public TxIn this[OutPoint outpoint]
		{
			get { return this[outpoint.N]; }
			set { this[outpoint.N] = value; }
		}
	}

	public class TxOutList: UnsignedList<TxOut>
	{
		public IEnumerable<TxOut> To(BitcoinAddress address)
		{
			return this.Where(r => r.IsTo(address));
		}

		public IEnumerable<TxOut> To(PubKey pubKey)
		{
			return this.Where(r => r.IsTo(pubKey));
		}

		public IEnumerable<TxOut> To(TxDestination destination)
		{
			return this.Where(r => r.IsTo(destination));
		}

		public IEnumerable<IndexedTxOut> Spendable()
		{
			// We want i as the index of txOut in Outputs[], not index in enumerable after where filter
			return this.Select((r, i) => new TxOutIndex() { txOut = r, i = (uint)i })
				.Where(r => !r.txOut.ScriptPubKey.IsUnspendable);
		}

		public interface IndexedTxOut
		{
			TxOut txOut {get;}
			uint i {get;}
		}

		private class TxOutIndex: IndexedTxOut
		{
			public TxOut txOut { get; set; }
			public uint i { get; set; }
		}
	}

	public enum RawFormat
	{
		Satoshi,
		BlockExplorer,
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
		TxInList vin = new TxInList();
		TxOutList vout = new TxOutList();
		LockTime nLockTime;

		public Transaction()
		{
		}
		public Transaction(string hex)
		{
			this.FromBytes(Encoders.Hex.DecodeData(hex));
		}
		public Transaction(byte[] bytes)
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

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref nVersion);
			stream.ReadWrite<TxInList,TxIn>(ref vin);
			stream.ReadWrite<TxOutList, TxOut>(ref vout);
			stream.ReadWriteStruct(ref nLockTime);
		}

		#endregion

		public uint256 GetHash()
		{
			return Hashes.Hash256(this.ToBytes());
		}

		public bool IsCoinBase
		{
			get
			{
				return (Inputs.Count == 1 && Inputs[0].PrevOut.IsNull);
			}
		}

		public const long nMinTxFee = 10000;  // Override with -mintxfee
		public const long nMinRelayTxFee = 1000;

		public static uint CURRENT_VERSION = 2;
		public static uint MAX_STANDARD_TX_SIZE = 100000;

		public TxOut AddOutput(Money money, BitcoinAddress address)
		{
			return AddOutput(new TxOut(money, address));
		}
		public TxOut AddOutput(Money money, KeyId keyId)
		{
			return AddOutput(new TxOut(money, keyId));
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
		public void Sign(BitcoinSecret secret, bool assumeP2SH)
		{
			Sign(secret.Key, assumeP2SH);
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
			return GetFormatter(format, network).Parse(tx);
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
	}

}
