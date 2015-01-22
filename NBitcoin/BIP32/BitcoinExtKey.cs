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

	public class BitcoinExtKey : BitcoinExtKeyBase
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

		ExtKey _key;
		public ExtKey ExtKey
		{
			get
			{
				if(_key == null)
				{
					_key = new ExtKey();
					_key.ReadWrite(vchData);
				}
				return _key;
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
	}
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

		ExtPubKey _pubKey;
		public ExtPubKey ExtPubKey
		{
			get
			{
				if(_pubKey == null)
				{
					_pubKey = new ExtPubKey();
					_pubKey.ReadWrite(vchData);
				}
				return _pubKey;
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
	}
}
