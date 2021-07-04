#if !NOSOCKET
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;

namespace NBitcoin.Protocol.Behaviors
{
	public class CompactFiltersChainBehavior : NodeBehavior
	{
		private readonly SlimChain _Chain;
		public SlimChain Chain
		{
			get
			{
				return _Chain;
			}
		}
		public CompactFiltersChainBehavior(SlimChain chain)
		{
			if (chain == null)
				throw new ArgumentNullException(nameof(chain));
			_Chain = chain;
		}

		public override object Clone()
		{
			return new CompactFiltersChainBehavior(Chain);
		}

		Timer _Refresh;
		protected override void AttachCore()
		{
			_Refresh = new Timer(o =>
			{
				TrySync();
			}, null, 0, (int)TimeSpan.FromMinutes(10).TotalMilliseconds);
			RegisterDisposable(_Refresh);
			AttachedNode.StateChanged += AttachedNode_StateChanged;
			RegisterDisposable(AttachedNode.Filters.Add(Intercept));
			if (AttachedNode.State == NodeState.HandShaked)
			{
				var message = new GetCompactFilterHeadersPayload( 
					FilterType.Basic,
					0,
					Chain.Tip
				);
				AttachedNode.SendMessageAsync(message);
				TrySync();
			}
		}

		void Intercept(IncomingMessage message, Action act)
		{
			if (message.Node.State == NodeState.HandShaked)
			{
				message.Message.IfPayloadIs<CompactFilterHeadersPayload>(cfheaders =>
				{
					if (cfheaders.FilterType != FilterType.Basic)
					{
						return;
					}
					bool updated = false;
					uint256 prevcfheader = null; 
					foreach (var cfheader in cfheaders.FilterHeaders)
					{
						updated |= Chain.TrySetTip(cfheader, prevcfheader, true);
						prevcfheader = cfheader;
					}
					if (updated)
					{
						message.Node.SendMessageAsync(new GetCompactFilterHeadersPayload( 
							FilterType.Basic,
							(uint)Chain.Height,
							Chain.Tip
						));
					}
				});
			}
			act();
		}

		private void TrySync()
		{
			var node = AttachedNode;
			if (node != null)
			{
				if (node.State == NodeState.HandShaked)
				{
					node.SendMessageAsync(new GetCompactFilterHeadersPayload( 
						FilterType.Basic,
						(uint)Chain.Height,
						Chain.Tip
					));
				}
			}
		}

		void AttachedNode_StateChanged(Node node, NodeState oldState)
		{
			if (node.State == NodeState.HandShaked)
			{
				node.SendMessageAsync(new GetCompactFilterHeadersPayload( 
					FilterType.Basic,
					(uint)Chain.Height,
					Chain.Tip
				));
				TrySync();
			}
		}

		protected override void DetachCore()
		{
			AttachedNode.StateChanged -= AttachedNode_StateChanged;
		}
	}
}
#endif
