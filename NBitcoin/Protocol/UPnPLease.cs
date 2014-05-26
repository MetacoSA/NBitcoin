using System.Diagnostics;
using Open.Nat;
using System;
using System.Linq;
using System.Net;
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
		public Mapping Mapping
		{
			get;
			internal set;
		}

		static NatDevice Device
		{
			get;
			set;
		}


		internal bool DetectExternalEndpoint(CancellationToken cancellation = default(CancellationToken))
		{
		    int externalPort = 0;

		    using(Trace.Open())
			{
			    try
				{
					using(Trace.Open(false))
					{
                        SearchUpnpNatDevice();

                        if (cancellation.IsCancellationRequested){
                            NatUtility.StopDiscovery();
                            NodeServerTrace.Information("Discovery cancelled");
                            return false;
                        }
						IPAddress externalIp = Device.GetExternalIPAsync().Result;

						Mapping = new Mapping(Open.Nat.Protocol.Tcp, InternalPort, BitcoinPorts[0], RuleName);

                        var task = Task.Run(async () => { await Device.CreatePortMapAsync(Mapping); });
                        task.Wait();
								
						NodeServerTrace.Information("Port mapping added " + Mapping);
	
                        ExternalEndpoint = Utils.EnsureIPv6(new IPEndPoint(externalIp, externalPort));
                        NodeServerTrace.Information("External endpoint detected " + ExternalEndpoint);
                        return true;
                    }
				}
				catch(Exception ex)
				{
//                    NodeServerTrace.Error("Error during address port detection on the upnp device", ex);
                    NodeServerTrace.Error("Error during upnp discovery", ex);
				}
			    return false;
			}
		}

	    private void SearchUpnpNatDevice()
	    {
            if(Device!=null) return;

            var searching = new ManualResetEvent(false);
            NatUtility.PortMapper = PortMapper.Upnp;
            NatUtility.DeviceFound += (s, e) => {
                NatUtility.StopDiscovery();
                Device = e.Device;
                searching.Set();
            };
            NatUtility.DiscoveryTimedout += (s,e) =>searching.Set();
            NatUtility.UnhandledException += (s, e) => searching.Set();
            NatUtility.Initialize();
            NatUtility.StartDiscovery();
	        searching.WaitOne();
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
			var mappings = Device.GetAllMappingsAsync().Result;
            return mappings.Any(m => m.Description == Mapping.Description &&
						  m.PublicPort == Mapping.PublicPort &&
						  m.PrivatePort == Mapping.PrivatePort);
		}
	}
}
