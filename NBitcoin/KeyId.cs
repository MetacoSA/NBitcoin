#nullable enable
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System;

namespace NBitcoin
{
	public class KeyId : IAddressableDestination
	{
		public KeyId()
			: this(0)
		{

		}
		readonly uint160 v;
		public KeyId(byte[] value)
		{
			if (value.Length != 20)
				throw new ArgumentException("value should be 20 bytes", "value");
			v = new uint160(value);
		}
		public KeyId(uint160 value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			this.v = value;
		}

		public KeyId(string value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			var bytes = Encoders.Hex.DecodeData(value);
			v = new uint160(bytes);
		}

		public Script ScriptPubKey
		{
			get
			{
				return PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(this);
			}
		}

		public BitcoinPubKeyAddress GetAddress(Network network)
		{
			return network.NetworkStringParser.CreateP2PKH(this, network);
		}
		BitcoinAddress IAddressableDestination.GetAddress(Network network)
		{
			return GetAddress(network);
		}

		public bool IsSupported(Network network)
		{
			return true;
		}


		public override bool Equals(object? obj)
		{
			if (obj is KeyId id)
				return this.v == id.v;
			return false;
		}
		public static bool operator ==(KeyId? a, KeyId? b)
		{
			if (a is KeyId && b is KeyId)
				return a.Equals(b);
			return a is null && b is null;
		}

		public static bool operator !=(KeyId? a, KeyId? b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return v.GetHashCode();
		}

		public byte[] ToBytes()
		{
			return v.ToBytes();
		}

		public override string ToString()
		{
			return Encoders.Hex.EncodeData(v.ToBytes());
		}
	}
	public class WitKeyId : IAddressableDestination
	{
		public WitKeyId()
			: this(0)
		{

		}
		readonly uint160 v;
		public WitKeyId(byte[] value)
		{
			if (value.Length != 20)
				throw new ArgumentException("value should be 20 bytes", "value");
			v = new uint160(value);
		}
		public WitKeyId(uint160 value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			this.v = value;
		}

		public WitKeyId(string value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			var bytes = Encoders.Hex.DecodeData(value);
			v = new uint160(bytes);
		}


		public Script ScriptPubKey
		{
			get
			{
				return PayToWitTemplate.Instance.GenerateScriptPubKey(OpcodeType.OP_0, v.ToBytes());
			}
		}

		public KeyId AsKeyId()
		{
			return new KeyId(v);
		}

		public BitcoinWitPubKeyAddress GetAddress(Network network)
		{
			return network.NetworkStringParser.CreateP2WPKH(this, network);
		}
		BitcoinAddress IAddressableDestination.GetAddress(Network network)
		{
			return GetAddress(network);
		}

		public bool IsSupported(Network network)
		{
			return network.Consensus.SupportSegwit;
		}


		public override bool Equals(object? obj)
		{
			if (obj is WitKeyId id)
				return this.v == id.v;
			return false;
		}
		public static bool operator ==(WitKeyId? a, WitKeyId? b)
		{
			if (a is WitKeyId && b is WitKeyId)
				return a.Equals(b);
			return a is null && b is null;
		}

		public static bool operator !=(WitKeyId? a, WitKeyId? b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return v.GetHashCode();
		}

		public byte[] ToBytes()
		{
			return v.ToBytes();
		}

		public override string ToString()
		{
			return Encoders.Hex.EncodeData(v.ToBytes());
		}
	}

	public class WitScriptId : IAddressableDestination
	{
		public WitScriptId()
			: this(0)
		{

		}
		readonly uint256 v;
		public WitScriptId(byte[] value)
		{
			if (value.Length != 32)
				throw new ArgumentException("value should be 20 bytes", "value");
			v = new uint256(value);
		}
		public WitScriptId(uint256 value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			this.v = value;
		}

		public WitScriptId(string value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			var bytes = Encoders.Hex.DecodeData(value);
			v = new uint256(bytes);
		}

		public WitScriptId(Script script)
			: this(Hashes.SHA256(script._Script))
		{
		}

		/// <summary>
		/// When we store internal ScriptId -> Script lookup, having another
		/// WitScriptId -> WitScript KVMap will complicate implementation. And require
		/// More space because WitScriptId is bigger than ScriptId. But if we use Hash160 as ID,
		/// It will cause a problem in case of p2sh-p2wsh because we must hold two scripts
		/// (witness program and witness script) with one ScriptId. So instead we use single-RIPEMD160
		/// This is the same way with how bitcoin core handles scripts internally.
		/// </summary>
		public ScriptId? _HashForLookUp;
		public ScriptId HashForLookUp 
		{
			get{
				return _HashForLookUp ?? (_HashForLookUp = new ScriptId(new uint160(Hashes.RIPEMD160(this.ToBytes()))));
			}
		}


		public Script ScriptPubKey
		{
			get
			{
				return PayToWitTemplate.Instance.GenerateScriptPubKey(OpcodeType.OP_0, ToBytes());
			}
		}

		public BitcoinWitScriptAddress GetAddress(Network network)
		{
			return network.NetworkStringParser.CreateP2WSH(this, network);
		}
		BitcoinAddress IAddressableDestination.GetAddress(Network network)
		{
			return GetAddress(network);
		}
		public bool IsSupported(Network network)
		{
			return network.Consensus.SupportSegwit;
		}
		public override bool Equals(object? obj)
		{
			if (obj is WitScriptId id)
				return this.v == id.v;
			return false;
		}
		public static bool operator ==(WitScriptId? a, WitScriptId? b)
		{
			if (a is WitScriptId && b is WitScriptId)
				return a.Equals(b);
			return a is null && b is null;
		}

		public static bool operator !=(WitScriptId? a, WitScriptId? b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return v.GetHashCode();
		}

		public byte[] ToBytes()
		{
			return v.ToBytes();
		}

		public override string ToString()
		{
			return Encoders.Hex.EncodeData(v.ToBytes());
		}
	}

	public class ScriptId : IAddressableDestination
	{
		public ScriptId()
			: this(0)
		{

		}
		readonly uint160 v;
		public ScriptId(byte[] value)
		{
			if (value.Length != 20)
				throw new ArgumentException("value should be 20 bytes", "value");
			v = new uint160(value);
		}
		public ScriptId(uint160 value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			this.v = value;
		}

		public ScriptId(string value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			var bytes = Encoders.Hex.DecodeData(value);
			v = new uint160(bytes);
		}

		public ScriptId(Script script)
			: this(Hashes.Hash160(script._Script))
		{
		}

		public Script ScriptPubKey
		{
			get
			{
				return PayToScriptHashTemplate.Instance.GenerateScriptPubKey(this);
			}
		}

		public BitcoinScriptAddress GetAddress(Network network)
		{
			return network.NetworkStringParser.CreateP2SH(this, network);
		}
		BitcoinAddress IAddressableDestination.GetAddress(Network network)
		{
			return GetAddress(network);
		}
		public bool IsSupported(Network network)
		{
			return true;
		}

		public override bool Equals(object? obj)
		{
			if (obj is ScriptId id)
				return this.v == id.v;
			return false;
		}
		public static bool operator ==(ScriptId? a, ScriptId? b)
		{
			if (a is ScriptId && b is ScriptId)
				return a.Equals(b);
			return a is null && b is null;
		}

		public static bool operator !=(ScriptId? a, ScriptId? b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return v.GetHashCode();
		}

		public byte[] ToBytes()
		{
			return v.ToBytes();
		}

		public override string ToString()
		{
			return Encoders.Hex.EncodeData(v.ToBytes());
		}
	}
}
