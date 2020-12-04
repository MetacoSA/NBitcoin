#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.DataEncoders;
using NBitcoin.BouncyCastle.Crypto.Digests;
using NBitcoin.Crypto;

namespace NBitcoin.Protocol
{
		public class NetworkAddress : IBitcoinSerializable, IEquatable<NetworkAddress>
	{
		public static readonly uint AddrV2Format = 0x20000000;

		// A network type.
		// @note An address may belong to more than one network, for example `10.0.0.1`
		// belongs to both `NET_UNROUTABLE` and `NET_IPV4`.
		// Keep these sequential starting from 0 and `NET_MAX` as the last entry.
		protected enum Network
		{
			/// Addresses from these networks are not publicly routable on the global Internet.
			Unroutable = 0,
			IPv4,
			IPv6,
			/// TOR (v2 or v3)
			Onion,
			/// I2P
			I2P,
			/// CJDNS
			Cjdns,
			/// A set of addresses that represent the hash of a string or FQDN. We use
			/// them in CAddrMan to keep track of which DNS seeds were used.
			Internal,
			Max,
		};

		enum BIP155Network : byte
		{
			IPv4 = 1,  // 0x02 len: 4 IPv4 address (globally routed internet)
			IPv6,  // 0x02 len: 16 bytes - IPv6 address (globally routed internet)
			TORv2, // 0x03 len: 10 bytes - Tor v2 hidden service address
			TORv3, // 0x04 len: 32 bytes - Tor v3 hidden service address
			I2P,   // 0x05 len: 32 bytes - I2P overlay network address
			Cjdns, // 0x06 len: 16 bytes - Cjdns overlay network address
		}

		/// Size of IPv4 address (in bytes).
		protected const int ADDR_IPV4_SIZE = 4;

		/// Size of IPv6 address (in bytes).
		protected  const int ADDR_IPV6_SIZE = 16;

		/// Size of TORv2 address (in bytes).
		const int ADDR_TORV2_SIZE = 10;

		/// Size of TORv3 address (in bytes). This is the length of just the address
		/// as used in BIP155, without the checksum and the version byte.
		const int ADDR_TORV3_SIZE = 32;

		/// Size of I2P address (in bytes).
		const int ADDR_I2P_SIZE = 32;

		/// Size of CJDNS address (in bytes).
		const int ADDR_CJDNS_SIZE = 16;

		/// Size of "internal" (NET_INTERNAL) address (in bytes).
		const int ADDR_INTERNAL_SIZE = 10;

		/// Size of the TORv3 address checksum (in bytes)
		const int TORV3_ADDR_CHECKSUM_LEN = 2;

		/// Size of the TORv3 address version number (in bytes)
		const int TORV3_ADDR_VERSION_LEN = 1;


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

		/// Prefix of an IPv6 Hurricane Electric address.
		static readonly byte[] HE_PREFIX = new byte[]{
			0x20, 0x01, 0x04, 0x70
		};

		/**
			* Maximum size of an address as defined in BIP155 (in bytes).
			* This is only the size of the address, not the entire CNetAddr object
			* when serialized.
			*/
		const int MAX_ADDRV2_SIZE = 512;

		static readonly byte[] IPV6_NONE = new byte[ADDR_IPV6_SIZE];
		static readonly byte[] IPV4_NONE = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
		static readonly byte[] IPV4_ANY = new byte[ADDR_IPV4_SIZE];

		protected byte network = (byte)Network.IPv6;
		protected byte[] addr = new byte[ADDR_IPV6_SIZE];

		public NetworkAddress()
		{
		}

		public NetworkAddress(IPAddress ip)
		{
			SetIp(ip);
		}

		public bool IsIPv4 => (Network)network == Network.IPv4;
		public bool IsIPv6 => (Network)network == Network.IPv6;
		public bool IsTor => (Network)network == Network.Onion;
		public bool IsI2P => (Network)network == Network.I2P;
		public bool IsCjdns => (Network)network == Network.Cjdns;

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

		public bool HasLinkedIPv4 =>
			IsRoutable(false) && (IsIPv4 || IsRFC6145 || IsRFC6052 || IsRFC3964 || IsRFC4380);

		public int GetLinkedIPv4()
		{
			int ReadBE32(byte[] data, int offset = 0) =>
				BitConverter.ToInt32(data, offset);

			if (IsIPv4)
			{
				return ReadBE32(addr);
			}
			else if (IsRFC6052 || IsRFC6145)
			{
				// mapped IPv4, SIIT translated IPv4: the IPv4 address is the last 4 bytes of the address
				return ReadBE32(addr, ADDR_IPV6_SIZE - ADDR_IPV4_SIZE);
			}
			else if (IsRFC3964)
			{
				// 6to4 tunneled IPv4: the IPv4 address is in bytes 2-6
				return ReadBE32(addr, 2);
			}
			else if (IsRFC4380)
			{
				// Teredo tunneled IPv4: the IPv4 address is in the last 4 bytes of the address, but bitflipped
				return ~ReadBE32(addr, ADDR_IPV6_SIZE - ADDR_IPV4_SIZE);
			}
			throw new Exception("Network Address is not IPv4 linked.");
		}

		public bool IsHeNet =>
			IsIPv6 && HasPrefix(addr, HE_PREFIX);

		public bool IsInternal =>
			(Network)network == Network.Internal;

		protected void SetIp(IPAddress ip)
		{
			SetLegacyIpv6(ip.MapToIPv6().GetAddressBytes());
		}

		public void SetLegacyIpv6(byte[] ipv6)
		{
			if (ipv6.Length != ADDR_IPV6_SIZE)
			{
				throw new ArgumentException("The received value is not a valid IPv6 byte array.", nameof(ipv6));
			}

			var skip = 0;
			if (HasPrefix(ipv6, IPV4_IN_IPV6_PREFIX))
			{
				network = (byte)Network.IPv4;
				skip = IPV4_IN_IPV6_PREFIX.Length;
			}
			else if(HasPrefix(ipv6, TORV2_IN_IPV6_PREFIX))
			{
				// TORv2-in-IPv6
				network = (byte)Network.Onion;
				skip = TORV2_IN_IPV6_PREFIX.Length;
			} 
			else if (HasPrefix(ipv6, INTERNAL_IN_IPV6_PREFIX))
			{
				// Internal-in-IPv6
				network = (byte)Network.Internal;
				skip = INTERNAL_IN_IPV6_PREFIX.Length;
			} 
			else
			{
				// IPv6
				network = (byte)Network.IPv6;
			}	

			addr = ipv6.Skip(skip).ToArray();
		}

		// Create an "internal" address that represents a name or FQDN. CAddrMan uses
		// these fake addresses to keep track of which DNS seeds were used.
		// @returns Whether or not the operation was successful.
		// @see NET_INTERNAL, INTERNAL_IN_IPV6_PREFIX, CNetAddr::IsInternal(), CNetAddr::IsRFC4193()
		public bool SetInternal(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return false;
			}
			network = (byte)Network.Internal;
			var hash = Hashes.SHA256(Encoding.UTF8.GetBytes(name));
			addr = hash.SafeSubarray(0, ADDR_INTERNAL_SIZE);
			return true;
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
				network = (byte)Network.Onion;
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

				network = (byte)Network.Onion;
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


		#region IBitcoinSerializable Members

		public virtual void ReadWrite(BitcoinStream stream)
		{
			var useV2Format = stream.ProtocolVersion is {} protcocolVersion && (protcocolVersion & AddrV2Format) != 0;
			if (useV2Format)
			{
				if (stream.Serializing)
				{
					var bip155net = (byte)GetBIP155Network();
					stream.ReadWrite(bip155net);
					stream.ReadWriteAsVarString(ref addr);
				}
				else
				{
					stream.ReadWrite(ref network);
					stream.ReadWriteAsVarString(ref addr);
					
					if (SetNetFromBIP155Network((BIP155Network)network, addr.Length))
					{
						if (HasPrefix(addr, IPV4_IN_IPV6_PREFIX) || HasPrefix(addr, TORV2_IN_IPV6_PREFIX))
						{
							// set to invalid because IPv4 and Torv2 should not be embeded in IPv6.
							addr = IPV6_NONE;
							network = (byte)Network.IPv6;
						}
					}
					else
					{
						// set to invalid because IPv4 and Torv2 should not be embeded in IPv6.
						addr = IPV6_NONE;
						network = (byte)Network.IPv6;
					}
				}
			}
			else
			{
				if (stream.Serializing)
				{
					var localAddr = IsAddrV1Compatible ? SerializeV1Array() : IPV6_NONE;
					stream.ReadWrite(ref localAddr);
				}
				else
				{
					var localAddr = new byte[ADDR_IPV6_SIZE];
					stream.ReadWrite(ref localAddr);
					SetLegacyIpv6(localAddr);
				}
			}
		}

		#endregion

		private BIP155Network GetBIP155Network()
		{
			switch ((Network)network) 
			{
				case Network.IPv4:
					return BIP155Network.IPv4;
				case Network.IPv6:
					return BIP155Network.IPv6;
				case Network.Onion:
					if (addr.Length == ADDR_TORV2_SIZE)
					{
						return BIP155Network.TORv2;
					}
					else if (addr.Length == ADDR_TORV3_SIZE)
					{
						return BIP155Network.TORv3;
					}
					throw new Exception("Onion address size doesn't match the TORv2 nor TORv3 sizes.");
				case Network.I2P:
					return BIP155Network.I2P;
				case Network.Cjdns:
					return BIP155Network.Cjdns;
				case Network.Internal:   // should have been handled before calling this function
				case Network.Unroutable: // m_net is never and should not be set to NET_UNROUTABLE
				case Network.Max:        // m_net is never and should not be set to NET_MAX
					throw new Exception("Unknown network.");
			} // no default case, so the compiler can warn about missing cases
			throw new Exception("Unknown network.");
		}

		private bool SetNetFromBIP155Network(BIP155Network bip155Network, int length)
		{
			byte AssignIfCorrectSize(Network net, int expectedSize)
			{
				if (length != expectedSize)
				{
					throw new Exception($"BIP155 {(BIP155Network)bip155Network} address with length {length} (should be {expectedSize}).");
				}
				return (byte)net;
			}

			switch(bip155Network)
			{
				case BIP155Network.IPv4:
					network = AssignIfCorrectSize(Network.IPv4, ADDR_IPV4_SIZE);
					break;
				case BIP155Network.IPv6:
					network = AssignIfCorrectSize(Network.IPv6, ADDR_IPV6_SIZE);
					break;
				case BIP155Network.TORv2:
					network = AssignIfCorrectSize(Network.Onion, ADDR_TORV2_SIZE);
					break;
				case BIP155Network.TORv3:
					network = AssignIfCorrectSize(Network.Onion, ADDR_TORV3_SIZE);
					break;
				case BIP155Network.I2P:
					network = AssignIfCorrectSize(Network.I2P, ADDR_I2P_SIZE);
					break;
				case BIP155Network.Cjdns:
					network = AssignIfCorrectSize(Network.Cjdns, ADDR_CJDNS_SIZE);
					break;
				default:
					return false;
			}
			return true;
		}


		// Serialize in pre-ADDRv2/BIP155 format to an array.
		private byte[] SerializeV1Array() =>
			(((Network)network) switch 
			{
				Network.IPv6  => addr,
				Network.IPv4  => IPV4_IN_IPV6_PREFIX.Concat(addr),
				Network.Onion when addr.Length == ADDR_TORV3_SIZE => new byte[ADDR_TORV3_SIZE],
				Network.Onion => TORV2_IN_IPV6_PREFIX.Concat(addr),
				Network.Internal => INTERNAL_IN_IPV6_PREFIX.Concat(addr), 
				Network.I2P   => new byte[ADDR_I2P_SIZE],
				Network.Cjdns => new byte[ADDR_CJDNS_SIZE],
				_ => throw new InvalidOperationException("Unknown Address network.") 
			}).ToArray();

		public bool IsAddrV1Compatible =>
			((Network)network, addr.Length) switch 
			{
				(Network.IPv4, _) => true,
				(Network.IPv6, _) => true,
				(Network.Internal, _) => true,
				(Network.Onion, ADDR_TORV2_SIZE) => true,
				(Network.Onion, ADDR_TORV3_SIZE) => false,
				(Network.I2P, _ ) => false,
				(Network.Cjdns,_) => false,
				_ => throw new InvalidOperationException("Unknown Address network.")
			};

		public byte[] GetAddressBytes() => 
			IsAddrV1Compatible ? SerializeV1Array() : addr;

		public bool IsValid 
		{
			get
			{
				// unspecified IPv6 address (::/128)
				if (IsIPv6 && Utils.ArrayEqual(addr, 0, IPV6_NONE, 0, 16))
				{
					return false;
				}

				// documentation IPv6 address
				if (IsRFC3849)
				{
					return false;
				}

				if (IsInternal)
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
				(!allowLocal && IsRFC1918) || IsRFC3927 || IsRFC4862 ||	(IsRFC4193 && !IsTor) || IsRFC4843 || (!allowLocal && IsLocal) || IsInternal);

		public byte[] GetGroup()
		{
			var vchRet = new List<byte>();

			var netClass = GetNetClass();
			vchRet.Add((byte)netClass);
			int nBits = 0;

			if (IsLocal)
			{
				// all local addresses belong to the same group
			}
			else if (IsInternal)
			{
				// all internal-usage addresses get their own group
				nBits = ADDR_INTERNAL_SIZE * 8;
			}
			else if (!IsRoutable(true))
			{
				// all other unroutable addresses belong to the same group
			}
			else if (HasLinkedIPv4)
			{
				// IPv4 addresses (and mapped IPv4 addresses) use /16 groups
				var ipv4 = GetLinkedIPv4();
				vchRet.Add((byte)((ipv4 >> 24) & 0xFF));
				vchRet.Add((byte)((ipv4 >> 16) & 0xFF));
				return vchRet.ToArray();
			}
			else if (IsTor || IsI2P || IsCjdns)
			{
				nBits = 4;
			}
			else if (IsHeNet)
			{
				// for he.net, use /36 groups
				nBits = 36;
			}
			else
			{
				// for the rest of the IPv6 network, use /32 groups
				nBits = 32;
			}

			// Push our address onto vchRet.
			int numBytes = nBits / 8;
			vchRet.AddRange(addr.Take(numBytes));
			nBits %= 8;
			// ...for the last byte, push nBits and for the rest of the byte push 1's
			if (nBits > 0)
			{
				var b = addr[numBytes] | ((1 << (8 - nBits)) - 1);
				vchRet.Add((byte)b);
			}

			return vchRet.ToArray();
		}


		private Network GetNetClass() 
		{
			// Make sure that if we return NET_IPV6, then IsIPv6() is true. The callers expect that.

			// Check for "internal" first because such addresses are also !IsRoutable()
			// and we don't want to return NET_UNROUTABLE in that case.
			if (IsInternal)
			{
				return Network.Internal;
			}
			if (!IsRoutable(false))
			{
				return Network.Unroutable;
			}
			if (HasLinkedIPv4)
			{
				return Network.IPv4;
			}
			return (Network)network;
		}

		public override string ToString()
		{
			switch ((Network)network)
			{
				case Network.IPv4:
				case Network.IPv6:
				case Network.Cjdns:
					return new IPAddress(addr).ToString();
				case Network.Onion:
					if (addr.Length == ADDR_TORV2_SIZE)
					{
						return Encoders.Base32.EncodeData(addr) + ".onion";
					}
					else
					{
						var chksum = CalculateChecksum(addr);
						var data = addr.Concat(chksum, new byte[]{ 3 });
						return Encoders.Base32.EncodeData(data) + ".onion";
					}
				case Network.I2P:
					return Encoders.Base32.EncodeData(addr).Replace("=", "") + ".b32.i2p"; // FIXME: Base32 withour padding
				default:
					throw new InvalidOperationException($"{network} is unknown");
			}
		}
		
		private bool HasPrefix(byte[] arr, byte[] prefix) =>
			Utils.ArrayEqual(arr, 0, prefix, 0, prefix.Length);

		public bool Equals(NetworkAddress other) =>
			network == other.network && Utils.ArrayEqual(addr, other.addr);
	}

	public class Service : NetworkAddress, IEquatable<Service>
	{
		private ushort port;

		public ushort Port => port;

		public Service()
		{
		}
		public Service(IPAddress ip)
		{
			Endpoint = new IPEndPoint(ip, 0);
		}
		public Service(IPEndPoint endpoint)
		{
			Endpoint = endpoint;
		}
		public Service(IPAddress address, int port)
		{
			Endpoint = new IPEndPoint(address, port);
		}
		public Service(Service service)
		{
			network = service.network;
			addr = service.addr;
			port = service.port;
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
				SetIp(value.Address);
			}
		}

		public byte[] GetKey()
		{
			var buffer = GetAddressBytes();
			var vKey = new byte[buffer.Length + 2];
			Array.Copy(buffer, vKey, buffer.Length);
			vKey[buffer.Length - 2] = (byte)(port / 0x100);
			vKey[buffer.Length - 1] = (byte)(port & 0x0FF);
			return vKey;
		}

		#region IBitcoinSerializable Members

		public override void ReadWrite(BitcoinStream stream)
		{
			base.ReadWrite(stream);
			using (stream.BigEndianScope())
			{
				stream.ReadWrite(ref port);
			}
		}

		#endregion

		public override string ToString()
		{
			if (IsIPv4 || IsTor || IsI2P || IsInternal)
			{
				return $"{base.ToString()}:{Port}";
			} 
			else
			{
				return $"[{base.ToString()}]:{Port}";
			}
		}

		public override int GetHashCode() =>
			(network, addr, port).GetHashCode();

		public override bool Equals(object obj) =>
			obj is Service service ? Equals(service) : false;

		public bool Equals(Service other) =>
			other is {} && port == other.Port && base.Equals((NetworkAddress)other);

		public static bool operator == (Service a, Service b)
		{
			if (System.Object.ReferenceEquals(a, b))
				return true;
			if ((a is null) || (b is null))
				return false;
			return a.Equals(b);
		}

		public static bool operator != (Service a, Service b) =>
			!(a == b);
	}

	public class Address : Service
	{
		static uint TIME_INIT = 100000000;

		// disk and network only
		internal uint nTime = TIME_INIT;

		private ulong services = (ulong)NodeServices.Nothing;

		public Address()
			: base()
		{
		}
		public Address(IPAddress ip)
			: base(ip)
		{
		}
		public Address(IPEndPoint endpoint)
			: base(endpoint)
		{
		}
		public Address(IPAddress address, int port)
			: base(address, port)
		{
		}
		public Address(Service service)
			: base(service)
		{
		}

		public Address(Address address)
			: base(address)
		{
			services = address.services;
			nTime = address.nTime;
		}

		public NodeServices Services
		{
			get
			{
				return (NodeServices)services;
			}
			set
			{
				services = (ulong)value;
			}
		}

		public DateTimeOffset Time
		{
			get
			{
				return Utils.UnixTimeToDateTime(nTime);
			}
			set
			{
				nTime = Utils.DateTimeToUnixTime(value);
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

		#region IBitcoinSerializable Members

		public override void ReadWrite(BitcoinStream stream)
		{
			var version = stream.ProtocolVersion ?? 0;
			if (stream.Type == SerializationType.Disk)
			{
				stream.ReadWrite(ref version);
			}
			if (
				stream.Type == SerializationType.Disk ||
				(stream.ProtocolCapabilities.SupportTimeAddress && stream.Type != SerializationType.Hash))
			{
				// The only time we serialize a CAddress object without nTime is in
				// the initial VERSION messages which contain two CAddress records.
				// At that point, the serialization version is INIT_PROTO_VERSION.
				// After the version handshake, serialization version is >=
				// MIN_PEER_PROTO_VERSION and all ADDR messages are serialized with
				// nTime.
				stream.ReadWrite(ref nTime);
			}
			if ((version & AddrV2Format) != 0)
			{
				stream.ReadWriteAsVarInt(ref services);
			}
			else
			{
				stream.ReadWrite(ref services);
			}

			base.ReadWrite(stream);
		}

		#endregion

		public void Adjust()
		{
			var nNow = Utils.DateTimeToUnixTime(DateTimeOffset.UtcNow);
			if (nTime <= 100000000 || nTime > nNow + 10 * 60)
				nTime = nNow - 5 * 24 * 60 * 60;
		}

		public void ZeroTime()
		{
			nTime = 0;
		}

	};
}
#endif
