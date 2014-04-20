using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
	[Payload("version")]
	public class VersionPayload : Payload, IBitcoinSerializable
	{
		uint version;

		public uint Version
		{
			get
			{
				return version;
			}
			set
			{
				version = value;
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
		public IPEndPoint AddressReciever
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
		VarString user_agent;
		public string UserAgent
		{
			get
			{
				return Utils.BytesToString(user_agent.GetString());
			}
			set
			{
				user_agent = new VarString(Utils.StringToBytes(value));
			}
		}

		#region IBitcoinSerializable Members

		public override void ReadWrite(BitcoinStream stream)
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

	
	}
}
