using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
	public class NetworkAddress : IBitcoinSerializable
	{
		uint time;
		ulong service;
		byte[] ip = new byte[16];
		ushort port;

		IPEndPoint _Endpoint;
		public IPEndPoint Endpoint
		{
			get
			{
				return new IPEndPoint(new IPAddress(ip), port);
			}
		}


		public DateTimeOffset Time
		{
			get
			{
				return Utils.UnixTimeToDateTime(time);
			}
			set
			{
				time = Utils.DateTimeToUnixTime(value);
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if(stream.ProtocolVersion >= ProtocolVersion.CADDR_TIME_VERSION)
				stream.ReadWrite(ref time);
			stream.ReadWrite(ref service);
			stream.ReadWrite(ref ip);
			stream.ReadWrite(ref port);
		}

		#endregion

	}
}
