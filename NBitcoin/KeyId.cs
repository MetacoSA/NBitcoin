using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class TxDestination : IDestination
	{
		byte[] _DestBytes;

		public TxDestination()
		{
			_DestBytes = new byte[] { 0 };
		}

		public TxDestination(byte[] value)
		{
			if(value == null)
				throw new ArgumentNullException("value");
			_DestBytes = value;
		}
		public TxDestination(uint160 value)
			: this(value.ToBytes())
		{
		}

		public TxDestination(string value)
		{
			_DestBytes = Encoders.Hex.DecodeData(value);
			_Str = value;
		}

		public BitcoinAddress GetAddress(Network network)
		{
			return BitcoinAddress.Create(this, network);
		}

		[Obsolete("Use ScriptPubKey instead")]
		public Script CreateScriptPubKey()
		{
			return ScriptPubKey;
		}

		#region IDestination Members

		public virtual Script ScriptPubKey
		{
			get
			{
				return null;
			}
		}

		#endregion


		public byte[] ToBytes()
		{
			return ToBytes(false);
		}
		public byte[] ToBytes(bool @unsafe)
		{
			if(@unsafe)
				return _DestBytes;
			var array = new byte[_DestBytes.Length];
			Array.Copy(_DestBytes, array, _DestBytes.Length);
			return array;
		}

		public override bool Equals(object obj)
		{
			TxDestination item = obj as TxDestination;
			if(item == null)
				return false;
			return Utils.ArrayEqual(_DestBytes, item._DestBytes);
		}
		public static bool operator ==(TxDestination a, TxDestination b)
		{
			if(System.Object.ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return Utils.ArrayEqual(a._DestBytes, b._DestBytes);
		}

		public static bool operator !=(TxDestination a, TxDestination b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return Utils.GetHashCode(_DestBytes);
		}

		string _Str;
		public override string ToString()
		{
			if(_Str == null)
				_Str = Encoders.Hex.EncodeData(_DestBytes);
			return _Str;
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

		public override Script ScriptPubKey
		{
			get
			{
				return PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(this);
			}
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

		public ScriptId(Script script)
			: this(Hashes.Hash160(script._Script))
		{
		}

		public override Script ScriptPubKey
		{
			get
			{
				return PayToScriptHashTemplate.Instance.GenerateScriptPubKey(this);
			}
		}
	}
}
