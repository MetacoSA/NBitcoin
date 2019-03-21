#if !NOSOCKET
using System;
using System.Collections.Generic;
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
		/// <summary>
		/// If the socks endpoint to connect to
		/// </summary>
		public EndPoint SocksEndpoint { get; set; }
		/// <summary>
		/// If the socks proxy is only used for TOR traffic (default: true)
		/// </summary>
		public bool OnlyForOnionHosts { get; set; } = true;
		public override object Clone()
		{
			return new SocksSettingsBehavior(SocksEndpoint, OnlyForOnionHosts);
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