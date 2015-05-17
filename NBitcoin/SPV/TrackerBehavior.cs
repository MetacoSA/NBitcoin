using NBitcoin.Protocol.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.SPV
{
	internal class TrackerBehavior : NodeBehavior, ICloneable
	{
		Tracker _Tracker;
		public TrackerBehavior(Tracker tracker)
		{
			if(tracker == null)
				throw new ArgumentNullException("tracker");
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

			}
		}

		#region ICloneable Members

		public object Clone()
		{
			var clone = new TrackerBehavior(_Tracker);
			clone._Tweak = _Tweak;
			return clone;
		}

		#endregion
	}
}
