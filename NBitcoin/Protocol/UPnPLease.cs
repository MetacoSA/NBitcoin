using System.Diagnostics;
using Open.Nat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class UPnPLease : IDisposable
	{
		public string RuleName
		{
			get;
			private set;
		}

	    static UPnPLease()
	    {
            NatUtility.TraceSource.Listeners.Add(new ConsoleTraceListener());
            NatUtility.TraceSource.Switch.Level = SourceLevels.All;
            NatUtility.PortMapper = PortMapper.Upnp;
	    }

		public UPnPLease(int[] bitcoinPorts, int internalPort, string ruleName)
		{
			RuleName = ruleName;
			_BitcoinPorts = bitcoinPorts;
			_InternalPort = internalPort;
			Trace = new TraceCorrelation(NodeServerTrace.Trace, "UPNP external address and port detection");
		}
		private readonly int _InternalPort;
		public int InternalPort
		{
			get
			{
				return _InternalPort;
			}
		}
		public IPEndPoint ExternalEndpoint
		{
			get;
			set;
		}
		private readonly int[] _BitcoinPorts;
		public int[] BitcoinPorts
		{
			get
			{
				return _BitcoinPorts;
			}
		}
		public TraceCorrelation Trace
		{
			get;
			private set;
		}
		public TimeSpan LeasePeriod
		{
			get;
			set;
		}
		public Mapping Mapping
		{
			get;
			internal set;
		}

		NatDevice Device
		{
			get;
			set;
		}



		internal bool DetectExternalEndpoint(CancellationToken cancellation = default(CancellationToken))
		{
			using(Trace.Open())
			{
				bool result = false;
				IPAddress externalIp = null;
				int externalPort = 0;

				try
				{
                    var searching = new ManualResetEvent(false);
					DiscoverDevice((s, e) =>
					{
                        NatUtility.StopDiscovery();
						using(Trace.Open(false))
						{
							if(cancellation.IsCancellationRequested)
								return;
							try
							{
								externalIp = e.Device.GetExternalIPAsync().Result;

							    Mapping = new Mapping(Open.Nat.Protocol.Tcp, InternalPort, BitcoinPorts[0], RuleName);

                                var task = Task.Run(async () => { await e.Device.CreatePortMapAsync(Mapping); });
                                task.Wait();
								
								NodeServerTrace.Information("Port mapping added " + Mapping);
								Device = e.Device;
							    result = true;
							}
							catch(Exception ex)
							{
								NodeServerTrace.Error("Error during address port detection on the upnp device", ex);
							}
                            finally
							{
                                searching.Set();
							}
						}
					}, 
                    (s,e)=> searching.Set(), 
                    cancellation);
				    searching.WaitOne();
                    if(result)
                    {
                        ExternalEndpoint = Utils.EnsureIPv6(new IPEndPoint(externalIp, externalPort));
                        NodeServerTrace.Information("External endpoint detected " + ExternalEndpoint);
                        return true;
                    }
				}
				catch(OperationCanceledException)
				{
					NodeServerTrace.Information("Discovery cancelled");
					throw;
				}
				catch(Exception ex)
				{
					NodeServerTrace.Error("Error during upnp discovery", ex);
				}
			    return false;
			}
		}

		private void LogNextLeaseRenew()
		{
			NodeServerTrace.Information("Next lease renewal at " + (DateTime.Now + CalculateNextRefresh()));
		}


		private TimeSpan CalculateNextRefresh()
		{
			return TimeSpan.FromTicks((LeasePeriod.Ticks - (LeasePeriod.Ticks / 10L)));
		}


		volatile bool isDisposed;
		public void Dispose()
		{
			if(!isDisposed)
			{
				isDisposed = true;
			}
            NatUtility.ReleaseAll();
        }

		public bool IsOpen()
		{
			var mappingsTask = Device.GetAllMappingsAsync();
		    mappingsTask.Wait();
            return mappingsTask.Result.Any(m => m.Description == Mapping.Description &&
						  m.PublicPort == Mapping.PublicPort &&
						  m.PrivatePort == Mapping.PrivatePort);
		}


		static void DiscoverDevice(EventHandler<DeviceEventArgs> deviceFound, EventHandler<UnhandledExceptionEventArgs> exea, CancellationToken cancellation)
		{
            NatUtility.DeviceFound += deviceFound;
            NatUtility.UnhandledException += exea;
            NatUtility.Initialize();
            NatUtility.StartDiscovery();
		}
	}
}
