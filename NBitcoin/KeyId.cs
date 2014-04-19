using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class TxDestination
	{
		string _Dest;
		public TxDestination()
		{
			_Dest = "00";
		}

		public TxDestination(byte[] value)
		{
			_Dest = Encoders.Hex.EncodeData(value);
		}
		public TxDestination(uint160 value)
			: this(value.ToBytes())
		{
		}

		public TxDestination(string value)
		{
			//Ensure is hex
			Encoders.Hex.DecodeData(value);
			_Dest = value;
		}

		public byte[] ToBytes()
		{
			return Encoders.Hex.DecodeData(_Dest);
		}

		public override bool Equals(object obj)
		{
			TxDestination item = obj as TxDestination;
			if(item == null)
				return false;
			return _Dest.Equals(item._Dest);
		}
		public static bool operator ==(TxDestination a, TxDestination b)
		{
			if(System.Object.ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return a._Dest == b._Dest;
		}

		public static bool operator !=(TxDestination a, TxDestination b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return _Dest.GetHashCode();
		}

		public override string ToString()
		{
			return _Dest;
		}
	}
	public class KeyId : TxDestination
	{
		public KeyId()
			: base(0)
		{

		}

		public KeyId(byte[] value)
			: base(value)
		{

		}
		public KeyId(uint160 value)
			: base(value)
		{

		}

		public KeyId(string value)
			: base(value)
		{
		}

	}
	public class ScriptId : TxDestination
	{
		public ScriptId()
			: base(0)
		{

		}

		public ScriptId(byte[] value)
			: base(value)
		{

		}
		public ScriptId(uint160 value)
			: base(value)
		{

		}

		public ScriptId(string value)
			: base(value)
		{
		}

	}
}
