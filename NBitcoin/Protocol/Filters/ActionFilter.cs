using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Filters
{
	public class ActionFilter : INodeFilter
	{
		Action<IncomingMessage, Action> _Act;
		public ActionFilter(Action<IncomingMessage, Action> act)
		{
			if(act == null)
				throw new ArgumentNullException("act");
			_Act = act;
		}
		#region INodeFilter Members

		public void Invoke(IncomingMessage message, Action next)
		{
			_Act(message, next);
		}

		#endregion
	}
}
