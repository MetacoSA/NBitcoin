using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{

	public interface IBase58Data : IBitcoinString
	{
		Base58Type Type
		{
			get;
		}
	}

	/// <summary>
	/// Base class for all Base58 check representation of data
	/// </summary>
	public abstract class Base58Data : IBase58Data
	{
		protected byte[] vchData = new byte[0];
		protected byte[] vchVersion = new byte[0];
		protected string wifData = "";
		private Network _Network;
		public Network Network
		{
			get
			{
				return _Network;
			}
		}

		protected void Init<T>(string base64, Network expectedNetwork = null) where T : Base58Data
		{
			_Network = expectedNetwork;
			SetString<T>(base64);
		}

		protected Base58Data(byte[] rawBytes, Network network)
		{
			if(network == null)
				throw new ArgumentNullException(nameof(network));
			_Network = network;
			SetData(rawBytes);
		}
		public Base58Data()
		{

		}
		private void SetString<T>(string psz) where T : Base58Data
		{
			if(_Network == null)
			{
				_Network = Network.GetNetworkFromBase58Data(psz, Type);
				if(_Network == null)
					throw new FormatException("Invalid " + this.GetType().Name);
			}

			byte[] vchTemp = Encoders.Base58Check.DecodeData(psz);
			var expectedVersion = _Network.GetVersionBytes(Type, true);


			vchVersion = vchTemp.SafeSubarray(0, expectedVersion.Length);
			if(!Utils.ArrayEqual(vchVersion, expectedVersion))
			{
				if(_Network.NetworkStringParser.TryParse(psz, Network, out T other))
				{
					this.vchVersion = other.vchVersion;
					this.vchData = other.vchData;
					this.wifData = other.wifData;
				}
				else
				{
					throw new FormatException("The version prefix does not match the expected one " + String.Join(",", expectedVersion));
				}
			}
			else
			{
				vchData = vchTemp.SafeSubarray(expectedVersion.Length);
				wifData = psz;
			}

			if(!IsValid)
				throw new FormatException("Invalid " + this.GetType().Name);

		}


		private void SetData(byte[] vchData)
		{
			this.vchData = vchData;
			this.vchVersion = _Network.GetVersionBytes(Type, true);
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

		public override bool Equals(object obj)
		{
			Base58Data item = obj as Base58Data;
			if(item == null)
				return false;
			return ToString().Equals(item.ToString());
		}
		public static bool operator ==(Base58Data a, Base58Data b)
		{
			if(System.Object.ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return a.ToString() == b.ToString();
		}

		public static bool operator !=(Base58Data a, Base58Data b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}
	}
}
