﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class NodeServerTrace
	{
		static TraceSource _Trace = new TraceSource("NBitcoin.NodeServer");
		public static TraceSource Trace
		{
			get
			{
				return _Trace;
			}
		}

		public static void ErrorWhileRetrievingDNSSeedIp(string name, Exception ex)
		{
			_Trace.TraceEvent(TraceEventType.Warning, 0, "Impossible to resolve dns for seed " + name + " " + Utils.ExceptionToString(ex));
		}


		public static void Warning(string msg, Exception ex)
		{
			_Trace.TraceEvent(TraceEventType.Warning, 0, msg + " " + Utils.ExceptionToString(ex));
		}

		public static void ExternalIpRecieved(string ip)
		{
			_Trace.TraceInformation("External ip recieved : " + ip);
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
