using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class MessageProducer<T>
	{
		List<MessageListener<T>> _Listeners = new List<MessageListener<T>>();
		public IDisposable AddMessageListener(MessageListener<T> listener)
		{
			lock(_Listeners)
			{
				return new Scope(() =>
				{
					_Listeners.Add(listener);
				}, () =>
				{
					lock(_Listeners)
					{
						_Listeners.Remove(listener);
					}
				});
			}
		}

		public void RemoveMessageListener(MessageListener<T> listener)
		{
			lock(_Listeners)
			{
				_Listeners.Add(listener);
			}
		}

		public void PushMessage(T message)
		{
			lock(_Listeners)
			{
				foreach(var listener in _Listeners)
				{
					listener.PushMessage(message);
				}
			}
		}


		public void PushMessages(IEnumerable<T> messages)
		{
			lock(_Listeners)
			{
				foreach(var message in messages)
				{
					foreach(var listener in _Listeners)
					{
						listener.PushMessage(message);
					}
				}
			}
		}
	}
}
