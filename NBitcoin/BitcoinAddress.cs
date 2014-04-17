using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class BitcoinScriptAddress : BitcoinAddress
	{
		public BitcoinScriptAddress(string address) : base(address)
		{
		}
		protected override bool IsValid
		{
			get
			{
				return true;
			}
		}
		public override byte[] ExpectedVersion
		{
			get
			{
				return new byte[] { 5 };
			}
		}
	}
	public class BitcoinAddress : Base58Data
	{
		public BitcoinAddress CreateFrom(string address)
		{
			if(address[0] == '1')
				return new BitcoinAddress(address);
			if(address[0] == '3')
				return new BitcoinScriptAddress(address);
			return new BitcoinAddress(address);
		}
		public BitcoinAddress(string address)
		{
			this.SetString(address);
		}

		protected override bool IsValid
		{
			get
			{
				return vchData.Length == 20;
			}
		}

		public KeyId ID
		{
			get
			{
				if(vchVersion[0] == 0)
					return new KeyId(vchData);
				return null;
			}
		}

		public bool VerifyMessage(string message, string signature)
		{
			var key = PubKey.RecoverFromMessage(message, signature);
			return key.ID == ID;
		}

		public override byte[] ExpectedVersion
		{
			get
			{
				return new byte[] { 0 };
			}
		}
	}
}
