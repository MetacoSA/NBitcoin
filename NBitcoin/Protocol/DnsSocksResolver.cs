#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin.Protocol.Connectors;
using NBitcoin.Socks;

namespace NBitcoin.Protocol
{
	public class DnsSocksResolver : IDnsResolver
	{
		public DnsSocksResolver(EndPoint socksEndpoint)
		{
			if (socksEndpoint == null)
				throw new ArgumentNullException(nameof(socksEndpoint));
			SocksEndpoint = socksEndpoint;
		}

		public EndPoint SocksEndpoint { get; }

		public async Task<IPAddress[]> GetHostAddressesAsync(string hostNameOrAddress, CancellationToken cancellationToken)
		{
			// https://gitweb.torproject.org/torspec.git/tree/socks-extensions.txt#n44
			if (string.IsNullOrEmpty(hostNameOrAddress))
				throw new ArgumentNullException(nameof(hostNameOrAddress));


			var ascii = Encoding.ASCII.GetBytes(hostNameOrAddress);
			if (ascii.Length > 255)
				throw new ArgumentException("hostNameOrAddress should be less than 256 chars", nameof(hostNameOrAddress));
			
			using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			await socket.ConnectAsync(SocksEndpoint, cancellationToken).ConfigureAwait(false);
			await SocksHelper.Handshake(socket, GetCredentials(), cancellationToken).ConfigureAwait(false);

			var req = new byte[7 + ascii.Length];
			int o = 0;
			req[o++] = 0x05;
			req[o++] = 0xf0;
			req[o++] = 0x00;
			req[o++] = 0x03;
			req[o++] = (byte)ascii.Length;
			ascii.CopyTo(req, o);
			o += ascii.Length;
			req[o++] = 0x00;
			req[o++] = 0x00;

			NetworkStream stream = new NetworkStream(socket, false);
			await stream.WriteCancellableAsync(req, 0, req.Length, cancellationToken).ConfigureAwait(false);

			
			await stream.ReadCancellableAsync(req, 0, 4, cancellationToken).ConfigureAwait(false);

			var ipLen = req[3] == 1 ? 4 : req[3] == 4 ? 16 :
				throw new SocketException(11001);

			var ip = new byte[ipLen];
			await stream.ReadCancellableAsync(ip, 0, ipLen, cancellationToken).ConfigureAwait(false);
			var address = new IPAddress(ip);
			await stream.ReadCancellableAsync(req, 0, 2, cancellationToken).ConfigureAwait(false);
			return new[] { address };
		}

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

		private NetworkCredential GenerateCredentials()
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			var identity = new string(Enumerable.Repeat(chars, 21)
			.Select(s => s[(int)(RandomUtils.GetUInt32() % s.Length)]).ToArray());
			return new NetworkCredential(identity, identity);
		}
	}
}
#endif
