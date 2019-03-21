#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Socks
{
	public class SocksHelper
	{
		static readonly byte[] SelectionMessage = new byte[] { 5, 1, 0 };
		internal static byte[] CreateConnectMessage(EndPoint endpoint)
		{
			byte[] sendBuffer = null;
			ushort port = 0;

			if (endpoint is IPEndPoint onionIP && onionIP.Address.IsTor())
				endpoint = onionIP.AsOnionDNSEndpoint();
			else if (endpoint is IPEndPoint ip6mapped && ip6mapped.Address.IsIPv4MappedToIPv6Ex())
				endpoint = new IPEndPoint(ip6mapped.Address.MapToIPv4Ex(), ip6mapped.Port);

			if (endpoint is DnsEndPoint dns)
			{
				if (dns.Host.Length > 255)
					throw new InvalidOperationException("Hostname is too long (more than 255 chars)");
				port = (ushort)dns.Port;
				sendBuffer = new byte[4 + 1 + dns.Host.Length + 2];
				sendBuffer[0] = 5;
				sendBuffer[1] = 1;
				sendBuffer[2] = 0;
				sendBuffer[3] = 3;
				sendBuffer[4] = (byte)dns.Host.Length;
				Encoding.ASCII.GetBytes(dns.Host, 0, dns.Host.Length, sendBuffer, 5);
			}
			else if (endpoint is IPEndPoint ip && ip.AddressFamily == AddressFamily.InterNetwork)
			{
				port = (ushort)ip.Port;
				sendBuffer = new byte[4 + 4 + 2];
				sendBuffer[0] = 5;
				sendBuffer[1] = 1;
				sendBuffer[2] = 0;
				sendBuffer[3] = 1;
				var ipv4 = ip.Address.GetAddressBytes();
				Array.Copy(ipv4, 0, sendBuffer, 4, 4);
			}
			else if (endpoint is IPEndPoint ip2 && ip2.AddressFamily == AddressFamily.InterNetworkV6)
			{
				port = (ushort)ip2.Port;
				sendBuffer = new byte[4 + 16 + 2];
				sendBuffer[0] = 5;
				sendBuffer[1] = 1;
				sendBuffer[2] = 0;
				sendBuffer[3] = 4;
				var ipv6 = ip2.Address.GetAddressBytes();
				Array.Copy(ipv6, 0, sendBuffer, 4, 16);
			}
			else
				throw new NotSupportedException("Endpoint type not supported");
			sendBuffer[sendBuffer.Length - 2] = (byte)((port & 0xff00) >> 8);
			sendBuffer[sendBuffer.Length - 1] = (byte)((port & 0x00ff));
			return sendBuffer;
		}

		public static async Task Handshake(Socket socket, EndPoint endpoint, CancellationToken cancellationToken)
		{
			NetworkStream stream = new NetworkStream(socket, false);
			await stream.WriteAsync(SelectionMessage, 0, SelectionMessage.Length).WithCancellation(cancellationToken).ConfigureAwait(false);
			await stream.FlushAsync().WithCancellation(cancellationToken).ConfigureAwait(false);

			var selectionResponse = new byte[2];
			await stream.ReadAsync(selectionResponse, 0, 2).WithCancellation(cancellationToken);
			if (selectionResponse[0] != 5)
				throw new SocksException("Invalid version in selection reply");
			if (selectionResponse[1] != 0)
				throw new SocksException("Unsupported authentication method in selection reply");

			var connectBytes = CreateConnectMessage(endpoint);
			await stream.WriteAsync(connectBytes, 0, connectBytes.Length).WithCancellation(cancellationToken).ConfigureAwait(false);
			await stream.FlushAsync().WithCancellation(cancellationToken).ConfigureAwait(false);

			var connectResponse = new byte[10];
			await stream.ReadAsync(connectResponse, 0, 10).WithCancellation(cancellationToken);
			if (connectResponse[0] != 5)
				throw new SocksException("Invalid version in connect reply");
			if (connectResponse[1] != 0)
			{
				var code = (SocksErrorCode)connectResponse[1];
				throw new SocksException(code);
			}
			if (connectResponse[2] != 0)
				throw new SocksException("Invalid RSV in connect reply");
			if (connectResponse[3] != 1)
				throw new SocksException("Invalid ATYP in connect reply");
			for (int i = 4; i < 4 + 4; i++)
			{
				if (connectResponse[i] != 0)
					throw new SocksException("Invalid BIND address in connect reply");
			}
			if (connectResponse[8] != 0 || connectResponse[9] != 0)
				throw new SocksException("Invalid PORT address connect reply");
		}
	}
}
#endif