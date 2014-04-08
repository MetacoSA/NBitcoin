using Bitcoin.Private.Bitcoin.DataEncoders;
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
		protected string wifData = "";

		protected virtual void SetString(string psz, uint nVersionBytes = 1)
		{
			byte[] vchTemp = Encoders.Base58Check.DecodeData(psz);
			vchVersion = vchTemp.Take((int)nVersionBytes).ToArray();
			vchData = vchTemp.Skip((int)nVersionBytes).ToArray();
			wifData = psz;
			Clean(vchTemp);
			if(!IsValid)
				throw new FormatException("Invalid " + this.GetType().Name);
		}

		protected virtual bool IsValid
		{
			get
			{
				return true;
			}
		}

		private void Clean(byte[] arr)
		{
			Array.Clear(arr, 0, arr.Length);
		}

		public string ToWif()
		{
			return wifData;
		}

		public override string ToString()
		{
			return wifData;
		}
	}
}
