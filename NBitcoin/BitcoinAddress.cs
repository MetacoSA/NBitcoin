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
		public BitcoinScriptAddress(string address, Network network)
			: base(address, network)
		{
		}
		protected override bool IsValid
		{
			get
			{
				return true;
			}
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.SCRIPT_ADDRESS;
			}
		}
	}
	public class BitcoinAddress : Base58Data
	{
		public BitcoinAddress(string base58, Network network)
			: base(base58, network)
		{
		}

		public BitcoinAddress(byte[] rawBytes, Network network)
			: base(rawBytes, network)
		{
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

		public override Base58Type Type
		{
			get
			{
				return Base58Type.PUBKEY_ADDRESS;
			}
		}
	}
}
