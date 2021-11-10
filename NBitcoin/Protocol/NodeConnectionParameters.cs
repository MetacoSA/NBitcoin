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
using NBitcoin.Protocol.Connectors;

namespace NBitcoin.Protocol
{
	public class NodeConnectionParameters
	{

		public NodeConnectionParameters()
		{
			TemplateBehaviors.Add(new PingPongBehavior());
			Version = null;
			IsRelay = true;
			Services = NodeServices.Nothing;
			ConnectCancellation = default(CancellationToken);
			// Use max supported by MAC OSX Yosemite/Mavericks/Sierra (https://fasterdata.es.net/host-tuning/osx/)
			this.SocketSettings.ReceiveBufferSize = 1048576;
			this.SocketSettings.SendBufferSize = 1048576;
			////////////////////////
			UserAgent = VersionPayload.GetNBitcoinUserAgent();
			PreferredTransactionOptions = TransactionOptions.All;
		}

		public NodeConnectionParameters(NodeConnectionParameters other)
		{
			Version = other.Version;
			IsRelay = other.IsRelay;
			Services = other.Services;
			ConnectCancellation = other.ConnectCancellation;
			UserAgent = other.UserAgent;
			AddressFrom = other.AddressFrom;
			Nonce = other.Nonce;
			Advertize = other.Advertize;
			PreferredTransactionOptions = other.PreferredTransactionOptions;
			EndpointConnector = other.EndpointConnector.Clone();
			SocketSettings = other.SocketSettings.Clone();
			foreach (var behavior in other.TemplateBehaviors)
			{
				TemplateBehaviors.Add(behavior.Clone());
			}
		}

		/// <summary>
		/// Send addr unsolicited message of the AddressFrom peer when passing to Handshaked state
		/// </summary>
		public bool Advertize
		{
			get;
			set;
		}

		public uint? Version
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

		public SocketSettings SocketSettings { get; set; } = new SocketSettings();

		public IEnpointConnector EndpointConnector { get; set; } = new DefaultEndpointConnector();

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

		public EndPoint AddressFrom
		{
			get;
			set;
		}

		public ulong? Nonce
		{
			get;
			set;
		}

		public VersionPayload CreateVersion(EndPoint peer, Network network)
		{
			VersionPayload version = new VersionPayload()
			{
				Nonce = Nonce == null ? RandomUtils.GetUInt64() : Nonce.Value,
				UserAgent = UserAgent,
				Version = Version == null ? network.MaxP2PVersion : Version.Value,
				Timestamp = DateTimeOffset.UtcNow,
				AddressReceiver = peer,
				AddressFrom = AddressFrom ?? new IPEndPoint(IPAddress.Parse("0.0.0.0").MapToIPv6(), network.DefaultPort),
				Relay = IsRelay,
				Services = Services
			};
			return version;
		}
	}
}
#endif
