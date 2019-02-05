﻿using System;
using System.Linq;
using System.Text;
using NBitcoin.DataEncoders;
using Xunit;

namespace NBitcoin.Tests
{
	[Trait("UnitTest", "UnitTest")]
	public class Bech32Test
	{
		private static string[] VALID_CHECKSUM =
		{
			"A12UEL5L",
			"a12uel5l",
			"an83characterlonghumanreadablepartthatcontainsthenumber1andtheexcludedcharactersbio1tt5tgs",
			"abcdef1qpzry9x8gf2tvdw0s3jn54khce6mua7lmqqqxw",
			"11qqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqc8247j",
			"split1checkupstagehandshakeupstreamerranterredcaperred2y9e3w",
			"?1ezyfcl"
		};

		private static string[] INVALID_CHECKSUM =
		{
			(char)0x20 + "1nwldj5", // HRP character out of range
			(char)0x7F + "1axkwrx", // HRP character out of range
			(char)0x80 + "1eym55h", // HRP character out of range
			"an84characterslonghumanreadablepartthatcontainsthenumber1andtheexcludedcharactersbio1569pvx", // overall max length exceeded
			"pzry9x0s0muk", // No separator character
			"1pzry9x0s0muk", // Empty HRP
			"x1b4n0q5v", // Invalid data character
			"li1dgmt3", // Too short checksum
			"de1lg7wt" + (char)0xFF, // Invalid character in checksum
			"A1G7SGD8", // checksum calculated with uppercase form of HRP
			"10a06t8", // empty HRP
			"1qzzfhee", // empty HRP
		};

		private static string[][] VALID_ADDRESS = {
			new [] { "BC1QW508D6QEJXTDG4Y5R3ZARVARY0C5XW7KV8F3T4", "0014751e76e8199196d454941c45d1b3a323f1433bd6"},
			new [] { "tb1qrp33g0q5c5txsp9arysrx4k6zdkfs4nce4xj0gdcccefvpysxf3q0sl5k7","00201863143c14c5166804bd19203356da136c985678cd4d27a1b8c6329604903262"},
			new [] { "bc1pw508d6qejxtdg4y5r3zarvary0c5xw7kw508d6qejxtdg4y5r3zarvary0c5xw7k7grplx", "5128751e76e8199196d454941c45d1b3a323f1433bd6751e76e8199196d454941c45d1b3a323f1433bd6"},
			new [] { "BC1SW50QA3JX3S", "6002751e"},
			new [] { "bc1zw508d6qejxtdg4y5r3zarvaryvg6kdaj", "5210751e76e8199196d454941c45d1b3a323"},
			new [] { "tb1qqqqqp399et2xygdj5xreqhjjvcmzhxw4aywxecjdzew6hylgvsesrxh6hy", "0020000000c4a5cad46221b2a187905e5266362b99d5e91c6ce24d165dab93e86433"},
		};

		private static string[] INVALID_ADDRESS = {
			"tc1qw508d6qejxtdg4y5r3zarvary0c5xw7kg3g4ty",
			"bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t5",
			"BC13W508D6QEJXTDG4Y5R3ZARVARY0C5XW7KN40WF2",
			"bc1rw5uspcuh",
			"bc10w508d6qejxtdg4y5r3zarvary0c5xw7kw508d6qejxtdg4y5r3zarvary0c5xw7kw5rljs90",
			"BC1QR508D6QEJXTDG4Y5R3ZARVARYV98GJ9P",
			"tb1qrp33g0q5c5txsp9arysrx4k6zdkfs4nce4xj0gdcccefvpysxf3q0sL5k7",
			"tb1pw508d6qejxtdg4y5r3zarqfsj6c3",
			"tb1qrp33g0q5c5txsp9arysrx4k6zdkfs4nce4xj0gdcccefvpysxf3pjxtptv",
		};

		[Fact]
		public void CanDetectError()
		{
			var bech = Encoders.Bech32("bc");
			byte wit;
			var ex = Assert.Throws<Bech32FormatException>(() => bech.Decode("bc1zw508e6qejxtdg4y5r3zarvaryvg6kdaj", out wit));
			Assert.Single(ex.ErrorIndexes);
			Assert.Equal(8, ex.ErrorIndexes[0]);

			ex = Assert.Throws<Bech32FormatException>(() => bech.Decode("bc1zw508e6qeextdg4y5r3zarvaryvg6kdaj", out wit));
			Assert.Equal(2, ex.ErrorIndexes.Length);
			Assert.Equal(8, ex.ErrorIndexes[0]);
			Assert.Equal(12, ex.ErrorIndexes[1]);
		}

		[Fact]
		public void ValidateValidChecksum()
		{
			foreach(var test in VALID_CHECKSUM)
			{
				var bech = Bech32Encoder.ExtractEncoderFromString(test);
				var pos = test.LastIndexOf('1');
				var test2 = test.Substring(0, pos + 1) + ((test[pos + 1]) ^ 1) + test.Substring(pos + 2);
				Assert.Throws<FormatException>(() => bech.Decode(test2, out var wit));
			}
		}

		[Fact]
		public void DetectInvalidChecksum()
		{
			foreach(var test in INVALID_CHECKSUM)
			{
				try
				{
					var bech = Bech32Encoder.ExtractEncoderFromString(test);
					var pos = test.LastIndexOf('1');
					var test2 = test.Substring(0, pos + 1) + ((test[pos + 1]) ^ 1) + test.Substring(pos + 2);
					bech.Decode(test2, out var wit);
					throw new Exception($"The \"{test}\" string was recognized as a valid bech32 encoded string. FormatException was expected.");
				}
				catch(FormatException)
				{}
			}
		}

		Bech32Encoder bech32 = Encoders.Bech32("bc");
		Bech32Encoder tbech32 = Encoders.Bech32("tb");
		[Fact]
		public void ValidAddress()
		{
			foreach(var address in VALID_ADDRESS)
			{
				byte witVer;
				byte[] witProg;
				Bech32Encoder encoder = bech32;
				try
				{
					witProg = bech32.Decode(address[0], out witVer);
					encoder = bech32;
				}
				catch
				{
					witProg = tbech32.Decode(address[0], out witVer);
					encoder = tbech32;
				}

				var scriptPubkey = Scriptpubkey(witVer, witProg);
				var hex = string.Join("", scriptPubkey.Select(x => x.ToString("x2")));
				Assert.Equal(hex, address[1]);

				var addr = encoder.Encode(witVer, witProg);
				Assert.Equal(address[0].ToLowerInvariant(), addr);
			}
		}

		[Fact]
		public void InvalidAddress()
		{
			foreach(var test in INVALID_ADDRESS)
			{
				byte witver;
				try
				{
					bech32.Decode(test, out witver);
				}
				catch(FormatException) { }
				try
				{
					tbech32.Decode(test, out witver);
				}
				catch(FormatException) { }
			}
		}

		private static byte[] Scriptpubkey(byte witver, byte[] witprog)
		{
			var v = witver > 0 ? witver + 0x50 : 0;
			return (new[] { (byte)v, (byte)witprog.Length }).Concat(witprog);
		}
	}
}
