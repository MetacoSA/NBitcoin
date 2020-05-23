using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

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
#if HAS_SPAN
		protected byte[] vchData = Array.Empty<byte>();
		protected ReadOnlyMemory<byte> vchVersion;
#else
		protected byte[] vchData = new byte[0];
		protected byte[] vchVersion = new byte[0];
#endif
		protected string wifData = "";
		private Network _Network;
		public Network Network
		{
			get
			{
				return _Network;
			}
		}

		protected Base58Data(string wif, Network network) : this(wif, DecodeData(wif, network), network)
		{

		}

		private static byte[] DecodeData(string wif, Network network)
		{
			if (wif is null)
				throw new ArgumentNullException(nameof(wif));
			if (network is null)
				throw new ArgumentNullException(nameof(network));
			return network.NetworkStringParser.GetBase58CheckEncoder().DecodeData(wif);
		}

		protected Base58Data(byte[] rawBytes, Network network) : this(EncodeData(rawBytes, network), rawBytes, network)
		{

		}

		private static string EncodeData(byte[] rawBytes, Network network)
		{
			if (rawBytes is null)
				throw new ArgumentNullException(nameof(rawBytes));
			if (network is null)
				throw new ArgumentNullException(nameof(network));
			return network.NetworkStringParser.GetBase58CheckEncoder().EncodeData(rawBytes);
		}

		protected Base58Data(string wif, byte[] rawBytes, Network network)
		{
			if (wif == null)
				throw new ArgumentNullException(nameof(wif));
			if (rawBytes is null)
				throw new ArgumentNullException(nameof(rawBytes));
			if (network is null)
				throw new ArgumentNullException(nameof(network));
			_Network = network;
			byte[] vchTemp = rawBytes;
#if HAS_SPAN
			if (!(_Network.GetVersionMemory(Type, false) is ReadOnlyMemory<byte> expectedVersion))
				throw new FormatException("Invalid " + this.GetType().Name);
#else
			var expectedVersion = _Network.GetVersionBytes(Type, false);
			if (expectedVersion is null)
				throw new FormatException("Invalid " + this.GetType().Name);
#endif

#if HAS_SPAN
			var vchTempMemory = vchTemp.AsMemory();
			vchVersion = vchTempMemory.Slice(0, expectedVersion.Length);
#else
			vchVersion = vchTemp.SafeSubarray(0, expectedVersion.Length);
#endif
#if HAS_SPAN
			if (!vchVersion.Span.SequenceEqual(expectedVersion.Span))
#else
			if (!Utils.ArrayEqual(vchVersion, expectedVersion))
#endif
			{
				if (_Network.NetworkStringParser.TryParse(wif, Network, this.GetType(), out var o) && o is Base58Data other)
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
#if HAS_SPAN
				vchData = vchTempMemory.Slice(expectedVersion.Length).ToArray();
#else
				vchData = vchTemp.SafeSubarray(expectedVersion.Length);
#endif
				wifData = wif;
			}

			if (!IsValid)
				throw new FormatException("Invalid " + this.GetType().Name);
		}

		private void SetData(byte[] vchData)
		{
			this.vchData = vchData;
#if HAS_SPAN
			if (!(_Network.GetVersionMemory(Type, false) is ReadOnlyMemory<byte> v))
				throw new FormatException("Invalid " + this.GetType().Name);
			this.vchVersion = v;
			Span<byte> buffer = vchVersion.Length + vchData.Length is int length &&
								length > 256 ? new byte[length] : stackalloc byte[length];
			this.vchVersion.Span.CopyTo(buffer);
			this.vchData.CopyTo(buffer.Slice(this.vchVersion.Length));
			wifData = _Network.NetworkStringParser.GetBase58CheckEncoder().EncodeData(buffer);
#else
			this.vchVersion = _Network.GetVersionBytes(Type, false);
			wifData = _Network.NetworkStringParser.GetBase58CheckEncoder().EncodeData(vchVersion.Concat(vchData).ToArray());
#endif

			if (!IsValid)
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
			if (item == null)
				return false;
			return ToString().Equals(item.ToString());
		}
		public static bool operator ==(Base58Data a, Base58Data b)
		{
			if (System.Object.ReferenceEquals(a, b))
				return true;
			if (((object)a == null) || ((object)b == null))
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
