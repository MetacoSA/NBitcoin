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
		static readonly uint AddrV2Format = 0x20000000;

		public enum BIP155Network : byte
		{
			IPV4,  // 0x02 len: 4 IPv4 address (globally routed internet)
			IPV6,  // 0x02 len: 16 bytes - IPv6 address (globally routed internet)
			TorV2, // 0x03 len: 10 bytes - Tor v2 hidden service address
			TorV3, // 0x04 len: 32 bytes - Tor v3 hidden service address
			I2P,   // 0x05 len: 32 bytes - I2P overlay network address
			Cjdns, // 0x06 len: 16 bytes - Cjdns overlay network address
		}

		private static int GetSize(BIP155Network network)
		{
			return network switch 
			{
				BIP155Network.IPV4  =>  4,
				BIP155Network.IPV6  => 16,
				BIP155Network.TorV2 => 10,
				BIP155Network.TorV3 => 32,
				BIP155Network.I2P   => 32,
				BIP155Network.Cjdns => 16,
				_ => throw new ArgumentOutOfRangeException(nameof(network))
			};
		}

		/// Prefix of an IPv6 address when it contains an embedded IPv4 address.
		/// Used when (un)serializing addresses in ADDRv1 format (pre-BIP155).
		static byte[] IPV4_IN_IPV6_PREFIX = new byte[]{
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF
		};

		/// Prefix of an IPv6 address when it contains an embedded TORv2 address.
		/// Used when (un)serializing addresses in ADDRv1 format (pre-BIP155).
		/// Such dummy IPv6 addresses are guaranteed to not be publicly routable as they
		/// fall under RFC4193's fc00::/7 subnet allocated to unique-local addresses.
		static byte[] TORV2_IN_IPV6_PREFIX = new byte[]{
			0xFD, 0x87, 0xD8, 0x7E, 0xEB, 0x43
		};

		/// Prefix of an IPv6 address when it contains an embedded "internal" address.
		/// Used when (un)serializing addresses in ADDRv1 format (pre-BIP155).
		/// The prefix comes from 0xFD + SHA256("bitcoin")[0:5].
		/// Such dummy IPv6 addresses are guaranteed to not be publicly routable as they
		/// fall under RFC4193's fc00::/7 subnet allocated to unique-local addresses.
		static byte[] INTERNAL_IN_IPV6_PREFIX = new byte[]{
			0xFD, 0x6B, 0x88, 0xC0, 0x87, 0x24 // 0xFD + sha256("bitcoin")[0:5].
		};

		public NetworkAddress()
		{
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
		public bool IsOnion => (BIP155Network)network == BIP155Network.TorV2 || (BIP155Network)network == BIP155Network.TorV3;
		public bool IsI2P => (BIP155Network)network == BIP155Network.I2P;
		public bool IsCjdns => (BIP155Network)network == BIP155Network.Cjdns;

		ulong service = 1;
		byte[] addr = new byte[16];
		ushort port;

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

			if (bytes.Length == GetSize(BIP155Network.TorV2))
			{
				network = (byte)BIP155Network.TorV2;
				addr = bytes;
			}
			else if(bytes.Length == GetSize(BIP155Network.TorV3))
			{
				var pubkey = bytes.SafeSubarray( 0, 32);
				var chksum = bytes.SafeSubarray(32,  2);
				var version = bytes[34];

				var calculatedChecksum = CalculateChecksum(pubkey);

				if (!Utils.ArrayEqual(chksum, calculatedChecksum) || version != 3)
				{
					return false;
				}
			}
			return true;
		}

		private static byte[] CalculateChecksum(byte[] pubkey)
		{
			// TORv3 CHECKSUM = H(".onion checksum" | PUBKEY | VERSION)[:2]
			var prefix = Encoding.ASCII.GetBytes(".onion checksum");
			var prefixWithLength = prefix.Prepend((byte)prefix.Length).ToArray();

			var hasher = new KeccakDigest(256);

			hasher.BlockUpdate(prefixWithLength, 0, prefixWithLength.Length);
			hasher.BlockUpdate(pubkey, 0, pubkey.Length);
			hasher.BlockUpdate(new byte[]{ 3 }, 0, 1);

			var fullChecksum = new byte[32];
			hasher.DoFinal(fullChecksum, 0);
			return fullChecksum;
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
				if (ipBytes.Length == 16)
				{
					addr = ipBytes;
				}
				else if (ipBytes.Length == 4)
				{
					//Convert to ipv4 mapped to ipv6
					//In these addresses, the first 80 bits are zero, the next 16 bits are one, and the remaining 32 bits are the IPv4 address
					addr = new byte[16];
					Array.Copy(ipBytes, 0, addr, 12, 4);
					Array.Copy(new byte[] { 0xFF, 0xFF }, 0, addr, 10, 2);
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
			if (
				stream.Type == SerializationType.Disk ||
				(stream.ProtocolCapabilities.SupportTimeAddress && stream.Type != SerializationType.Hash))
			{
				if ((version & AddrV2Format) != 0)
				{
					stream.ReadWriteAsVarInt(ref ntime);
				}
				else
				{
					stream.ReadWrite(ref ntime);
				}
			}

			if ((version & AddrV2Format) != 0)
			{
				stream.ReadWriteAsVarInt(ref service);
				stream.ReadWrite(ref network);
				stream.ReadWrite(ref addr);
			}
			else
			{
				stream.ReadWrite(ref service);
				stream.ReadWrite(ref addr);
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
				BIP155Network.TorV3 => new byte[GetSize(BIP155Network.IPV6)],
				BIP155Network.I2P   => new byte[GetSize(BIP155Network.IPV6)],
				BIP155Network.Cjdns => new byte[GetSize(BIP155Network.IPV6)],
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
				BIP155Network.Cjdns => false
			};

		public byte[] GetAddressBytes() =>
			IsAddrV1Compatible ? SerializeV1Array() : addr;

		internal byte[] GetKey()
		{
			var buffer = GetAddressBytes();
			var vKey = new byte[buffer.Length + 2];
			Array.Copy(buffer, vKey, buffer.Length);
			vKey[buffer.Length - 2] = (byte)(port / 0x100);
			vKey[buffer.Length - 1] = (byte)(port & 0x0FF);
			return vKey;
		}
	}
}
#endif