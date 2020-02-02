#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using NBitcoin.DataEncoders;

namespace NBitcoin
{
	public static class IpExtensions
	{
#if CLASSICDOTNET
		interface ICompatibility
		{
			IPAddress MapToIPv6(IPAddress address);
			IPAddress MapToIPv4(IPAddress address);
			bool IsIPv4MappedToIPv6(IPAddress address);
		}
		class MonoCompatibility : ICompatibility
		{
			public bool IsIPv4MappedToIPv6(IPAddress address)
			{
				return Utils.IsIPv4MappedToIPv6(address);
			}

			public IPAddress MapToIPv6(IPAddress address)
			{
				return Utils.MapToIPv6(address);
			}

			public IPAddress MapToIPv4(IPAddress address)
			{
				return Utils.MapToIPv4(address);
			}
		}
		class WinCompatibility : ICompatibility
		{
			public bool IsIPv4MappedToIPv6(IPAddress address)
			{
				return address.IsIPv4MappedToIPv6;
			}

			public IPAddress MapToIPv6(IPAddress address)
			{
				return address.MapToIPv6();
			}
			public IPAddress MapToIPv4(IPAddress address)
			{
				return address.MapToIPv4();
			}
		}
		static ICompatibility _Compatibility;
		static ICompatibility Compatibility
		{
			get
			{
				if(_Compatibility == null)
				{
					_Compatibility = IsRunningOnMono() ? (ICompatibility)new MonoCompatibility() : new WinCompatibility();
				}
				return _Compatibility;
			}
		}
#endif
		public static bool IsRFC1918(this IPAddress address)
		{
			address = address.EnsureIPv6();
			var bytes = address.GetAddressBytes();
			return address.IsIPv4() && (
				bytes[15 - 3] == 10 ||
				(bytes[15 - 3] == 192 && bytes[15 - 2] == 168) ||
				(bytes[15 - 3] == 172 && (bytes[15 - 2] >= 16 && bytes[15 - 2] <= 31)));
		}


		public static bool IsIPv4(this IPAddress address)
		{
			return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork || address.IsIPv4MappedToIPv6Ex();
		}

		public static bool IsRFC3927(this IPAddress address)
		{
			address = address.EnsureIPv6();
			var bytes = address.GetAddressBytes();
			return address.IsIPv4() && (bytes[15 - 3] == 169 && bytes[15 - 2] == 254);
		}

		public static bool IsRFC3849(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			return bytes[15 - 15] == 0x20 && bytes[15 - 14] == 0x01 && bytes[15 - 13] == 0x0D && bytes[15 - 12] == 0xB8;
		}

		public static bool IsRFC3964(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			return (bytes[15 - 15] == 0x20 && bytes[15 - 14] == 0x02);
		}

		readonly static byte[] pchRFC6052 = new byte[] { 0, 0x64, 0xFF, 0x9B, 0, 0, 0, 0, 0, 0, 0, 0 };
		public static bool IsRFC6052(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			return ((Utils.ArrayEqual(bytes, 0, pchRFC6052, 0, pchRFC6052.Length) ? 0 : 1) == 0);
		}

		public static bool IsRFC4380(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			return (bytes[15 - 15] == 0x20 && bytes[15 - 14] == 0x01 && bytes[15 - 13] == 0 && bytes[15 - 12] == 0);
		}

		readonly static byte[] pchRFC4862 = new byte[] { 0xFE, 0x80, 0, 0, 0, 0, 0, 0 };
		public static bool IsRFC4862(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			return ((Utils.ArrayEqual(bytes, 0, pchRFC4862, 0, pchRFC4862.Length) ? 0 : 1) == 0);
		}

		public static bool IsRFC4193(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			return ((bytes[15 - 15] & 0xFE) == 0xFC);
		}
		readonly static byte[] pchRFC6145 = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0xFF, 0xFF, 0, 0 };
		public static bool IsRFC6145(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			return ((Utils.ArrayEqual(bytes, 0, pchRFC6145, 0, pchRFC6145.Length) ? 0 : 1) == 0);
		}

		public static bool IsRFC4843(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			return (bytes[15 - 15] == 0x20 && bytes[15 - 14] == 0x01 && bytes[15 - 13] == 0x00 && (bytes[15 - 12] & 0xF0) == 0x10);
		}

		public static byte[] GetGroup(this IPAddress address)
		{
			List<byte> vchRet = new List<byte>();
			int nClass = 2;
			int nStartByte = 0;
			int nBits = 16;

			address = address.EnsureIPv6();
			var bytes = address.GetAddressBytes();

			// all local addresses belong to the same group
			if (address.IsLocal())
			{
				nClass = 255;
				nBits = 0;
			}

			// all unroutable addresses belong to the same group
			if (!address.IsRoutable(true))
			{
				nClass = 0;
				nBits = 0;
			}
			// for IPv4 addresses, '1' + the 16 higher-order bits of the IP
			// includes mapped IPv4, SIIT translated IPv4, and the well-known prefix
			else if (address.IsIPv4() || address.IsRFC6145() || address.IsRFC6052())
			{
				nClass = 1;
				nStartByte = 12;
			}
			// for 6to4 tunnelled addresses, use the encapsulated IPv4 address
			else if (address.IsRFC3964())
			{
				nClass = 1;
				nStartByte = 2;
			}
			// for Teredo-tunnelled IPv6 addresses, use the encapsulated IPv4 address

			else if (address.IsRFC4380())
			{
				vchRet.Add(1);
				vchRet.Add((byte)(bytes[15 - 3] ^ 0xFF));
				vchRet.Add((byte)(bytes[15 - 2] ^ 0xFF));
				return vchRet.ToArray();
			}
			else if (address.IsTor())
			{
				nClass = 3;
				nStartByte = 6;
				nBits = 4;
			}
			// for he.net, use /36 groups
			else if (bytes[15 - 15] == 0x20 && bytes[15 - 14] == 0x01 && bytes[15 - 13] == 0x04 && bytes[15 - 12] == 0x70)
				nBits = 36;
			// for the rest of the IPv6 network, use /32 groups
			else
				nBits = 32;

			vchRet.Add((byte)nClass);
			while (nBits >= 8)
			{
				vchRet.Add(bytes[15 - (15 - nStartByte)]);
				nStartByte++;
				nBits -= 8;
			}
			if (nBits > 0)
				vchRet.Add((byte)(bytes[15 - (15 - nStartByte)] | ((1 << nBits) - 1)));

			return vchRet.ToArray();
		}

		static readonly byte[] pchOnionCat = new byte[] { 0xFD, 0x87, 0xD8, 0x7E, 0xEB, 0x43 };
		public static bool IsTor(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			return ((Utils.ArrayEqual(bytes, 0, pchOnionCat, 0, pchOnionCat.Length) ? 0 : 1) == 0);
		}
		public static bool IsTor(this EndPoint endpoint)
		{
			if (endpoint == null)
				throw new ArgumentNullException(nameof(endpoint));
			if (endpoint is IPEndPoint ip)
				return ip.IsTor();
			else if (endpoint is DnsEndPoint dns)
				return dns.IsTor();
			else
				return false;
		}
		public static bool IsTor(this DnsEndPoint dnsEndPoint)
		{
			if (dnsEndPoint == null)
				throw new ArgumentNullException(nameof(dnsEndPoint));
			return dnsEndPoint.Host.EndsWith(".onion", StringComparison.OrdinalIgnoreCase);
		}
		public static bool IsTor(this IPEndPoint iPEndPoint)
		{
			if (iPEndPoint == null)
				throw new ArgumentNullException(nameof(iPEndPoint));
			return iPEndPoint.Address.IsTor();
		}

		/// <summary>
		/// Return {host}:{port} of this endpoint.
		/// </summary>
		/// <param name="endpoint"></param>
		/// <returns>{host}:{port} representation of this endpoint</returns>
		public static string ToEndpointString(this EndPoint endpoint)
		{
			if (endpoint == null)
				throw new ArgumentNullException(nameof(endpoint));
			if (endpoint is DnsEndPoint dns)
			{
				return $"{dns.Host}:{dns.Port}";
			}
			return endpoint.ToString();
		}

		/// <summary>
		/// Convert an onion cat IPEndpoint to an onion DnsEndpoint
		/// If endpoint is already an onion DnsEndpoint, return it.
		/// Else returns null.
		/// </summary>
		/// <param name="endpoint"></param>
		/// <returns>An onion DNS endpoint or null</returns>
		public static DnsEndPoint AsOnionDNSEndpoint(this EndPoint endpoint)
		{
			TryConvertToOnionDNSEndpoint(endpoint, out var dns);
			return dns;
		}

		/// <summary>
		/// Convert an onion cat IPEndpoint to an onion DnsEndpoint
		/// If endpoint is already an onion DnsEndpoint, return it.
		/// If the endpoint is not an onion endpoint v2, return false.
		/// </summary>
		/// <param name="endpoint">The tor endpoint</param>
		/// <param name="dnsEndpoint">The onion dns enpoint</param>
		/// <returns>True if the onioncat address has been successfully parsed as a dns onion address</returns>
		public static bool TryConvertToOnionDNSEndpoint(this EndPoint endpoint, out DnsEndPoint dnsEndpoint)
		{
			if (endpoint == null)
				throw new ArgumentNullException(nameof(endpoint));
			if (endpoint is IPEndPoint ip)
			{
				var bytes = ip.Address.GetAddressBytes();
				if (((Utils.ArrayEqual(bytes, 0, pchOnionCat, 0, pchOnionCat.Length) ? 0 : 1) != 0))
				{
					dnsEndpoint = null;
					return false;
				}
				try
				{
					var onionHost = Encoders.Base32.EncodeData(bytes, pchOnionCat.Length, 16 - pchOnionCat.Length);
					dnsEndpoint = new DnsEndPoint($"{onionHost}.onion", ip.Port);
					return true;
				}
				catch
				{
					dnsEndpoint = null;
					return false;
				}
			}
			else if (endpoint is DnsEndPoint dns)
			{
				if (AsOnionCatIPEndpoint(dns) != null)
				{
					dnsEndpoint = dns;
					return true;
				}
			}
			dnsEndpoint = null;
			return false;
		}

		/// <summary>
		/// Convert an onion DNS endpoint to an onioncat IpEndpoint
		/// If endpoint is already an onioncat IPEndpoint, return it.
		/// Else returns null.
		/// </summary>
		/// <param name="endpoint"></param>
		/// <returns></returns>
		public static IPEndPoint AsOnionCatIPEndpoint(this EndPoint endpoint)
		{
			if (endpoint == null)
				throw new ArgumentNullException(nameof(endpoint));
			if (endpoint is IPEndPoint ip)
			{
				if (!IsTor(ip.Address))
					return null;
				return ip;
			}
			if (endpoint is DnsEndPoint dns)
			{
				if (!dns.Host.EndsWith(".onion", StringComparison.OrdinalIgnoreCase) || dns.Host.Length != 16 + 6)
					return null;
				var ipArray = new byte[16];
				try
				{
					var vchAddr = Encoders.Base32.DecodeData(dns.Host.Substring(0, dns.Host.Length - 6));
					if (vchAddr.Length != 16 - pchOnionCat.Length)
						return null;
					Array.Copy(pchOnionCat, ipArray, pchOnionCat.Length);
					Array.Copy(vchAddr, 0, ipArray, pchOnionCat.Length, vchAddr.Length);
					return new IPEndPoint(new IPAddress(ipArray), dns.Port);
				}
				catch
				{
					return null;
				}
			}
			return null;
		}

		/// <summary>
		/// <para>Will properly convert <paramref name="endpoint"/> to IPEndpoint
		/// If <paramref name="endpoint"/> is a DNSEndpoint is an onion host (Tor v2), it will be converted into onioncat address
		/// else, a DNS resolution will be made and all resolved addresses will be returned</para>
		/// <para>If <paramref name="endpoint"/> is a IPEndpoint, it will be returned as-is.</para>
		/// You can pass any endpoint parsed by <see cref="NBitcoin.Utils.ParseEndpoint(string, int)"/>
		/// </summary>
		/// <param name="endpoint">The endpoint to convert to IPEndpoint</param>
		/// <exception cref="System.ArgumentNullException">The endpoint is null</exception>
		/// <exception cref="System.Net.Sockets.SocketException">An error is encountered when resolving the dns name.</exception>
		/// <exception cref="System.NotSupportedException">The endpoint passed can't be converted into an Ip (eg. An onion host which is not TorV2)</exception>
		public static async Task<IPEndPoint[]> ResolveToIPEndpointsAsync(this EndPoint endpoint)
		{
			if (endpoint == null)
				throw new ArgumentNullException(nameof(endpoint));
			if (endpoint is IPEndPoint ip)
			{
				return new[] { ip };
			}
			else if (endpoint.AsOnionCatIPEndpoint() is IPEndPoint ip2)
			{
				return new[] { ip2 };
			}
			else if (endpoint is DnsEndPoint dns)
			{
				if (dns.IsTor())
					throw new NotSupportedException($"{endpoint} is not a Tor v2 address, and can't be converted into an IPEndpoint");
				var ips = await Dns.GetHostAddressesAsync(dns.Host).ConfigureAwait(false);
				return ips.Select(i => new IPEndPoint(i, dns.Port)).ToArray();
			}
			else
				throw new NotSupportedException(endpoint.ToString());
		}

		public static IPAddress EnsureIPv6(this IPAddress address)
		{
			if (address.AddressFamily == AddressFamily.InterNetworkV6)
				return address;
			return address.MapToIPv6Ex();
		}

		static bool? _IsRunningOnMono;
		public static bool IsRunningOnMono()
		{
			if (_IsRunningOnMono == null)
				_IsRunningOnMono = Type.GetType("Mono.Runtime") != null;
			return _IsRunningOnMono.Value;
		}

		public static IPAddress MapToIPv6Ex(this IPAddress address)
		{
#if CLASSICDOTNET
			return Compatibility.MapToIPv6(address);
#else
			return address.MapToIPv6();
#endif
		}
		public static IPAddress MapToIPv4Ex(this IPAddress address)
		{
#if CLASSICDOTNET
			return Compatibility.MapToIPv4(address);
#else
			return address.MapToIPv4();
#endif
		}
		public static IPEndPoint MapToIPv6Ex(this IPEndPoint endpoint)
		{
			if (endpoint == null)
				throw new ArgumentNullException(nameof(endpoint));
			if (endpoint.AddressFamily == AddressFamily.InterNetworkV6)
				return endpoint;
			var ipv6 = endpoint.Address.MapToIPv6Ex();
			return new IPEndPoint(ipv6, endpoint.Port);
		}
		public static bool IsIPv4MappedToIPv6Ex(this IPAddress address)
		{
#if CLASSICDOTNET
			return Compatibility.IsIPv4MappedToIPv6(address);
#else
			return address.IsIPv4MappedToIPv6;
#endif

		}

		readonly static byte[] pchLocal = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
		public static bool IsLocal(this IPAddress address)
		{
			address = address.EnsureIPv6();
			var bytes = address.GetAddressBytes();
			// IPv4 loopback
			if (address.IsIPv4() && (bytes[15 - 3] == 127 || bytes[15 - 3] == 0))
				return true;

			// IPv6 loopback (::1/128)
			if ((Utils.ArrayEqual(bytes, 0, pchLocal, 0, 16) ? 0 : 1) == 0)
				return true;

			return false;
		}

		public static bool IsMulticast(this IPAddress address)
		{
			address = address.EnsureIPv6();
			var bytes = address.GetAddressBytes();
			return (address.IsIPv4() && (bytes[15 - 3] & 0xF0) == 0xE0)
				   || (bytes[15 - 15] == 0xFF);
		}



		public static bool IsRoutable(this IPAddress address, bool allowLocal)
		{
			return address.IsValid() && !(
											(!allowLocal && address.IsRFC1918()) ||
											address.IsRFC3927() ||
											address.IsRFC4862() ||
											(address.IsRFC4193() && !address.IsTor()) ||
											address.IsRFC4843() || (!allowLocal && address.IsLocal())
											);
		}
		static readonly byte[] ipNone = new byte[16];
		static readonly byte[] inadddr_none = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
		static readonly byte[] ipNone_v4 = new byte[4];
		public static bool IsValid(this IPAddress address)
		{
			address = address.EnsureIPv6();
			var ip = address.GetAddressBytes();
			// unspecified IPv6 address (::/128)
			if ((Utils.ArrayEqual(ip, 0, ipNone, 0, 16) ? 0 : 1) == 0)
				return false;

			// documentation IPv6 address
			if (address.IsRFC3849())
				return false;

			if (address.IsIPv4())
			{
				//// INADDR_NONE
				if (Utils.ArrayEqual(ip, 12, inadddr_none, 0, 4))
					return false;

				//// 0
				if (Utils.ArrayEqual(ip, 12, ipNone_v4, 0, 4))
					return false;
			}

			return true;
		}
	}
}
#endif
