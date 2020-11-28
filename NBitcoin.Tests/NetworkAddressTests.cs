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
			Assert.True(addr.IsTOR);

			Assert.True(addr.IsAddrV1Compatible);
			Assert.Equal("6hzph5hv6337r6p2.onion", addr.ToString());

			// TORv3
			var torv3_addr = "pg6mmjiyjmcrsslvykfwnntlaru7p5svn6y2ymmju6nubxndf4pscryd.onion";
			Assert.True(addr.SetSpecial(torv3_addr));
			Assert.True(addr.IsTOR);

			Assert.False(addr.IsAddrV1Compatible);
			Assert.Equal(addr.ToString(), torv3_addr);

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
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void SerializeV1()
		{
			var addr = new NetworkAddress();
			var mem = new MemoryStream();
			var stream = new BitcoinStream(mem, true);

			string HexStr() =>
				NBitcoin.DataEncoders.Encoders.Hex.EncodeData(mem.ToArray());

			// Add ADDRV2_FORMAT to the version so that the CNetAddr
			// serialize method produces an address in v2 format.
			stream.Type = SerializationType.Hash;

			stream.ReadWrite(addr);
			Assert.Equal("00000000000000000000000000000000", HexStr());

			mem.Seek(0, SeekOrigin.Begin);
			addr = new NetworkAddress(IPAddress.Parse("1.2.3.4"));
			stream.ReadWrite(addr);
			Assert.Equal("00000000000000000000ffff01020304", HexStr());

			mem.Seek(0, SeekOrigin.Begin);
			addr = new NetworkAddress(IPAddress.Parse("1a1b:2a2b:3a3b:4a4b:5a5b:6a6b:7a7b:8a8b"));
			stream.ReadWrite(addr);
			Assert.Equal("1a1b2a2b3a3b4a4b5a5b6a6b7a7b8a8b", HexStr());

			mem.Seek(0, SeekOrigin.Begin);
			addr = new NetworkAddress();
			addr.SetSpecial("6hzph5hv6337r6p2.onion");
			stream.ReadWrite(addr);
			Assert.Equal("fd87d87eeb43f1f2f3f4f5f6f7f8f9fa", HexStr());

			mem.Seek(0, SeekOrigin.Begin);
			addr = new NetworkAddress();
			addr.SetSpecial("pg6mmjiyjmcrsslvykfwnntlaru7p5svn6y2ymmju6nubxndf4pscryd.onion");
			stream.ReadWrite(addr);
			Assert.Equal("00000000000000000000000000000000", HexStr());

			mem.Seek(0, SeekOrigin.Begin);
			addr = new NetworkAddress();
			addr.SetInternal("a");
			stream.ReadWrite(addr);
			Assert.Equal("fd6b88c08724ca978112ca1bbdcafac2", HexStr());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void SerializeV2()
		{
			var addr = new NetworkAddress();
			var mem = new MemoryStream();
			var stream = new BitcoinStream(mem, true);

			string HexStr() =>
				NBitcoin.DataEncoders.Encoders.Hex.EncodeData(mem.ToArray());

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

			/*
			BOOST_REQUIRE(addr.SetInternal("a"));
			s << addr;
			BOOST_CHECK_EQUAL(HexStr(s), "0210fd6b88c08724ca978112ca1bbdcafac2");
			s.clear();
			*/
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void UnserializeV2()
		{
			var addr = new NetworkAddress();

			/*
			// Valid IPv6, contains embedded "internal".
			var payload = Encoders.Hex.DecodeData(
				"02" +         // network type (IPv6)
				"10" +         // address length
				"fd6b88c08724ca978112ca1bbdcafac2" +  // address: 0xfd + sha256("bitcoin")[0:5] + sha256(name)[0:10]
				"0000" );      // FIXME: the port

			var stream = new BitcoinStream(payload){
				ProtocolVersion = NetworkAddress.AddrV2Format,
				Type = SerializationType.Hash
			};
			stream.ReadWrite(ref addr);
			Assert.True(addr.IsInternal);
			Assert.True(addr.IsAddrV1Compatible);
			Assert.Equal("zklycewkdo64v6wc.internal", addr.ToString());
			Assert.True(stream.Inner.Length == stream.Inner.Position);
			*/

			// Valid IPv4.
			var payload = Encoders.Hex.DecodeData(
				"01" +         // network type (IPv4)
				"04" +         // address length
				"01020304");   // address
			var stream = new BitcoinStream(payload){
				ProtocolVersion = NetworkAddress.AddrV2Format,
				Type = SerializationType.Hash
			};
			stream.ReadWrite(ref addr);
			Assert.True(addr.IsValid);
			Assert.True(addr.IsIPv4);
			Assert.True(addr.IsAddrV1Compatible);
			Assert.Equal("1.2.3.4", addr.ToString());
			Assert.True(stream.Inner.Length == stream.Inner.Position);

			// Invalid IPv4, valid length but address itself is shorter.
			payload = Encoders.Hex.DecodeData(
				"01" +         // network type (IPv4)
				"04" +         // address length
				"0102");       // address

			stream = new BitcoinStream(payload){
				ProtocolVersion = NetworkAddress.AddrV2Format,
				Type = SerializationType.Hash
			};
			Assert.Throws<EndOfStreamException>(()=> stream.ReadWrite(ref addr));
			Assert.True(stream.Inner.Length == stream.Inner.Position);

			// Invalid IPv4, with bogus length.
			// Invalid IPv4, valid length but address itself is shorter.
			payload = Encoders.Hex.DecodeData(
				"01" +         // network type (IPv4)
				"05" +         // address length
				"0102030400"); // address
			stream = new BitcoinStream(payload){
				ProtocolVersion = NetworkAddress.AddrV2Format,
				Type = SerializationType.Hash
			};
			var ex = Assert.Throws<Exception>(()=> stream.ReadWrite(ref addr));
			Assert.Equal("BIP155 IPv4 address with length 5 (should be 4).", ex.Message);

			// Valid IPv6.
			payload = Encoders.Hex.DecodeData(
				"02" +         // network type (IPv6)
				"10" +         // address length
				"0102030405060708090a0b0c0d0e0f10" );   // address
			stream = new BitcoinStream(payload){
				ProtocolVersion = NetworkAddress.AddrV2Format,
				Type = SerializationType.Hash
			};
			stream.ReadWrite(ref addr);

			Assert.True(addr.IsValid);
			Assert.True(addr.IsIPv6);
			Assert.True(addr.IsAddrV1Compatible);
			Assert.Equal("102:304:506:708:90a:b0c:d0e:f10", addr.ToString());
			Assert.True(stream.Inner.Length == stream.Inner.Position);

			// Invalid IPv6, with bogus length.
			payload = Encoders.Hex.DecodeData(
				"02" +         // network type (IPv6)
				"04" +         // address length
				"00000000");   // address				
			stream = new BitcoinStream(payload){
				ProtocolVersion = NetworkAddress.AddrV2Format,
				Type = SerializationType.Hash
			};

			ex = Assert.Throws<Exception>(()=> stream.ReadWrite(ref addr));
			Assert.Equal("BIP155 IPv6 address with length 4 (should be 16).", ex.Message);

			// Invalid IPv6, contains embedded IPv4.
			payload = Encoders.Hex.DecodeData(
				"02" +         // network type (IPv6)
				"10" +         // address length
				"00000000000000000000ffff01020304");  // address
			stream = new BitcoinStream(payload){
				ProtocolVersion = NetworkAddress.AddrV2Format,
				Type = SerializationType.Hash
			};
			stream.ReadWrite(ref addr);

			Assert.False(addr.IsValid);
			Assert.True(stream.Inner.Length == stream.Inner.Position);

			// Invalid IPv6, contains embedded TORv2.
			payload = Encoders.Hex.DecodeData(
				"02" +         // network type (IPv6)
				"10" +         // address length
				"fd87d87eeb430102030405060708090a");   // address

			stream = new BitcoinStream(payload){
				ProtocolVersion = NetworkAddress.AddrV2Format,
				Type = SerializationType.Hash
			};
			stream.ReadWrite(ref addr);
			Assert.False(addr.IsValid);
			Assert.True(stream.Inner.Length == stream.Inner.Position);

			// Valid TORv2.
			payload = Encoders.Hex.DecodeData(
				"03" +         // network type (TORv2)
				"0a" +         // address length
				"f1f2f3f4f5f6f7f8f9fa" );   // address
			stream = new BitcoinStream(payload){
				ProtocolVersion = NetworkAddress.AddrV2Format,
				Type = SerializationType.Hash
			};
			stream.ReadWrite(ref addr);
			Assert.True(addr.IsValid);
			Assert.True(addr.IsTOR);
			Assert.True(addr.IsAddrV1Compatible);
			Assert.Equal("6hzph5hv6337r6p2.onion", addr.ToString());
			Assert.True(stream.Inner.Length == stream.Inner.Position);

			// Invalid TORv2, with bogus length.
			payload = Encoders.Hex.DecodeData(
				"03" +         // network type (TORv2)
				"07" +         // address length
				"00000000000000" );   // address

			stream = new BitcoinStream(payload){
				ProtocolVersion = NetworkAddress.AddrV2Format,
				Type = SerializationType.Hash
			};
			ex = Assert.Throws<Exception>(()=> stream.ReadWrite(ref addr));
			Assert.Equal("BIP155 TORv2 address with length 7 (should be 10).", ex.Message);

			// Valid TORv3.
			payload = Encoders.Hex.DecodeData(
				"04" +         // network type (TORv3)
				"20" +         // address length
				"79bcc625184b05194975c28b66b66b0469f7f6556fb1ac3189a79b40dda32f1f");   // address

			stream = new BitcoinStream(payload){
				ProtocolVersion = NetworkAddress.AddrV2Format,
				Type = SerializationType.Hash
			};
			stream.ReadWrite(ref addr);
			Assert.True(addr.IsValid);
			Assert.True(addr.IsTOR);
			Assert.False(addr.IsAddrV1Compatible);
			Assert.Equal("pg6mmjiyjmcrsslvykfwnntlaru7p5svn6y2ymmju6nubxndf4pscryd.onion", addr.ToString());
			Assert.True(stream.Inner.Length == stream.Inner.Position);

			// Invalid TORv3, with bogus length.
			payload = Encoders.Hex.DecodeData(
				"04" +         // network type (TORv3)
				"00" +         // address length
				"00" );        // address

			stream = new BitcoinStream(payload){
				ProtocolVersion = NetworkAddress.AddrV2Format,
				Type = SerializationType.Hash
			};
			ex = Assert.Throws<Exception>(()=> stream.ReadWrite(ref addr));
			Assert.Equal("BIP155 TORv3 address with length 0 (should be 32).", ex.Message);
			Assert.True(stream.Inner.Length != stream.Inner.Position);

			// Valid I2P.
			payload = Encoders.Hex.DecodeData(
				"05" +         // network type (I2P)
				"20" +         // address length
				"a2894dabaec08c0051a481a6dac88b64f98232ae42d4b6fd2fa81952dfe36a87");         // address

			stream = new BitcoinStream(payload){
				ProtocolVersion = NetworkAddress.AddrV2Format,
				Type = SerializationType.Hash
			};
			stream.ReadWrite(ref addr);
			Assert.True(addr.IsValid);
			Assert.True(addr.IsI2P);
			Assert.False(addr.IsAddrV1Compatible);
			Assert.Equal("ukeu3k5oycgaauneqgtnvselmt4yemvoilkln7jpvamvfx7dnkdq.b32.i2p", addr.ToString());
			Assert.True(stream.Inner.Length == stream.Inner.Position);

			// Invalid I2P, with bogus length.
			payload = Encoders.Hex.DecodeData(
				"05" +         // network type (I2P)
				"03" +         // address length
				"000000");     // address

			stream = new BitcoinStream(payload){
				ProtocolVersion = NetworkAddress.AddrV2Format,
				Type = SerializationType.Hash
			};
			ex = Assert.Throws<Exception>(()=> stream.ReadWrite(ref addr));
			Assert.Equal("BIP155 I2P address with length 3 (should be 32).", ex.Message);

			// Valid CJDNS.
			payload = Encoders.Hex.DecodeData(
				"06" +         // network type (CJDNS)
				"10" +         // address length
				"fc000001000200030004000500060007");     // address

			stream = new BitcoinStream(payload){
				ProtocolVersion = NetworkAddress.AddrV2Format,
				Type = SerializationType.Hash
			};
			stream.ReadWrite(ref addr);
			Assert.True(addr.IsValid);
			Assert.True(addr.IsCjdns);
			Assert.False(addr.IsAddrV1Compatible);
			Assert.Equal("fc00:1:2:3:4:5:6:7", addr.ToString());
			Assert.True(stream.Inner.Length == stream.Inner.Position);

			// Invalid CJDNS, with bogus length.
			payload = Encoders.Hex.DecodeData(
				"06" +         // network type (CJDNS)
				"01" +         // address length
				"00");         // address

			stream = new BitcoinStream(payload){
				ProtocolVersion = NetworkAddress.AddrV2Format,
				Type = SerializationType.Hash
			};
			ex = Assert.Throws<Exception>(()=> stream.ReadWrite(ref addr));
			Assert.Equal("BIP155 Cjdns address with length 1 (should be 16).", ex.Message);

			// Unknown, with extreme length.
			payload = Encoders.Hex.DecodeData(
				"aa" +         // network type (unknown)
				"fe00000002" + // address length (CompactSize's MAX_SIZE)
				"01020304050607");         // address

			stream = new BitcoinStream(payload){
				ProtocolVersion = NetworkAddress.AddrV2Format,
				Type = SerializationType.Hash
			};
			ex = Assert.Throws<ArgumentOutOfRangeException>(()=> stream.ReadWrite(ref addr));
			Assert.Equal("Specified argument was out of the range of valid values. (Parameter 'Array size too big')", ex.Message);
			Assert.True(stream.Inner.Length != stream.Inner.Position);

			// Unknown, with reasonable length.
			payload = Encoders.Hex.DecodeData(
				"aa" +         // network type (unknown)
				"04" +         // address length
				"01020304");   // address

			stream = new BitcoinStream(payload){
				ProtocolVersion = NetworkAddress.AddrV2Format,
				Type = SerializationType.Hash
			};
			stream.ReadWrite(ref addr);
			Assert.False(addr.IsValid);
			Assert.True(stream.Inner.Length == stream.Inner.Position);

			// Unknown, with zero length.
			payload = Encoders.Hex.DecodeData(
				"aa" +         // network type (unknown)
				"00");         // address length

			stream = new BitcoinStream(payload){
				ProtocolVersion = NetworkAddress.AddrV2Format,
				Type = SerializationType.Hash
			};
			stream.ReadWrite(ref addr);
			Assert.False(addr.IsValid);
			Assert.True(stream.Inner.Length == stream.Inner.Position);
		}

		static Address[] fixture_addresses = new Address[]{
			new Address(IPAddress.IPv6Loopback, 0)
			{
				Services = NodeServices.Nothing,
				Time = Utils.UnixTimeToDateTime(0x4966bc61U) /* Fri Jan  9 02:54:25 UTC 2009 */
			},
			new Address(IPAddress.IPv6Loopback, 0x00f1 /* port */)
			{
				Services = NodeServices.Network,
				Time = Utils.UnixTimeToDateTime(0x83766279U) /* Tue Nov 22 11:22:33 UTC 2039 */
			},
			new Address(IPAddress.IPv6Loopback, 0xf1f2 /* port */)
			{
				Services = NodeServices.NODE_WITNESS | NodeServices.NODE_NETWORK_LIMITED | NodeServices.NODE_COMPACT_FILTERS,
				Time = Utils.UnixTimeToDateTime(0xffffffffU) /* Sun Feb  7 06:28:15 UTC 2106 */
			}
		};

		// fixture_addresses should equal to this when serialized in V1 format.
		// When this is unserialized from V1 format it should equal to fixture_addresses.
		static string stream_addrv1_hex =
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

		// fixture_addresses should equal to this when serialized in V2 format.
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
			Address[] addresses = null;
			stream.ReadWrite(ref addresses);
			Assert.Equal(fixture_addresses.Length, addresses.Length);
			Assert.All(Enumerable.Zip(fixture_addresses, addresses), t => Assert.Equal(t.First, t.Second));
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