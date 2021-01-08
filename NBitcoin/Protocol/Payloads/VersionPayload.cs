#if !NOSOCKET
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

#if WINDOWS_UWP
using Windows.ApplicationModel;
#endif

namespace NBitcoin.Protocol
{
	[Flags]
	public enum NodeServices : ulong
	{
		Nothing = 0,
		/// <summary>
		/// NODE_NETWORK means that the node is capable of serving the block chain. It is currently
		/// set by all Bitcoin Core nodes, and is unset by SPV clients or other peers that just want
		/// network services but don't provide them.
		/// </summary>
		Network = (1 << 0),

		/// <summary>
		///  NODE_GETUTXO means the node is capable of responding to the getutxo protocol request.
		/// Bitcoin Core does not support this but a patch set called Bitcoin XT does.
		/// See BIP 64 for details on how this is implemented.
		/// </summary>
		GetUTXO = (1 << 1),

		/// <summary> NODE_BLOOM means the node is capable and willing to handle bloom-filtered connections.
		/// Bitcoin Core nodes used to support this by default, without advertising this bit,
		/// but no longer do as of protocol version 70011 (= NO_BLOOM_VERSION)
		/// </summary>
		NODE_BLOOM = (1 << 2),

		/// <summary> Indicates that a node can be asked for blocks and transactions including
		/// witness data. 
		/// </summary> 
		NODE_WITNESS = (1 << 3),

		// NODE_COMPACT_FILTERS means the node will serve basic block filter requests.
		// See BIP157 and BIP158 for details on how this is implemented.

		NODE_COMPACT_FILTERS = (1 << 6),

		/// <summary> NODE_NETWORK_LIMITED means the same as NODE_NETWORK with the limitation of only
		/// serving the last 288 (2 day) blocks
		/// See BIP159 for details on how this is implemented.
		/// </summary> 
		NODE_NETWORK_LIMITED = (1 << 10)
	}
	[Payload("version")]
	public class VersionPayload : Payload, IBitcoinSerializable
	{
		static string _NUserAgent;
		public static string GetNBitcoinUserAgent()
		{
			if (_NUserAgent == null)
			{
#if WINDOWS_UWP
				// get the app version
				Package package = Package.Current;
				var version = package.Id.Version;
				_NUserAgent = "/NBitcoin:" + version.Major + "." + version.Minor + "." + version.Build + "/";
#else
#if !NETSTANDARD1X
				var version = typeof(VersionPayload).Assembly.GetName().Version;
#else
				var version = typeof(VersionPayload).GetTypeInfo().Assembly.GetName().Version;
#endif
				_NUserAgent = "/NBitcoin:" + version.Major + "." + version.MajorRevision + "." + version.Build + "/";
#endif

			}
			return _NUserAgent;
		}
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

		public NodeServices Services
		{
			get
			{
				return (NodeServices)services;
			}
			set
			{
				services = (ulong)value;
			}
		}

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
		public EndPoint AddressReceiver
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
		public EndPoint AddressFrom
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
			using (stream.ProtocolVersionScope(version))
			{
				stream.ReadWrite(ref services);
				stream.ReadWrite(ref timestamp);
				using (stream.SerializationTypeScope(SerializationType.Hash)) //No time field in version message
				{
					stream.ReadWrite(ref addr_recv);
				}
				if (version >= 106)
				{
					using (stream.SerializationTypeScope(SerializationType.Hash)) //No time field in version message
					{
						stream.ReadWrite(ref addr_from);
					}
					stream.ReadWrite(ref nonce);
					stream.ReadWrite(ref user_agent);
					if (!stream.ProtocolCapabilities.SupportUserAgent)
						if (user_agent.Length != 0)
							throw new FormatException("Should not find user agent for current version " + version);
					stream.ReadWrite(ref start_height);
					if (version >= 70001)
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