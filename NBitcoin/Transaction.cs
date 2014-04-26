using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
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
				return (hash == 0 && n == unchecked((uint)-1));
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
		Script scriptSig;
		uint nSequence = uint.MaxValue;

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
			var template = new PayToPubkeyHashScriptTemplate();
			var result = template.ExtractScriptSigParameters(ScriptSig);
			return result != null && result.PublicKey == pubKey;
		}
	}

	public class TxOut : IBitcoinSerializable
	{
		Script publicKey = new Script();
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
			SetDestination(bitcoinAddress);
		}

		public TxOut(Money value, KeyId keyId)
		{
			Value = value;
			SetDestination(keyId);
		}

		public TxOut(Money value, Script scriptPubKey)
		{
			Value = value;
			ScriptPubKey = scriptPubKey;
		}

		private void SetDestination(KeyId keyId)
		{
			ScriptPubKey = new PayToPubkeyHashScriptTemplate().GenerateOutputScript(keyId);
		}

		public void SetDestination(BitcoinAddress address)
		{
			ScriptPubKey = new PayToPubkeyHashScriptTemplate().GenerateOutputScript(address);
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

		public bool IsTo(BitcoinAddress address)
		{
			if(address == null)
				throw new ArgumentNullException("address");
			return IsTo(address.ID);
		}

		private bool IsTo(KeyId keyId)
		{
			return ScriptPubKey.GetDestination() == keyId;
		}

		public bool IsTo(PubKey pubkey)
		{
			return IsTo(pubkey.ID);
		}
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
		List<TxIn> vin = new List<TxIn>();
		List<TxOut> vout = new List<TxOut>();
		uint nLockTime = 0;

		public Transaction()
		{
			nVersion = CURRENT_VERSION;
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
				return VOut.Sum(v => v.Value);
			}
		}

		public uint LockTime
		{
			get
			{
				return nLockTime;
			}
		}

		public List<TxIn> VIn
		{
			get
			{
				return vin;
			}
		}
		public List<TxOut> VOut
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
			stream.ReadWrite(ref vin);
			stream.ReadWrite(ref vout);
			stream.ReadWrite(ref nLockTime);
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
				return (VIn.Count == 1 && VIn[0].PrevOut.IsNull);
			}
		}

		public const long nMinTxFee = 10000;  // Override with -mintxfee
		public const long nMinRelayTxFee = 1000;

		public static uint CURRENT_VERSION = 2;
		public static uint MAX_STANDARD_TX_SIZE = 100000;

		public void AddOutput(Money money, BitcoinAddress address)
		{
			AddOutput(new TxOut(money, address));
		}
		public void AddOutput(Money money, KeyId keyId)
		{
			AddOutput(new TxOut(money, keyId));
		}
		public void AddOutput(TxOut @out)
		{
			this.vout.Add(@out);
		}
		public TxIn AddInput(TxIn @in)
		{
			this.vin.Add(@in);
			return @in;
		}

		public TxIn AddInput(Transaction prevTx, int outIndex)
		{
			if(outIndex >= prevTx.VOut.Count)
				throw new InvalidOperationException("Output " + outIndex + " is not present in the prevTx");
			var @in = new TxIn();
			@in.PrevOut.Hash = prevTx.GetHash();
			@in.PrevOut.N = (uint)outIndex;
			AddInput(@in);
			return @in;
		}


	}
}
