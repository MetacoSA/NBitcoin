#if !NOSOCKET
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	[Payload("version")]
	public class VersionPayload : Payload, IBitcoinSerializable
	{
		static string _NUserAgent;
		public static string GetNBitcoinUserAgent()
		{
			if(_NUserAgent == null)
			{
				var version = typeof(VersionPayload).Assembly.GetName().Version;
				_NUserAgent = "/NBitcoin:" + version.Major + "." + version.MajorRevision + "." + version.Minor + "/";
			}
			return _NUserAgent;
		}
		uint version;

		public ProtocolVersion Version
		{
			get
			{
				if(version == 10300) //A version number of 10300 is converted to 300 before being processed
					return (ProtocolVersion)(300);  //https://en.bitcoin.it/wiki/Version_Handshake
				return (ProtocolVersion)version;
			}
			set
			{
				if(value == (ProtocolVersion)10300)
					value = (ProtocolVersion)300;
				version = (uint)value;
			}
		}
		ulong services;
		long timestamp;

		public DateTimeOffset Timestamp
		{
			get
			{
				return Utils.UnixTimeToDateTime((uint)timestamp);
			}
			set
			{
				timestamp = Utils.DateTimeToUnixTime(value);
			}
		}

		NetworkAddress addr_recv = new NetworkAddress();
		public IPEndPoint AddressReceiver
		{
			get
			{
				return addr_recv.Endpoint;
			}
			set
			{
				addr_recv.Endpoint = value;
			}
		}
		NetworkAddress addr_from = new NetworkAddress();
		public IPEndPoint AddressFrom
		{
			get
			{
				return addr_from.Endpoint;
			}
			set
			{
				addr_from.Endpoint = value;
			}
		}

		ulong nonce;
		public ulong Nonce
		{
			get
			{
				return nonce;
			}
			set
			{
				nonce = value;
			}
		}
		int start_height;

		public int StartHeight
		{
			get
			{
				return start_height;
			}
			set
			{
				start_height = value;
			}
		}

		bool relay;
		public bool Relay
		{
			get
			{
				return relay;
			}
			set
			{
				relay = value;
			}
		}

		VarString user_agent;
		public string UserAgent
		{
			get
			{
				return Encoders.ASCII.EncodeData(user_agent.GetString());
			}
			set
			{
				user_agent = new VarString(Encoders.ASCII.DecodeData(value));
			}
		}

		#region IBitcoinSerializable Members

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref version);
			using(stream.ProtocolVersionScope((ProtocolVersion)version))
			{
				stream.ReadWrite(ref services);
				stream.ReadWrite(ref timestamp);
				using(stream.ProtocolVersionScope(ProtocolVersion.CADDR_TIME_VERSION - 1)) //No time field in version message
				{
					stream.ReadWrite(ref addr_recv);
				}
				if(version >= 106)
				{
					using(stream.ProtocolVersionScope(ProtocolVersion.CADDR_TIME_VERSION - 1)) //No time field in version message
					{
						stream.ReadWrite(ref addr_from);
					}
					stream.ReadWrite(ref nonce);
					stream.ReadWrite(ref user_agent);
					if(version < 60002)
						if(user_agent.Length != 0)
							throw new FormatException("Should not find user agent for current version " + version);
					stream.ReadWrite(ref start_height);
					if(version >= 70001)
						stream.ReadWrite(ref relay);
				}
			}
		}

		#endregion


		public override string ToString()
		{
			return Version.ToString();
		}
	}
}
#endif