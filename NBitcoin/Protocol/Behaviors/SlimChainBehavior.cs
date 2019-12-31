#if !NOSOCKET
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NBitcoin.Protocol.Behaviors
{
	/// <summary>
	/// Behavior to keep a SlimChain in sync with the remote node
	/// </summary>
	public class SlimChainBehavior : NodeBehavior
	{

		private readonly SlimChain _Chain;
		public SlimChain Chain
		{
			get
			{
				return _Chain;
			}
		}
		public SlimChainBehavior(SlimChain chain)
		{
			if (chain == null)
				throw new ArgumentNullException(nameof(chain));
			_Chain = chain;
		}



		public override object Clone()
		{
			return new SlimChainBehavior(Chain);
		}

		Timer _Refresh;
		protected override void AttachCore()
		{
			_Refresh = new Timer(o =>
			{
				TrySync();
			}, null, 0, (int)TimeSpan.FromMinutes(10).TotalMilliseconds);
			RegisterDisposable(_Refresh);
			if (AttachedNode.State == NodeState.Connected)
			{
				AttachedNode.MyVersion.StartHeight = Chain.Height;
			}
			AttachedNode.StateChanged += AttachedNode_StateChanged;
			RegisterDisposable(AttachedNode.Filters.Add(Intercept));
			if (AttachedNode.State == NodeState.HandShaked)
			{
				AttachedNode.SendMessageAsync(new SendHeadersPayload());
				TrySync();
			}
		}

		void Intercept(IncomingMessage message, Action act)
		{
			if (message.Node.State == NodeState.HandShaked)
			{
				message.Message.IfPayloadIs<HeadersPayload>(headers =>
				{
					bool updated = false;
					foreach (var h in headers.Headers)
					{
						updated |= AddToChain(h);
					}
					if (updated)
					{
						message.Node.SendMessageAsync(new GetHeadersPayload()
						{
							BlockLocators = Chain.GetTipLocator()
						});
					}
				});

				message.Message.IfPayloadIs<InvPayload>(invs =>
				{
					var needSync = invs.Where(v => v.Type == InventoryType.MSG_BLOCK)
						.Any(b => !_Chain.Contains(b.Hash));
					if (needSync)
						TrySync();
				});
			}
			act();
		}

		private bool AddToChain(BlockHeader blockHeader)
		{
			return Chain.TrySetTip(blockHeader.GetHash(), blockHeader.HashPrevBlock, true);
		}

		private void TrySync()
		{
			var node = AttachedNode;
			if (node != null)
			{
				if (node.State == NodeState.HandShaked)
				{
					node.SendMessageAsync(new GetHeadersPayload()
					{
						BlockLocators = Chain.GetTipLocator()
					});
				}
			}
		}

		void AttachedNode_StateChanged(Node node, NodeState oldState)
		{
			if (node.State == NodeState.HandShaked)
			{
				node.SendMessageAsync(new SendHeadersPayload());
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
