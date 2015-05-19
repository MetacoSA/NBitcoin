using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Filters
{
	public interface INodeFilter
	{
		/// <summary>
		/// Intercept a message before it can be processed by Node.MessageReceived
		/// </summary>
		/// <param name="message">The message</param>
		/// <param name="next">The rest of the pipeline</param>
		void Invoke(IncomingMessage message, Action next);
	}
}
