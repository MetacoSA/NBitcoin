#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Socks
{
	static class SocketExtensions
	{
		public static async Task WithCancellation(this Task task, CancellationToken cancellationToken)
		{
			using (var delayCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
			{
				var waiting = Task.Delay(-1, delayCTS.Token);
				var doing = task;
				await Task.WhenAny(waiting, doing);
				delayCTS.Cancel();
				cancellationToken.ThrowIfCancellationRequested();
			}
		}
	}
}
#endif