#if !NOSOCKET
using System;

namespace NBitcoin.Protocol.Filters
{
	public class NodeFiltersCollection : ThreadSafeCollection<INodeFilter>
	{
		public IDisposable Add(Action<IncomingMessage, Action> onReceiving, Action<Node, Payload, Action> onSending = null)
		{
			return base.Add(new ActionFilter(onReceiving, onSending));
		}
	}
}
#endif