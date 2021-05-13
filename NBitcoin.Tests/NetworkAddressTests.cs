using System.IO;
using System.Linq;
using System.Threading;
using NBitcoin;
using Xunit;
using System;
using NBitcoin.Protocol;
using System.Net;
using NBitcoin.DataEncoders;

namespace NBitcoin.Tests
{
	public class NetworkAddressTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseSpecialAddresses()
		{
			var addr = new NetworkAddress();

			// TORv2
			Assert.True(addr.SetSpecial("6hzph5hv6337r6p2.onion"));
			Assert.True(addr.AddressType == NetworkAddressType.Onion);

			Assert.True(addr.IsAddrV1Compatible);
			Assert.Equal("6hzph5hv6337r6p2.onion", addr.ToAddressString());

			// TORv3
			var torv3_addr = "pg6mmjiyjmcrsslvykfwnntlaru7p5svn6y2ymmju6nubxndf4pscryd.onion";
			Assert.True(addr.SetSpecial(torv3_addr));
			Assert.True(addr.AddressType == NetworkAddressType.Onion);

			Assert.False(addr.IsAddrV1Compatible);
			Assert.Equal(addr.ToAddressString(), torv3_addr);

			// TORv3, broken, with wrong checksum
			Assert.False(addr.SetSpecial("pg6mmjiyjmcrsslvykfwnntlaru7p5svn6y2ymmju6nubxndf4pscsad.onion"));

			// TORv3, broken, with wrong version
			Assert.False(addr.SetSpecial("pg6mmjiyjmcrsslvykfwnntlaru7p5svn6y2ymmju6nubxndf4pscrye.onion"));

			// TORv3, malicious
			Assert.False(addr.SetSpecial("pg6mmjiyjmcrsslvykfwnntlaru7p5svn6y2ymmju6nubxndf4pscryd\0wtf.onion"));

			// TOR, bogus length
			Assert.False(addr.SetSpecial("mfrggzak.onion"));

			// TOR, invalid base32
			Assert.False(addr.SetSpecial("mf*g zak.onion"));

			// I2P
			var i2p_addr = "udhdrtrcetjm5sxzskjyr5ztpeszydbh4dpl3pl4utgqqw2v4jna.b32.i2p";
			Assert.True(addr.SetSpecial(i2p_addr));
			Assert.True(addr.AddressType == NetworkAddressType.I2P);
			Assert.False(addr.IsAddrV1Compatible);
			Assert.Equal(addr.ToAddressString(), i2p_addr);

			// I2P, malicious
			Assert.False(addr.SetSpecial("udhdrtrcetjm5sxzskjyr5ztpeszydbh4dpl3pl4utgqqw2v4jna\0wtf.b32.i2p"));

			// I2P, valid but unsupported 
			Assert.False(addr.SetSpecial("pg6mmjiyjmcrsslvykfwnntlaru7p5svn6y2ymmju6nubxndf4pscsad.b32.i2p"));

			// I2P, invalid base32
			Assert.False(addr.SetSpecial("tp*szydbh4dp.b32.i2p"));

			// General validations
			Assert.False(addr.SetSpecial(".onion"));
			Assert.False(addr.SetSpecial(""));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void SerializeV1()
		{
			var addr = new NetworkAddress();
			var mem = new MemoryStream();
			var stream = new BitcoinStream(mem, true);

			string AddressHex()
			{
				var arr = mem.ToArray();
				return Encoders.Hex.EncodeData(arr, 8, 16);
			}

			// serialize method produces an address in v2 format.
			stream.Type = SerializationType.Hash;

			stream.ReadWrite(addr);
			Assert.Equal("00000000000000000000000000000000", AddressHex());

			mem.Clear();
			addr = new NetworkAddress(IPAddress.Parse("1.2.3.4"));
			stream.ReadWrite(addr);
			Assert.Equal("00000000000000000000ffff01020304", AddressHex());

			mem.Clear();
			addr = new NetworkAddress(IPAddress.Parse("1a1b:2a2b:3a3b:4a4b:5a5b:6a6b:7a7b:8a8b"));
			stream.ReadWrite(addr);
			Assert.Equal("1a1b2a2b3a3b4a4b5a5b6a6b7a7b8a8b", AddressHex());

			mem.Clear();
			addr = new NetworkAddress();
			addr.SetSpecial("6hzph5hv6337r6p2.onion");
			stream.ReadWrite(addr);
			Assert.Equal("fd87d87eeb43f1f2f3f4f5f6f7f8f9fa", AddressHex());

			mem.Clear();
			addr = new NetworkAddress();
			addr.SetSpecial("pg6mmjiyjmcrsslvykfwnntlaru7p5svn6y2ymmju6nubxndf4pscryd.onion");
			stream.ReadWrite(addr);
			Assert.Equal("00000000000000000000000000000000", AddressHex());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void SerializeV2()
		{
			var addr = new NetworkAddress();
			var mem = new MemoryStream();
			var stream = new BitcoinStream(mem, true);

			string HexStr()
			{
				var arr = mem.ToArray();
				return Encoders.Hex.EncodeData(arr, 1, arr.Length - 3);
			}

			// Add ADDRV2_FORMAT to the version so that the CNetAddr
			// serialize method produces an address in v2 format.
			stream.ProtocolVersion = NetworkAddress.AddrV2Format;
			stream.Type = SerializationType.Hash;

			stream.ReadWrite(addr);
			Assert.Equal("021000000000000000000000000000000000", HexStr());

			mem.Clear();
			addr = new NetworkAddress(IPAddress.Parse("1.2.3.4"));
			stream.ReadWrite(addr);
			Assert.Equal("010401020304", HexStr());

			mem.Clear();
			addr = new NetworkAddress(IPAddress.Parse("1a1b:2a2b:3a3b:4a4b:5a5b:6a6b:7a7b:8a8b"));
			stream.ReadWrite(addr);
			Assert.Equal("02101a1b2a2b3a3b4a4b5a5b6a6b7a7b8a8b", HexStr());

			mem.Clear();
			addr = new NetworkAddress();
			addr.SetSpecial("6hzph5hv6337r6p2.onion");
			stream.ReadWrite(addr);
			Assert.Equal("030af1f2f3f4f5f6f7f8f9fa", HexStr());

			mem.Clear();
			addr = new NetworkAddress();
			addr.SetSpecial("kpgvmscirrdqpekbqjsvw5teanhatztpp2gl6eee4zkowvwfxwenqaid.onion");
			stream.ReadWrite(addr);
			Assert.Equal("042053cd5648488c4707914182655b7664034e09e66f7e8cbf1084e654eb56c5bd88", HexStr());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void UnserializeV2()
		{
			static BitcoinStream MakeNewStream(byte[] payload)
			{
				return new BitcoinStream(payload)
				{
					ProtocolVersion = NetworkAddress.AddrV2Format,
					Type = SerializationType.Hash
				};
			}

			var addr = new NetworkAddress();

			// Valid IPv4.
			var payload = Encoders.Hex.DecodeData(
				"00" +         // services (varint)
				"01" +         // network type (IPv4)
				"04" +         // address length
				"01020304" +   // address
				"0000");       // port
			var stream = MakeNewStream(payload);
			stream.ReadWrite(ref addr);
			Assert.True(addr.Endpoint.IsValid());
			Assert.True(addr.AddressType == NetworkAddressType.IPv4);
			Assert.True(addr.IsAddrV1Compatible);
			Assert.Equal("1.2.3.4", addr.ToAddressString());
			Assert.Equal(stream.Inner.Length, stream.Inner.Position);

			// Invalid IPv4, valid length but address itself is shorter.
			payload = Encoders.Hex.DecodeData(
				"00" +         // services (varint)
				"01" +         // network type (IPv4)
				"04" +         // address length
				"0102" +       // address
				"0000");       // port
			stream = MakeNewStream(payload);
			Assert.Throws<EndOfStreamException>(() => stream.ReadWrite(ref addr));
			Assert.Equal(stream.Inner.Length, stream.Inner.Position);

			// Invalid IPv4, with bogus length.
			// Invalid IPv4, valid length but address itself is shorter.
			payload = Encoders.Hex.DecodeData(
				"00" +         // services (varint)
				"01" +         // network type (IPv4)
				"05" +         // address length
				"0102030400"); // address
			stream = MakeNewStream(payload);
			var ex = Assert.Throws<ArgumentException>(() => stream.ReadWrite(ref addr));
			Assert.Equal("BIP155 IPv4 address is 5 bytes long. Expected length is 4 bytes.", ex.Message);

			// Valid IPv6.
			payload = Encoders.Hex.DecodeData(
				"00" +         // services (varint)
				"02" +         // network type (IPv6)
				"10" +         // address length
				"0102030405060708090a0b0c0d0e0f10" +  // address
				"0000");       // port
			stream = MakeNewStream(payload);
			stream.ReadWrite(ref addr);

			Assert.True(addr.Endpoint.IsValid());
			Assert.True(addr.AddressType == NetworkAddressType.IPv6);
			Assert.True(addr.IsAddrV1Compatible);
			Assert.Equal("102:304:506:708:90a:b0c:d0e:f10", addr.ToAddressString());
			Assert.Equal(stream.Inner.Length, stream.Inner.Position);

			// Invalid IPv6, with bogus length.
			payload = Encoders.Hex.DecodeData(
				"00" +         // services (varint)
				"02" +         // network type (IPv6)
				"04" +         // address length
				"00000000");   // address
			stream = MakeNewStream(payload);
			ex = Assert.Throws<ArgumentException>(() => stream.ReadWrite(ref addr));
			Assert.Equal("BIP155 IPv6 address is 4 bytes long. Expected length is 16 bytes.", ex.Message);

			// Invalid IPv6, contains embedded IPv4.
			payload = Encoders.Hex.DecodeData(
				"00" +         // services (varint)
				"02" +         // network type (IPv6)
				"10" +         // address length
				"00000000000000000000ffff01020304" +  // address
				"0000");       // port
			stream = MakeNewStream(payload);
			stream.ReadWrite(ref addr);

			Assert.False(addr.Endpoint.IsValid());
			Assert.Equal(stream.Inner.Length, stream.Inner.Position);

			// Invalid IPv6, contains embedded TORv2.
			payload = Encoders.Hex.DecodeData(
				"00" +         // services (varint)
				"02" +         // network type (IPv6)
				"10" +         // address length
				"fd87d87eeb430102030405060708090a" +   // address
				"0000");       // port
			stream = MakeNewStream(payload);
			stream.ReadWrite(ref addr);
			Assert.False(addr.Endpoint.IsValid());
			Assert.Equal(stream.Inner.Length, stream.Inner.Position);

			// Valid TORv2.
			payload = Encoders.Hex.DecodeData(
				"00" +         // services (varint)
				"03" +         // network type (TORv2)
				"0a" +         // address length
				"f1f2f3f4f5f6f7f8f9fa" +  // address
				"0000");       // port
			stream = MakeNewStream(payload);
			stream.ReadWrite(ref addr);
			Assert.True(addr.Endpoint.IsValid());
			Assert.True(addr.AddressType == NetworkAddressType.Onion);
			Assert.True(addr.IsAddrV1Compatible);
			Assert.Equal("6hzph5hv6337r6p2.onion", addr.ToAddressString());
			Assert.Equal(stream.Inner.Length, stream.Inner.Position);

			// Invalid TORv2, with bogus length.
			payload = Encoders.Hex.DecodeData(
				"00" +         // services (varint)
				"03" +         // network type (TORv2)
				"07" +         // address length
				"00000000000000" );   // address
			stream = MakeNewStream(payload);
			ex = Assert.Throws<ArgumentException>(() => stream.ReadWrite(ref addr));
			Assert.Equal("BIP155 TORv2 address is 7 bytes long. Expected length is 10 bytes.", ex.Message);

			// Valid TORv3.
			payload = Encoders.Hex.DecodeData(
				"00" +         // services (varint)
				"04" +         // network type (TORv3)
				"20" +         // address length
				"79bcc625184b05194975c28b66b66b0469f7f6556fb1ac3189a79b40dda32f1f" +   // address
				"0000");       // port
			stream = MakeNewStream(payload);
			stream.ReadWrite(ref addr);
			Assert.True(addr.Endpoint.IsValid());
			Assert.True(addr.AddressType == NetworkAddressType.Onion);
			Assert.False(addr.IsAddrV1Compatible);
			Assert.Equal("pg6mmjiyjmcrsslvykfwnntlaru7p5svn6y2ymmju6nubxndf4pscryd.onion", addr.ToAddressString());
			Assert.Equal(stream.Inner.Length, stream.Inner.Position);

			// Invalid TORv3, with bogus length.
			payload = Encoders.Hex.DecodeData(
				"00" +         // services (varint)
				"04" +         // network type (TORv3)
				"00" +         // address length
				"00" );        // address
			stream = MakeNewStream(payload);
			ex = Assert.Throws<ArgumentException>(() => stream.ReadWrite(ref addr));
			Assert.Equal("BIP155 TORv3 address is 0 bytes long. Expected length is 32 bytes.", ex.Message);
			Assert.NotEqual(stream.Inner.Length, stream.Inner.Position);

			// Valid I2P.
			payload = Encoders.Hex.DecodeData(
				"00" +         // services (varint)
				"05" +         // network type (I2P)
				"20" +         // address length
				"a2894dabaec08c0051a481a6dac88b64f98232ae42d4b6fd2fa81952dfe36a87" + // address
				"0000");       // port
			stream = MakeNewStream(payload);
			stream.ReadWrite(ref addr);
			Assert.True(addr.Endpoint.IsValid());
			Assert.True(addr.AddressType == NetworkAddressType.I2P);
			Assert.False(addr.IsAddrV1Compatible);
			Assert.Equal("ukeu3k5oycgaauneqgtnvselmt4yemvoilkln7jpvamvfx7dnkdq.b32.i2p", addr.ToAddressString());
			Assert.Equal(stream.Inner.Length, stream.Inner.Position);

			// Invalid I2P, with bogus length.
			payload = Encoders.Hex.DecodeData(
				"00" +         // services (varint)
				"05" +         // network type (I2P)
				"03" +         // address length
				"000000");     // address
			stream = MakeNewStream(payload);
			ex = Assert.Throws<ArgumentException>(() => stream.ReadWrite(ref addr));
			Assert.Equal("BIP155 I2P address is 3 bytes long. Expected length is 32 bytes.", ex.Message);

			// Valid CJDNS.
			payload = Encoders.Hex.DecodeData(
				"00" +         // services (varint)
				"06" +         // network type (CJDNS)
				"10" +         // address length
				"fc000001000200030004000500060007" + // address
				"0000");       // port
			stream = MakeNewStream(payload);
			stream.ReadWrite(ref addr);
			Assert.True(addr.Endpoint.IsValid());
			Assert.True(addr.AddressType == NetworkAddressType.Cjdns);
			Assert.False(addr.IsAddrV1Compatible);
			Assert.Equal("fc00:1:2:3:4:5:6:7", addr.ToAddressString());
			Assert.Equal(stream.Inner.Length, stream.Inner.Position);

			// Invalid CJDNS, with bogus length.
			payload = Encoders.Hex.DecodeData(
				"00" +         // services (varint)
				"06" +         // network type (CJDNS)
				"01" +         // address length
				"00");         // address
			stream = MakeNewStream(payload);
			ex = Assert.Throws<ArgumentException>(() => stream.ReadWrite(ref addr));
			Assert.Equal("BIP155 Cjdns address is 1 bytes long. Expected length is 16 bytes.", ex.Message);

			// Unknown, with extreme length.
			payload = Encoders.Hex.DecodeData(
				"00" +         // services (varint)
				"aa" +         // network type (unknown)
				"fe00000002" + // address length (CompactSize's MAX_SIZE)
				"01020304050607"); // address
			stream = MakeNewStream(payload);
			ex = Assert.Throws<ArgumentOutOfRangeException>(() => stream.ReadWrite(ref addr));
			Assert.Contains("Array size too big", ex.Message);
			Assert.NotEqual(stream.Inner.Length, stream.Inner.Position);

			// Unknown, with reasonable length.
			payload = Encoders.Hex.DecodeData(
				"00" +         // services (varint)
				"aa" +         // network type (unknown)
				"04" +         // address length
				"01020304" +   // address
				"0000");       // port
			stream = MakeNewStream(payload);
			stream.ReadWrite(ref addr);
			Assert.False(addr.Endpoint.IsValid());
			Assert.Equal(stream.Inner.Length, stream.Inner.Position);

			// Unknown, with zero length.
			payload = Encoders.Hex.DecodeData(
				"00" +         // services (varint)
				"aa" +         // network type (unknown)
				"00" +         // address length
				"0000");       // port
			stream = MakeNewStream(payload);
			stream.ReadWrite(ref addr);
			Assert.False(addr.Endpoint.IsValid());
			Assert.Equal(stream.Inner.Length, stream.Inner.Position);
		}

		static NetworkAddress[] fixture_addresses = new NetworkAddress[]
		{
			new NetworkAddress(IPAddress.IPv6Loopback, 0)
			{
				Services = NodeServices.Nothing,
				Time = Utils.UnixTimeToDateTime(0x4966bc61U) /* Fri Jan  9 02:54:25 UTC 2009 */
			},
			new NetworkAddress(IPAddress.IPv6Loopback, 0x00f1 /* port */)
			{
				Services = NodeServices.Network,
				Time = Utils.UnixTimeToDateTime(0x83766279U) /* Tue Nov 22 11:22:33 UTC 2039 */
			},
			new NetworkAddress(IPAddress.IPv6Loopback, 0xf1f2 /* port */)
			{
				Services = NodeServices.NODE_WITNESS | NodeServices.NODE_NETWORK_LIMITED | NodeServices.NODE_COMPACT_FILTERS,
				Time = Utils.UnixTimeToDateTime(0xffffffffU) /* Sun Feb  7 06:28:15 UTC 2106 */
			}
		};

		// fixture_addresses should be equal to this when serialized in V1 format.
		// When this is unserialized from V1 format it should equal to fixture_addresses.
		const string stream_addrv1_hex =
			  "03" // number of entries

			+ "61bc6649"                         // time, Fri Jan  9 02:54:25 UTC 2009
			+ "0000000000000000"                 // service flags, NODE_NONE
			+ "00000000000000000000000000000001" // address, fixed 16 bytes (IPv4 embedded in IPv6)
			+ "0000"                             // port

			+ "79627683"                         // time, Tue Nov 22 11:22:33 UTC 2039
			+ "0100000000000000"                 // service flags, NODE_NETWORK
			+ "00000000000000000000000000000001" // address, fixed 16 bytes (IPv6)
			+ "00f1"                             // port

			+ "ffffffff"                         // time, Sun Feb  7 06:28:15 UTC 2106
			+ "4804000000000000"                 // service flags, NODE_WITNESS | NODE_COMPACT_FILTERS | NODE_NETWORK_LIMITED
			+ "00000000000000000000000000000001" // address, fixed 16 bytes (IPv6)
			+ "f1f2";                            // port

		// fixture_addresses should be equal to this when serialized in V2 format.
		// When this is unserialized from V2 format it should equal to fixture_addresses.
		static string stream_addrv2_hex =
			  "03" // number of entries

			+ "61bc6649"                         // time, Fri Jan  9 02:54:25 UTC 2009
			+ "00"                               // service flags, COMPACTSIZE(NODE_NONE)
			+ "02"                               // network id, IPv6
			+ "10"                               // address length, COMPACTSIZE(16)
			+ "00000000000000000000000000000001" // address
			+ "0000"                             // port

			+ "79627683"                         // time, Tue Nov 22 11:22:33 UTC 2039
			+ "01"                               // service flags, COMPACTSIZE(NODE_NETWORK)
			+ "02"                               // network id, IPv6
			+ "10"                               // address length, COMPACTSIZE(16)
			+ "00000000000000000000000000000001" // address
			+ "00f1"                             // port

			+ "ffffffff"                         // time, Sun Feb  7 06:28:15 UTC 2106
			+ "fd4804"                           // service flags, COMPACTSIZE(NODE_WITNESS | NODE_COMPACT_FILTERS | NODE_NETWORK_LIMITED)
			+ "02"                               // network id, IPv6
			+ "10"                               // address length, COMPACTSIZE(16)
			+ "00000000000000000000000000000001" // address
			+ "f1f2";                            // port


		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void SerializeAddressV1()
		{
			var mem = new MemoryStream();
			var stream = new BitcoinStream(mem, serializing: true){
				Type = SerializationType.Network
			};
			stream.ReadWrite(ref fixture_addresses);
			Assert.Equal(stream_addrv1_hex, Encoders.Hex.EncodeData(mem.ToArray()));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void SerializeAddressV2()
		{
			var mem = new MemoryStream();
			var stream = new BitcoinStream(mem, serializing: true){
				Type = SerializationType.Network,
				ProtocolVersion = NetworkAddress.AddrV2Format
			};
			stream.ReadWrite(ref fixture_addresses);
			Assert.Equal(stream_addrv2_hex, Encoders.Hex.EncodeData(mem.ToArray()));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void DeserializeAddressV2()
		{
			var stream = new BitcoinStream(Encoders.Hex.DecodeData(stream_addrv2_hex)){
				Type = SerializationType.Network,
				ProtocolVersion = NetworkAddress.AddrV2Format
			};
			NetworkAddress[] addresses = null;
			stream.ReadWrite(ref addresses);
			Assert.Equal(fixture_addresses.Length, addresses.Length);
			for (var i = 0; i < fixture_addresses.Length; i++)
			{
				Assert.Equal(fixture_addresses[i].ToAddressString(), addresses[i].ToAddressString());
			}
		}
	}

	public static class StreamExtensions
	{
		public static void Clear(this Stream s)
		{
			s.Seek(0, SeekOrigin.Begin);
			s.SetLength(0);
		}
	}
}
