using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Diagnostics
{
	internal enum TraceEventType
	{
		Error,
		Information,
		Start,
		Warning,
		Verbose,
		Stop,
	}

	internal class CorrelationManager
	{
		public Guid ActivityId
		{
			get;
			set;
		}
	}
	internal class Trace
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
	internal class TraceSource
	{
		private string p;

		public TraceSource(string p)
		{
			
			this.p = p;
		}

		internal void TraceEvent(TraceEventType traceEventType, int p, string msg, object[] args)
		{
			
		}

		internal void TraceEvent(TraceEventType traceEventType, int p, string msg)
		{
			
		}

		internal void TraceTransfer(int p1, string p2, Guid activity)
		{
			
		}

		internal void TraceInformation(string p)
		{
			
		}
	}
}
