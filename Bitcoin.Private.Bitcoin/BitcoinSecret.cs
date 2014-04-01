using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitcoin.Private.Bitcoin
{
	public class BitcoinSecret : Base58Data
	{

		public override bool SetString(string str, uint nVersionBytes = 1)
		{
			return base.SetString(str,nVersionBytes) && IsValid;
		}

		public Key GetKey()
		{
			Key ret = new Key();
			ret.Set(vchData, 32, vchData.Length > 32 && vchData[32] == 1);
			return ret;
		}

		public bool IsValid
		{
			get
			{
				bool fExpectedFormat = vchData.Length == 32 || (vchData.Length == 33 && vchData[32] == 1);
				//https://en.bitcoin.it/wiki/Base58Check_encoding
				bool fCorrectVersion = vchVersion[0] == 128;
				return fExpectedFormat && fCorrectVersion;
			}
		}
	}
}
