#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Filters
{
	public class NodeFiltersCollection : ThreadSafeList<INodeFilter>
	{
		public IDisposable Add(Action<IncomingMessage, Action> onReceiving, Action<Node, Payload, Action> onSending = null)
		{
			return base.Add(new ActionFilter(onReceiving, onSending));
		}
	}
}
#endif
