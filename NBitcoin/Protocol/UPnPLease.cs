using Mono.Nat;
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
		Timer Timer
		{
			get;
			set;
		}
		public Mapping Mapping
		{
			get;
			internal set;
		}

		INatDevice Device
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
					DiscoverDevice((s, e) =>
					{
						using(Trace.Open(false))
						{
							if(cancellation.IsCancellationRequested)
								return;
							try
							{
								externalIp = e.Device.GetExternalIP();
								var mapping = e.Device.GetAllMappings();
								externalPort = BitcoinPorts.FirstOrDefault(p => mapping.All(m => m.PublicPort != p));
								if(externalPort != 0)
								{
									result = true;
									Mapping = new Mapping(Mono.Nat.Protocol.Tcp, InternalPort, externalPort, (int)LeasePeriod.TotalSeconds)
									{
										Description = RuleName
									};
									try
									{
										e.Device.CreatePortMap(Mapping);
									}
									catch(MappingException ex)
									{
										if(ex.ErrorCode != 725) //Does not support lease
											throw;

										Mapping.Lifetime = 0;
										e.Device.CreatePortMap(Mapping);
									}
									NodeServerTrace.Information("Port mapping added " + Mapping);
									if(Mapping.Lifetime != 0)
									{
										LogNextLeaseRenew();
										Timer = new Timer(o =>
										{
											if(isDisposed)
												return;
											using(Trace.Open(false))
											{
												try
												{
													e.Device.CreatePortMap(Mapping);
													NodeServerTrace.Information("Port mapping renewed");
													LogNextLeaseRenew();
												}
												catch(Exception ex)
												{
													NodeServerTrace.Error("Error when refreshing the port mapping with UPnP", ex);
												}
												finally
												{
													Timer.Change((int)CalculateNextRefresh().TotalMilliseconds, Timeout.Infinite);
												}
											}
										});
										Device = e.Device;
										Timer.Change((int)CalculateNextRefresh().TotalMilliseconds, Timeout.Infinite);
									}
									else
										NodeServerTrace.Error("Bitcoin node ports already used " + string.Join(",", BitcoinPorts), null);
								}
							}
							catch(Exception ex)
							{
								NodeServerTrace.Error("Error during address port detection on the upnp device", ex);
							}
						}
					}, cancellation);
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
				if(result)
				{
					ExternalEndpoint = Utils.EnsureIPv6(new IPEndPoint(externalIp, externalPort));
					NodeServerTrace.Information("External endpoint detected " + ExternalEndpoint);
				}
				return result;
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
				using(Trace.Open())
				{
					StopRenew();
					if(Device != null)
					{
						Device.DeletePortMap(Mapping);
						NodeServerTrace.Information("Port mapping removed " + Mapping);
					}
				}
			}
		}

		public void StopRenew()
		{
			if(Timer != null)
			{
				using(Trace.Open())
				{
					Timer.Dispose();
					Timer = null;
					NodeServerTrace.Information("Port mapping renewal stopped");
				}
			}
		}

		public bool IsOpen()
		{
			return Device.GetAllMappings()
				  .Any(m => m.Description == Mapping.Description &&
						  m.PublicPort == Mapping.PublicPort &&
						  m.PrivatePort == Mapping.PrivatePort);
		}


		static void DiscoverDevice(EventHandler<DeviceEventArgs> deviceFound, CancellationToken cancellation)
		{
			UpnpSearcher searcher = new UpnpSearcher();
			var device = searcher.SearchAndReceive(cancellation);
			if(device != null)
				deviceFound(searcher, new DeviceEventArgs(device));
		}

		public static void ReleaseAll(string ruleName, CancellationToken cancellation = default(CancellationToken))
		{
			DiscoverDevice((s, e) =>
				{
					foreach(var m in e.Device.GetAllMappings())
					{
						if(m.Description == ruleName)
							e.Device.DeletePortMap(m);
					}
				}, cancellation);
		}
	}
}
