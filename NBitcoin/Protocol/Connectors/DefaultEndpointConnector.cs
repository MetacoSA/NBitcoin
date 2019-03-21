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
		public SocketType SocketType { get; set; } = SocketType.Stream;
		public ProtocolType ProtocolType { get; set; } = ProtocolType.Tcp;

		public DefaultEndpointConnector()
		{

		}

		public async Task ConnectSocket(Socket socket, EndPoint endpoint, NodeConnectionParameters nodeConnectionParameters, CancellationToken cancellationToken)
		{
			var socksSettings = nodeConnectionParameters.TemplateBehaviors.Find<SocksSettingsBehavior>();
			bool socks = endpoint.IsTor() || socksSettings?.OnlyForOnionHosts is false;
			if (socks && socksSettings?.SocksEndpoint == null)
				throw new InvalidOperationException("SocksSettingsBehavior.SocksEndpoint is not set but the connection is expecting using socks proxy");
			var socketEndpoint = socks ? socksSettings.SocksEndpoint : endpoint;
			if (socketEndpoint is IPEndPoint mappedv4 && mappedv4.Address.IsIPv4MappedToIPv6Ex())
				socketEndpoint = new IPEndPoint(mappedv4.Address.MapToIPv4Ex(), mappedv4.Port);
#if NETCORE
			await socket.ConnectAsync(socketEndpoint).WithCancellation(cancellationToken).ConfigureAwait(false);
#else
			await socket.ConnectAsync(socketEndpoint, cancellationToken).ConfigureAwait(false);
#endif
			if (!socks)
				return;

			await SocksHelper.Handshake(socket, endpoint, cancellationToken);
		}
	}
}
#endif