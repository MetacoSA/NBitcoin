using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public abstract class Base58Data
	{
		protected byte[] vchData = new byte[0];
		protected byte[] vchVersion = new byte[0];
		protected string wifData = "";

		protected virtual void SetString(string psz)
		{
			byte[] vchTemp = Encoders.Base58Check.DecodeData(psz);
			vchVersion = vchTemp.Take((int)ExpectedVersion.Length).ToArray();
			if(!Utils.ArrayEqual(vchVersion, ExpectedVersion))
				throw new FormatException("The version prefix does not match the expected one " + String.Join(",", ExpectedVersion));

			vchData = vchTemp.Skip((int)ExpectedVersion.Length).ToArray();
			wifData = psz;

			if(!IsValid)
				throw new FormatException("Invalid " + this.GetType().Name);
		}


		protected void SetData(byte[] vchData)
		{
			this.vchData = vchData;
			this.vchVersion = ExpectedVersion;
			wifData = Encoders.Base58Check.EncodeData(vchVersion.Concat(vchData).ToArray());

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

		public abstract byte[] ExpectedVersion
		{
			get;
		}



		public string ToWif()
		{
			return wifData;
		}
		public byte[] ToBytes()
		{
			return vchData.ToArray();
		}
		public override string ToString()
		{
			return wifData;
		}
	}
}
