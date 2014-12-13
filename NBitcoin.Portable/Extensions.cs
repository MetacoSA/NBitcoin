#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace System.Net.Sockets
{
	public static class PortableExtensions
	{
		public static IPAddress MapToIPv6(this IPAddress address)
		{
			if(address.AddressFamily == AddressFamily.InterNetworkV6)
				return address;
			if(address.AddressFamily != AddressFamily.InterNetwork)
				throw new Exception("Only AddressFamily.InterNetworkV4 can be converted to IPv6");

			byte[] ipv4Bytes = address.GetAddressBytes();
			byte[] ipv6Bytes = new byte[16] { 
 			0,0, 0,0, 0,0, 0,0, 0,0, 0xFF,0xFF, 
 			ipv4Bytes [0], ipv4Bytes [1], ipv4Bytes [2], ipv4Bytes [3] 
 			};
			return new IPAddress(ipv6Bytes);

		}

		public static bool IsIPv4MappedToIPv6(this IPAddress address)
		{
			if(address.AddressFamily != AddressFamily.InterNetworkV6)
				return false;

			byte[] bytes = address.GetAddressBytes();

			for(int i = 0 ; i < 10 ; i++)
			{
				if(bytes[0] != 0)
					return false;
			}
			return bytes[10] == 0xFF && bytes[11] == 0xFF;
		}
	}
}
#endif