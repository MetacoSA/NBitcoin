#if !NOSOCKET
using NBitcoin.Protocol.Behaviors;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class NodeConnectionParameters
	{

		public NodeConnectionParameters()
		{
			ReuseBuffer = true;
			TemplateBehaviors.Add(new PingPongBehavior());
			Version = ProtocolVersion.PROTOCOL_VERSION;
			IsRelay = true;
			Services = NodeServices.Nothing;
			ConnectCancellation = default(CancellationToken);

			// Use max supported by MAC OSX Yosemite/Mavericks/Sierra (https://fasterdata.es.net/host-tuning/osx/)
			ReceiveBufferSize = 1048576; 
			SendBufferSize = 1048576;
			////////////////////////

			UserAgent = VersionPayload.GetNBitcoinUserAgent();
			PreferredTransactionOptions = TransactionOptions.All;
		}

		public NodeConnectionParameters(NodeConnectionParameters other)
		{
			Version = other.Version;
			IsRelay = other.IsRelay;
			Services = other.Services;
			ReceiveBufferSize = other.ReceiveBufferSize;
			SendBufferSize = other.SendBufferSize;
			ConnectCancellation = other.ConnectCancellation;
			UserAgent = other.UserAgent;
			AddressFrom = other.AddressFrom;
			Nonce = other.Nonce;
			Advertize = other.Advertize;
			ReuseBuffer = other.ReuseBuffer;
			PreferredTransactionOptions = other.PreferredTransactionOptions;
			foreach(var behavior in other.TemplateBehaviors)
			{
				TemplateBehaviors.Add(behavior.Clone());
			}
		}

		/// <summary>
		/// Send addr unsollicited message of the AddressFrom peer when passing to Handshaked state
		/// </summary>
		public bool Advertize
		{
			get;
			set;
		}

		public ProtocolVersion Version
		{
			get;
			set;
		}

		/// <summary>
		/// If true, the node will receive all incoming transactions if no bloomfilter are set
		/// </summary>
		public bool IsRelay
		{
			get;
			set;
		}

		public NodeServices Services
		{
			get;
			set;
		}

		public TransactionOptions PreferredTransactionOptions
		{
			get;
			set;
		}

		public string UserAgent
		{
			get;
			set;
		}
		public int ReceiveBufferSize
		{
			get;
			set;
		}
		public int SendBufferSize
		{
			get;
			set;
		}

		/// <summary>
		/// Whether we reuse a 1MB buffer for deserializing messages, for limiting GC activity (Default : true)
		/// </summary>
		public bool ReuseBuffer
		{
			get;
			set;
		}
		public CancellationToken ConnectCancellation
		{
			get;
			set;
		}

		private readonly NodeBehaviorsCollection _TemplateBehaviors = new NodeBehaviorsCollection(null);
		public NodeBehaviorsCollection TemplateBehaviors
		{
			get
			{
				return _TemplateBehaviors;
			}
		}

		public NodeConnectionParameters Clone()
		{
			return new NodeConnectionParameters(this);
		}

		public IPEndPoint AddressFrom
		{
			get;
			set;
		}

		public ulong? Nonce
		{
			get;
			set;
		}

		public VersionPayload CreateVersion(IPEndPoint peer, Network network)
		{
			VersionPayload version = new VersionPayload()
			{
				Nonce = Nonce == null ? RandomUtils.GetUInt64() : Nonce.Value,
				UserAgent = UserAgent,
				Version = Version,
				Timestamp = DateTimeOffset.UtcNow,
				AddressReceiver = peer,
				AddressFrom = AddressFrom ?? new IPEndPoint(IPAddress.Parse("0.0.0.0").MapToIPv6Ex(), network.DefaultPort),
				Relay = IsRelay,
				Services = Services
			};
			return version;
		}
	}
}
#endif