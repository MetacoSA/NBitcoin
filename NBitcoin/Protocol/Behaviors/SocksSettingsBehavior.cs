#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace NBitcoin.Protocol.Behaviors
{
	public class SocksSettingsBehavior : NodeBehavior
	{
		public SocksSettingsBehavior()
		{

		}
		public SocksSettingsBehavior(EndPoint socksEndpoint)
		{
			SocksEndpoint = socksEndpoint;
		}
		public SocksSettingsBehavior(EndPoint socksEndpoint, bool onlyForOnionHosts)
		{
			SocksEndpoint = socksEndpoint;
			OnlyForOnionHosts = onlyForOnionHosts;
		}
		public SocksSettingsBehavior(EndPoint socksEndpoint, bool onlyForOnionHosts, NetworkCredential networkCredential, bool streamIsolation)
		{
			SocksEndpoint = socksEndpoint;
			OnlyForOnionHosts = onlyForOnionHosts;
			StreamIsolation = streamIsolation;
			NetworkCredential = networkCredential;
		}
		/// <summary>
		/// If the socks endpoint to connect to
		/// </summary>
		public EndPoint SocksEndpoint { get; set; }
		/// <summary>
		/// If the socks proxy is only used for Tor traffic (default: true)
		/// </summary>
		public bool OnlyForOnionHosts { get; set; } = true;


		/// <summary>
		/// Credentials to connect to the SOCKS proxy (Use StreamIsolation instead if you want Tor isolation)
		/// </summary>
		public NetworkCredential NetworkCredential { get; set; }

		/// <summary>
		/// Randomize the NetworkCredentials to the Socks proxy
		/// </summary>
		public bool StreamIsolation { get; set; }

		internal NetworkCredential GetCredentials()
		{
			return NetworkCredential ??
				(StreamIsolation ? GenerateCredentials() : null);
		}

		public DnsSocksResolver CreateDnsResolver()
		{
			if (SocksEndpoint is null)
				throw new InvalidOperationException("SocksEndpoint is not set");
			return new DnsSocksResolver(SocksEndpoint)
			{
				NetworkCredential = NetworkCredential,
				StreamIsolation = StreamIsolation
			};
		}

		private NetworkCredential GenerateCredentials()
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			var identity = new string(Enumerable.Repeat(chars, 21)
			.Select(s => s[(int)(RandomUtils.GetUInt32() % s.Length)]).ToArray());
			return new NetworkCredential(identity, identity);
		}

		public override object Clone()
		{
			return new SocksSettingsBehavior(SocksEndpoint, OnlyForOnionHosts, NetworkCredential, StreamIsolation);
		}

		protected override void AttachCore()
		{

		}

		protected override void DetachCore()
		{

		}
	}
}
#endif
