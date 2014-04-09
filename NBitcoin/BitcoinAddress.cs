using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class BitcoinAddress : Base58Data
	{
		public BitcoinAddress(string address)
		{
			this.SetString(address);
		}

		protected override bool IsValid
		{
			get
			{
				bool fCorrectSize = vchData.Length == 20;
				bool fKnownVersion = vchVersion[0] == 0 || vchVersion[0] == 5;
				return fCorrectSize && fKnownVersion;
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
	}
}
