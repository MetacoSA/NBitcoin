using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public static class NodeServerTrace
	{

		static TraceSource _Trace = TraceSourceFactory.CreateTraceSource("NBitcoin.NodeServer");
		internal static TraceSource Trace
		{
			get
			{
				return _Trace;
			}
		}

		public static void Transfer(Guid activityId)
		{
			_Trace.TraceTransfer(0, "t", activityId);
		}

		public static void ErrorWhileRetrievingDNSSeedIp(string name, Exception ex)
		{
			_Trace.TraceEvent(TraceEventType.Warning, 0, "Impossible to resolve dns for seed " + name + " " + Utils.ExceptionToString(ex));
		}


		public static void Warning(string msg, Exception ex)
		{
			_Trace.TraceEvent(TraceEventType.Warning, 0, msg + " " + Utils.ExceptionToString(ex));
		}

		public static void ExternalIpReceived(string ip)
		{
			_Trace.TraceInformation("External ip received : " + ip);
		}

		internal static void ExternalIpFailed(Exception ex)
		{
			_Trace.TraceEvent(TraceEventType.Error, 0, "External ip cannot be detected " + Utils.ExceptionToString(ex));
		}

		internal static void Information(string info)
		{
			_Trace.TraceInformation(info);
		}

		internal static void Error(string msg, Exception ex)
		{
			_Trace.TraceEvent(TraceEventType.Error, 0, msg + " " + Utils.ExceptionToString(ex));
		}

		internal static void Warning(string msg)
		{
			Warning(msg, null);
		}

		internal static void PeerTableRemainingPeerToGet(int count)
		{
			_Trace.TraceInformation("Remaining peer to get : " + count);
		}

		internal static void ConnectionToSelfDetected()
		{
			Warning("Connection to self detected, abort connection");
		}

		internal static void Verbose(string str)
		{
			_Trace.TraceEvent(TraceEventType.Verbose, 0, str);
		}
	}
}
