#if !NOSOCKET
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.SPV
{
	/// <summary>
	/// Load a bloom filter based on the data of the tracker, refreshable manually
	/// </summary>
	public class TrackerBehavior : NodeBehavior, ICloneable
	{
		Tracker _Tracker;
		ConcurrentChain _Chain;
		public TrackerBehavior(Tracker tracker, ConcurrentChain chain)
		{
			if(tracker == null)
				throw new ArgumentNullException("tracker");
			if(chain == null)
				throw new ArgumentNullException("chain");
			_Chain = chain;
			_Tracker = tracker;
			_Tweak = RandomUtils.GetUInt32();
		}

		protected override void AttachCore()
		{
			if(AttachedNode.State == Protocol.NodeState.HandShaked)
				RefreshBloomFilter();
			AttachedNode.StateChanged += AttachedNode_StateChanged;
		}

		void AttachedNode_StateChanged(Protocol.Node node, Protocol.NodeState oldState)
		{
			if(node.State == Protocol.NodeState.HandShaked)
				RefreshBloomFilter();
		}

		protected override void DetachCore()
		{
			AttachedNode.StateChanged -= AttachedNode_StateChanged;
			AttachedNode.MessageReceived += AttachedNode_MessageReceived;
		}

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
						Notify(tx, merkleBlock.Object);
				}
			}

			var invs = message.Message.Payload as InvPayload;
			if(invs != null)
			{
				foreach(var inv in invs)
				{
					node.SendMessage(new GetDataPayload(inv));
				}
			}

			var txPayload = message.Message.Payload as TxPayload;
			if(txPayload != null)
			{
				var tx = txPayload.Object;
				MerkleBlock blk;
				_TransactionsToBlock.TryGetValue(tx.GetHash(), out blk);
				Notify(tx, blk);
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


		uint _Tweak; //Tweak must be constant or the peer might attempt to intersect 2 filters to find out what belong to us
		public void RefreshBloomFilter()
		{
			var node = AttachedNode;
			if(node != null)
			{
				var datas = _Tracker.GetDataToTrack().ToList();
				BloomFilter filter = new BloomFilter(datas.Count, 0.005, _Tweak);
				foreach(var data in datas)
				{
					filter.Insert(data);
				}
				Task.Factory.StartNew(() =>
				{
					node.SendMessage(new FilterLoadPayload(filter));
				});
			}
		}

		#region ICloneable Members

		public object Clone()
		{
			var clone = new TrackerBehavior(_Tracker, _Chain);
			clone._Tweak = _Tweak;
			return clone;
		}

		#endregion
	}
}
#endif