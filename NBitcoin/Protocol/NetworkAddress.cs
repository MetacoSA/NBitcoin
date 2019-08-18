#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class NetworkAddress : IBitcoinSerializable
	{
		public NetworkAddress()
		{

		}
		public NetworkAddress(IPEndPoint endpoint)
		{
			Endpoint = endpoint;
		}
		public NetworkAddress(IPAddress address, int port)
		{
			Endpoint = new IPEndPoint(address, port);
		}

		internal uint ntime;
		ulong service = 1;
		byte[] ip = new byte[16];
		ushort port;

		public ulong Service
		{
			get
			{
				return service;
			}
			set
			{
				service = value;
			}
		}

		public TimeSpan Ago
		{
			get
			{
				return DateTimeOffset.UtcNow - Time;
			}
			set
			{
				Time = DateTimeOffset.UtcNow - value;
			}
		}

		public void Adjust()
		{
			var nNow = Utils.DateTimeToUnixTime(DateTimeOffset.UtcNow);
			if (ntime <= 100000000 || ntime > nNow + 10 * 60)
				ntime = nNow - 5 * 24 * 60 * 60;
		}

		public IPEndPoint Endpoint
		{
			get
			{
				return new IPEndPoint(new IPAddress(ip), port);
			}
			set
			{
				port = (ushort)value.Port;
				var ipBytes = value.Address.GetAddressBytes();
				if (ipBytes.Length == 16)
				{
					ip = ipBytes;
				}
				else if (ipBytes.Length == 4)
				{
					//Convert to ipv4 mapped to ipv6
					//In these addresses, the first 80 bits are zero, the next 16 bits are one, and the remaining 32 bits are the IPv4 address
					ip = new byte[16];
					Array.Copy(ipBytes, 0, ip, 12, 4);
					Array.Copy(new byte[] { 0xFF, 0xFF }, 0, ip, 10, 2);
				}
				else
					throw new NotSupportedException("Invalid IP address type");
			}
		}

		public DateTimeOffset Time
		{
			get
			{
				return Utils.UnixTimeToDateTime(ntime);
			}
			set
			{
				ntime = Utils.DateTimeToUnixTime(value);
			}
		}
		uint version = 100100;
		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if (stream.Type == SerializationType.Disk)
			{
				stream.ReadWrite(ref version);
			}
			if (
				stream.Type == SerializationType.Disk ||
				(stream.ProtocolCapabilities.SupportTimeAddress && stream.Type != SerializationType.Hash))
				stream.ReadWrite(ref ntime);
			stream.ReadWrite(ref service);
			stream.ReadWrite(ref ip);
			using (stream.BigEndianScope())
			{
				stream.ReadWrite(ref port);
			}
		}

		#endregion

		public void ZeroTime()
		{
			this.ntime = 0;
		}

		internal byte[] GetKey()
		{
			var vKey = new byte[18];
			Array.Copy(ip, vKey, 16);
			vKey[16] = (byte)(port / 0x100);
			vKey[17] = (byte)(port & 0x0FF);
			return vKey;
		}
	}
}
#endif