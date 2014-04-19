using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public abstract class BitcoinExtKeyBase : Base58Data
	{
		public BitcoinExtKeyBase(IBitcoinSerializable key, Network network)
			: base(key.ToBytes(), network)
		{
		}
		public BitcoinExtKeyBase(string base58, Network network)
			: base(base58, network)
		{
		}

	}

	public class BitcoinExtKey : BitcoinExtKeyBase
	{
		public BitcoinExtKey(string base58, Network network)
			: base(base58, network)
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
		public ExtKey Key
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
	}
	public class BitcoinExtPubKey : BitcoinExtKeyBase
	{
		public BitcoinExtPubKey(ExtPubKey key, Network network)
			: base(key, network)
		{

		}

		ExtPubKey _PubKey;
		public ExtPubKey PubKey
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
	}
}
