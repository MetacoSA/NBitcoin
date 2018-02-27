#if NOTRACESOURCE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Diagnostics
{
	public class TraceSourceFactory
	{
		public static TraceSource CreateTraceSource(string name)
		{
			return CreateTraceSourceFactory(name);
		}

		public static Func<string, TraceSource> CreateTraceSourceFactory
		{
			get;
			set;
		} = (n) => new NullTraceSource(n);
	}

	public enum TraceEventType
	{
		Error,
		Information,
		Start,
		Warning,
		Verbose,
		Stop,
	}

	public class CorrelationManager
	{
		public Guid ActivityId
		{
			get;
			set;
		}
	}
	public class Trace
	{
		static CorrelationManager _CorrelationManager = new CorrelationManager();
		public static CorrelationManager CorrelationManager
		{
			get
			{
				return _CorrelationManager;
			}
		}
	}
	public class Switch
	{
		public bool ShouldTrace(TraceEventType level)
		{
			return false;
		}
	}
	public interface TraceSource
	{
		Switch Switch
		{
			get;
			set;
		}

		void TraceEvent(TraceEventType traceEventType, int eventId, string msg, object[] args);

		void TraceEvent(TraceEventType traceEventType, int eventId, string msg);

		void TraceTransfer(int eventId, string p2, Guid activity);

		void TraceInformation(string msg);
	}

	class NullTraceSource : TraceSource
	{
		string n;
		public NullTraceSource(string n)
		{
			this.n = n;
		}
		public Switch Switch
		{
			get;
			set;
		} = new Switch();

		public void TraceEvent(TraceEventType traceEventType, int eventId, string msg, object[] args)
		{
			
		}

		public void TraceEvent(TraceEventType traceEventType, int eventId, string msg)
		{
			
		}

		public void TraceInformation(string msg)
		{
			
		}

		public void TraceTransfer(int eventId, string p2, Guid activity)
		{
			
		}
	}
}
#else
namespace System.Diagnostics
{
	public class TraceSourceFactory
	{
		public static TraceSource CreateTraceSource(string name)
		{
			return new TraceSource(name);
		}
	}
}
#endif