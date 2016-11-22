using System;

namespace nStratis
{
	public class SequenceLock
	{
		public SequenceLock(int minHeight, DateTimeOffset minTime)
		{
			MinHeight = minHeight;
			MinTime = minTime;
		}
		public SequenceLock(int minHeight, long minTime)
			: this(minHeight, (DateTimeOffset) Utils.UnixTimeToDateTime(minTime))
		{
		}
		public int MinHeight
		{
			get;
			set;
		}
		public DateTimeOffset MinTime
		{
			get;
			set;
		}

		public bool Evaluate(ChainedBlock block)
		{
			var nBlockTime = block.Previous == null ? Utils.UnixTimeToDateTime(0) : block.Previous.GetMedianTimePast();
			return this.MinHeight < block.Height && this.MinTime < nBlockTime;
		}
	}
}
