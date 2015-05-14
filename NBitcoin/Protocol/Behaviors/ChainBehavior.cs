using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Behaviors
{
	public class ChainBehavior : NodeBehavior
	{
		public ChainBehavior(ConcurrentChain chain)
		{
			if(chain == null)
				throw new ArgumentNullException("chain");
			_Chain = chain;
			CanSync = true;
			CanRespondToGetHeaders = true;
		}

		public bool CanSync
		{
			get;
			set;
		}
		public bool CanRespondToGetHeaders
		{
			get;
			set;
		}

		ConcurrentChain _Chain;
		public ConcurrentChain Chain
		{
			get
			{
				return _Chain;
			}
			set
			{
				AssertNotAttached();
				_Chain = value;
			}
		}

		Timer _Refresh;
		protected override void AttachCore()
		{
			_Refresh = new Timer(o =>
			{
				TrySync();
			}, null, 0, (int)TimeSpan.FromMinutes(10).TotalMilliseconds);
			RegisterDisposable(_Refresh);
			if(AttachedNode.State == NodeState.Connected)
			{
				AttachedNode.MyVersion.StartHeight = Chain.Height;
			}
			AttachedNode.StateChanged += AttachedNode_StateChanged;
			AttachedNode.MessageReceived += AttachedNode_MessageReceived;
		}

		void AttachedNode_MessageReceived(Node node, IncomingMessage message)
		{
			var inv = message.Message.Payload as InvPayload;
			if(inv != null)
			{
				if(inv.Inventory.Any(i => (i.Type == InventoryType.MSG_BLOCK) && !Chain.Contains(i.Hash)))
				{
					_Refresh.Dispose(); //No need of periodical refresh, the peer is notifying us
					TrySync();
				}
			}

			var getheaders = message.Message.Payload as GetHeadersPayload;
			if(getheaders != null && CanRespondToGetHeaders)
			{
				HeadersPayload headers = new HeadersPayload();
				var fork = Chain.FindFork(getheaders.BlockLocators);
				if(fork != null)
					foreach(var header in Chain.EnumerateToTip(fork).Skip(1))
					{
						headers.Headers.Add(header.Header);
						if(header.HashBlock == getheaders.HashStop || headers.Headers.Count == 2000)
							break;
					}
				node.SendMessage(headers);
			}

			var newheaders = message.Message.Payload as HeadersPayload;
			if(newheaders != null && CanSync)
			{
				var tip = GetPendingTip();
				foreach(var header in newheaders.Headers)
				{
					var prev = tip.FindAncestorOrSelf(header.HashPrevBlock);
					if(prev == null)
						break;
					tip = new ChainedBlock(header, header.GetHash(), prev);
					if(!AttachedNode.IsTrusted)
					{
						if(!tip.Validate(AttachedNode.Network))
						{
							invalidHeaderReceived = true;
							break;
						}
					}
					_PendingTip = tip;
				}
				if(_PendingTip.Height > Chain.Tip.Height)
				{
					Chain.SetTip(_PendingTip);
				}
				if(newheaders.Headers.Count != 0)
					TrySync();
			}
		}


		ChainedBlock _PendingTip; //Might be different than Chain.Tip, in the rare event of large fork > 2000 blocks

		private bool invalidHeaderReceived;
		void AttachedNode_StateChanged(Node node, NodeState oldState)
		{
			TrySync();
		}

		/// <summary>
		/// Asynchronously try to sync the chain
		/// </summary>
		public void TrySync()
		{
			if(AttachedNode.State == NodeState.HandShaked && CanSync && !invalidHeaderReceived)
			{
				AttachedNode.SendMessage(new GetHeadersPayload()
				{
					BlockLocators = GetPendingTip().GetLocator()
				});
			}
		}

		private ChainedBlock GetPendingTip()
		{
			_PendingTip = _PendingTip ?? Chain.Tip;
			if(Chain.Contains(_PendingTip))
			{
				_PendingTip = Chain.Tip;  //The chain was updated beyond what this behavior knows
			}
			return _PendingTip;
		}

		protected override void DetachCore()
		{
			AttachedNode.StateChanged -= AttachedNode_StateChanged;
			AttachedNode.MessageReceived -= AttachedNode_MessageReceived;
		}
	}
}
