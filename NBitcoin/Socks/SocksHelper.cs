#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Socks
{
	public class SocksHelper
	{
		// https://gitweb.torproject.org/torspec.git/tree/socks-extensions.txt
		// The "NO AUTHENTICATION REQUIRED" (SOCKS5) authentication method[00] is
		// supported; and as of Tor 0.2.3.2-alpha, the "USERNAME/PASSWORD" (SOCKS5)
		// authentication method[02] is supported too, and used as a method to
		// implement stream isolation.As an extension to support some broken clients,
		// we allow clients to pass "USERNAME/PASSWORD" authentication to us even if
		// no authentication was selected.
		static readonly byte[] SelectionMessageNoAuthenticationRequired = new byte[] { 5, 1, 0 };
		static readonly byte[] SelectionMessageUsernamePassword = new byte[] { 5, 1, 2 };

		internal static byte[] CreateConnectMessage(EndPoint endpoint)
		{
			if (endpoint == null)
				throw new ArgumentNullException(nameof(endpoint));
			byte[] sendBuffer = null;
			ushort port = 0;

			if (endpoint.TryConvertToOnionDNSEndpoint(out var onionEndpoint))
				endpoint = onionEndpoint;
			else if (endpoint is IPEndPoint ip6mapped && ip6mapped.Address.IsIPv4MappedToIPv6)
				endpoint = new IPEndPoint(ip6mapped.Address.MapToIPv4(), ip6mapped.Port);

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
		static Encoding NoBOMUTF8 = new UTF8Encoding(false);
		public static Task Handshake(Socket socket, EndPoint endpoint, CancellationToken cancellationToken)
		{
			return Handshake(socket, endpoint, null, cancellationToken);
		}

		public static async Task Handshake(Socket socket, NetworkCredential credentials, CancellationToken cancellationToken)
		{
			NetworkStream stream = new NetworkStream(socket, false);
			var selectionMessage = credentials is null ? SelectionMessageNoAuthenticationRequired : SelectionMessageUsernamePassword;

			await stream.WriteCancellableAsync(selectionMessage, 0, selectionMessage.Length, cancellationToken).ConfigureAwait(false);
			await stream.FlushCancellableAsync(cancellationToken).ConfigureAwait(false);

			var selectionResponse = new byte[2];

			await stream.ReadCancellableAsync(selectionResponse, 0, 2, cancellationToken).ConfigureAwait(false);

			if (selectionResponse[0] != 5)
				throw new SocksException("Invalid version in selection reply");
			if (selectionResponse[1] == 2)
			{
				if (credentials == null)
					throw new SocksException("Authentication to socks proxy required, but the credentials are not specified");

				// https://tools.ietf.org/html/rfc1929#section-2
				// Once the SOCKS V5 server has started, and the client has selected the
				// Username / Password Authentication protocol, the Username / Password
				// subnegotiation begins.  This begins with the client producing a
				// Username / Password request:

				var uName = NoBOMUTF8.GetBytes(credentials.UserName);
				var passwd = NoBOMUTF8.GetBytes(credentials.Password);

				int index = 0;
				var usernamePasswordRequest = new byte[1 + 1 + uName.Length + 1 + passwd.Length];
				usernamePasswordRequest[index++] = 1;
				if (uName.Length > 255)
					throw new ArgumentException("The username should be less than 255 bytes", nameof(uName));
				usernamePasswordRequest[index++] = (byte)uName.Length;
				Array.Copy(uName, 0, usernamePasswordRequest, index, uName.Length);
				index += uName.Length;
				if (passwd.Length > 255)
					throw new ArgumentException("The password should be less than 255 bytes", nameof(passwd));
				usernamePasswordRequest[index++] = (byte)passwd.Length;
				Array.Copy(passwd, 0, usernamePasswordRequest, index, passwd.Length);

				await stream.WriteCancellableAsync(usernamePasswordRequest, 0, usernamePasswordRequest.Length, cancellationToken).ConfigureAwait(false);
				await stream.FlushCancellableAsync(cancellationToken).ConfigureAwait(false);

				var userNamePasswordResponse = new byte[2];

				await stream.ReadCancellableAsync(userNamePasswordResponse, 0, 2, cancellationToken).ConfigureAwait(false);

				if (userNamePasswordResponse[0] != 1)
				{
					throw new SocksException($"Authentication version {userNamePasswordResponse[0]} is not supported. Only version {1} is supported.");
				}

				if (userNamePasswordResponse[1] != 0) // Tor authentication is different, this will never happen;
				{
					// https://tools.ietf.org/html/rfc1929#section-2
					// A STATUS field of X'00' indicates success. If the server returns a
					// `failure' (STATUS value other than X'00') status, it MUST close the
					// connection.
					throw new SocksException("Wrong username and/or password.");
				}
			}
			else if (selectionResponse[1] != 0)
				throw new SocksException("Unsupported authentication method in selection reply");
		}
		public static async Task Handshake(Socket socket, EndPoint endpoint, NetworkCredential credentials, CancellationToken cancellationToken)
		{
			await Handshake(socket, credentials, cancellationToken).ConfigureAwait(false);
			NetworkStream stream = new NetworkStream(socket, false);
			var connectBytes = CreateConnectMessage(endpoint);
			await stream.WriteCancellableAsync(connectBytes, 0, connectBytes.Length, cancellationToken).ConfigureAwait(false);
			await stream.FlushCancellableAsync(cancellationToken).ConfigureAwait(false);

			var connectResponse = new byte[10];
			await stream.ReadCancellableAsync(connectResponse, 0, 10, cancellationToken).ConfigureAwait(false);
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
