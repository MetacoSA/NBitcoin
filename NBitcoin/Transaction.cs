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

		private uint256 hash;
		private uint n;

		public uint256 Hash
		{
			get
			{
				return hash;
			}
		}
		public uint N
		{
			get
			{
				return n;
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
		bool IsNull
		{
			get
			{
				return (hash == 0 && n == uint.MaxValue);
			}
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
			return (a.hash == b.hash && a.n == b.n);
		}

		public static bool operator !=(OutPoint a, OutPoint b)
		{
			return !(a == b);
		}
		public override bool Equals(object obj)
		{
			OutPoint item = obj as OutPoint;
			if(object.ReferenceEquals(null,item))
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

		OutPoint prevout;
		Script scriptSig;
		uint nSequence;

		public uint Sequence
		{
			get
			{
				return nSequence;
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
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref prevout);
			stream.ReadWrite(ref scriptSig);
			stream.ReadWrite(ref nSequence);
		}

		#endregion
	}

	public class TxOut : IBitcoinSerializable
	{
		Script publicKey;
		public Script PublicKey
		{
			get
			{
				return this.publicKey;
			}
		}

		private ulong value;
		Money _MoneyValue;
		public Money Value
		{
			get
			{
				if(_MoneyValue == null)
					_MoneyValue = new Money(value);
				return _MoneyValue;
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
	}

	//https://en.bitcoin.it/wiki/Transactions
	//https://en.bitcoin.it/wiki/Protocol_specification
	public class Transaction : IBitcoinSerializable
	{
		uint nVersion = 0;
		TxIn[] vin = new TxIn[0];
		TxOut[] vout = new TxOut[0];
		uint nLockTime = 0;

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

		public uint LockTime
		{
			get
			{
				return nLockTime;
			}
		}

		public TxIn[] VIn
		{
			get
			{
				return vin;
			}
		}
		public TxOut[] VOut
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
	}
}
