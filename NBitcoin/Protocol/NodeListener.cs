#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class NodeListener : PollMessageListener<IncomingMessage>, IDisposable
	{
		private readonly Node _Node;
		public Node Node
		{
			get
			{
				return _Node;
			}
		}
		IDisposable _Subscription;
		public NodeListener(Node node)
		{
			_Subscription = node.MessageProducer.AddMessageListener(this);
			_Node = node;
		}

		public NodeListener Where(Func<IncomingMessage, bool> predicate)
		{
			_Predicates.Add(predicate);
			return this;
		}
		public NodeListener OfType<TPayload>() where TPayload : Payload
		{
			_Predicates.Add(i => i.Message.Payload is TPayload);
			return this;
		}

		public TPayload ReceivePayload<TPayload>(CancellationToken cancellationToken = default(CancellationToken))
			where TPayload : Payload
		{
			if (!Node.IsConnected)
				throw new InvalidOperationException("The node is not in a connected state");
			Queue<IncomingMessage> pushedAside = new Queue<IncomingMessage>();
			try
			{
				using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, Node._Connection.Cancel.Token))
				{
					while (true)
					{
						var message = ReceiveMessage(cts.Token);
						if (_Predicates.All(p => p(message)))
						{
							if (message.Message.Payload is TPayload)
								return (TPayload)message.Message.Payload;
							else
							{
								pushedAside.Enqueue(message);
							}

						}
					}
				}
			}
			catch (OperationCanceledException)
			{
				if (Node._Connection.Cancel.IsCancellationRequested)
					throw new InvalidOperationException("The node is not in a connected state");
				throw;
			}
			finally
			{
				while (pushedAside.Count != 0)
					PushMessage(pushedAside.Dequeue());
			}
		}

		List<Func<IncomingMessage, bool>> _Predicates = new List<Func<IncomingMessage, bool>>();

		#region IDisposable Members

		public void Dispose()
		{
			if (_Subscription != null)
				_Subscription.Dispose();
		}

		#endregion
	}
}
#endif