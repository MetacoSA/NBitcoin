#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Connectors
{
	public class DefaultEndpointConnector : IEnpointConnector
	{
		public AddressFamily AddressFamily { get; set; } = AddressFamily.InterNetworkV6;
		public SocketType SocketType { get; set; } = SocketType.Stream;
		public ProtocolType ProtocolType { get; set; } = ProtocolType.Tcp;
		Action<Socket> _configure = NotIPv6Only;
		public static void NotIPv6Only(Socket socket)
		{
			socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
		}

		public DefaultEndpointConnector ConfigureSocket(Action<Socket> configure)
		{
			_configure = configure;
			return this;
		}
		public DefaultEndpointConnector()
		{

		}

		public Socket CreateSocket(EndPoint endpoint)
		{
			var socket = new Socket(AddressFamily, SocketType, ProtocolType);
			if (_configure != null)
				_configure(socket);
			return socket;
		}

		public async Task ConnectSocket(Socket socket, EndPoint endpoint, CancellationToken cancellationToken)
		{
#if NO_RCA
			TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
#else
			TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
			var args = new SocketAsyncEventArgs();
			using (cancellationToken.Register(() =>
			{
				completion.TrySetCanceled();
			}, false))
			{
				args.RemoteEndPoint = endpoint;
				args.Completed += (s, a) =>
				{
					completion.TrySetResult(true);
				};
				if (!socket.ConnectAsync(args))
				{
					completion.TrySetResult(true);
				}
				try
				{
					await completion.Task;
				}
				catch
				{
					cancellationToken.ThrowIfCancellationRequested();
					throw;
				}
			}
				if (args.SocketError != SocketError.Success)
					throw new SocketException((int)args.SocketError);
		}
	}
}
#endif