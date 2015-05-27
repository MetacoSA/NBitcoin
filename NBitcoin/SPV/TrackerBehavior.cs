﻿#if !NOSOCKET
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.SPV
{
	public class TrackerScanPosition
	{
		public BlockLocator Locator
		{
			get;
			set;
		}
		public DateTimeOffset From
		{
			get;
			set;
		}
	}
	/// <summary>
	/// Load a bloom filter on the node, and push transactions in the Tracker
	/// </summary>
	public class TrackerBehavior : NodeBehavior
	{
		Tracker _Tracker;
		ConcurrentChain _Chain;
		ConcurrentChain _ExplicitChain;
		/// <summary>
		/// Create a new TrackerBehavior instance
		/// </summary>
		/// <param name="tracker">The Tracker registering transactions and confirmations</param>
		/// <param name="chain">The chain used to fetch height of incoming blocks, if null, use the chain of ChainBehavior</param>
		public TrackerBehavior(Tracker tracker, ConcurrentChain chain = null)
		{
			if(tracker == null)
				throw new ArgumentNullException("tracker");
			FalsePositiveRate = 0.0005;
			_Chain = chain;
			_ExplicitChain = chain;
			_Tracker = tracker;
		}

		public Tracker Tracker
		{
			get
			{
				return _Tracker;
			}
		}

		protected override void AttachCore()
		{
			if(_Chain == null)
			{
				var chainBehavior = AttachedNode.Behaviors.Find<ChainBehavior>();
				if(chainBehavior == null)
					throw new InvalidOperationException("A chain should either be passed in the constructor of TrackerBehavior, or a ChainBehavior should be attached on the node");
				_Chain = chainBehavior.Chain;
			}
			if(AttachedNode.State == Protocol.NodeState.HandShaked)
				SetBloomFilter();
			AttachedNode.StateChanged += AttachedNode_StateChanged;
			AttachedNode.MessageReceived += AttachedNode_MessageReceived;
			Timer timer = new Timer(StartScan, null, 5000, 10000);
			RegisterDisposable(timer);
		}

		void StartScan(object unused)
		{
			var node = AttachedNode;
			if(!IsScanning(node))
			{
				if(Monitor.TryEnter(cs))
				{
					try
					{
						if(!IsScanning(node))
						{
							GetDataPayload payload = new GetDataPayload();
							var fork = _Chain.FindFork(_CurrentProgress);
							foreach(var block in _Chain
								.EnumerateAfter(fork)
								.Where(b => b.Header.BlockTime + TimeSpan.FromHours(5.0) > _SkipBefore) //Take 5 more hours, block time might not be right
								.Partition(10000)
								.FirstOrDefault() ?? new List<ChainedBlock>())
							{
								if(block.HashBlock != _LastSeen)
								{
									payload.Inventory.Add(new InventoryVector(InventoryType.MSG_FILTERED_BLOCK, block.HashBlock));
									_InFlight.TryAdd(block.HashBlock, block.HashBlock);
								}
							}
							if(payload.Inventory.Count > 0)
								node.SendMessageAsync(payload);
						}
					}
					finally
					{
						Monitor.Exit(cs);
					}
				}
			}
		}

		private bool IsScanning(Node node)
		{
			return _InFlight.Count != 0 || _CurrentProgress == null || node == null;
		}

		void AttachedNode_StateChanged(Protocol.Node node, Protocol.NodeState oldState)
		{
			if(node.State == Protocol.NodeState.HandShaked)
				SetBloomFilter();
		}

		protected override void DetachCore()
		{
			AttachedNode.StateChanged -= AttachedNode_StateChanged;
			AttachedNode.MessageReceived -= AttachedNode_MessageReceived;
		}

		ConcurrentDictionary<uint256, uint256> _InFlight = new ConcurrentDictionary<uint256, uint256>();

		BoundedDictionary<uint256, MerkleBlock> _TransactionsToBlock = new BoundedDictionary<uint256, MerkleBlock>(1000);
		BoundedDictionary<uint256, Transaction> _KnownTxs = new BoundedDictionary<uint256, Transaction>(1000);
		void AttachedNode_MessageReceived(Protocol.Node node, Protocol.IncomingMessage message)
		{
			var merkleBlock = message.Message.Payload as MerkleBlockPayload;
			if(merkleBlock != null)
			{
				foreach(var txId in merkleBlock.Object.PartialMerkleTree.GetMatchedTransactions())
				{
					_TransactionsToBlock.AddOrUpdate(txId, merkleBlock.Object, (k, v) => merkleBlock.Object);
					var tx = _Tracker.GetKnownTransaction(txId);
					if(tx != null)
					{
						Notify(tx, merkleBlock.Object);
					}
				}

				var h = merkleBlock.Object.Header.GetHash();
				uint256 unused;
				if(_InFlight.TryRemove(h, out unused))
				{
					if(_InFlight.Count == 0)
					{
						_LastSeen = h;
						var chained = _Chain.GetBlock(h);
						_CurrentProgress = chained == null ? _CurrentProgress : chained.GetLocator();
						StartScan(unused);
					}
				}
			}

			var notfound = message.Message.Payload as NotFoundPayload;
			if(notfound != null)
			{
				foreach(var txid in notfound)
				{
					uint256 unusued;
					if(_InFlight.TryRemove(txid.Hash, out unusued))
					{
						if(_InFlight.Count == 0)
							StartScan(null);
					}
				}
			}

			var invs = message.Message.Payload as InvPayload;
			if(invs != null)
			{
				foreach(var inv in invs)
				{
					if(inv.Type == InventoryType.MSG_BLOCK)
						node.SendMessageAsync(new GetDataPayload(new InventoryVector(InventoryType.MSG_FILTERED_BLOCK, inv.Hash)));
					if(inv.Type == InventoryType.MSG_TX)
						node.SendMessageAsync(new GetDataPayload(inv));
				}
			}

			var txPayload = message.Message.Payload as TxPayload;
			if(txPayload != null)
			{
				var tx = txPayload.Object;
				MerkleBlock blk;
				var h = tx.GetHash();
				_TransactionsToBlock.TryGetValue(h, out blk);
				Notify(tx, blk);
			}
		}

		object cs = new object();
		BlockLocator _CurrentProgress;
		uint256 _LastSeen;
		DateTimeOffset _SkipBefore;

		public BlockLocator CurrentProgress
		{
			get
			{
				return _CurrentProgress;
			}
		}

		/// <summary>
		/// Start a scan
		/// </summary>
		/// <param name="locator"></param>
		public void Scan(BlockLocator locator, DateTimeOffset skipBefore)
		{
			lock(cs)
			{
				_SkipBefore = skipBefore;
				_CurrentProgress = locator;
			}
		}

		private void Notify(Transaction tx, MerkleBlock blk)
		{
			if(blk == null)
			{
				_Tracker.NotifyTransaction(tx);
			}
			else
			{
				var prev = _Chain.GetBlock(blk.Header.HashPrevBlock);
				if(prev != null)
				{
					var header = new ChainedBlock(blk.Header, null, prev);
					_Tracker.NotifyTransaction(tx, header, blk);
				}
				else
				{
					_Tracker.NotifyTransaction(tx);
				}
			}
		}

		public double FalsePositiveRate
		{
			get;
			set;
		}


		void SetBloomFilter()
		{
			var node = AttachedNode;
			if(node != null)
			{
				var filter = _Tracker.CreateBloomFilter(FalsePositiveRate);
				node.SendMessageAsync(new FilterLoadPayload(filter));
			}
		}


		public override object Clone()
		{
			var clone = new TrackerBehavior(_Tracker, _ExplicitChain);
			clone.FalsePositiveRate = FalsePositiveRate;
			clone._SkipBefore = _SkipBefore;
			clone._CurrentProgress = _CurrentProgress;
			return clone;
		}

	}
}
#endif