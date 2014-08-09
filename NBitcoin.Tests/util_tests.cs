﻿using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class util_tests
	{
		[Fact]
		[Trait("Core", "Core")]
		public void util_MedianFilter()
		{
			MedianFilterInt32 filter = new MedianFilterInt32(5, 15);

			AssertEx.Equal(filter.Median, 15);

			filter.Input(20); // [15 20]
			AssertEx.Equal(filter.Median, 17);

			filter.Input(30); // [15 20 30]
			AssertEx.Equal(filter.Median, 20);

			filter.Input(3); // [3 15 20 30]
			AssertEx.Equal(filter.Median, 17);

			filter.Input(7); // [3 7 15 20 30]
			AssertEx.Equal(filter.Median, 15);

			filter.Input(18); // [3 7 18 20 30]
			AssertEx.Equal(filter.Median, 18);

			filter.Input(0); // [0 3 7 18 30]
			AssertEx.Equal(filter.Median, 7);
		}


		static byte[] ParseHex_expected = new byte[]{
    0x04, 0x67, 0x8a, 0xfd, 0xb0, 0xfe, 0x55, 0x48, 0x27, 0x19, 0x67, 0xf1, 0xa6, 0x71, 0x30, 0xb7,
    0x10, 0x5c, 0xd6, 0xa8, 0x28, 0xe0, 0x39, 0x09, 0xa6, 0x79, 0x62, 0xe0, 0xea, 0x1f, 0x61, 0xde,
    0xb6, 0x49, 0xf6, 0xbc, 0x3f, 0x4c, 0xef, 0x38, 0xc4, 0xf3, 0x55, 0x04, 0xe5, 0x1e, 0xc1, 0x12,
    0xde, 0x5c, 0x38, 0x4d, 0xf7, 0xba, 0x0b, 0x8d, 0x57, 0x8a, 0x4c, 0x70, 0x2b, 0x6b, 0xf1, 0x1d,
    0x5f};

		[Fact]
		[Trait("Core", "Core")]
		public void util_ParseHex()
		{
			// Basic test vector
			var result = Encoders.Hex.DecodeData("04678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0EA1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5f");
			AssertEx.CollectionEquals(result, ParseHex_expected);

			// Spaces between bytes must be supported
			result = Encoders.Hex.DecodeData("12 34 56 78");
			Assert.True(result.Length == 4 && result[0] == 0x12 && result[1] == 0x34 && result[2] == 0x56 && result[3] == 0x78);

			// Stop parsing at invalid value
			result = Encoders.Hex.DecodeData("1234 invalid 1234");
			Assert.True(result.Length == 2 && result[0] == 0x12 && result[1] == 0x34);
		}

		[Fact]
		[Trait("Core", "Core")]
		public void util_HexStr()
		{
			AssertEx.Equal(
	  new HexEncoder().EncodeData(ParseHex_expected),
	  "04678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5f");

			AssertEx.Equal(
				new HexEncoder()
				{
					Space = true
				}.EncodeData(ParseHex_expected, 5),
				"04 67 8a fd b0");

			AssertEx.Equal(
				new HexEncoder()
				{
					Space = true
				}.EncodeData(ParseHex_expected, 0),
				"");

			var ParseHex_vec = ParseHex_expected.Take(5).ToArray();

			AssertEx.Equal(
				new HexEncoder()
				{
					Space = true
				}.EncodeData(ParseHex_vec),
				"04 67 8a fd b0");
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ScryptTest()
		{
			var message = "Hello world message";
			var salt = Encoding.UTF8.GetBytes("This is salt");
			var result = SCrypt.BitcoinComputeDerivedKey(message, salt);
			Assert.Equal("2331e1fe210127c9ac8fa95eb388e9dd072893890e2ee5646318ceb66089bbfe5ab45f762feeddf53d21c9a2cb183869247c9814f2bff1917fbea8239c548d1d"
				, Encoders.Hex.EncodeData(result));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		//https://en.bitcoin.it/wiki/Difficulty
		public void CanReadConvertTargetToDifficulty()
		{
			var packed = new Target(TestUtils.ParseHex("1b0404cb"));
			var unpacked = new Target(new uint256("00000000000404CB000000000000000000000000000000000000000000000000"));
			Assert.Equal(packed, unpacked);
			Assert.Equal(packed, new Target(0x1b0404cb));

			packed = new Target(TestUtils.ParseHex("1b8404cb"));
			Assert.True(packed.ToBigInteger() < 0);
			Assert.Equal(packed, new Target(0x1b8404cb));

			packed = new Target(TestUtils.ParseHex("1d00ffff"));
			Assert.Equal(1, packed.Difficulty);
			Assert.Equal(Target.Difficulty1, packed);

			packed = new Target(TestUtils.ParseHex("1b0404cb"));
			Assert.Equal(16307.420938523983D, packed.Difficulty, "420938523983".Length);

			Assert.Equal(packed, new Target((uint)0x1b0404cb));
			Assert.Equal((uint)packed, (uint)0x1b0404cb);

			packed = new Target(0x1d00ffff);
			Assert.Equal((uint)packed, (uint)0x1d00ffff);

			//Check http://blockchain.info/block-index/392672/0000000000000000511e193e22d2dfc02aea8037988f0c58e9834f4550e97702
			packed = new Target(419470732);
			Assert.Equal(6978842649.592383, packed.Difficulty, "592383".Length);
			Assert.Equal((uint)packed, (uint)419470732);
			Assert.True(new uint256("0x0000000000000000511e193e22d2dfc02aea8037988f0c58e9834f4550e97702") < packed.ToUInt256());

			//Check http://blockchain.info/block-index/394713/0000000000000000729a4a7e084c90f932d038c407a6535a51dfecdfba1c8906
			Assert.True(new uint256("0x0000000000000000729a4a7e084c90f932d038c407a6535a51dfecdfba1c8906 ") < new Target(419470732).ToUInt256());

			var genesis = Network.Main.GetGenesis();
			Assert.True(genesis.GetHash() < genesis.Header.Bits.ToUInt256());
		}

		[Fact]
		[Trait("Core", "Core")]
		public void util_FormatMoney()
		{
			AssertEx.Equal(new Money(0).ToString(false), "0.00");
			AssertEx.Equal(new Money((Money.COIN / 10000) * 123456789).ToString(false), "12345.6789");
			AssertEx.Equal(new Money(Money.COIN).ToString(true), "+1.00");
			AssertEx.Equal(new Money(-Money.COIN).ToString(false), "-1.00");
			AssertEx.Equal(new Money(-Money.COIN).ToString(true), "-1.00");

			AssertEx.Equal(new Money(Money.COIN * 100000000).ToString(false), "100000000.00");
			AssertEx.Equal(new Money(Money.COIN * 10000000).ToString(false), "10000000.00");
			AssertEx.Equal(new Money(Money.COIN * 1000000).ToString(false), "1000000.00");
			AssertEx.Equal(new Money(Money.COIN * 100000).ToString(false), "100000.00");
			AssertEx.Equal(new Money(Money.COIN * 10000).ToString(false), "10000.00");
			AssertEx.Equal(new Money(Money.COIN * 1000).ToString(false), "1000.00");
			AssertEx.Equal(new Money(Money.COIN * 100).ToString(false), "100.00");
			AssertEx.Equal(new Money(Money.COIN * 10).ToString(false), "10.00");
			AssertEx.Equal(new Money(Money.COIN).ToString(false), "1.00");
			AssertEx.Equal(new Money(Money.COIN / 10).ToString(false), "0.10");
			AssertEx.Equal(new Money(Money.COIN / 100).ToString(false), "0.01");
			AssertEx.Equal(new Money(Money.COIN / 1000).ToString(false), "0.001");
			AssertEx.Equal(new Money(Money.COIN / 10000).ToString(false), "0.0001");
			AssertEx.Equal(new Money(Money.COIN / 100000).ToString(false), "0.00001");
			AssertEx.Equal(new Money(Money.COIN / 1000000).ToString(false), "0.000001");
			AssertEx.Equal(new Money(Money.COIN / 10000000).ToString(false), "0.0000001");
			AssertEx.Equal(new Money(Money.COIN / 100000000).ToString(false), "0.00000001");
		}

		[Fact]
		[Trait("Core", "Core")]
		public void util_ParseMoney()
		{
			foreach(var prefix in new string[] { "", "+", "-" })
			{
				int multiplier = prefix == "-" ? -1 : 1;
				Money ret = new Money(0);
				Assert.True(Money.TryParse(prefix + "0.0", out ret));
				AssertEx.Equal(ret, multiplier * new Money(0));

				Assert.True(Money.TryParse(prefix + "12345.6789", out  ret));
				AssertEx.Equal(ret, multiplier * new Money((Money.COIN / 10000) * 123456789));

				Assert.True(Money.TryParse(prefix + "100000000.00", out  ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN * 100000000));
				Assert.True(Money.TryParse(prefix + "10000000.00", out  ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN * 10000000));
				Assert.True(Money.TryParse(prefix + "1000000.00", out  ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN * 1000000));
				Assert.True(Money.TryParse(prefix + "100000.00", out ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN * 100000));
				Assert.True(Money.TryParse(prefix + "10000.00", out  ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN * 10000));
				Assert.True(Money.TryParse(prefix + "1000.00", out  ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN * 1000));
				Assert.True(Money.TryParse(prefix + "100.00", out  ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN * 100));
				Assert.True(Money.TryParse(prefix + "10.00", out ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN * 10));
				Assert.True(Money.TryParse(prefix + "1.00", out  ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN));
				Assert.True(Money.TryParse(prefix + "0.1", out  ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN / 10));
				Assert.True(Money.TryParse(prefix + "0.01", out  ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN / 100));
				Assert.True(Money.TryParse(prefix + "0.001", out  ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN / 1000));
				Assert.True(Money.TryParse(prefix + "0.0001", out  ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN / 10000));
				Assert.True(Money.TryParse(prefix + "0.00001", out  ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN / 100000));
				Assert.True(Money.TryParse(prefix + "0.000001", out  ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN / 1000000));
				Assert.True(Money.TryParse(prefix + "0.0000001", out ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN / 10000000));
				Assert.True(Money.TryParse(prefix + "0.00000001", out  ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN / 100000000));

				// Attempted 63 bit overflow should fail
				Assert.True(!Money.TryParse(prefix + "92233720368.54775808", out  ret));
			}
		}
		[Fact]
		[Trait("Core", "Core")]
		public void util_IsHex()
		{
			Assert.True(HexEncoder.IsWellFormed("00"));
			Assert.True(HexEncoder.IsWellFormed("00112233445566778899aabbccddeeffAABBCCDDEEFF"));
			Assert.True(HexEncoder.IsWellFormed("ff"));
			Assert.True(HexEncoder.IsWellFormed("FF"));

			Assert.True(!HexEncoder.IsWellFormed(""));
			Assert.True(!HexEncoder.IsWellFormed("0"));
			Assert.True(!HexEncoder.IsWellFormed("a"));
			Assert.True(!HexEncoder.IsWellFormed("eleven"));
			Assert.True(!HexEncoder.IsWellFormed("00xx00"));
			Assert.True(!HexEncoder.IsWellFormed("0x0000"));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanRoundTripBigIntegerToBytes()
		{
			foreach(var expected in Enumerable.Range(-100, 100))
			{
				var bytes = Utils.BigIntegerToBytes(expected);
				var actual = Utils.BytesToBigInteger(bytes);
				Assert.Equal(expected, actual);
			}
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanConvertBigIntegerToBytes()
		{
			Assert.Equal<BigInteger>(0, Utils.BytesToBigInteger(new byte[0]));
			Assert.Equal<BigInteger>(0, Utils.BytesToBigInteger(new byte[] { 0 }));
			Assert.Equal<BigInteger>(0, Utils.BytesToBigInteger(new byte[] { 0x80 }));
			Assert.Equal<BigInteger>(1, Utils.BytesToBigInteger(new byte[] { 1 }));
			Assert.Equal<BigInteger>(-1, Utils.BytesToBigInteger(new byte[] { 0x81 }));
			Assert.Equal<BigInteger>(-128, Utils.BytesToBigInteger(new byte[] { 0x80, 0x80 }));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void NetworksAreValid()
		{
			foreach(var network in Network.GetNetworks())
			{
				Assert.NotNull(network);
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanGenerateScriptFromAddress()
		{
			var address = new BitcoinAddress(new KeyId("47376c6f537d62177a2c41c4ca9b45829ab99083"), Network.Main);
			Assert.Equal("OP_DUP OP_HASH160 47376c6f537d62177a2c41c4ca9b45829ab99083 OP_EQUALVERIFY OP_CHECKSIG", address.PaymentScript.ToString());

			var scriptAddress = new BitcoinScriptAddress(new ScriptId("8f55563b9a19f321c211e9b9f38cdf686ea07845"), Network.Main);
			Assert.Equal("OP_HASH160 8f55563b9a19f321c211e9b9f38cdf686ea07845 OP_EQUAL", scriptAddress.PaymentScript.ToString());

			var pubKey = new PubKey("0359d3092e4a8d5f3b3948235b5dec7395259273ccf3c4e9d5e16695a3fc9588d6");
			Assert.Equal("OP_DUP OP_HASH160 4d29186f76581c7375d70499afd1d585149d42cd OP_EQUALVERIFY OP_CHECKSIG", pubKey.HashPaymentScript.ToString());
			Assert.Equal("0359d3092e4a8d5f3b3948235b5dec7395259273ccf3c4e9d5e16695a3fc9588d6 OP_CHECKSIG", pubKey.PaymentScript.ToString());

			Script script = new Script("0359d3092e4a8d5f3b3948235b5dec7395259273ccf3c4e9d5e16695a3fc9588d6 OP_CHECKSIG");
			Assert.Equal("OP_HASH160 a216e3bce8c1b3adf376731b6cd0b6936c4e053f OP_EQUAL", script.PaymentScript.ToString());
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		//https://en.bitcoin.it/wiki/List_of_address_prefixes
		public void CanDetectBase58NetworkAndType()
		{
			var tests = new[]
				{
					new
					{
						Base58 = "17VZNX1SN5NtKa8UQFxwQbFeFc3iqRYhem",
						ExpectedType = typeof(BitcoinAddress),
						Network = Network.Main
					},
					new
					{
						Base58 = "3EktnHQD7RiAE6uzMj2ZifT9YgRrkSgzQX",
						ExpectedType = typeof(BitcoinScriptAddress),
						Network = Network.Main
					},
					new
					{
						Base58 = "mipcBbFg9gMiCh81Kj8tqqdgoZub1ZJRfn",
						ExpectedType = typeof(BitcoinAddress),
						Network = Network.TestNet
					},
					new
					{
						Base58 = "5Hwgr3u458GLafKBgxtssHSPqJnYoGrSzgQsPwLFhLNYskDPyyA",
						ExpectedType = typeof(BitcoinSecret),
						Network = Network.Main
					},
					new
					{
						Base58 = "92Pg46rUhgTT7romnV7iGW6W1gbGdeezqdbJCzShkCsYNzyyNcc",
						ExpectedType = typeof(BitcoinSecret),
						Network = Network.TestNet
					},
					new
					{
						Base58 = "DA1796XbaYxBwSc41yTDiirr1uuNkS446P",
						ExpectedType = (Type)null,
						Network = (Network)null
					},
					new
					{
						Base58 = "6PYLtMnXvfG3oJde97zRyLYFZCYizPU5T3LwgdYJz1fRhh16bU7u6PPmY7",
						ExpectedType = typeof(BitcoinEncryptedSecretNoEC),
						Network = Network.Main
					},
					new
					{
						Base58 = "6PfQu77ygVyJLZjfvMLyhLMQbYnu5uguoJJ4kMCLqWwPEdfpwANVS76gTX",
						ExpectedType = typeof(BitcoinEncryptedSecretEC),
						Network = Network.Main
					},
					new
					{
						Base58 = "passphrasepxFy57B9v8HtUsszJYKReoNDV6VHjUSGt8EVJmux9n1J3Ltf1gRxyDGXqnf9qm",
						ExpectedType = typeof(BitcoinPassphraseCode),
						Network = Network.Main
					},
					new
					{
						Base58 = "cfrm38V8aXBn7JWA1ESmFMUn6erxeBGZGAxJPY4e36S9QWkzZKtaVqLNMgnifETYw7BPwWC9aPD",
						ExpectedType = typeof(BitcoinConfirmationCode),
						Network = Network.Main
					},
					new
					{
						Base58 = "xprv9s21ZrQH143K3Gx1VAAD1ueDmwoPQUApekxWYSJ1f4W4m1nUPpRGdV5sTVhixZJT5cP2NqtEMZ2mrwHdW5RWpohCwspWidCpcLALvioXDyz",
						ExpectedType = typeof(BitcoinExtKey),
						Network = Network.Main
					},
					new
					{
						Base58 = "xpub661MyMwAqRbcEhHavVcryjNF2uA5woK6JCNRNJB8Z3dxPU8VNBd9E8GP7fusw2bhgYe7BXt6izr5iUaYo483919jjdtfEpG8j97djnEgJqo",
						ExpectedType = typeof(BitcoinExtPubKey),
						Network = Network.Main
					}
				};

			foreach(var test in tests)
			{
				if(test.ExpectedType == null)
				{
					Assert.Throws<FormatException>(() => Network.CreateFromBase58Data(test.Base58));
				}
				else
				{
					var result = Network.CreateFromBase58Data(test.Base58);
					Assert.True(test.ExpectedType == result.GetType());
					Assert.True(test.Network == result.Network);
				}
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseBlockJSON()
		{
			var jobj = JObject.Parse(File.ReadAllText("Data/blocks/Block1.json"));
			var array = (JArray)jobj["mrkl_tree"];
			var expected = array.OfType<JValue>().Select(v => new uint256(v.ToString())).ToList();
			var block = Block.Parse(File.ReadAllText("Data/blocks/Block1.json"));
			Assert.Equal("000000000000000040cd080615718eb68f00a0138706e7afd4068f3e08d4ca20", block.GetHash().ToString());
			block.CheckMerkleRoot();

			AssertEx.CollectionEquals(block.vMerkleTree.ToArray(), expected.ToArray());
			Assert.True(block.CheckMerkleRoot());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanConvertToUnixTime()
		{
			var date = Utils.UnixTimeToDateTime(1368576000);
			Assert.Equal(new DateTimeOffset(2013, 5, 15, 0, 0, 0, TimeSpan.Zero), date);
			Assert.Equal((uint)1368576000, Utils.DateTimeToUnixTime(date));

			Utils.DateTimeToUnixTime(Utils.UnixTimeToDateTime(uint.MaxValue));
			Utils.DateTimeToUnixTime(Utils.UnixTimeToDateTime(0));

			Assert.Throws<ArgumentOutOfRangeException>(() => Utils.DateTimeToUnixTime(Utils.UnixTimeToDateTime(uint.MaxValue) + TimeSpan.FromSeconds(1)));
			Assert.Throws<ArgumentOutOfRangeException>(() => Utils.DateTimeToUnixTime(Utils.UnixTimeToDateTime(0) - TimeSpan.FromSeconds(1)));
		}
	}
}
