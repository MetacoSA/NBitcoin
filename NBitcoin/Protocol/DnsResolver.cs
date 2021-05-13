#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
		public async Task<IPAddress[]> GetHostAddressesAsync(string hostNameOrAddress, CancellationToken cancellationToken)
		{
			var dns = new DnsEndPoint(hostNameOrAddress, 0);
			if (dns.IsTor() || dns.IsI2P())
				throw new SocketException(11001);
			var addr = await Dns.GetHostAddressesAsync(hostNameOrAddress).WithCancellation(cancellationToken).ConfigureAwait(false);
			if (addr.Length is 0)
				throw new SocketException(11001);
			return addr;
		}
	}
}
#endif
