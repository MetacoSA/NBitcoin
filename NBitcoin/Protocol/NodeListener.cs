﻿using System;
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
			Queue<IncomingMessage> pushedAside = new Queue<IncomingMessage>();
			try
			{
				while(true)
				{
					var message = RecieveMessage(CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, Node._Connection.Cancel.Token).Token);
					if(_Predicates.All(p => p(message)))
					{
						if(message.Message.Payload is TPayload)
							return (TPayload)message.Message.Payload;
						else
						{
							pushedAside.Enqueue(message);
						}
					}
				}
			}
			catch(OperationCanceledException ex)
			{
				if(ex.CancellationToken == Node._Connection.Cancel.Token)
					throw new InvalidOperationException("Connection dropped");
				throw;
			}
			finally
			{
				while(pushedAside.Count != 0)
					PushMessage(pushedAside.Dequeue());
			}
			throw new InvalidProgramException("Bug in Node.RecieveMessage");
		}

		List<Func<IncomingMessage, bool>> _Predicates = new List<Func<IncomingMessage, bool>>();

		#region IDisposable Members

		public void Dispose()
		{
			if(_Subscription != null)
				_Subscription.Dispose();
		}

		#endregion
	}
}
