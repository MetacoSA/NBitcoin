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
	public class TransactionBroadcast
	{
		public BroadcastState State
		{
			get;
			internal set;
		}
		public Transaction Transaction
		{
			get;
			internal set;
		}
		internal ulong PingValue
		{
			get;
			set;
		}
		public DateTime AnnouncedTime
		{
			get;
			internal set;
		}
	}
	public enum BroadcastState
	{
		NotSent,
		Announced,
		Broadcasted,
		Rejected,
		Accepted
	}

	/// <summary>
	/// Behavior to broadcast a transaction reliabily
	/// </summary>
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
							.Select(n => n.Key.Behaviors.Find<BroadcastTransactionBehavior>())
							.ToArray();
				foreach(var node in nodes)
				{
					node.BroadcastTransactionCore(transaction);
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

		ConcurrentDictionary<uint256, TransactionBroadcast> _HashToTransaction = new ConcurrentDictionary<uint256, TransactionBroadcast>();
		ConcurrentDictionary<ulong, TransactionBroadcast> _PingToTransaction = new ConcurrentDictionary<ulong, TransactionBroadcast>();

		TransactionBroadcast GetTransaction(uint256 hash, bool remove)
		{
			TransactionBroadcast result;

			if(remove)
			{
				if(_HashToTransaction.TryRemove(hash, out result))
				{
					TransactionBroadcast unused;
					_PingToTransaction.TryRemove(result.PingValue, out unused);
				}
			}
			else
			{
				_HashToTransaction.TryGetValue(hash, out result);
			}
			return result;
		}
		TransactionBroadcast GetTransaction(ulong pingValue, bool remove)
		{
			TransactionBroadcast result;

			if(remove)
			{
				if(_PingToTransaction.TryRemove(pingValue, out result))
				{
					TransactionBroadcast unused;
					_HashToTransaction.TryRemove(result.Transaction.GetHash(), out unused);
				}
			}
			else
			{
				_PingToTransaction.TryGetValue(pingValue, out result);
			}
			return result;
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
			foreach(var tx in _Shared.BroadcastedTransaction)
			{
				_HashToTransaction.TryAdd(tx.Key, new TransactionBroadcast()
				{
					State = BroadcastState.NotSent,
					Transaction = tx.Value
				});
			}
		}


		void AttachedNode_StateChanged(Node node, NodeState oldState)
		{
			if(node.State == NodeState.HandShaked)
			{
				_Shared.Nodes.TryAdd(node, node);
				AnnounceAll();
			}
		}

		private void AnnounceAll()
		{
			foreach(var broadcasted in _HashToTransaction)
			{
				if(broadcasted.Value.State == BroadcastState.NotSent ||
				   (DateTime.UtcNow - broadcasted.Value.AnnouncedTime) < TimeSpan.FromMinutes(5.0))
					Announce(broadcasted.Value, broadcasted.Key);
			}
		}


		void BroadcastTransactionCore(Transaction transaction)
		{
			if(transaction == null)
				throw new ArgumentNullException("transaction");
			var tx = new TransactionBroadcast();
			tx.Transaction = transaction;
			tx.State = BroadcastState.NotSent;
			var hash = transaction.GetHash();
			if(_HashToTransaction.TryAdd(hash, tx))
			{
				Announce(tx, hash);
			}
		}

		private void Announce(TransactionBroadcast tx, uint256 hash)
		{
			var node = AttachedNode;
			if(node != null && node.State == NodeState.HandShaked)
			{
				tx.State = BroadcastState.Announced;
				tx.AnnouncedTime = DateTime.UtcNow;
				var unused = node.SendMessageAsync(new InvPayload(InventoryType.MSG_TX, hash)).ConfigureAwait(false);
			}
		}


		/// <summary>
		/// Broadcast a transaction, the same behavior as been shared with other nodes, they will also broadcast
		/// </summary>
		/// <param name="transaction">The transaction to broadcast</param>
		/// <returns>The cause of the rejection or null</returns>
		public Task<RejectPayload> BroadcastTransactionAsync(Transaction transaction)
		{
			if(transaction == null)
				throw new ArgumentNullException("transaction");

			TaskCompletionSource<RejectPayload> completion = new TaskCompletionSource<RejectPayload>();
			var hash = transaction.GetHash();
			if(_Shared.BroadcastedTransaction.TryAdd(hash, transaction))
			{
				TransactionBroadcastedDelegate broadcasted = null;
				TransactionRejectedDelegate rejected = null;
				broadcasted = (t) =>
					{
						if(t.GetHash() == hash)
						{
							completion.SetResult(null);
							_Shared.TransactionRejected -= rejected;
							_Shared.TransactionBroadcasted -= broadcasted;
						}
					};
				_Shared.TransactionBroadcasted += broadcasted;
				rejected = (t, r) =>
				{
					if(r.Hash == hash)
					{
						completion.SetResult(r);
						_Shared.TransactionRejected -= rejected;
						_Shared.TransactionBroadcasted -= broadcasted;
					}
				};
				_Shared.TransactionRejected += rejected;
				_Shared.OnBroadcastTransaction(transaction);
			}
			return completion.Task;
		}

		Timer _Flush;
		protected override void AttachCore()
		{
			AttachedNode.StateChanged += AttachedNode_StateChanged;
			AttachedNode.MessageReceived += AttachedNode_MessageReceived;
			_Shared.TransactionBroadcasted += _Shared_TransactionBroadcasted;
			_Shared.TransactionRejected += _Shared_TransactionRejected;
			_Flush = new Timer(o =>
			{
				AnnounceAll();
			}, null, 0, (int)TimeSpan.FromMinutes(10).TotalMilliseconds);
		}

		void _Shared_TransactionRejected(Transaction transaction, RejectPayload reject)
		{
			GetTransaction(reject.Hash, true);
		}

		void _Shared_TransactionBroadcasted(Transaction transaction)
		{
			GetTransaction(transaction.GetHash(), true);
		}

		protected override void DetachCore()
		{
			AttachedNode.StateChanged -= AttachedNode_StateChanged;
			AttachedNode.MessageReceived -= AttachedNode_MessageReceived;
			_Shared.TransactionBroadcasted -= _Shared_TransactionBroadcasted;
			_Shared.TransactionRejected -= _Shared_TransactionRejected;
			
			Node unused;
			_Shared.Nodes.TryRemove(AttachedNode, out unused);
			_Flush.Dispose();
		}

		void AttachedNode_MessageReceived(Node node, IncomingMessage message)
		{
			InvPayload invPayload = message.Message.Payload as InvPayload;
			if(invPayload != null)
			{

				foreach(var hash in invPayload.Where(i => i.Type == InventoryType.MSG_TX).Select(i => i.Hash))
				{
					var tx = GetTransaction(hash, true);
					if(tx != null)
						tx.State = BroadcastState.Accepted;
					Transaction unused;
					if(_Shared.BroadcastedTransaction.TryRemove(hash, out unused))
					{
						_Shared.OnTransactionBroadcasted(tx.Transaction);
					}
				}
			}
			RejectPayload reject = message.Message.Payload as RejectPayload;
			if(reject != null && reject.Message == "tx")
			{
				var tx = GetTransaction(reject.Hash, true);
				if(tx != null)
					tx.State = BroadcastState.Rejected;
				Transaction tx2;
				if(_Shared.BroadcastedTransaction.TryRemove(reject.Hash, out tx2))
				{
					_Shared.OnTransactionRejected(tx2, reject);
				}

			}

			GetDataPayload getData = message.Message.Payload as GetDataPayload;
			if(getData != null)
			{
				foreach(var inventory in getData.Inventory.Where(i => i.Type == InventoryType.MSG_TX))
				{
					var tx = GetTransaction(inventory.Hash, false);
					if(tx != null)
					{
						tx.State = BroadcastState.Broadcasted;
						var ping = new PingPayload();
						tx.PingValue = ping.Nonce;
						_PingToTransaction.TryAdd(tx.PingValue, tx);
						node.SendMessageAsync(new TxPayload(tx.Transaction));
						node.SendMessageAsync(ping);
					}
				}
			}

			PongPayload pong = message.Message.Payload as PongPayload;
			if(pong != null)
			{
				var tx = GetTransaction(pong.Nonce, true);
				if(tx != null)
				{
					tx.State = BroadcastState.Accepted;
					Transaction unused;
					if(_Shared.BroadcastedTransaction.TryRemove(tx.Transaction.GetHash(), out unused))
					{
						_Shared.OnTransactionBroadcasted(tx.Transaction);
					}
				}
			}
		}

		public override object Clone()
		{
			return new BroadcastTransactionBehavior(this);
		}

		public IEnumerable<TransactionBroadcast> Broadcasts
		{
			get
			{
				return _HashToTransaction.Values;
			}
		}
	}
}
