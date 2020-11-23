#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.DataEncoders;
using NBitcoin.BouncyCastle.Crypto.Digests;

namespace NBitcoin.Protocol
{	
	public class NetworkAddress : IBitcoinSerializable
	{
		public static readonly uint AddrV2Format = 0x20000000;

		public enum BIP155Network : byte
		{
			IPV4,  // 0x02 len: 4 IPv4 address (globally routed internet)
			IPV6,  // 0x02 len: 16 bytes - IPv6 address (globally routed internet)
			TorV2, // 0x03 len: 10 bytes - Tor v2 hidden service address
			TorV3, // 0x04 len: 32 bytes - Tor v3 hidden service address
			I2P,   // 0x05 len: 32 bytes - I2P overlay network address
			Cjdns, // 0x06 len: 16 bytes - Cjdns overlay network address
		}

		/// Size of IPv4 address (in bytes).
		static readonly int ADDR_IPV4_SIZE = 4;

		/// Size of IPv6 address (in bytes).
		static readonly int ADDR_IPV6_SIZE = 16;

		/// Size of TORv2 address (in bytes).
		static readonly int ADDR_TORV2_SIZE = 10;

		/// Size of TORv3 address (in bytes). This is the length of just the address
		/// as used in BIP155, without the checksum and the version byte.
		static readonly int ADDR_TORV3_SIZE = 32;

		/// Size of I2P address (in bytes).
		static readonly int ADDR_I2P_SIZE = 32;

		/// Size of CJDNS address (in bytes).
		static readonly int ADDR_CJDNS_SIZE = 16;

		/// Size of "internal" (NET_INTERNAL) address (in bytes).
		static readonly int ADDR_INTERNAL_SIZE = 10;

		/// Size of the TORv3 address checksum (in bytes)
		static readonly int TORV3_ADDR_CHECKSUM_LEN = 2;

		/// Size of the TORv3 address version number (in bytes)
		static readonly int TORV3_ADDR_VERSION_LEN = 1;

		/// Prefix of an IPv6 address when it contains an embedded IPv4 address.
		/// Used when (un)serializing addresses in ADDRv1 format (pre-BIP155).
		static readonly byte[] IPV4_IN_IPV6_PREFIX = new byte[]{
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF
		};

		/// Prefix of an IPv6 address when it contains an embedded TORv2 address.
		/// Used when (un)serializing addresses in ADDRv1 format (pre-BIP155).
		/// Such dummy IPv6 addresses are guaranteed to not be publicly routable as they
		/// fall under RFC4193's fc00::/7 subnet allocated to unique-local addresses.
		static readonly byte[] TORV2_IN_IPV6_PREFIX = new byte[]{
			0xFD, 0x87, 0xD8, 0x7E, 0xEB, 0x43
		};

		/// Prefix of an IPv6 address when it contains an embedded "internal" address.
		/// Used when (un)serializing addresses in ADDRv1 format (pre-BIP155).
		/// The prefix comes from 0xFD + SHA256("bitcoin")[0:5].
		/// Such dummy IPv6 addresses are guaranteed to not be publicly routable as they
		/// fall under RFC4193's fc00::/7 subnet allocated to unique-local addresses.
		static readonly byte[] INTERNAL_IN_IPV6_PREFIX = new byte[]{
			0xFD, 0x6B, 0x88, 0xC0, 0x87, 0x24 // 0xFD + sha256("bitcoin")[0:5].
		};

        /**
         * Maximum size of an address as defined in BIP155 (in bytes).
         * This is only the size of the address, not the entire CNetAddr object
         * when serialized.
         */
        static readonly int MAX_ADDRV2_SIZE = 512;

		static readonly byte[] IPV6_NONE = new byte[ADDR_IPV6_SIZE];
		static readonly byte[] IPV4_NONE = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
		static readonly byte[] IPV4_ANY = new byte[ADDR_IPV4_SIZE];


		public NetworkAddress()
		{
		}
		public NetworkAddress(IPAddress ip)
		{
			Endpoint = new IPEndPoint(ip, 0);
		}
		public NetworkAddress(IPEndPoint endpoint)
		{
			Endpoint = endpoint;
		}
		public NetworkAddress(IPAddress address, int port)
		{
			Endpoint = new IPEndPoint(address, port);
		}

		uint version = 100100;

		internal uint ntime;
		byte network;

		public bool IsIPv4 => (BIP155Network)network == BIP155Network.IPV4;
		public bool IsIPv6 => (BIP155Network)network == BIP155Network.IPV6;
		public bool IsTORv2 => (BIP155Network)network == BIP155Network.TorV2;
		public bool IsTORv3 => (BIP155Network)network == BIP155Network.TorV3;
		public bool IsOnion => IsTORv2 || IsTORv3;
		public bool IsI2P => (BIP155Network)network == BIP155Network.I2P;
		public bool IsCjdns => (BIP155Network)network == BIP155Network.Cjdns;

		public bool IsLocal 
		{
			get
			{
			    // IPv4 loopback (127.0.0.0/8 or 0.0.0.0/8)
				if (IsIPv4 && (addr[0] == 127 || addr[0] == 0))
				{
					return true;
				}
			    // IPv6 loopback (::1/128)
    			if (IsIPv6 && Utils.ArrayEqual(addr, new byte[] {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1}))
				{
			        return true;
    			}

    			return false;
			}
		}

		public bool IsRFC1918 =>
			IsIPv4 && (
				addr[0] == 10 ||
				(addr[0] == 192 && addr[1] == 168) ||
				(addr[0] == 172 && (addr[1] >= 16 && addr[1] <= 31)));

		public bool IsRFC3927 =>
			IsIPv4 && (addr[0] == 169 && addr[1] == 254);

		public bool IsRFC3849 =>
			IsIPv6 && addr[0] == 0x20 && addr[1] == 0x01 && addr[2] == 0x0D && addr[3] == 0xB8;

		public bool IsRFC3964 =>
			IsIPv6 && (addr[0] == 0x20 && addr[1] == 0x02);

		readonly static byte[] pchRFC6052 = new byte[] { 0, 0x64, 0xFF, 0x9B, 0, 0, 0, 0, 0, 0, 0, 0 };
		public bool IsRFC6052 =>
			IsIPv6 && ((Utils.ArrayEqual(addr, 0, pchRFC6052, 0, pchRFC6052.Length) ? 0 : 1) == 0);

		public bool IsRFC4380 =>
			IsIPv6 && (addr[0] == 0x20 && addr[1] == 0x01 && addr[2] == 0 && addr[3] == 0);

		readonly static byte[] pchRFC4862 = new byte[] { 0xFE, 0x80, 0, 0, 0, 0, 0, 0 };
		public bool IsRFC4862 =>
			IsIPv6 && ((Utils.ArrayEqual(addr, 0, pchRFC4862, 0, pchRFC4862.Length) ? 0 : 1) == 0);

		public bool IsRFC4193 =>
			IsIPv6 && ((addr[0] & 0xFE) == 0xFC);

		readonly static byte[] pchRFC6145 = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0xFF, 0xFF, 0, 0 };
		public bool IsRFC6145 =>
			IsIPv6 && ((Utils.ArrayEqual(addr, 0, pchRFC6145, 0, pchRFC6145.Length) ? 0 : 1) == 0);

		public bool IsRFC4843 =>
			IsIPv6 && (addr[0] == 0x20 && addr[1] == 0x01 && addr[2] == 0x00 && (addr[3] & 0xF0) == 0x10);


/*
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
*/

		ulong service = 1;
		byte[] addr = new byte[ADDR_IPV6_SIZE];
		ushort port;

		public void SetLegacyIpv6(byte[] ipv6)
		{
			if (ipv6.Length != ADDR_IPV6_SIZE)
			{
				throw new ArgumentException("The received value is not a valid IPv6 byte array.", nameof(ipv6));
			}

			var skip = 0;
			if (HasPrefix(ipv6, IPV4_IN_IPV6_PREFIX))
			{
				network = (byte)BIP155Network.IPV4;
				skip = IPV4_IN_IPV6_PREFIX.Length;
			}
			else if(HasPrefix(ipv6, TORV2_IN_IPV6_PREFIX))
			{
				// TORv2-in-IPv6
				network = (byte)BIP155Network.TorV2;
				skip = TORV2_IN_IPV6_PREFIX.Length;
			} 
			else if (HasPrefix(ipv6, INTERNAL_IN_IPV6_PREFIX))
			{
				// Internal-in-IPv6
				//network = NET_INTERNAL;
				skip = INTERNAL_IN_IPV6_PREFIX.Length;
			} 
			else
			{
				// IPv6
				network = (byte)BIP155Network.IPV6;
    		}	

			addr = ipv6;
		}

		public bool SetSpecial(string address)
		{
			if (!address.EndsWith(".onion"))
			{
				return false;
			}

			byte[] bytes;
			try
			{
				bytes = Encoders.Base32.DecodeData(address.Substring(0, address.Length - ".onion".Length));
			}
			catch(FormatException)
			{
				return false;
			}

			if (bytes.Length == ADDR_TORV2_SIZE)
			{
				network = (byte)BIP155Network.TorV2;
				addr = bytes;
				return true;
			}
			else if(bytes.Length == ADDR_TORV3_SIZE  + TORV3_ADDR_CHECKSUM_LEN + TORV3_ADDR_VERSION_LEN)
			{
				var version = bytes[bytes.Length - 1];

				if (version != 3)
				{
					return false;
				}

				var pubkey = bytes.SafeSubarray( 0, ADDR_TORV3_SIZE);
				var chksum = bytes.SafeSubarray(ADDR_TORV3_SIZE,  TORV3_ADDR_CHECKSUM_LEN);

				var calculatedChecksum = CalculateChecksum(pubkey);

				if (!Utils.ArrayEqual(chksum, calculatedChecksum) )
				{
					return false;
				}

				network = (byte)BIP155Network.TorV3;
				addr = pubkey;
				return true;
			}
			return false;
		}

		private static byte[] CalculateChecksum(byte[] pubkey)
		{
			// TORv3 CHECKSUM = H(".onion checksum" | PUBKEY | VERSION)[:2]
			var prefix = Encoding.UTF8.GetBytes(".onion checksum");

			var hasher = new Sha3Digest(256);

			hasher.BlockUpdate(prefix, 0, prefix.Length);
			hasher.BlockUpdate(pubkey, 0, pubkey.Length);
			hasher.BlockUpdate(new byte[]{3}, 0, 1);

			var fullChecksum = new byte[hasher.GetByteLength()];
			var size = hasher.DoFinal(fullChecksum, 0);
			return fullChecksum.SafeSubarray(0, TORV3_ADDR_CHECKSUM_LEN);
		}

		public ulong Service
		{
			get
			{
				return service;
			}
			set
			{
				service = value;
			}
		}

		public TimeSpan Ago
		{
			get
			{
				return DateTimeOffset.UtcNow - Time;
			}
			set
			{
				Time = DateTimeOffset.UtcNow - value;
			}
		}

		public void Adjust()
		{
			var nNow = Utils.DateTimeToUnixTime(DateTimeOffset.UtcNow);
			if (ntime <= 100000000 || ntime > nNow + 10 * 60)
				ntime = nNow - 5 * 24 * 60 * 60;
		}

		public IPEndPoint Endpoint
		{
			get
			{
				return new IPEndPoint(new IPAddress(addr), port);
			}
			set
			{
				port = (ushort)value.Port;
				var ipBytes = value.Address.GetAddressBytes();
				if (ipBytes.Length == ADDR_IPV6_SIZE)
				{
					addr = ipBytes;
					network = (byte)BIP155Network.IPV6;
				}
				else if (ipBytes.Length == ADDR_IPV4_SIZE)
				{
					//Convert to ipv4 mapped to ipv6
					//In these addresses, the first 80 bits are zero, the next 16 bits are one, and the remaining 32 bits are the IPv4 address
					addr = new byte[ADDR_IPV6_SIZE];
					Array.Copy(ipBytes, 0, addr, ADDR_IPV6_SIZE - ADDR_IPV4_SIZE, ADDR_IPV4_SIZE);
					Array.Copy(new byte[] { 0xFF, 0xFF }, 0, addr, ADDR_IPV6_SIZE - ADDR_IPV4_SIZE - 2, 2);
					network = (byte)BIP155Network.IPV4;
				}
				else
					throw new NotSupportedException("Invalid IP address type");
			}
		}

		public DateTimeOffset Time
		{
			get
			{
				return Utils.UnixTimeToDateTime(ntime);
			}
			set
			{
				ntime = Utils.DateTimeToUnixTime(value);
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if (stream.Type == SerializationType.Disk)
			{
				stream.ReadWrite(ref version);
			}
			var useV2Format = stream.ProtocolVersion is {} protcocolVersion && (protcocolVersion & AddrV2Format) != 0;
			if (
				stream.Type == SerializationType.Disk ||
				(stream.ProtocolCapabilities.SupportTimeAddress && stream.Type != SerializationType.Hash))
			{
				if (useV2Format)
				{
					stream.ReadWriteAsVarInt(ref ntime);
				}
				else
				{
					stream.ReadWrite(ref ntime);
				}
			}

			if (useV2Format)
			{
				stream.ReadWriteAsVarInt(ref service);
				stream.ReadWrite(ref network);
				stream.ReadWriteAsVarString(ref addr);
			}
			else
			{
				stream.ReadWrite(ref service);
				if (stream.Serializing)
				{
					stream.ReadWrite(ref addr);
				}
				else
				{
					var localAddr = new byte[ADDR_IPV6_SIZE];
					stream.ReadWrite(ref localAddr);
					SetLegacyIpv6(localAddr);
				}
			}
			using (stream.BigEndianScope())
			{
				stream.ReadWrite(ref port);
			}
		}

		#endregion


		// Serialize in pre-ADDRv2/BIP155 format to an array.
		private byte[] SerializeV1Array() =>
			(((BIP155Network)network) switch 
			{
				BIP155Network.IPV6  => addr,
				BIP155Network.IPV4  => IPV4_IN_IPV6_PREFIX.Concat(addr),
				BIP155Network.TorV2 => TORV2_IN_IPV6_PREFIX.Concat(addr),
				BIP155Network.TorV3 => new byte[ADDR_TORV3_SIZE],
				BIP155Network.I2P   => new byte[ADDR_I2P_SIZE],
				BIP155Network.Cjdns => new byte[ADDR_CJDNS_SIZE],
			}).ToArray();

		public void ZeroTime()
		{
			this.ntime = 0;
		}

		public bool IsAddrV1Compatible =>
			(BIP155Network)network switch 
			{
				BIP155Network.IPV4 => true,
				BIP155Network.IPV6 => true,
				BIP155Network.TorV2 => true,
				BIP155Network.TorV3 => false,
				BIP155Network.I2P => false,
				BIP155Network.Cjdns => false,
				_ => throw new InvalidOperationException("Unknown Address network.")
			};

		public byte[] GetAddressBytes() =>
			IsAddrV1Compatible ? SerializeV1Array() : addr;

		public bool IsValid 
		{
			get
			{
				// unspecified IPv6 address (::/128)
				if (IsIPv6 && !Utils.ArrayEqual(addr, 0, IPV6_NONE, 0, 16))
				{
					return false;
				}

				// documentation IPv6 address
				if (IsRFC3849)
				{
					return false;
				}

				if (IsIPv4)
				{
					//// INADDR_NONE
					if (Utils.ArrayEqual(addr, IPV4_NONE))
						return false;

					//// INADDR_ANY
					if (Utils.ArrayEqual(addr, IPV4_ANY))
						return false;
				}

				return true;
			}
		}

		public bool IsRoutable( bool allowLocal) =>
			IsValid && !(
				(!allowLocal && IsRFC1918) || IsRFC3927 || IsRFC4862 ||	(IsRFC4193 && !IsOnion) || IsRFC4843 || (!allowLocal && IsLocal));

		public byte[] GetGroup()
		{
			List<byte> vchRet = new List<byte>();
			int nClass = 2;
			int nStartByte = 0;
			int nBits = 16;

			// all local addresses belong to the same group
			if (IsLocal)
			{
				nClass = 255;
				nBits = 0;
			}

			// all unroutable addresses belong to the same group
			if (!IsRoutable(true))
			{
				nClass = 0;
				nBits = 0;
			}
			// for IPv4 addresses, '1' + the 16 higher-order bits of the IP
			// includes mapped IPv4, SIIT translated IPv4, and the well-known prefix
			else if (IsIPv4 || IsRFC6145 || IsRFC6052)
			{
				nClass = 1;
				nStartByte = 12;
			}
			// for 6to4 tunnelled addresses, use the encapsulated IPv4 address
			else if (IsRFC3964)
			{
				nClass = 1;
				nStartByte = 2;
			}
			// for Teredo-tunnelled IPv6 addresses, use the encapsulated IPv4 address

			else if (IsRFC4380)
			{
				vchRet.Add(1);
				vchRet.Add((byte)(addr[12] ^ 0xFF));
				vchRet.Add((byte)(addr[13] ^ 0xFF));
				return vchRet.ToArray();
			}
			else if (IsOnion)
			{
				nClass = 3;
				nStartByte = 6;
				nBits = 4;
			}
			// for he.net, use /36 groups
			else if (addr[0] == 0x20 && addr[1] == 0x01 && addr[2] == 0x04 && addr[3] == 0x70)
			{
				nBits = 36;
			}
			// for the rest of the IPv6 network, use /32 groups
			else
			{
				nBits = 32;
			}

			vchRet.Add((byte)nClass);
			while (nBits >= 8)
			{
				vchRet.Add(addr[nStartByte]);
				nStartByte++;
				nBits -= 8;
			}
			if (nBits > 0)
			{
				vchRet.Add((byte)(addr[nStartByte] | ((1 << nBits) - 1)));
			}
			return vchRet.ToArray();
		}

        public override string ToString()
        {
			switch ((BIP155Network)network)
			{
				case BIP155Network.IPV4:
				case BIP155Network.IPV6:
				case BIP155Network.Cjdns:
					return new IPAddress(addr).ToString();
				case BIP155Network.TorV2:
					return Encoders.Base32.EncodeData(addr) + ".onion";
				case BIP155Network.TorV3:
					var chksum = CalculateChecksum(addr);
					var data = addr.Concat(chksum, new byte[]{ 3 });
					return Encoders.Base32.EncodeData(data) + ".onion";
				case BIP155Network.I2P:
					return Encoders.Base32.EncodeData(addr) + ".b32.i2p";
				default:
					throw new InvalidOperationException($"{network} is unknown");					
			}
        }
        
		internal byte[] GetKey()
		{
			var buffer = GetAddressBytes();
			var vKey = new byte[buffer.Length + 2];
			Array.Copy(buffer, vKey, buffer.Length);
			vKey[buffer.Length - 2] = (byte)(port / 0x100);
			vKey[buffer.Length - 1] = (byte)(port & 0x0FF);
			return vKey;
		}

		internal byte[] EnsureIPv6()
		{
			if (IsIPv6 || IsTORv2)
			{
				return addr;
			}
			else if (IsIPv4)
			{
				return new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xFF, 0xFF, addr[0], addr[1], addr[2], addr[3] };
			}
			else
			{
				throw new Exception("Only IP addresses can be converted to IPv6");
			}
		}

		private bool HasPrefix(byte[] arr, byte[] prefix) =>
			Utils.ArrayEqual(arr, 0, prefix, 0, prefix.Length);
	}
}
#endif