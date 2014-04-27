using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{

	public interface MessageListener<T>
	{
		void PushMessage(T message);
	}

	public class EventLoopMessageListener<T> : MessageListener<T>, IDisposable
	{
		public EventLoopMessageListener(Action<T> processMessage)
		{
			new Thread(new ThreadStart(() =>
			{
				try
				{
					while(!cancellationSource.IsCancellationRequested)
					{
						var message = _MessageQueue.Take(cancellationSource.Token);
						if(message != null)
						{
							try
							{
								processMessage(message);
							}
							catch(Exception ex)
							{
								ProtocolTrace.Error("Unexpected expected during message loop", ex);
							}
						}
					}
				}
				catch(OperationCanceledException)
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

		public T RecieveMessage(CancellationToken cancellationToken = default(CancellationToken))
		{
			return MessageQueue.Take(cancellationToken);
		}

		#region MessageListener Members

		public void PushMessage(T message)
		{
			_MessageQueue.Add(message);
		}

		#endregion
	}
}
