#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace NBitcoin
{
	public static class IpExtensions
	{
#if WIN
		interface ICompatibility
		{
			IPAddress MapToIPv6(IPAddress address);
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

		public static bool IsRFC6052(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			byte[] pchRFC6052 = new byte[] { 0, 0x64, 0xFF, 0x9B, 0, 0, 0, 0, 0, 0, 0, 0 };
			return ((Utils.ArrayEqual(bytes, 0, pchRFC6052, 0, pchRFC6052.Length) ? 0 : 1) == 0);
		}

		public static bool IsRFC4380(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			return (bytes[15 - 15] == 0x20 && bytes[15 - 14] == 0x01 && bytes[15 - 13] == 0 && bytes[15 - 12] == 0);
		}

		public static bool IsRFC4862(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			byte[] pchRFC4862 = new byte[] { 0xFE, 0x80, 0, 0, 0, 0, 0, 0 };
			return ((Utils.ArrayEqual(bytes, 0, pchRFC4862, 0, pchRFC4862.Length) ? 0 : 1) == 0);
		}

		public static bool IsRFC4193(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			return ((bytes[15 - 15] & 0xFE) == 0xFC);
		}

		public static bool IsRFC6145(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			byte[] pchRFC6145 = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0xFF, 0xFF, 0, 0 };
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
			if(address.IsLocal())
			{
				nClass = 255;
				nBits = 0;
			}

			// all unroutable addresses belong to the same group
			if(!address.IsRoutable(true))
			{
				nClass = 0;
				nBits = 0;
			}
			// for IPv4 addresses, '1' + the 16 higher-order bits of the IP
			// includes mapped IPv4, SIIT translated IPv4, and the well-known prefix
			else if(address.IsIPv4() || address.IsRFC6145() || address.IsRFC6052())
			{
				nClass = 1;
				nStartByte = 12;
			}
			// for 6to4 tunnelled addresses, use the encapsulated IPv4 address
			else if(address.IsRFC3964())
			{
				nClass = 1;
				nStartByte = 2;
			}
			// for Teredo-tunnelled IPv6 addresses, use the encapsulated IPv4 address

			else if(address.IsRFC4380())
			{
				vchRet.Add(1);
				vchRet.Add((byte)(bytes[15 - 3] ^ 0xFF));
				vchRet.Add((byte)(bytes[15 - 2] ^ 0xFF));
				return vchRet.ToArray();
			}
			else if(address.IsTor())
			{
				nClass = 3;
				nStartByte = 6;
				nBits = 4;
			}
			// for he.net, use /36 groups
			else if(bytes[15 - 15] == 0x20 && bytes[15 - 14] == 0x01 && bytes[15 - 13] == 0x04 && bytes[15 - 12] == 0x70)
				nBits = 36;
			// for the rest of the IPv6 network, use /32 groups
			else
				nBits = 32;

			vchRet.Add((byte)nClass);
			while(nBits >= 8)
			{
				vchRet.Add(bytes[15 - (15 - nStartByte)]);
				nStartByte++;
				nBits -= 8;
			}
			if(nBits > 0)
				vchRet.Add((byte)(bytes[15 - (15 - nStartByte)] | ((1 << nBits) - 1)));

			return vchRet.ToArray();
		}

		static byte[] pchOnionCat = new byte[] { 0xFD, 0x87, 0xD8, 0x7E, 0xEB, 0x43 };
		public static bool IsTor(this IPAddress address)
		{
			var bytes = address.GetAddressBytes();
			return ((Utils.ArrayEqual(bytes, 0, pchOnionCat, 0, pchOnionCat.Length) ? 0 : 1) == 0);
		}
		public static IPAddress EnsureIPv6(this IPAddress address)
		{
			if(address.AddressFamily == AddressFamily.InterNetworkV6)
				return address;
			return address.MapToIPv6Ex();
		}

		static bool? _IsRunningOnMono;
		public static bool IsRunningOnMono()
		{
			if(_IsRunningOnMono == null)
				_IsRunningOnMono = Type.GetType("Mono.Runtime") != null;
			return _IsRunningOnMono.Value;
		}

		public static IPAddress MapToIPv6Ex(this IPAddress address)
		{
#if WIN
			return Compatibility.MapToIPv6(address);
#else
			return Utils.MapToIPv6(address);
#endif
		}
		public static bool IsIPv4MappedToIPv6Ex(this IPAddress address)
		{
#if WIN
			return Compatibility.IsIPv4MappedToIPv6(address);
#else
			return Utils.IsIPv4MappedToIPv6(address);
#endif

		}

		public static bool IsLocal(this IPAddress address)
		{
			address = address.EnsureIPv6();
			var bytes = address.GetAddressBytes();
			// IPv4 loopback
			if(address.IsIPv4() && (bytes[15 - 3] == 127 || bytes[15 - 3] == 0))
				return true;

			// IPv6 loopback (::1/128)
			byte[] pchLocal = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
			if((Utils.ArrayEqual(bytes, 0, pchLocal, 0, 16) ? 0 : 1) == 0)
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
		public static bool IsValid(this IPAddress address)
		{
			address = address.EnsureIPv6();
			var ip = address.GetAddressBytes();
			// unspecified IPv6 address (::/128)
			byte[] ipNone = new byte[16];
			if((Utils.ArrayEqual(ip, 0, ipNone, 0, 16) ? 0 : 1) == 0)
				return false;

			// documentation IPv6 address
			if(address.IsRFC3849())
				return false;

			if(address.IsIPv4())
			{
				//// INADDR_NONE
				if(Utils.ArrayEqual(ip, 12, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, 0, 4))
					return false;

				//// 0
				if(Utils.ArrayEqual(ip, 12, new byte[] { 0x0, 0x0, 0x0, 0x0 }, 0, 4))
					return false;
			}

			return true;
		}
	}
}
#endif