using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public abstract class BitcoinExtKeyBase : Base58Data
	{
		public BitcoinExtKeyBase(IBitcoinSerializable key)
		{
			SetData(key.ToBytes());
		}
		public BitcoinExtKeyBase(string wif)
		{
			SetString(wif);
		}

	}

	public class BitcoinExtKey : BitcoinExtKeyBase
	{
		public BitcoinExtKey(string wif)
			: base(wif)
		{

		}
		public BitcoinExtKey(ExtKey key)
			: base(key)
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

		public override byte[] ExpectedVersion
		{
			get
			{
				return new byte[] { (0x04), (0x88), (0xAD), (0xE4) };
			}
		}
	}
	public class BitcoinExtPubKey : BitcoinExtKeyBase
	{
		public BitcoinExtPubKey(ExtPubKey key)
			: base(key)
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

		public override byte[] ExpectedVersion
		{
			get
			{
				return new byte[] { (0x04), (0x88), (0xB2), (0x1E) };
			}
		}
	}
}
