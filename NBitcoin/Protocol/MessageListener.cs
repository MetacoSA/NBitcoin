#if !NOSOCKET
using System;
using System.Collections.Concurrent;
using System.Threading;
#if !NO_CHANNELS
using System.Threading.Channels;
#endif
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
#if NO_CHANNELS
	public class PollMessageListener<T> : MessageListener<T>
	{

		BlockingCollection<T> _MessageQueue = new BlockingCollection<T>(new ConcurrentQueue<T>());
		public int Count => _MessageQueue.Count;
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
#else
	public class PollMessageListener<T> : MessageListener<T>
	{

		Channel<T> _MessageQueue = Channel.CreateUnbounded<T>();
		public int Count => _MessageQueue.Reader.Count;
		public Channel<T> MessageQueue
		{
			get
			{
				return _MessageQueue;
			}
		}

		public T ReceiveMessage(CancellationToken cancellationToken = default(CancellationToken))
		{
			return ReceiveMessageAsync(cancellationToken).GetAwaiter().GetResult();
		}

		public virtual async Task<T> ReceiveMessageAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return await _MessageQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
		}

		#region MessageListener Members

		public virtual void PushMessage(T message)
		{
			_MessageQueue.Writer.TryWrite(message);
		}

		#endregion
	}
#endif
}
#endif
