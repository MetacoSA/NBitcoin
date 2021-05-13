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
	// A network type.
	// @note An address may belong to more than one network, for example `10.0.0.1`
	// belongs to both `NET_UNROUTABLE` and `NET_IPV4`.
	// Keep these sequential starting from 0 and `NET_MAX` as the last entry.
	public enum NetworkAddressType
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
		Cjdns
	}
	public class NetworkAddress : IBitcoinSerializable
	{
		/// see: https://github.com/bitcoin/bips/blob/master/bip-0155.mediawiki
		public const uint AddrV2Format = 0x20000000;

		
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
			Max,
		};


		public NetworkAddressType AddressType => network;

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
		private const int ADDR_IPV4_SIZE = 4;

		/// Size of IPv6 address (in bytes).
		private  const int ADDR_IPV6_SIZE = 16;

		/// Size of TORv2 address (in bytes).
		private const int ADDR_TORV2_SIZE = 10;

		/// Size of TORv3 address (in bytes). This is the length of just the address
		/// as used in BIP155, without the checksum and the version byte.
		private const int ADDR_TORV3_SIZE = 32;

		/// Size of I2P address (in bytes).
		private const int ADDR_I2P_SIZE = 32;

		/// Size of CJDNS address (in bytes).
		private const int ADDR_CJDNS_SIZE = 16;

		/// Size of the TORv3 address checksum (in bytes)
		private const int TORV3_ADDR_CHECKSUM_LEN = 2;

		/// Size of the TORv3 address version number (in bytes)
		private const int TORV3_ADDR_VERSION_LEN = 1;


		/// Prefix of an IPv6 address when it contains an embedded IPv4 address.
		/// Used when (un)serializing addresses in ADDRv1 format (pre-BIP155).
		private static readonly byte[] IPV4_IN_IPV6_PREFIX = new byte[]{
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF
		};

		/// Prefix of an IPv6 address when it contains an embedded TORv2 address.
		/// Used when (un)serializing addresses in ADDRv1 format (pre-BIP155).
		/// Such dummy IPv6 addresses are guaranteed to not be publicly routable as they
		/// fall under RFC4193's fc00::/7 subnet allocated to unique-local addresses.
		private static readonly byte[] TORV2_IN_IPV6_PREFIX = new byte[]{
			0xFD, 0x87, 0xD8, 0x7E, 0xEB, 0x43
		};

		/// Prefix of an IPv6 address when it contains an embedded "internal" address.
		/// Used when (un)serializing addresses in ADDRv1 format (pre-BIP155).
		/// The prefix comes from 0xFD + SHA256("bitcoin")[0:5].
		/// Such dummy IPv6 addresses are guaranteed to not be publicly routable as they
		/// fall under RFC4193's fc00::/7 subnet allocated to unique-local addresses.
		private static readonly byte[] INTERNAL_IN_IPV6_PREFIX = new byte[]{
			0xFD, 0x6B, 0x88, 0xC0, 0x87, 0x24 // 0xFD + sha256("bitcoin")[0:5].
		};


		private static readonly byte[] IPV6_NONE = new byte[ADDR_IPV6_SIZE];

		private NetworkAddressType network = NetworkAddressType.IPv6;
		private byte[] addr = new byte[ADDR_IPV6_SIZE];

		private ushort port;

		public ushort Port => port;

		static uint TIME_INIT = 100000000;

		// disk and network only
		internal uint nTime = TIME_INIT;

		private ulong services = (ulong)NodeServices.Nothing;

		public NetworkAddress()
		{
		}

		public NetworkAddress(IPAddress ip, int port)
			: this(ip)
		{
			this.port = (ushort)port;
		}

		public NetworkAddress(IPAddress ip)
		{
			SetIp(ip);
		}

		public NetworkAddress(EndPoint endPoint)
		{
			if (endPoint == null)
				throw new ArgumentNullException(nameof(endPoint));
			Endpoint = endPoint;
			if (endPoint.IsTor())
				network = NetworkAddressType.Onion;
			else if (endPoint is IPEndPoint ip)
				SetIp(ip.Address);
			else
				throw new NotSupportedException($"Unsupported endpoint of type {endPoint.GetType()}");
		}

		[Obsolete("Use AddressType instead")]
		public bool IsIPv4 => network == NetworkAddressType.IPv4;
		[Obsolete("Use AddressType instead")]
		public bool IsIPv6 => network == NetworkAddressType.IPv6;
		[Obsolete("Use AddressType instead")]
		public bool IsTor => network == NetworkAddressType.Onion;
		[Obsolete("Use AddressType instead")]
		public bool IsI2P => network == NetworkAddressType.I2P;
		[Obsolete("Use AddressType instead")]
		public bool IsCjdns => network == NetworkAddressType.Cjdns;

		public EndPoint Endpoint
		{
			get
			{
				if (IsAddrV1Compatible)
				{
					return new IPEndPoint(new IPAddress(SerializeV1Array()), port);
				}
				else
				{
					return new DnsEndPoint(ToAddressString(), port);
				}
			}
			set
			{
				if (value is IPEndPoint ipEndPoint)
				{
					SetIp(ipEndPoint.Address);
					port = (ushort)ipEndPoint.Port;
				}
				else if (value is DnsEndPoint dnsEndPoint)
				{
					SetSpecial(dnsEndPoint.Host);
					port = (ushort)dnsEndPoint.Port;
				}
				else
				{
					throw new ArgumentException($"Not supported {nameof(EndPoint)} type {value.GetType().FullName}.");
				}
			}
		} 

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
				network = NetworkAddressType.IPv4;
				skip = IPV4_IN_IPV6_PREFIX.Length;
			}
			else if(HasPrefix(ipv6, TORV2_IN_IPV6_PREFIX))
			{
				// TORv2-in-IPv6
				network = NetworkAddressType.Onion;
				skip = TORV2_IN_IPV6_PREFIX.Length;
			} 
			else
			{
				// IPv6
				network = NetworkAddressType.IPv6;
			}	

			addr = ipv6.SafeSubarray(skip);
		}

		private bool SetTor(string address)
		{
			if (!address.EndsWith(".onion", StringComparison.Ordinal))
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
				network = NetworkAddressType.Onion;
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

				if (!Utils.ArrayEqual(chksum, calculatedChecksum))
				{
					return false;
				}

				network = NetworkAddressType.Onion;
				addr = pubkey;
				return true;
			}
			return false;
		}

		public bool SetI2P(string address)
		{
			address = address.ToLowerInvariant();
			if (!address.EndsWith(".b32.i2p", StringComparison.Ordinal))
			{
				return false;
			}

			var encoded = address.Substring(0, address.Length - ".b32.i2p".Length);
			if (encoded.Length != 52)
			{
				return false;
			}

			byte[] bytes;
			try
			{
				bytes = Encoders.Base32.DecodeData(encoded + "====");
			}
			catch(FormatException)
			{
				return false;
			}

			if (bytes.Length != ADDR_I2P_SIZE + 3)
			{
				return false;
			}

			network = NetworkAddressType.I2P;
			addr = bytes.SafeSubarray(0, ADDR_I2P_SIZE);
			return true;
		}

		public bool SetSpecial(string address)
		{
			return SetTor(address) || SetI2P(address);
		}

		private static byte[] CalculateChecksum(byte[] pubkey)
		{
			// TORv3 CHECKSUM = H(".onion checksum" | PUBKEY | VERSION)[:2]
			var prefix = Encoders.ASCII.DecodeData(".onion checksum");

			var hasher = new Sha3Digest(256);

			hasher.BlockUpdate(prefix, 0, prefix.Length);
			hasher.BlockUpdate(pubkey, 0, pubkey.Length);
			hasher.Update((byte)3);

			var fullChecksum = new byte[hasher.GetByteLength()];
			hasher.DoFinal(fullChecksum, 0);
			return fullChecksum.SafeSubarray(0, TORV3_ADDR_CHECKSUM_LEN);
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

		public virtual void ReadWrite(BitcoinStream stream)
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
					byte n = 0;
					stream.ReadWrite(ref n);
					stream.ReadWriteAsVarString(ref addr);
					
					if (SetNetFromBIP155Network((BIP155Network)n, addr.Length))
					{
						if (network == NetworkAddressType.IPv6)
						{
							if (HasPrefix(addr, IPV4_IN_IPV6_PREFIX) || HasPrefix(addr, TORV2_IN_IPV6_PREFIX))
							{
								// set to invalid because IPv4 and Torv2 should not be embeded in IPv6.
								addr = IPV6_NONE;
								network = NetworkAddressType.IPv6;
							}
						}
					}
					else
					{
						// set to invalid because IPv4 and Torv2 should not be embeded in IPv6.
						addr = IPV6_NONE;
						network = NetworkAddressType.IPv6;
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
			using (stream.BigEndianScope())
			{
				stream.ReadWrite(ref port);
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
				case Network.Unroutable: // m_net is never and should not be set to NET_UNROUTABLE
				case Network.Max:        // m_net is never and should not be set to NET_MAX
					throw new Exception("Unknown network.");
			} // no default case, so the compiler can warn about missing cases
			throw new Exception("Unknown network.");
		}

		private bool SetNetFromBIP155Network(BIP155Network bip155Network, int length)
		{
			NetworkAddressType AssignIfCorrectSize(NetworkAddressType net, int expectedSize)
			{
				if (length != expectedSize)
				{
					throw new ArgumentException($"BIP155 {(BIP155Network)bip155Network} address is {length} bytes long. Expected length is {expectedSize} bytes.");
				}
				return net;
			}

			switch(bip155Network)
			{
				case BIP155Network.IPv4:
					network = AssignIfCorrectSize(NetworkAddressType.IPv4, ADDR_IPV4_SIZE);
					break;
				case BIP155Network.IPv6:
					network = AssignIfCorrectSize(NetworkAddressType.IPv6, ADDR_IPV6_SIZE);
					break;
				case BIP155Network.TORv2:
					network = AssignIfCorrectSize(NetworkAddressType.Onion, ADDR_TORV2_SIZE);
					break;
				case BIP155Network.TORv3:
					network = AssignIfCorrectSize(NetworkAddressType.Onion, ADDR_TORV3_SIZE);
					break;
				case BIP155Network.I2P:
					network = AssignIfCorrectSize(NetworkAddressType.I2P, ADDR_I2P_SIZE);
					break;
				case BIP155Network.Cjdns:
					network = AssignIfCorrectSize(NetworkAddressType.Cjdns, ADDR_CJDNS_SIZE);
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
				Network.I2P   => new byte[ADDR_I2P_SIZE],
				Network.Cjdns => new byte[ADDR_CJDNS_SIZE],
				_ => throw new InvalidOperationException("Unknown Address network.") 
			}).ToArray();

		public bool IsAddrV1Compatible
		{
			get
			{
				var net = (Network)network;
				switch (net)
				{
					case Network.IPv4:
					case Network.IPv6:
						return true;
					case Network.Onion:
						switch (addr.Length)
						{
							case ADDR_TORV2_SIZE:
								return true;
							case ADDR_TORV3_SIZE:
								return false;
							default:
								throw new InvalidOperationException("Unknown Address network.");
						}
					case Network.I2P:
						return false;
					case Network.Cjdns: 
						return false;
					default:
						throw new InvalidOperationException("Unknown Address network.");
				}
			} 
		}

		public byte[] GetAddressBytes() => 
			IsAddrV1Compatible ? SerializeV1Array() : addr;

		public string ToAddressString()
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
					return Encoders.Base32.EncodeData(addr).Replace("=", "") + ".b32.i2p"; // FIXME: Base32 without padding
				default:
					throw new InvalidOperationException($"{network} is unknown");
			}
		}

		public override string ToString()
		{
			if (network == NetworkAddressType.IPv4 ||
				network == NetworkAddressType.Onion||
				network == NetworkAddressType.I2P)
			{
				return $"{ToAddressString()}:{Port}";
			} 
			else
			{
				return $"[{ToAddressString()}]:{Port}";
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


		private bool HasPrefix(byte[] arr, byte[] prefix) =>
			Utils.ArrayEqual(arr, 0, prefix, 0, prefix.Length);
	}
}
#endif
