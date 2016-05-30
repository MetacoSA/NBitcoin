namespace NBitcoin
{
	public abstract class BitcoinExtKeyBase : Base58Data, IDestination
	{
		protected BitcoinExtKeyBase(IBitcoinSerializable key, Network network)
			: base(key.ToBytes(), network)
		{
		}

		protected BitcoinExtKeyBase(string base58, Network network)
			: base(base58, network)
		{
		}


		#region IDestination Members

		public abstract Script ScriptPubKey
		{
			get;
		}

		#endregion
	}

	/// <summary>
	/// Base58 representation of an ExtKey
	/// </summary>
	public class BitcoinExtKey : BitcoinExtKeyBase, ISecret
	{
		public BitcoinExtKey(string base58, Network expectedNetwork = null)
			: base(base58, expectedNetwork)
		{

		}
		public BitcoinExtKey(ExtKey key, Network network)
			: base(key, network)
		{

		}

		protected override bool IsValid
		{
			get
			{
				return vchData.Length == 74;
			}
		}

		ExtKey _Key;
		public ExtKey ExtKey
		{
			get
			{
				if(_Key == null)
				{
					_Key = new ExtKey();
					_Key.ReadWrite(vchData);
				}
				return _Key;
			}
		}


		public override Base58Type Type
		{
			get
			{
				return Base58Type.EXT_SECRET_KEY;
			}
		}

		public override Script ScriptPubKey
		{
			get
			{
				return ExtKey.ScriptPubKey;
			}
		}

		public BitcoinExtPubKey Neuter()
		{
			return ExtKey.Neuter().GetWif(Network);
		}

		#region ISecret Members

		public Key PrivateKey
		{
			get
			{
				return ExtKey.PrivateKey;
			}
		}

		#endregion

		public static implicit operator ExtKey(BitcoinExtKey key)
		{
			if(key == null)
				return null;
			return key.ExtKey;
		}
	}

	/// <summary>
	/// Base58 representation of an ExtPubKey
	/// </summary>
	public class BitcoinExtPubKey : BitcoinExtKeyBase
	{
		public BitcoinExtPubKey(ExtPubKey key, Network network)
			: base(key, network)
		{

		}

		public BitcoinExtPubKey(string base58, Network expectedNetwork = null)
			: base(base58, expectedNetwork)
		{
		}

		ExtPubKey _PubKey;
		public ExtPubKey ExtPubKey
		{
			get
			{
				if(_PubKey == null)
				{
					_PubKey = new ExtPubKey();
					_PubKey.ReadWrite(vchData);
				}
				return _PubKey;
			}
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.EXT_PUBLIC_KEY;
			}
		}

		public override Script ScriptPubKey
		{
			get
			{
				return ExtPubKey.ScriptPubKey;
			}
		}

		public static implicit operator ExtPubKey(BitcoinExtPubKey key)
		{
			if(key == null)
				return null;
			return key.ExtPubKey;
		}
	}
}
