#if !NOSOCKET && !NOUPNP
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
				int externalPort = 0;

				try
				{
					var device = GetDevice(cancellation);
					if(device == null)
						return false;
					using(Trace.Open(false))
					{
						try
						{
							var externalIp = device.GetExternalIP();
							ExternalEndpoint = Utils.EnsureIPv6(new IPEndPoint(externalIp, externalPort));
							NodeServerTrace.Information("External endpoint detected " + ExternalEndpoint);

							var mapping = device.GetAllMappings();
							externalPort = BitcoinPorts.FirstOrDefault(p => mapping.All(m => m.PublicPort != p));

							if(externalPort == 0)
								NodeServerTrace.Error("Bitcoin node ports already used " + string.Join(",", BitcoinPorts), null);

							Mapping = new Mapping(Mono.Nat.Protocol.Tcp, InternalPort, externalPort, (int)LeasePeriod.TotalSeconds)
							{
								Description = RuleName
							};
							try
							{
								device.CreatePortMap(Mapping);
							}
							catch(MappingException ex)
							{
								if(ex.ErrorCode != 725) //Does not support lease
									throw;

								Mapping.Lifetime = 0;
								device.CreatePortMap(Mapping);
							}
							NodeServerTrace.Information("Port mapping added " + Mapping);
							Device = device;
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
											device.CreatePortMap(Mapping);
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
								Timer.Change((int)CalculateNextRefresh().TotalMilliseconds, Timeout.Infinite);
							}

						}
						catch(Exception ex)
						{
							NodeServerTrace.Error("Error during address port detection on the upnp device", ex);
						}
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
				return true;
			}
		}

		private static INatDevice GetDevice(CancellationToken cancellation)
		{
			UpnpSearcher searcher = new UpnpSearcher();
			var device = searcher.SearchAndReceive(cancellation);
			if(device == null)
			{
				NodeServerTrace.Information("No UPnP device found");
				return null;
			}
			return device;
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



		public static void ReleaseAll(string ruleName, CancellationToken cancellation = default(CancellationToken))
		{
			var device = GetDevice(cancellation);
			if(device == null)
				return;

			foreach(var m in device.GetAllMappings())
			{
				if(m.Description == ruleName)
					device.DeletePortMap(m);
			}
		}
	}
}
#endif