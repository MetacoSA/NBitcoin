using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class TraceCorrelationScope : IDisposable
	{
		private Guid activity;
		private Guid old;

		public Guid OldActivity
		{
			get
			{
				return old;
			}
			private set
			{
				old = value;
			}
		}

		public TraceCorrelationScope(Guid activity)
		{
			this.old = Trace.CorrelationManager.ActivityId;
			this.activity = activity;
			Trace.CorrelationManager.ActivityId = activity;
		}


		#region IDisposable Members

		public void Dispose()
		{
			Trace.CorrelationManager.ActivityId = old;
		}

		#endregion
	}
	public class TraceCorrelation
	{
		
		Guid activity;

		public Guid Activity
		{
			get
			{
				return activity;
			}
			private set
			{
				activity = value;
			}
		}

		public TraceCorrelationScope Open()
		{
			return new TraceCorrelationScope(activity);
		}

		public void LogInside(Action act)
		{
			using(Open())
			{
				act();
			}
		}

		public TraceCorrelation():this(Guid.NewGuid())
		{

		}
		public TraceCorrelation(Guid activity)
		{
			this.activity = activity;
		}
	}
}
