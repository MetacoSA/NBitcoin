using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class PerformanceSnapshot
	{

		public PerformanceSnapshot(long readen, long written)
		{
			_TotalWrittenBytes = written;
			_TotalReadenBytes = readen;
		}
		private readonly long _TotalWrittenBytes;
		public long TotalWrittenBytes
		{
			get
			{
				return _TotalWrittenBytes;
			}
		}

		long _TotalReadenBytes;
		public long TotalReadenBytes
		{
			get
			{
				return _TotalReadenBytes;
			}
			set
			{
				_TotalReadenBytes = value;
			}
		}
		public TimeSpan Elapsed
		{
			get
			{
				return Taken - Start;
			}
		}
		public ulong ReadenBytesPerSecond
		{
			get
			{
				return (ulong)((double)TotalReadenBytes / Elapsed.TotalSeconds);
			}
		}
		public ulong WrittenBytesPerSecond
		{
			get
			{
				return (ulong)((double)TotalWrittenBytes / Elapsed.TotalSeconds);
			}
		}

		public static PerformanceSnapshot operator -(PerformanceSnapshot end, PerformanceSnapshot start)
		{
			if(end.Start != start.Start)
			{
				throw new InvalidOperationException("Performance snapshot should be taken from the same point of time");
			}
			if(end.Taken < start.Taken)
			{
				throw new InvalidOperationException("The difference of snapshot can't be negative");
			}
			return new PerformanceSnapshot(end.TotalReadenBytes - start.TotalReadenBytes,
											end.TotalWrittenBytes - start.TotalWrittenBytes)
			{
				Start = start.Taken,
				Taken = end.Taken
			};
		}

		public override string ToString()
		{
			return "Read : " + ToKBSec(ReadenBytesPerSecond) + ", Write : " + ToKBSec(WrittenBytesPerSecond);
		}

		private string ToKBSec(ulong bytesPerSec)
		{
			double speed = ((double)bytesPerSec / 1024.0);
			return speed.ToString("0.00") + " KB/S)";
		}

		public DateTime Start
		{
			get;
			set;
		}

		public DateTime Taken
		{
			get;
			set;
		}
	}
	public class PerformanceCounter
	{
		public PerformanceCounter()
		{
			_Start = DateTime.UtcNow;
		}

		long _WrittenBytes;
		public long WrittenBytes
		{
			get
			{
				return _WrittenBytes;
			}
		}


		public void AddWritten(long count)
		{
			Interlocked.Add(ref _WrittenBytes, count);
		}
		public void AddReaden(long count)
		{
			Interlocked.Add(ref _ReadenBytes, count);
		}

		long _ReadenBytes;
		public long ReadenBytes
		{
			get
			{
				return _ReadenBytes;
			}
		}

		public PerformanceSnapshot Snapshot()
		{
#if !(PORTABLE || NETCORE)
			Thread.MemoryBarrier();
#endif
			var snap = new PerformanceSnapshot(ReadenBytes, WrittenBytes)
			{
				Start = Start,
				Taken = DateTime.UtcNow
			};
			return snap;
		}

		DateTime _Start;
		public DateTime Start
		{
			get
			{
				return _Start;
			}
		}
		public TimeSpan Elapsed
		{
			get
			{
				return DateTime.UtcNow - Start;
			}
		}

		public override string ToString()
		{
			return Snapshot().ToString();
		}

		internal void Add(PerformanceCounter counter)
		{
			AddWritten(counter.WrittenBytes);
			AddReaden(counter.ReadenBytes);
		}
	}
}
