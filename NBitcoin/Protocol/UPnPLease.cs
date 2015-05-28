#if !NOSOCKET && !NOUPNP
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Open.Nat;

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



		internal async Task DetectExternalEndpoint(CancellationToken cancellation = default(CancellationToken))
		{
			using (Trace.Open())
			{
				int externalPort = 0;

				try
				{
					var searcher = new NatDiscoverer();
					var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
					var device = await searcher.DiscoverDeviceAsync(PortMapper.Upnp, cancellationTokenSource);

					using (Trace.Open(false))
					{
						try
						{
							var externalIp = await device.GetExternalIPAsync();
							ExternalEndpoint = Utils.EnsureIPv6(new IPEndPoint(externalIp, externalPort));
							NodeServerTrace.Information("External endpoint detected " + ExternalEndpoint);

							var mapping = await device.GetAllMappingsAsync();
							externalPort = BitcoinPorts.FirstOrDefault(p => mapping.All(m => m.PublicPort != p));

							if (externalPort == 0)
								NodeServerTrace.Error("Bitcoin node ports already used " + string.Join(",", BitcoinPorts), null);

							Mapping = new Mapping(Open.Nat.Protocol.Tcp, InternalPort, externalPort, (int)LeasePeriod.TotalSeconds, RuleName);
							await device.CreatePortMapAsync(Mapping);

							NodeServerTrace.Information("Port mapping added " + Mapping);
							Device = device;
						}
						catch (Exception ex)
						{
							NodeServerTrace.Error("Error during address port detection on the upnp device", ex);
						}
					}
				}
				catch (NatDeviceNotFoundException)
				{
					NodeServerTrace.Information("No UPnP device found");
					throw;
				}
				catch (OperationCanceledException)
				{
					NodeServerTrace.Information("Discovery cancelled");
					throw;
				}
				catch (Exception ex)
				{
					NodeServerTrace.Error("Error during upnp discovery", ex);
				}
			}
		}

		volatile bool isDisposed;
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!isDisposed & disposing)
			{
				isDisposed = true;
				using (Trace.Open())
				{
					if (Device != null)
					{
						Device.DeletePortMapAsync(Mapping).Wait();
						NodeServerTrace.Information("Port mapping removed " + Mapping);
					}
				}
			}
		}

		public async Task<bool> IsOpenAsync()
		{
			var mappings = await Device.GetAllMappingsAsync();
			return mappings.Any(m => m.Equals(Mapping));
		}

		public static async Task ReleaseAll(string ruleName, CancellationToken cancellation = default(CancellationToken))
		{
			try
			{
				var searcher = new NatDiscoverer();
				var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
				var device = await searcher.DiscoverDeviceAsync(PortMapper.Upnp, cancellationTokenSource);

				foreach (var m in await device.GetAllMappingsAsync())
				{
					if (m.Description == ruleName)
						await device.DeletePortMapAsync(m);
				}
			}
			catch (Exception ex)
			{
				NodeServerTrace.Error("Error releasing mappings", ex);
			}
		}

	}
}
#endif