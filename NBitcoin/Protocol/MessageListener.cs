#if !NOSOCKET
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin.Logging;

namespace NBitcoin.Protocol
{

	public interface MessageListener<in T>
	{
		void PushMessage(T message);
	}

	public class NullMessageListener<T> : MessageListener<T>
	{
		#region MessageListener<T> Members

		public void PushMessage(T message)
		{
		}

		#endregion
	}

	public class NewThreadMessageListener<T> : MessageListener<T>
	{
		readonly Action<T> _Process;
		public NewThreadMessageListener(Action<T> process)
		{
			if (process == null)
				throw new ArgumentNullException(nameof(process));
			_Process = process;
		}
		#region MessageListener<T> Members

		public void PushMessage(T message)
		{
			if (message != null)
				Task.Factory.StartNew(() =>
				{
					try
					{
						_Process(message);
					}
					catch (Exception ex)
					{
						Logs.NodeServer.LogError(default, ex, "Unexpected expected during message loop");
					}
				});
		}

		#endregion
	}

	public class EventLoopMessageListener<T> : MessageListener<T>, IDisposable
	{
		public EventLoopMessageListener(Action<T> processMessage)
		{
			new Thread(new ThreadStart(() =>
			{
				try
				{
					while (!cancellationSource.IsCancellationRequested)
					{
						var message = _MessageQueue.Take(cancellationSource.Token);
						if (message != null)
						{
							try
							{
								processMessage(message);
							}
							catch (Exception ex)
							{
								Logs.NodeServer.LogError(default, ex, "Unexpected expected during message loop");
							}
						}
					}
				}
				catch (OperationCanceledException)
				{
				}
			})).Start();
		}
		BlockingCollection<T> _MessageQueue = new BlockingCollection<T>(new ConcurrentQueue<T>());
		public BlockingCollection<T> MessageQueue
		{
			get
			{
				return _MessageQueue;
			}
		}


		#region MessageListener Members

		public void PushMessage(T message)
		{
			_MessageQueue.Add(message);
		}

		#endregion

		#region IDisposable Members

		CancellationTokenSource cancellationSource = new CancellationTokenSource();
		public void Dispose()
		{
			if (cancellationSource.IsCancellationRequested)
				return;
			cancellationSource.Cancel();
		}

		#endregion

	}

	public class PollMessageListener<T> : MessageListener<T>
	{

		BlockingCollection<T> _MessageQueue = new BlockingCollection<T>(new ConcurrentQueue<T>());
		public BlockingCollection<T> MessageQueue
		{
			get
			{
				return _MessageQueue;
			}
		}

		public virtual T ReceiveMessage(CancellationToken cancellationToken = default(CancellationToken))
		{
			return MessageQueue.Take(cancellationToken);
		}

		#region MessageListener Members

		public virtual void PushMessage(T message)
		{
			_MessageQueue.Add(message);
		}

		#endregion
	}
}
#endif