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
	public interface IDnsResolver
	{
		//
		// Summary:
		//     Returns the Internet Protocol (IP) addresses for the specified host as an asynchronous
		//     operation.
		//
		// Parameters:
		//   hostNameOrAddress:
		//     The host name or IP address to resolve.
		//
		// Returns:
		//     The task object representing the asynchronous operation. The System.Threading.Tasks.Task`1.Result
		//     property on the task object returns an array of type System.Net.IPAddress that
		//     holds the IP addresses for the host that is specified by the hostNameOrAddress
		//     parameter.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     hostNameOrAddress is null.
		//
		//   T:System.ArgumentOutOfRangeException:
		//     The length of hostNameOrAddress is greater than 255 characters.
		//
		//   T:System.Net.Sockets.SocketException:
		//     An error is encountered when resolving hostNameOrAddress.
		//
		//   T:System.ArgumentException:
		//     hostNameOrAddress is an invalid IP address.
		Task<IPAddress[]> GetHostAddressesAsync(string hostNameOrAddress, CancellationToken cancellationToken);
	}
}
#endif
