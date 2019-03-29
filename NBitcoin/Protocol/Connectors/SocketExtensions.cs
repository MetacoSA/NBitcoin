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
	}
}
#endif