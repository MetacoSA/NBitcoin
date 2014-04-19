using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
	[Payload("version")]
	public class VersionPayload : IBitcoinSerializable
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
		NetworkAddress addr_recv;
		NetworkAddress addr_from;
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
		VarString user_agent;
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

		public void ReadWrite(BitcoinStream stream)
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
