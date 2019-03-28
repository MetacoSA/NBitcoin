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
	static class SocketExtensions
	{
#if NO_SOCKET_EXTENSIONS
		public static async Task ConnectAsync(this Socket socket, EndPoint socketEndpoint, CancellationToken cancellationToken)
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
				args.RemoteEndPoint = socketEndpoint;
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
					await completion.Task.ConfigureAwait(false);
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
#else
		public static Task ConnectAsync(this Socket socket, EndPoint socketEndpoint, CancellationToken cancellationToken)
		{
			return System.Net.Sockets.SocketTaskExtensions.ConnectAsync(socket, socketEndpoint).WithCancellation(cancellationToken);
		}
#endif
	}
}
#endif