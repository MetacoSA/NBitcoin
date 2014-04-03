using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitcoin.Private.Bitcoin
{
	public class BitcoinAddress : Base58Data
	{
		public BitcoinAddress(string address)
		{
			this.SetString(address);
		}

		public bool IsValid
		{
			get
			{
				bool fCorrectSize = vchData.Length == 20;
				bool fKnownVersion = vchVersion[0] == 0 || vchVersion[0] == 5;
				return fCorrectSize && fKnownVersion;
			}
		}



		public KeyId Get()
		{
			if(vchVersion[0] == 0)
				return new KeyId(new uint160(vchData));
			return null;
		}
	}
}
