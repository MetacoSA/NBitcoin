#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;

namespace NBitcoin
{
	static class StreamExtensions
	{
		public static async Task ReadCancellableAsync(this NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
#if !NO_SOCKETASYNC
			await stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
#else
			// Note that we use WithCancellation because underlying socket operations does not support true cancellation.
			// This mainly just abandon the operation.
			await stream.ReadAsync(buffer, offset, count).WithCancellation(cancellationToken).ConfigureAwait(false);
#endif
		}	

		public static async Task WriteCancellableAsync(this NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
#if !NO_SOCKETASYNC
			await stream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
#else
			await stream.WriteAsync(buffer, offset, count).WithCancellation(cancellationToken).ConfigureAwait(false);
#endif
		}	

		public static async Task FlushCancellableAsync(this NetworkStream stream, CancellationToken cancellationToken)
		{
#if !NO_SOCKETASYNC
			await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
#else
			await stream.FlushAsync().WithCancellation(cancellationToken).ConfigureAwait(false);
#endif
		}	
	}
}
#endif
