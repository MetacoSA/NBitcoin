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
#if !NO_SOCKETASYNC
		public static Task ConnectAsync(this Socket socket, EndPoint remoteEP, CancellationToken cancellationToken)
		{
			return socket.ConnectAsync(remoteEP, cancellationToken).AsTask();
		}
#elif !NO_BEGINCONNECT
		public static Task ConnectAsync(this Socket socket, EndPoint remoteEP, CancellationToken cancellationToken)
		{
			var tcs = new TaskCompletionSource<bool>(socket);
			socket.BeginConnect(remoteEP, iar =>
			{
				var innerTcs = (TaskCompletionSource<bool>)iar.AsyncState;
				try
				{
					((Socket)innerTcs.Task.AsyncState).EndConnect(iar);
					innerTcs.TrySetResult(true);
				}
				catch (Exception e) { innerTcs.TrySetException(e); }
			}, tcs);
			return tcs.Task.WithCancellation(cancellationToken);
		}
#else
		public static async Task ConnectAsync(this Socket socket, EndPoint remoteEP, CancellationToken cancellationToken)
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
				args.RemoteEndPoint = remoteEP;
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
#endif
	}
}
#endif
