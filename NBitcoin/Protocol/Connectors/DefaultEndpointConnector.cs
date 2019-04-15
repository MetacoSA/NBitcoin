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
		[Obsolete("Ignored, don't use, will be removed")]
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public bool AllowOnlyTorEndpoints { get; set; } = false;


		public DefaultEndpointConnector()
		{
		}

		[Obsolete("Ignored, don't use, will be removed")]
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public DefaultEndpointConnector(bool allowOnlyTorEndpoints)
		{
		}

		public IEnpointConnector Clone()
		{
			return new DefaultEndpointConnector();
		}

		public async Task ConnectSocket(Socket socket, EndPoint endpoint, NodeConnectionParameters nodeConnectionParameters, CancellationToken cancellationToken)
		{
			var socksSettings = nodeConnectionParameters.TemplateBehaviors.Find<SocksSettingsBehavior>();
			var socketEndpoint = endpoint;
			var useSocks = endpoint.IsTor() || socksSettings?.OnlyForOnionHosts is false;
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