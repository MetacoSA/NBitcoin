#if !NOSOCKET
using NBitcoin.Protocol.Behaviors;
using NBitcoin.Socks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Connectors
{
	public class DefaultEndpointConnector : IEnpointConnector
	{
		/// <summary>
		/// Connect to only hidden service nodes over Tor.
		/// Prevents connecting to clearnet nodes over Tor.
		/// </summary>
		public bool AllowOnlyTorEndpoints { get; set; } = false;

		public DefaultEndpointConnector()
		{
		}

		public DefaultEndpointConnector(bool allowOnlyTorEndpoints)
		{
			AllowOnlyTorEndpoints = allowOnlyTorEndpoints;
		}

		public IEnpointConnector Clone()
		{
			return new DefaultEndpointConnector(AllowOnlyTorEndpoints);
		}

		public async Task ConnectSocket(Socket socket, EndPoint endpoint, NodeConnectionParameters nodeConnectionParameters, CancellationToken cancellationToken)
		{
			var isTor = endpoint.IsTor();
			if (AllowOnlyTorEndpoints && !isTor)
				throw new InvalidOperationException($"The Endpoint connector is configured to allow only Tor endpoints and the '{endpoint}' enpoint is not one");

			var socksSettings = nodeConnectionParameters.TemplateBehaviors.Find<SocksSettingsBehavior>();
			var socketEndpoint = endpoint;
			var useSocks = isTor || socksSettings?.OnlyForOnionHosts is false;
			if (useSocks)
			{
				if (socksSettings?.SocksEndpoint == null)
					throw new InvalidOperationException("SocksSettingsBehavior.SocksEndpoint is not set but the connection is expecting using socks proxy");
				socketEndpoint = socksSettings.SocksEndpoint;
			}
			if (socketEndpoint is IPEndPoint mappedv4 && mappedv4.Address.IsIPv4MappedToIPv6Ex())
				socketEndpoint = new IPEndPoint(mappedv4.Address.MapToIPv4Ex(), mappedv4.Port);
			await socket.ConnectAsync(socketEndpoint, cancellationToken).ConfigureAwait(false);

			if (!useSocks)
				return;

			await SocksHelper.Handshake(socket, endpoint, socksSettings.GetCredentials(), cancellationToken).ConfigureAwait(false);
		}
	}
}
#endif