using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;
using System.Threading;

namespace NBitcoin.Protocol.Behaviors
{
	public delegate void TransactionBroadcastedDelegate(Transaction transaction);
	public delegate void TransactionRejectedDelegate(Transaction transaction, RejectPayload reject);
	public class BroadcastTransactionBehavior : NodeBehavior
	{

		class Shared
		{
			public ConcurrentDictionary<uint256, Transaction> BroadcastedTransaction = new ConcurrentDictionary<uint256, Transaction>();
			public ConcurrentDictionary<Node, Node> Nodes = new ConcurrentDictionary<Node, Node>();
			public event TransactionBroadcastedDelegate TransactionBroadcasted;
			public event TransactionRejectedDelegate TransactionRejected;

			internal void OnBroadcastTransaction(Transaction transaction)
			{
				var hash = transaction.GetHash();
				var nodes = Nodes
							.Where(n => n.Key.State == NodeState.HandShaked)
							.Select(n => n.Key.Behaviors.Find<BroadcastTransactionBehavior>())
							.Where(n => n != null)
							.Where(n => !n._Broadcasting.ContainsKey(hash))
							.ToArray();
				if(nodes.Length >= 2)
				{
					Utils.Shuffle(nodes);
					var sentNode = nodes.Length / 2;
					for(int i = 0 ; i < sentNode ; i++)
					{
						int local = i;
						nodes[local].BroadcastTransactionCore(transaction);
					}
				}
			}

			internal void OnTransactionRejected(Transaction tx, RejectPayload reject)
			{
				var evt = TransactionRejected;
				if(evt != null)
					evt(tx, reject);
			}

			internal void OnTransactionBroadcasted(Transaction tx)
			{
				var evt = TransactionBroadcasted;
				if(evt != null)
					evt(tx);
			}
		}
		Shared _Shared;

		public event TransactionBroadcastedDelegate TransactionBroadcasted
		{
			add
			{
				_Shared.TransactionBroadcasted += value;
			}
			remove
			{
				_Shared.TransactionBroadcasted -= value;
			}
		}

		public event TransactionRejectedDelegate TransactionRejected
		{
			add
			{
				_Shared.TransactionRejected += value;
			}
			remove
			{
				_Shared.TransactionRejected -= value;
			}
		}

		public BroadcastTransactionBehavior()
		{
			_Shared = new Shared();
		}

		private BroadcastTransactionBehavior(BroadcastTransactionBehavior cloned)
		{
			_Shared = cloned._Shared;
		}


		void AttachedNode_StateChanged(Node node, NodeState oldState)
		{
			if(node.State == NodeState.HandShaked)
			{
				_Shared.Nodes.TryAdd(node, node);
				Flush();
			}
		}

		private void Flush()
		{
			foreach(var transaction in _Shared.BroadcastedTransaction)
			{
				_Shared.OnBroadcastTransaction(transaction.Value);
			}
		}

		ConcurrentDictionary<uint256, Transaction> _Broadcasting = new ConcurrentDictionary<uint256, Transaction>();

		async void BroadcastTransactionCore(Transaction transaction)
		{
			if(transaction == null)
				throw new ArgumentNullException("transaction");
			var node = AttachedNode;
			if(node != null)
			{
				var id = transaction.GetHash();
				if(_Broadcasting.TryAdd(id, transaction))
				{
					await node.SendMessageAsync(new InvPayload(InventoryType.MSG_TX, id)).ConfigureAwait(false);
				}
			}
		}

		/// <summary>
		/// Broadcast a transaction to connected peers. Do not save transaction broadcasting in case of shutdown.
		/// Two nodes needs to share the behavior, else the Transaction is stored and sent until it is.
		/// </summary>
		/// <param name="transaction">The transaction to broadcast</param>
		public void BroadcastTransaction(Transaction transaction)
		{
			if(transaction == null)
				throw new ArgumentNullException("transaction");
			if(_Shared.BroadcastedTransaction.TryAdd(transaction.GetHash(), transaction))
				_Shared.OnBroadcastTransaction(transaction);
		}

		Timer _Flush;
		protected override void AttachCore()
		{
			AttachedNode.StateChanged += AttachedNode_StateChanged;
			AttachedNode.MessageReceived += AttachedNode_MessageReceived;

			_Flush = new Timer(o =>
			{
				Flush();	
			}, null, 0, (int)TimeSpan.FromMinutes(10).TotalMilliseconds);
		}
		protected override void DetachCore()
		{
			AttachedNode.StateChanged -= AttachedNode_StateChanged;
			AttachedNode.MessageReceived -= AttachedNode_MessageReceived;
			Node unused;
			_Shared.Nodes.TryRemove(AttachedNode, out unused);
			_Flush.Dispose();
		}


		void AttachedNode_MessageReceived(Node node, IncomingMessage message)
		{
			InvPayload invPayload = message.Message.Payload as InvPayload;
			if(invPayload != null)
			{
				Transaction tx;
				foreach(var hash in invPayload.Where(i => i.Type == InventoryType.MSG_TX).Select(i => i.Hash))
				{
					_Broadcasting.TryRemove(hash, out tx);
					if(_Shared.BroadcastedTransaction.TryRemove(hash, out tx))
					{
						_Shared.OnTransactionBroadcasted(tx);
					}
				}
			}
			RejectPayload reject = message.Message.Payload as RejectPayload;
			if(reject != null && reject.Message == "tx")
			{
				Transaction tx;
				if(_Shared.BroadcastedTransaction.TryRemove(reject.Hash, out tx))
				{
					_Shared.OnTransactionRejected(tx, reject);
				}
			}

			GetDataPayload getData = message.Message.Payload as GetDataPayload;
			if(getData != null)
			{
				foreach(var inventory in getData.Inventory.Where(i => i.Type == InventoryType.MSG_TX))
				{
					Transaction tx;
					if(_Broadcasting.TryRemove(inventory.Hash, out tx))
						node.SendMessageAsync(new TxPayload(tx));
				}
			}
		}

		public override object Clone()
		{
			return new BroadcastTransactionBehavior(this);
		}

		public int BroadcastingCount
		{
			get
			{
				return _Shared.BroadcastedTransaction.Count;
			}
		}
	}
}
