using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitcoin.Private.Bitcoin
{
	public class Base58Data
	{
		protected byte[] vchData = new byte[0];
		protected byte[] vchVersion = new byte[0];
		public virtual bool SetString(string psz, uint nVersionBytes = 1)
		{
			byte[] vchTemp;
			Utils.DecodeBase58Check(psz, out vchTemp);
			if(vchTemp.Length < nVersionBytes)
			{
				Clean(vchData);
				Clean(vchVersion);
				return false;
			}

			vchVersion = vchTemp.Take((int)nVersionBytes).ToArray();
			vchData = vchTemp.Skip((int)nVersionBytes).ToArray();
			Clean(vchTemp);
			return true;
		}

		private void Clean(byte[] arr)
		{
			Array.Clear(arr, 0, arr.Length);
		}
	}
}
