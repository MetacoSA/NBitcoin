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
		private readonly Network _Network;
		public Network Network
		{
			get
			{
				return _Network;
			}
		}

		public Base58Data(string base64, Network network)
		{
			_Network = network;
			SetString(base64);
		}
		public Base58Data(byte[] rawBytes, Network network)
		{
			_Network = network;
			SetData(rawBytes);
		}

		private void SetString(string psz)
		{
			byte[] vchTemp = Encoders.Base58Check.DecodeData(psz);
			var expectedVersion = _Network.GetVersionBytes(Type);


			vchVersion = vchTemp.Take((int)expectedVersion.Length).ToArray();
			if(!Utils.ArrayEqual(vchVersion, expectedVersion))
				throw new FormatException("The version prefix does not match the expected one " + String.Join(",", expectedVersion));

			vchData = vchTemp.Skip((int)expectedVersion.Length).ToArray();
			wifData = psz;

			if(!IsValid)
				throw new FormatException("Invalid " + this.GetType().Name);
		}


		private void SetData(byte[] vchData)
		{
			this.vchData = vchData;
			this.vchVersion = _Network.GetVersionBytes(Type);
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

		public abstract Base58Type Type
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
