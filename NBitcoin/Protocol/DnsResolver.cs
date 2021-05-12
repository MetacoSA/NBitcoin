#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class DnsResolver : IDnsResolver
	{
		private DnsResolver()
		{

		}
		private static DnsResolver _Instance = new DnsResolver();
		public static DnsResolver Instance => _Instance;
		public Task<IPAddress[]> GetHostAddressesAsync(string hostNameOrAddress, CancellationToken cancellationToken)
		{
			return Dns.GetHostAddressesAsync(hostNameOrAddress).WithCancellation(cancellationToken);
		}
	}
}
#endif
