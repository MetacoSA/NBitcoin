using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.OpenAsset;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Xunit;

namespace NBitcoin.Tests
{
	public class util_tests
	{
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

			// Spaces between bytes must not be supported
			Assert.Throws<FormatException>(() => Encoders.Hex.DecodeData("12 34 56 78"));

			// Stop parsing at invalid value
			Assert.Throws<FormatException>(() => Encoders.Hex.DecodeData("1234 invalid 1234"));
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanAddEntropyToRandom()
		{
			RandomUtils.AddEntropy(new byte[] { 1, 2, 3 });
			for(int i = 0; i < 100; i++)
			{
				Assert.Equal(50, RandomUtils.GetBytes(50).Length);
			}
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
				}.EncodeData(ParseHex_expected, 0, 5),
				"04 67 8a fd b0");

			AssertEx.Equal(
				new HexEncoder()
				{
					Space = true
				}.EncodeData(ParseHex_expected, 0, 0),
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
			var unpacked = new Target(uint256.Parse("00000000000404CB000000000000000000000000000000000000000000000000"));
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
			Assert.True(uint256.Parse("0x0000000000000000511e193e22d2dfc02aea8037988f0c58e9834f4550e97702") < packed.ToUInt256());

			//Check http://blockchain.info/block-index/394713/0000000000000000729a4a7e084c90f932d038c407a6535a51dfecdfba1c8906
			Assert.True(uint256.Parse("0x0000000000000000729a4a7e084c90f932d038c407a6535a51dfecdfba1c8906 ") < new Target(419470732).ToUInt256());

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
		[Trait("UnitTest", "UnitTest")]
		public void MoneyCoverage()
		{
			Money a = Money.Coins(2.0m);
			Money b = Money.Coins(4.0m);
			Assert.Equal(a, Money.Min(a, b));
			Assert.Equal(a, Money.Min(b, a));
			Assert.Equal(b, Money.Max(a, b));
			Assert.Equal(b, Money.Max(b, a));
			Assert.Equal(a, new Money(a.Satoshi));
			Assert.Equal(a.GetHashCode(), new Money(a.Satoshi).GetHashCode());
			Assert.True(Money.Coins(1.0m).Almost(Money.Coins(0.95m), 0.05m));
			Assert.False(Money.Coins(1.0m).Almost(Money.Coins(0.949m), 0.05m));
			Assert.Throws<ArgumentOutOfRangeException>(() => Money.Coins(1.0m).Almost(Money.Coins(0.949m), -0.05m));
			Assert.Throws<ArgumentOutOfRangeException>(() => Money.Coins(1.0m).Almost(Money.Coins(0.949m), -1.05m));
			long data = 5;
			Assert.Equal(Money.Coins(5), data * Money.Coins(1.0m));
			Assert.Equal(Money.Coins(5), Money.Coins(1.0m) * data);
			Assert.Equal(500000000L, (long)Money.Coins(5).Satoshi);
			Assert.Equal(500000000U, (uint)Money.Coins(5).Satoshi);
			Assert.Equal("5.00000000", Money.Coins(5).ToString());
		}


		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanConvertMoney()
		{
			var tests = new[]
			{
				new object[]{ 1.23456789m, MoneyUnit.BTC, 123456789m, MoneyUnit.Satoshi  },
				new object[]{ 1.23456789m, MoneyUnit.BTC, 1234.56789m, MoneyUnit.MilliBTC  },
				new object[]{ 1.23456789m, MoneyUnit.BTC, 1234567.89m, MoneyUnit.Bit  },
				new object[]{ 1.23456789m, MoneyUnit.BTC, 1.23456789m, MoneyUnit.BTC  },
			};

			foreach(var test in tests)
			{
				var inputAmount = (decimal)test[0];
				var inputUnit = (MoneyUnit)test[1];
				var outputAmount = (decimal)test[2];
				var outputUnit = (MoneyUnit)test[3];

				var result = new Money(inputAmount, inputUnit);
				var actual = result.ToUnit(outputUnit);

				Assert.Equal(outputAmount, actual);

				result = new Money(outputAmount, outputUnit);
				actual = result.ToUnit(inputUnit);

				Assert.Equal(inputAmount, actual);
			}
		}

		[Fact]
		[Trait("Core", "Core")]
		public void util_ParseMoney()
		{
			Money ret;
			foreach(var prefix in new string[] { "", "+", "-" })
			{
				int multiplier = prefix == "-" ? -1 : 1;
				Assert.True(Money.TryParse(prefix + "0.0", out ret));
				AssertEx.Equal(ret, multiplier * new Money(0));

				Assert.True(Money.TryParse(prefix + "12345.6789", out ret));
				AssertEx.Equal(ret, multiplier * new Money((Money.COIN / 10000) * 123456789));

				Assert.True(Money.TryParse(prefix + "100000000.00", out ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN * 100000000));
				Assert.True(Money.TryParse(prefix + "10000000.00", out ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN * 10000000));
				Assert.True(Money.TryParse(prefix + "1000000.00", out ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN * 1000000));
				Assert.True(Money.TryParse(prefix + "100000.00", out ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN * 100000));
				Assert.True(Money.TryParse(prefix + "10000.00", out ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN * 10000));
				Assert.True(Money.TryParse(prefix + "1000.00", out ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN * 1000));
				Assert.True(Money.TryParse(prefix + "100.00", out ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN * 100));
				Assert.True(Money.TryParse(prefix + "10.00", out ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN * 10));
				Assert.True(Money.TryParse(prefix + "1.00", out ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN));
				Assert.True(Money.TryParse(prefix + "0.1", out ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN / 10));
				Assert.True(Money.TryParse(prefix + "0.01", out ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN / 100));
				Assert.True(Money.TryParse(prefix + "0.001", out ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN / 1000));
				Assert.True(Money.TryParse(prefix + "0.0001", out ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN / 10000));
				Assert.True(Money.TryParse(prefix + "0.00001", out ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN / 100000));
				Assert.True(Money.TryParse(prefix + "0.000001", out ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN / 1000000));
				Assert.True(Money.TryParse(prefix + "0.0000001", out ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN / 10000000));
				Assert.True(Money.TryParse(prefix + "0.00000001", out ret));
				AssertEx.Equal(ret, multiplier * new Money(Money.COIN / 100000000));

				// Attempted 63 bit overflow should fail
				Assert.False(Money.TryParse(prefix + "92233720368.54775808", out ret));
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanSplitMoney()
		{
			CanSplitMoneyCore(Money.Satoshis(1234), 3);
			CanSplitMoneyCore(Money.Satoshis(1234), 2);
			CanSplitMoneyCore(Money.Satoshis(1234), 10);
			CanSplitMoneyCore(Money.Satoshis(1), 3);
			Assert.Throws<ArgumentOutOfRangeException>(() => CanSplitMoneyCore(Money.Satoshis(1000), 0));
			CanSplitMoneyCore(Money.Satoshis(0), 10);

			var result = Money.Satoshis(20).Split(3).ToArray();
			Assert.True(result[0].Satoshi == 7);
			Assert.True(result[1].Satoshi == 7);
			Assert.True(result[2].Satoshi == 6);
		}

		private void CanSplitMoneyCore(Money money, int parts)
		{
			var splitted = money.Split(parts).ToArray();
			Assert.True(splitted.Length == parts);
			Assert.True(splitted.Sum() == money);
			var groups = splitted.Select(s => s.Satoshi).GroupBy(o => o);
			var differentValues = groups.Count();
			Assert.True(differentValues == 1 || differentValues == 2);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanSplitMoneyBag()
		{
			var gold = new AssetId(new Key());
			MoneyBag bag = new MoneyBag();
			bag += Money.Coins(12);
			bag += new AssetMoney(gold, 10);
			var splitted = bag.Split(12).ToArray();
			Assert.Equal(Money.Coins(1.0m), splitted[0].GetAmount(null));
			Assert.Equal(new AssetMoney(gold, 1), splitted[0].GetAmount(gold));
			Assert.Equal(new AssetMoney(gold, 0), splitted[11].GetAmount(gold));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanSplitAssetMoney()
		{
			var gold = new AssetId(new Key());
			CanSplitAssetMoneyCore(gold, 1234, 3);
			CanSplitAssetMoneyCore(gold, 1234, 2);
			CanSplitAssetMoneyCore(gold, 1234, 10);
			CanSplitAssetMoneyCore(gold, 1, 3);
			Assert.Throws<ArgumentOutOfRangeException>(() => CanSplitAssetMoneyCore(gold, 1000, 0));
			CanSplitAssetMoneyCore(gold, 0, 10);

			var result = new AssetMoney(gold, 20).Split(3).ToArray();
			Assert.True(result[0].Quantity == 7);
			Assert.True(result[1].Quantity == 7);
			Assert.True(result[2].Quantity == 6);
			Assert.True(result[0].Id == gold);
		}

		private void CanSplitAssetMoneyCore(AssetId asset, long amount, int parts)
		{
			AssetMoney money = new AssetMoney(asset, amount);
			var splitted = money.Split(parts).ToArray();
			Assert.True(splitted.Length == parts);
			Assert.True(splitted.Sum(asset) == money);
			var groups = splitted.Select(s => s.Quantity).GroupBy(o => o);
			var differentValues = groups.Count();
			Assert.True(differentValues == 1 || differentValues == 2);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void MoneyUnitSanityCheck()
		{
			Money.FromUnit(10m, MoneyUnit.BTC);
			Money.FromUnit(10m, MoneyUnit.MilliBTC);
			Money.FromUnit(10m, MoneyUnit.Bit);
			Money.FromUnit(10m, MoneyUnit.Satoshi);

			Money.FromUnit(10m, (MoneyUnit)100000000);
			Money.FromUnit(10m, (MoneyUnit)100000);
			Money.FromUnit(10m, (MoneyUnit)100);
			Money.FromUnit(10m, (MoneyUnit)1);

			Assert.Throws<ArgumentException>(() => Money.FromUnit(10, (MoneyUnit)14));
			Assert.Throws<ArgumentException>(() => Money.FromUnit(10, (MoneyUnit)(-41)));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void Overflow()
		{
			Assert.Throws<OverflowException>(() => Money.Satoshis(decimal.MaxValue));
			Assert.Throws<OverflowException>(() => Money.Satoshis(decimal.MinValue));
			Assert.Throws<OverflowException>(() => Money.Satoshis(ulong.MaxValue));
			Assert.Throws<OverflowException>(() => Money.Satoshis(long.MinValue));

			Assert.Throws<OverflowException>(() => -1 * (Money)long.MinValue);

			Assert.Throws<OverflowException>(() =>
			{
				var m = (Money)long.MaxValue;
				m++;
			});
			Assert.Throws<OverflowException>(() =>
			{
				var m = (Money)(long.MinValue + 1);
				m--;
			});
			Assert.Throws<OverflowException>(() => -1 * (Money)long.MinValue);
		}

		[Fact]
		[Trait("Core", "Core")]
		public void util_IsHex()
		{
			Assert.True(HexEncoder.IsWellFormed("00"));
			Assert.True(HexEncoder.IsWellFormed("00112233445566778899aabbccddeeffAABBCCDDEEFF"));
			Assert.True(HexEncoder.IsWellFormed("ff"));
			Assert.True(HexEncoder.IsWellFormed("FF"));

			Assert.True(HexEncoder.IsWellFormed(""));
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
			var address = new BitcoinPubKeyAddress(new KeyId("47376c6f537d62177a2c41c4ca9b45829ab99083"), Network.Main);
			Assert.Equal("OP_DUP OP_HASH160 47376c6f537d62177a2c41c4ca9b45829ab99083 OP_EQUALVERIFY OP_CHECKSIG", address.ScriptPubKey.ToString());

			var scriptAddress = new BitcoinScriptAddress(new ScriptId("8f55563b9a19f321c211e9b9f38cdf686ea07845"), Network.Main);
			Assert.Equal("OP_HASH160 8f55563b9a19f321c211e9b9f38cdf686ea07845 OP_EQUAL", scriptAddress.ScriptPubKey.ToString());

			var pubKey = new PubKey("0359d3092e4a8d5f3b3948235b5dec7395259273ccf3c4e9d5e16695a3fc9588d6");
			Assert.Equal("OP_DUP OP_HASH160 4d29186f76581c7375d70499afd1d585149d42cd OP_EQUALVERIFY OP_CHECKSIG", pubKey.Hash.ScriptPubKey.ToString());
			Assert.Equal("0359d3092e4a8d5f3b3948235b5dec7395259273ccf3c4e9d5e16695a3fc9588d6 OP_CHECKSIG", pubKey.ScriptPubKey.ToString());

			Script script = new Script("0359d3092e4a8d5f3b3948235b5dec7395259273ccf3c4e9d5e16695a3fc9588d6 OP_CHECKSIG");
			Assert.Equal("OP_HASH160 a216e3bce8c1b3adf376731b6cd0b6936c4e053f OP_EQUAL", script.PaymentScript.ToString());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		//https://en.bitcoin.it/wiki/List_of_address_prefixes
		public void CanDeduceNetworkInBase58Constructor()
		{
			BitcoinAddress addr = new BitcoinPubKeyAddress("STnZQMnb6Sa5qsuvwgx1xVQCuPH9dnjTuE");
			Assert.Equal(addr.Network, Network.Main);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanUseSegwitAddress()
		{
			var address = (BitcoinWitPubKeyAddress)BitcoinAddress.Create("p2xtZoXeX5X8BP8JfFhQK2nD3emtjch7UeFm", BitcoinNetwork.Main);
			Assert.Equal("0014010966776006953d5567439e5e39f86a0d273bee", address.ScriptPubKey.ToHex());
			Assert.Equal("0014010966776006953d5567439e5e39f86a0d273bee", address.Hash.ScriptPubKey.ToHex());
			Assert.Equal("3R1ZpeYRXx5oFtJWNoUwLFoACixRQ7sDQa", address.GetScriptAddress().ToString());

			//Example of the BIP
			var pubkey = new PubKey("0450863AD64A87AE8A2FE83C1AF1A8403CB53F53E486D8511DAD8A04887E5B23522CD470243453A299FA9E77237716103ABC11A1DF38855ED6F2EE187E9C582BA6");
			Assert.Equal(new Script("OP_0 010966776006953D5567439E5E39F86A0D273BEE"), pubkey.GetSegwitAddress(BitcoinNetwork.Main).ScriptPubKey);
			Assert.Equal("p2xtZoXeX5X8BP8JfFhQK2nD3emtjch7UeFm", pubkey.GetSegwitAddress(BitcoinNetwork.Main).ToString());
		}

		//[Fact]
		//[Trait("UnitTest", "UnitTest")]
		//https://en.bitcoin.it/wiki/List_of_address_prefixes
		public void CanDetectBase58NetworkAndType()
		{
			// test disabled for now 

			new Key().PubKey.GetSegwitAddress(Network.TestNet);
			var tests = new[]
				{
					new
					{
						Base58 = "T7nYdHtL34xLZ2S5KwqgySNNzGxovhszhtDM3wQRUEfUbUVvRZzTW",
						ExpectedType = typeof(BitcoinWitScriptAddress),
						Network = Network.TestNet
					},
					new
					{
						Base58 = "p2yCHe3JxDcT62fvAraCKHYoiiCLZsUzdbRQ",
						ExpectedType = typeof(BitcoinWitPubKeyAddress),
						Network = Network.Main
					},
					new
					{
						Base58 = "QWzJyQDz7iRTPkLFBg6XEeJFwbYESFC5KXxk",
						ExpectedType = typeof(BitcoinWitPubKeyAddress),
						Network = Network.TestNet
					},
					new
					{
						Base58 = "bWqaKUZETiECYgmJNbNZUoanBxnAzoVjCNx",
						ExpectedType = typeof(BitcoinColoredAddress),
						Network = Network.TestNet
					},
					new
					{
						Base58 = "17VZNX1SN5NtKa8UQFxwQbFeFc3iqRYhem",
						ExpectedType = typeof(BitcoinPubKeyAddress),
						Network = Network.Main
					},
					new
					{
						Base58 = "17VZNX1SN5NtKa8UQFxwQbFeFc3iqRYhem",
						ExpectedType = typeof(BitcoinPubKeyAddress),
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
						ExpectedType = typeof(BitcoinPubKeyAddress),
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
						Base58 = "3qdi7TXgRo1qR",
						ExpectedType = (Type)null,
						Network = (Network)null
					},
					new
					{
						Base58 = "6PYLtMnXvfG3oJde97zRyLYFZCYizPU5T3LwgdYJz1fRhh16bU7u6PPmY7",
						ExpectedType = typeof(BitcoinEncryptedSecretNoEC),
						Network = (Network)null
					},
					new
					{
						Base58 = "6PfQu77ygVyJLZjfvMLyhLMQbYnu5uguoJJ4kMCLqWwPEdfpwANVS76gTX",
						ExpectedType = typeof(BitcoinEncryptedSecretEC),
						Network = (Network)null
					},
					new
					{
						Base58 = "passphrasepxFy57B9v8HtUsszJYKReoNDV6VHjUSGt8EVJmux9n1J3Ltf1gRxyDGXqnf9qm",
						ExpectedType = typeof(BitcoinPassphraseCode),
						Network = (Network)null
					},
					new
					{
						Base58 = "cfrm38V8aXBn7JWA1ESmFMUn6erxeBGZGAxJPY4e36S9QWkzZKtaVqLNMgnifETYw7BPwWC9aPD",
						ExpectedType = typeof(BitcoinConfirmationCode),
						Network = (Network)null
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
					},
					new
					{
						Base58 = "akB4NBW9UuCmHuepksob6yfZs6naHtRCPNy",
						ExpectedType = typeof(BitcoinColoredAddress),
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
					if(test.Network != null)
						Assert.Equal(test.Network, result.Network);
					Network.CreateFromBase58Data(test.Base58, test.Network);

					if(test.Network != null)
						foreach(var network in Network.GetNetworks())
						{
							if(network == test.Network)
								break;
							Assert.Throws<FormatException>(() => Network.CreateFromBase58Data(test.Base58, network));
						}
				}
			}
		}

		//[Fact]
		//[Trait("UnitTest", "UnitTest")]
		public void CanParseBlockJSON()
		{
			// disabled for now

			var jobj = JObject.Parse(File.ReadAllText("Data/blocks/Block1.json"));
			var array = (JArray)jobj["mrkl_tree"];
			var expected = array.OfType<JValue>().Select(v => uint256.Parse(v.ToString())).ToList();
			var block = Block.ParseJson(File.ReadAllText("Data/blocks/Block1.json"));
			Assert.Equal("000000000000000040cd080615718eb68f00a0138706e7afd4068f3e08d4ca20", block.GetHash().ToString());
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


		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void MoneyBagOperations()
		{
			var msft = new AssetId("8f316d9a09");
			var goog = new AssetId("097f175bc8");
			var usd = new AssetId("6d2e8c766a");

			// 10 MSFT + 3 GOOG
			var mb = new MoneyBag(new AssetMoney(msft, 10), new AssetMoney(goog, 3));

			// (10 MSFT + 3 GOOG) + 1000 satoshis
			Assert.Equal(
				new MoneyBag(new AssetMoney(msft, 10), new AssetMoney(goog, 3), new Money(1000)),
				mb + Money.Satoshis(1000));

			// (10 MSFT + 3 GOOG) + 30 GOOG == (10 MSFT + 33 GOOG)
			Assert.Equal(
				new MoneyBag(new AssetMoney(msft, 10), new AssetMoney(goog, 33)),
				mb + new AssetMoney(goog, 30));

			// (10 MSFT + 3 GOOG) + (10 MSFT + 3 GOOG) == (20 MSFT + 6 GOOG)
			Assert.Equal(
				new MoneyBag(new AssetMoney(msft, 20), new AssetMoney(goog, 6)),
				mb + mb);

			//-----
			// (10 MSFT + 3 GOOG) - 1000 satoshis
			Assert.Equal(
				new MoneyBag(new AssetMoney(msft, 10), new AssetMoney(goog, 3), new Money(-1000)),
				mb - (Money.Satoshis(1000)));

			// (10 MSFT + 3 GOOG) - 30 GOOG == (10 MSFT - 27 GOOG)
			Assert.Equal(
				new MoneyBag(new AssetMoney(msft, 10), new AssetMoney(goog, -27)),
				mb - (new AssetMoney(goog, 30)));

			// (10 MSFT + 3 GOOG) - (10 MSFT + 3 GOOG) == ()
			Assert.Equal(
				new MoneyBag(),
				mb - (mb));

			// (10 MSFT + 3 GOOG) - (1 MSFT - 5 GOOG) +  10000 Satoshi == (9 MSFT + 8 GOOG + 10000 Satoshi)
			var b1 = new MoneyBag(new AssetMoney(msft, 10), new AssetMoney(goog, 3));
			var b2 = new MoneyBag(new AssetMoney(msft, 1), new AssetMoney(goog, -5));

			var b1_2 = b1 - (b2) + (new Money(10000));
			Assert.True(
				b1_2.SequenceEqual(new IMoney[] { new AssetMoney(msft, 9), new AssetMoney(goog, 8), new Money(10000) }));
		}
	}
}
