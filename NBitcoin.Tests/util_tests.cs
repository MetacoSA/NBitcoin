#if !HAS_SPAN
using NBitcoin.BouncyCastle.Math;
#endif
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.JsonConverters;
using NBitcoin.OpenAsset;
using NBitcoin.RPC;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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
			for (int i = 0; i < 100; i++)
			{
				Assert.Equal(50, RandomUtils.GetBytes(50).Length);
			}
		}

		public class MyRandom : IRandom
		{
			private Random rnd = new Random(123456);
			public void GetBytes(byte[] output) => rnd.NextBytes(output);
#if HAS_SPAN
			public void GetBytes(Span<byte> output) => rnd.NextBytes(output);
#endif
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanDisableAdditionalEntropy()
		{
			RandomUtils.UseAdditionalEntropy = false;
			RandomUtils.Random = new MyRandom();
			var r1 = RandomUtils.GetUInt32();

			RandomUtils.Random = new MyRandom();
			var r2 = RandomUtils.GetUInt32();

			Assert.Equal(r1, r2);
		}

		[Fact]
		[Trait("Core", "Core")]
		public void util_HexStr()
		{
			AssertEx.Equal(
	  new HexEncoder().EncodeData(ParseHex_expected),
	  "04678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5f");

			AssertEx.Equal(
				new HexEncoder().EncodeData(ParseHex_expected, 0, 5),
				"04678afdb0");

			AssertEx.Equal(
				new HexEncoder().EncodeData(ParseHex_expected, 0, 0),
				"");

			var ParseHex_vec = ParseHex_expected.Take(5).ToArray();

			AssertEx.Equal(
				new HexEncoder().EncodeData(ParseHex_vec),
				"04678afdb0");
			var allHex = String.Join("", Enumerable.Range(0, 256).Select(v => v.ToString("x2")));
			var decoded = new HexEncoder().DecodeData(allHex);
			Assert.Equal(allHex, new HexEncoder().EncodeData(decoded));
			for (int i = 0; i < 256; i++)
			{
				Assert.Equal(i, decoded[i]);
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanGetAccountKeyPath()
		{
			Assert.Equal(new KeyPath(), new KeyPath().GetAddressKeyPath());
			Assert.Equal(new KeyPath("1"), new KeyPath("1").GetAddressKeyPath());
			Assert.Equal(new KeyPath(), new KeyPath("1'/2'").GetAddressKeyPath());
			Assert.Equal(new KeyPath("3/4"), new KeyPath("1'/2'/3/4").GetAddressKeyPath());
			Assert.Equal(new KeyPath("0/1"), new KeyPath("49'/0'/0'/0/1").GetAddressKeyPath());

			Assert.Equal(new KeyPath(), new KeyPath().GetAccountKeyPath());
			Assert.Equal(new KeyPath(), new KeyPath("1").GetAccountKeyPath());
			Assert.Equal(new KeyPath("1'/2'"), new KeyPath("1'/2'").GetAccountKeyPath());
			Assert.Equal(new KeyPath("1'/2'"), new KeyPath("1'/2'/3/4").GetAccountKeyPath());
			Assert.Equal(new KeyPath("49'/0'/0'"), new KeyPath("49'/0'/0'/0/1").GetAccountKeyPath());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseKeyPath()
		{
			var valid = new[]
			{
				"",
				"m",
				"m/0h",
				"m/0'",
				"m/0'/1",
				"m/0/1",
				"m/0h",
				"m/0h",
				"m/0h/1",
				"m/0/1",
				$"m/{0x80000000u - 1}h",
				$"m/{0x80000000u - 1}"
			}.ToList();
			var invalid = new[]
			{
				$"m/{uint.MaxValue}/1",
				$"m/{((long)uint.MaxValue) + 1}/1",
				$"k/0/1",
				$"h/1",
				$"m/'/1",
				$"m/'/1",
				$"m/{0x80000000u}h"
			}.ToList();
			var maxKeypath = new KeyPath(new uint[255]);
			Assert.Throws<ArgumentException>(() => new KeyPath(new uint[256]));
			valid.Add(maxKeypath.ToString());
			valid.Add("m/" + maxKeypath.ToString());
			invalid.Add("m/" + maxKeypath.ToString() + "/0");
			foreach (var v in valid)
			{
				KeyPath.Parse(v);
				Assert.True(KeyPath.TryParse(v, out _));
			}
			foreach (var v in invalid)
			{
				Assert.Throws<FormatException>(() => KeyPath.Parse(v));
				Assert.False(KeyPath.TryParse(v, out _));
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseRootedKeyPath()
		{
			var root = new BitcoinExtKey("tprv8ZgxMBicQKsPdXCKLrSbPbmWWCmwZwU6x9pQKyafAP76SpYnPy9tEMbsTgJ3rPPqvu1ZkM5Xz6w7v9rhvrHYUuPfQu2vu9YNwAxseujuABx", Network.RegTest);
			Assert.True(RootedKeyPath.TryParse("7b09d780/0'/0'/2'", out var result));
			Assert.Equal(root.GetPublicKey().GetHDFingerPrint(), result.MasterFingerprint);
			Assert.Equal("7b09d780/0'/0'/2'", result.ToString());
			Assert.Equal("7b09d780/0'/0'/2'/0/1", result.Derive(new KeyPath("0/1")).ToString());
			Assert.Equal("7b09d780/0'/0'/2'/2", result.Derive(2).ToString());
			Assert.Equal("7b09d780/0'/0'/2'", result.Derive(new KeyPath("0/1")).GetAccountKeyPath().ToString());
			Assert.Equal("7b09d780", result.MasterFingerprint.ToString());
			Assert.True(RootedKeyPath.TryParse("7b09d780/", out result));
			Assert.Equal("7b09d780", result.MasterFingerprint.ToString());
			Assert.Equal("7b09d780/", result.ToString());
			Assert.Equal(new KeyPath(), result.KeyPath);

			Assert.True(RootedKeyPath.TryParse(result.ToString(), out var result2));
			Assert.Equal(result, result2);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCalculateGoestlcoinTransactionHash()
		{
			var bs = new BitcoinStream(Encoders.Hex.DecodeData("020000000001010000000000000000000000000000000000000000000000000000000000000000ffffffff03510101ffffffff0200002cd6e21500002321023035994e950a694cd81e0384abc1850cfe3e541a9de9706ca669d21c61548f8bac0000000000000000266a24aa21a9ede2f61c3f71d1defd3fa999dfa36953755c690689799962b48bebd836974e8cf90120000000000000000000000000000000000000000000000000000000000000000000000000"));
			bs.ConsensusFactory = Altcoins.Groestlcoin.Instance.Mainnet.Consensus.ConsensusFactory;
			var tx = bs.ConsensusFactory.CreateTransaction();
			tx.ReadWrite(bs);
			Assert.Equal("f347692c823932e4329b2f720a9ecb84d945341a2d6e663ee4fbe23d53c50386", tx.GetHash().ToString());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanUseSegwitAddress()
		{
			var address = (BitcoinWitPubKeyAddress)BitcoinAddress.Create("bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4", Network.Main);
			var pubkey = new PubKey("0279BE667EF9DCBBAC55A06295CE870B07029BFCDB2DCE28D959F2815B16F81798");
			Assert.Equal(pubkey.WitHash.ScriptPubKey.ToHex(), address.ScriptPubKey.ToHex());
			Assert.Equal("0014751e76e8199196d454941c45d1b3a323f1433bd6", address.Hash.ScriptPubKey.ToHex());
			Assert.Equal("bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4", address.ScriptPubKey.GetDestinationAddress(address.Network).ToString());
			Assert.Equal("bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4", address.Hash.GetAddress(address.Network).ToString());
			Assert.Equal(Network.Main, address.Network);

			Assert.Equal("tb1qw508d6qejxtdg4y5r3zarvary0c5xw7kxpjzsx", address.ScriptPubKey.GetDestinationAddress(address.Network).ToNetwork(Network.TestNet).ToString());

			address = (BitcoinWitPubKeyAddress)BitcoinAddress.Create("tb1qw508d6qejxtdg4y5r3zarvary0c5xw7kxpjzsx", Network.TestNet);
			pubkey = new PubKey("0279BE667EF9DCBBAC55A06295CE870B07029BFCDB2DCE28D959F2815B16F81798");
			Assert.Equal(pubkey.WitHash.ScriptPubKey.ToHex(), address.ScriptPubKey.ToHex());
			Assert.Equal("tb1qw508d6qejxtdg4y5r3zarvary0c5xw7kxpjzsx", address.ScriptPubKey.GetDestinationAddress(address.Network).ToString());
			Assert.Equal("0014751e76e8199196d454941c45d1b3a323f1433bd6", address.Hash.ScriptPubKey.ToHex());
			Assert.Equal("tb1qw508d6qejxtdg4y5r3zarvary0c5xw7kxpjzsx", address.Hash.GetAddress(address.Network).ToString());
			Assert.Equal(Network.TestNet, address.Network);

			Assert.Throws<FormatException>(() => BitcoinAddress.Create("tb1qw508d6qejxtdg4y5r3zarvary0c5xw7kxpjzsx", Network.Main));

			var addressScript = (BitcoinWitScriptAddress)BitcoinAddress.Create("bc1qrp33g0q5c5txsp9arysrx4k6zdkfs4nce4xj0gdcccefvpysxf3qccfmv3", Network.Main);
			Assert.Equal(pubkey.ScriptPubKey.WitHash.ScriptPubKey.ToHex(), addressScript.ScriptPubKey.ToHex());
			Assert.Equal("bc1qrp33g0q5c5txsp9arysrx4k6zdkfs4nce4xj0gdcccefvpysxf3qccfmv3", addressScript.Hash.GetAddress(addressScript.Network).ToString());
			Assert.Equal("bc1qrp33g0q5c5txsp9arysrx4k6zdkfs4nce4xj0gdcccefvpysxf3qccfmv3", addressScript.ScriptPubKey.GetDestinationAddress(addressScript.Network).ToString());

			//Example of the BIP
			pubkey = new PubKey("0450863AD64A87AE8A2FE83C1AF1A8403CB53F53E486D8511DAD8A04887E5B23522CD470243453A299FA9E77237716103ABC11A1DF38855ED6F2EE187E9C582BA6");
			Assert.Equal(new Script("OP_0 010966776006953D5567439E5E39F86A0D273BEE"), pubkey.GetAddress(ScriptPubKeyType.Segwit, Network.Main).ScriptPubKey);


			//Test .ToNetwork()
			var addr = pubkey.GetAddress(ScriptPubKeyType.Legacy, Network.Main);
			Assert.Equal("16UwLL9Risc3QfPqBUvKofHmBQ7wMtjvM", addr.ToString());
			Assert.Equal("mfcSEPR8EkJrpX91YkTJ9iscdAzppJrG9j", addr.ToNetwork(Network.TestNet).ToString());

			Assert.Throws<FormatException>(() => Network.Parse<IBase58Data>("bc1qrp33g0q5c5txsp9arysrx4k6zdkfs4nce4xj0gdcccefvpysxf3qccfmv3", Network.Main));


			Network.Parse<IBitcoinString>("bc1qrp33g0q5c5txsp9arysrx4k6zdkfs4nce4xj0gdcccefvpysxf3qccfmv3", Network.Main);

			Assert.Throws<Bech32FormatException>(() => Network.Parse<IBitcoinString>("bc1qrp33g0q5c3txsp9arysrx4k6zdkfs4nce4xj0gdcccefvpysxf3qccfmv3", Network.Main));

			Assert.Throws<Bech32FormatException>(() => new BitcoinWitScriptAddress("bc1qrp33g0q5c3txsp9arysrx4k6zdkfs4nce4xj0gdcccefvpysxf3qccfmv3", Network.Main));

			Assert.Throws<Bech32FormatException>(() => new BitcoinWitPubKeyAddress("bc1qw507d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4", Network.Main));

			var extKey = new BitcoinExtKey(new ExtKey(), Network.Main);
			var segwit = extKey.AsHDScriptPubKey(ScriptPubKeyType.Segwit).ScriptPubKey.GetDestinationAddress(Network.Main);
			var segwit2 = extKey.ExtKey.AsHDScriptPubKey(ScriptPubKeyType.Segwit).ScriptPubKey.GetDestinationAddress(Network.Main);
			Assert.Equal(segwit, segwit2);
			Assert.IsType<NBitcoin.BitcoinWitPubKeyAddress>(segwit);
			var segwitP2SH = extKey.AsHDScriptPubKey(ScriptPubKeyType.SegwitP2SH).ScriptPubKey.GetDestinationAddress(Network.Main);
			var segwit2P2SH = extKey.ExtKey.AsHDScriptPubKey(ScriptPubKeyType.SegwitP2SH).ScriptPubKey.GetDestinationAddress(Network.Main);
			Assert.Equal(segwitP2SH, segwit2P2SH);
			Assert.IsType<NBitcoin.BitcoinScriptAddress>(segwitP2SH);
			var legacy = extKey.AsHDScriptPubKey(ScriptPubKeyType.Legacy).ScriptPubKey.GetDestinationAddress(Network.Main);
			var legacy2 = extKey.ExtKey.AsHDScriptPubKey(ScriptPubKeyType.Legacy).ScriptPubKey.GetDestinationAddress(Network.Main);
			var legacy3 = extKey.Neuter().AsHDScriptPubKey(ScriptPubKeyType.Legacy).ScriptPubKey.GetDestinationAddress(Network.Main);
			Assert.Equal(legacy, legacy2);
			Assert.Equal(legacy, legacy3);
			Assert.IsType<NBitcoin.BitcoinPubKeyAddress>(legacy);
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
#if NO_NATIVE_BIGNUM
			var max = NBitcoin.Target.ToBigInteger(new uint256("ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"));
			Assert.True(max.CompareTo(BigInteger.Zero) > 0);
			var maxExpected = new BigInteger(Encoders.Hex.DecodeData("ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff00").Reverse().ToArray());
			Assert.Equal(maxExpected, max);
			var maxminusone = NBitcoin.Target.ToBigInteger(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"));
			maxExpected = new BigInteger(Encoders.Hex.DecodeData("ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f").Reverse().ToArray());
			Assert.Equal(maxExpected, maxminusone);
#else
			var max = NBitcoin.Target.ToBigInteger(new uint256("ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"));
			Assert.True(max > System.Numerics.BigInteger.Zero);
			Assert.Equal(new uint256("ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"), NBitcoin.Target.ToUInt256(max));
			var maxExpected = new System.Numerics.BigInteger(Encoders.Hex.DecodeData("ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff00"));
			Assert.Equal(maxExpected, max);
			var maxminusone = NBitcoin.Target.ToBigInteger(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"));
			maxExpected = new System.Numerics.BigInteger(Encoders.Hex.DecodeData("ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f"));
			Assert.Equal(maxExpected, maxminusone);
			Assert.Equal(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"), NBitcoin.Target.ToUInt256(maxminusone));
#endif
			var limit = new Target(new uint256("00000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffff"));
			Assert.Equal(0x1D00FFFFU, (uint)limit);
			Assert.Equal("00000000ffff0000000000000000000000000000000000000000000000000000", limit.ToString());
			var limit2 = new Target(new uint256("000000007fffffffffffffffffffffffffffffffffffffffffffffffffffffff"));
			Assert.Equal(0x1c7fffffU, (uint)limit2);
			var packed = new Target(TestUtils.ParseHex("1b0404cb"));
			var unpacked = new Target(uint256.Parse("00000000000404CB000000000000000000000000000000000000000000000000"));

			Assert.Equal(packed, unpacked);
			Assert.Equal(packed, new Target(0x1b0404cb));

			packed = new Target(TestUtils.ParseHex("1b8404cb"));
#if NO_NATIVE_BIGNUM
			Assert.True(packed.ToBigInteger().CompareTo(BigInteger.Zero) > 0);
#else
			Assert.True(packed > System.Numerics.BigInteger.Zero);
#endif
			Assert.Equal(packed, new Target(0x1b8404cb));

			packed = new Target(TestUtils.ParseHex("1d00ffff"));
			Assert.Equal(packed, limit);
			Assert.Equal(1, packed.Difficulty);
			Assert.Equal(Target.Difficulty1, packed);

			packed = new Target(TestUtils.ParseHex("1b0404cb"));
			Assert.Equal(16307.420938523983D, packed.Difficulty, "420938523983".Length);

			Assert.Equal(packed, new Target(0x1b0404cb));
			Assert.Equal((uint)packed, (uint)0x1b0404cb);

			packed = new Target(0x1b0404d1);
			Assert.Equal(16307.04943863739, packed.Difficulty, "420938523983".Length);

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
			Assert.True(Target.Difficulty1 == Target.Difficulty1);


		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void Target_Should_Roundtrip_Implicit_Uint()
		{
			var orig = 0U;
			var target = (Target)orig;
			var roundtripped = (uint)target;

			Assert.Equal(orig, roundtripped);
		}


		[Fact]
		[Trait("Core", "Core")]
		public void util_FormatMoney()
		{
			AssertEx.Equal(Money.Zero.ToString(false), "0.00");
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
		public void CanDecodeBTrashAddress()
		{
			var bcash = NBitcoin.Altcoins.BCash.Instance.Mainnet;
			BitcoinAddress trashAddress = bcash.Parse<NBitcoin.Altcoins.BCash.BTrashPubKeyAddress>("bitcoincash:qpm2qsznhks23z7629mms6s4cwef74vcwvy22gdx6a");
			BitcoinAddress trashAddress2 = trashAddress.ScriptPubKey.GetDestinationAddress(bcash);
			Assert.Equal(trashAddress.ToString(), trashAddress2.ToString());

			trashAddress = bcash.Parse<NBitcoin.Altcoins.BCash.BTrashScriptAddress>("bitcoincash:ppm2qsznhks23z7629mms6s4cwef74vcwvn0h829pq");
			trashAddress2 = trashAddress.ScriptPubKey.GetDestinationAddress(bcash);
			Assert.Equal(trashAddress.ToString(), trashAddress2.ToString());
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
			Assert.Equal(500000000L, Money.Coins(5).Satoshi);
			Assert.Equal(500000000U, (uint)Money.Coins(5).Satoshi);
			Assert.Equal("5.00000000", Money.Coins(5).ToString());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCalculateBlockTimeCorrectly()
		{
			Assert.Equal(1.5, Network.Main.Consensus.GetExpectedBlocksFor(TimeSpan.FromMinutes(15)));
			Assert.Equal(TimeSpan.FromMinutes(15), Network.Main.Consensus.GetExpectedTimeFor(1.5));
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

			foreach (var test in tests)
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
			foreach (var prefix in new string[] { "", "+", "-" })
			{
				int multiplier = prefix == "-" ? -1 : 1;
				Assert.True(Money.TryParse(prefix + "0.0", out ret));
				AssertEx.Equal(ret, multiplier * Money.Zero);

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
		public void CanCompareKey()
		{
			var privateKey = new Key();
			var otherKey = new Key();
			var bitcoinSecret = privateKey.GetWif(Network.Main);
			var samePrivateKey = bitcoinSecret.PrivateKey;
			Assert.Equal(samePrivateKey, privateKey);
			Assert.True(samePrivateKey == privateKey);
			Assert.False(samePrivateKey != privateKey);
			Assert.True(samePrivateKey.GetHashCode() == privateKey.GetHashCode());

			Assert.NotEqual(otherKey, privateKey);
			Assert.False(otherKey == privateKey);
			Assert.True(otherKey != privateKey);
			Assert.False(otherKey.GetHashCode() == privateKey.GetHashCode());
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
		public void CanParseRPCCredentialString()
		{
			Assert.True(RPCCredentialString.Parse("default").UseDefault);
			Assert.Equal("c:/", RPCCredentialString.Parse("cookiefile=c:/").CookieFile);
			Assert.Equal("abc", RPCCredentialString.Parse("abc:def").UserPassword.UserName);
			Assert.Equal("def", RPCCredentialString.Parse("abc:def").UserPassword.Password);
			Assert.Equal("def:def", RPCCredentialString.Parse("abc:def:def").UserPassword.Password);

			Assert.Equal("abc", RPCCredentialString.Parse("wallet=abcd;abc:def").UserPassword.UserName);
			Assert.Equal("abcd", RPCCredentialString.Parse("wallet=abcd;abc:def").WalletName);
			Assert.Equal("wallet=abcd;abc:def", RPCCredentialString.Parse("wallet=abcd;abc:def").ToString());

			Assert.Equal("wallet=abcd;server=toto:3030;abc:def", RPCCredentialString.Parse("wallet=abcd;server=toto:3030;abc:def").ToString());
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
		[Trait("UnitTest", "UnitTest")]
		public void FeeRateCalculation()
		{
			Assert.Equal(Money.Satoshis(1L), new FeeRate(0.5m).GetFee(1));
			Assert.Equal(Money.Satoshis(3L), new FeeRate(3m).GetFee(1));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void FeeRateFormatting()
		{
			var fee = new FeeRate(0.5m);
			Assert.Equal("0.5 Sat/B", fee.ToString());
			fee = new FeeRate(1.0m);
			Assert.Equal("1 Sat/B", fee.ToString());
			fee = new FeeRate(0.521748274m);
			Assert.Equal("0.521 Sat/B", fee.ToString());
		}
#if !NO_SOCKET
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public async Task CanConvertEndpointToIPEndpoint()
		{
			var localIp = (await Dns.GetHostAddressesAsync("localhost")).First();
			var data = new (string Input, string ExpectedOutput)[]
			{
				( "FD87:D87E:EB43:edb1:8e4:3588:e546:35ca", "fd87:d87e:eb43:edb1:8e4:3588:e546:35ca" ),
				( "5wyqrzbvrdsumnok.onion", "fd87:d87e:eb43:edb1:8e4:3588:e546:35ca" ),
				( "10.10.1.3", "10.10.1.3"),
				( "localhost", localIp.ToString())
			};

			foreach (var test in data)
			{
				var endpoint = Utils.ParseEndpoint(test.Input, 10);
				var result = (await endpoint.ResolveToIPEndpointsAsync()).First();
				Assert.Equal(test.ExpectedOutput + ":10", result.ToEndpointString().Replace("[", "").Replace("]", ""));
			}
		}
#endif
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void FeeRateComparison()
		{
			var a = new FeeRate(Money.Coins(2.0m));
			var b = new FeeRate(Money.Coins(4.0m));
			Assert.True(a < b);
			Assert.True(b > a);
			Assert.False(b < a);
			Assert.False(a > b);
			Assert.True(a != b);
			Assert.True(b != a);

			Assert.True(FeeRate.Min(a, b) == a);
			Assert.True(FeeRate.Min(b, a) == a);
			Assert.True(FeeRate.Max(a, b) == b);
			Assert.True(FeeRate.Max(b, a) == b);

			Assert.True(a.CompareTo(b) < 0);
			Assert.True(b.CompareTo(a) > 0);
			Assert.True(a.CompareTo(a) == 0);
			Assert.True(b.CompareTo(b) == 0);

			var aa = new FeeRate(Money.Coins(2.0m));
			var bb = new FeeRate(Money.Coins(4.0m));
			var o = new object();
			Assert.True(a == aa);
			Assert.True(a.Equals(aa));
			Assert.False(a.Equals(o));
			Assert.True(b == bb);
			Assert.True(b != aa);
			Assert.True(b != null);
			Assert.True(null == (null as FeeRate));
			Assert.False(null != (null as FeeRate));
			Assert.False(a.Equals(b));
			Assert.True(aa == a);
			Assert.True(bb == b);
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
#if !HAS_SPAN
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanRoundTripBigIntegerToBytes()
		{
			foreach (var expected in Enumerable.Range(-100, 100))
			{
				var bytes = Utils.BigIntegerToBytes(BigInteger.ValueOf(expected));
				var actual = Utils.BytesToBigInteger(bytes);
				Assert.Equal<BigInteger>(BigInteger.ValueOf(expected), actual);
			}
		}
#endif

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanDivideMoney()
		{
			var bobInput = Money.Coins(1.1M);
			var aliceInput = Money.Coins(0.275M);
			var actual = (bobInput + aliceInput) / 2;
			Money expected = Money.Satoshis((bobInput.Satoshi + aliceInput.Satoshi) / 2);
			Assert.Equal(expected, actual);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanUseHDCache()
		{
			var k = new ExtKey();
			var cache = (HDKeyCache)k.AsHDKeyCache();
			var actual = cache.Derive(0);
			var expected = k.Derive(0);
			Assert.Equal(expected.GetPublicKey(), actual.GetPublicKey());
			actual = cache.Derive(0);
			Assert.Equal(expected.GetPublicKey(), actual.GetPublicKey());

			actual = actual.Derive(0);
			expected = k.Derive(new KeyPath("0/0"));
			Assert.Equal(expected.GetPublicKey(), actual.GetPublicKey());

			actual = k.AsHDKeyCache().Derive(new KeyPath("0/0"));
			expected = k.Derive(new KeyPath("0/0"));
			Assert.Equal(expected.GetPublicKey(), actual.GetPublicKey());

			actual = actual.Derive(new KeyPath("0/0"));
			expected = k.Derive(new KeyPath("0/0/0/0"));
			Assert.Equal(expected.GetPublicKey(), actual.GetPublicKey());


			var baseKeyPath = new KeyPath("0/0/0/0/1/2/3/4");
			var keypaths = Enumerable.Range(0, 20).Select(i => baseKeyPath.Derive((uint)i)).ToArray();
			cache = (HDKeyCache)k.AsHDKeyCache();
			var result = cache.Derive(keypaths);
			Assert.Equal(k.Derive(baseKeyPath.Derive(19U)).GetPublicKey(), result[19].GetPublicKey());
			Assert.Equal(8 + 20, cache.Cached);
		}
#if !HAS_SPAN
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanConvertBigIntegerToBytes()
		{
			CanConvertBigIntegerToBytesCore(BigInteger.Zero, new byte[0]);
			CanConvertBigIntegerToBytesCore(BigInteger.Zero, new byte[] { 0 }, false);
			CanConvertBigIntegerToBytesCore(BigInteger.Zero, new byte[] { 0x80 }, false);
			CanConvertBigIntegerToBytesCore(BigInteger.One, new byte[] { 1 });
			CanConvertBigIntegerToBytesCore(BigInteger.One.Negate(), new byte[] { 0x81 });
			CanConvertBigIntegerToBytesCore(BigInteger.ValueOf(-128), new byte[] { 0x80, 0x80 });
			CanConvertBigIntegerToBytesCore(BigInteger.ValueOf(-129), new byte[] { 0x81, 0x80 });
			CanConvertBigIntegerToBytesCore(BigInteger.ValueOf(-256), new byte[] { 0x00, 0x81 });
		}

		private void CanConvertBigIntegerToBytesCore(BigInteger b, byte[] bbytes, bool testByteSerialization = true)
		{
			Assert.Equal(b, Utils.BytesToBigInteger(bbytes));
			if (testByteSerialization)
				Assert.True(Utils.BigIntegerToBytes(b).SequenceEqual(bbytes));
		}
#endif

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanConvertBigIntegerLongToBytes()
		{
			CanConvertBigIntegerLongToBytesCore(0, new byte[0]);
			CanConvertBigIntegerLongToBytesCore(0, new byte[] { 0 }, false);
			CanConvertBigIntegerLongToBytesCore(0, new byte[] { 0x80 }, false);
			CanConvertBigIntegerLongToBytesCore(1, new byte[] { 1 });
			CanConvertBigIntegerLongToBytesCore(-1, new byte[] { 0x81 });
			CanConvertBigIntegerLongToBytesCore(-128, new byte[] { 0x80, 0x80 });
			CanConvertBigIntegerLongToBytesCore(-129, new byte[] { 0x81, 0x80 });
			CanConvertBigIntegerLongToBytesCore(-256, new byte[] { 0x00, 0x81 });
		}

		private void CanConvertBigIntegerLongToBytesCore(long b, byte[] bbytes, bool testByteSerialization = true)
		{
			// Assert.Equal(b, Utils.BytesToBigInteger(bbytes)); we don't care about this test
			if (testByteSerialization)
				Assert.True(Op.GetPushOp(b).PushData.SequenceEqual(bbytes));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void NetworksAreValid()
		{
			foreach (var network in Network.GetNetworks())
			{
				Assert.NotNull(network);
			}
		}

		// Pubkey: 04a5cf05bfe42daffaff4f1732f5868ed7c7919cba279fa7d940e6b02a8b059bde56be218077bcab1ad6b5f5dcb04c42534477fb8d21b6312b0063e08a8ae52b3e, Private: 7bd0db101160c888e9643f10594185a36a8db91b5308aaa7aad4c03245c6bdc1, Secret: a461392f592ff4292bfce732d808a07f1bc3f49c9a66a40d50761ffb8b2325f6
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanECDH()
		{
			var tests = new[]
			{
				new
				{
					Pubkey = "04a5cf05bfe42daffaff4f1732f5868ed7c7919cba279fa7d940e6b02a8b059bde56be218077bcab1ad6b5f5dcb04c42534477fb8d21b6312b0063e08a8ae52b3e",
					Private = "7bd0db101160c888e9643f10594185a36a8db91b5308aaa7aad4c03245c6bdc1",
					ExpectedSecret = "a461392f592ff4292bfce732d808a07f1bc3f49c9a66a40d50761ffb8b2325f6"
				},
				new
				{
					Pubkey = "043f12235bcf2776c8489ed138d4c9b85a1e29f3f4ad2787b9c8588e960867afc9de1e5702caa787665f5d0a4b04015c8bd5f1541e3d170efc3668f6ac587d43bc",
					Private = "1249b289c5959c71ae60e0a2a7d57dffbd5cb862aaf10442db205f6787791732",
					ExpectedSecret = "1d664ba11d3925cfcd938b2ef131213ba4ca986822944d0a7616b34027738e7c"
				},
				new
				{
					Pubkey = "04769c29328998917d9f2f7c6ce46f2f12a6064e937dff722b4811e9c88b4e1d45387fea132321541e8dbdc92384aef1944d650aa889bfa836db078897e5299262",
					Private = "41d0cbeeb3365b8c9e190f9898689997002f94006ad3bf1dcfbac28b6e4fb84d",
					ExpectedSecret = "7fcfa754a40ceaabee5cd3df1a99ee2e5d2c027fdcbd8e437d9be757ea58708f"
				}
			};
			foreach (var test in tests)
			{
				var pubKey = new PubKey(test.Pubkey);
				var key = new Key(Encoders.Hex.DecodeData(test.Private));
				var secret = pubKey.GetSharedPubkey(key);
				Assert.Equal(test.ExpectedSecret, Encoders.Hex.EncodeData(Hashes.SHA256(secret.ToBytes())));
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanEncryptAndDecryptMessages()
		{
			var key = GetKeyFromPassword("my-little-passwrod");

			var msgs = new byte[][]{
				new byte[555],
				Encoders.ASCII.DecodeData("Chancellor on the brink of second bailout for banks")
			};
			foreach (var plainText in msgs)
			{
				var cipherText1 = key.PubKey.Encrypt(plainText);
				var cipherText2 = key.PubKey.Encrypt(Encoders.ASCII.EncodeData(plainText));
				var text1 = key.Decrypt(cipherText1);
				var text2 = key.Decrypt(cipherText2);
				Assert.True(Utils.ArrayEqual(text1, plainText));
				Assert.Equal(text2, Encoders.ASCII.EncodeData(plainText));

				// Encrypt twice, should not give twice same cypher
				var cipherText3 = key.PubKey.Encrypt(plainText);
				Assert.True(!Utils.ArrayEqual(cipherText1, cipherText3));
			}

			// Electrum compatibility
			key = Key.Parse("cQnjfHeuxMBz9gVo7utDmhjFqppGAUTejtjVhgmULTgf5saZLX8Q", Network.TestNet);
			var text = key.Decrypt("QklFMQORN+6df13s/J7dSUIIK2Y9i9/MmHXwP3jA1daFwWjR+fRWP6qnW3+MZF+d6J8wOWDzrftx4O52fs4yplCyFL3gi+pSGE7YsngXHz/bLiulpQ==");
			Assert.Equal("Hello world", text);
		}

		private Key GetKeyFromPassword(string password)
		{
			var bytes = Encoding.UTF8.GetBytes(password);
#pragma warning disable CS0618 // Type or member is obsolete
#if NO_NATIVE_HMACSHA512
			var mac = new NBitcoin.BouncyCastle.Crypto.Macs.HMac(new NBitcoin.BouncyCastle.Crypto.Digests.Sha512Digest());
			mac.Init(new NBitcoin.BouncyCastle.Crypto.Parameters.KeyParameter(bytes));
			var secret = Pbkdf2.ComputeDerivedKey(mac, new byte[0], 1024, 32);
#else
			var secret = NBitcoin.Crypto.Pbkdf2.ComputeDerivedKey(new System.Security.Cryptography.HMACSHA512(bytes), new byte[0], 1024, 32);
#endif
#pragma warning restore CS0618
			return new Key(secret);
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

		public class DummyClass
		{
			public BitcoinExtPubKey ExtPubKey
			{
				get; set;
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanDetectBase58WithoutAmbiguity()
		{
			var address = new
			{
				Base58 = "bWyXRVD4J3Y8bG8VQ8aQmnnztdMNzExRdaw",
				ExpectedType = typeof(BitcoinColoredAddress),
				Network = Network.RegTest
			};

			var result = Network.Parse(address.Base58, address.Network);
			Assert.IsType<BitcoinColoredAddress>(result);
			Assert.True(result.Network == Network.RegTest);

			address = new
			{
				Base58 = new ExtKey().Neuter().ToString(Network.RegTest),
				ExpectedType = typeof(BitcoinExtPubKey),
				Network = Network.RegTest
			};

			result = Network.Parse(address.Base58, address.Network);
			Assert.IsType<BitcoinExtPubKey>(result);
			Assert.True(result.Network == Network.RegTest);

			result = Network.Parse(address.Base58, Network.TestNet);
			Assert.True(result.Network == Network.TestNet);

			var str = Serializer.ToString(new DummyClass() { ExtPubKey = new ExtKey().Neuter().GetWif(Network.RegTest) }, Network.RegTest);
			Assert.NotNull(Serializer.ToObject<DummyClass>(str, Network.RegTest));
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
						Base58 = "bc1qrp33g0q5c5txsp9arysrx4k6zdkfs4nce4xj0gdcccefvpysxf3qccfmv3",
						ExpectedType = typeof(BitcoinWitScriptAddress),
						Network = Network.Main
					},
					new
					{
						Base58 = "tb1qrp33g0q5c5txsp9arysrx4k6zdkfs4nce4xj0gdcccefvpysxf3q0sl5k7",
						ExpectedType = typeof(BitcoinWitScriptAddress),
						Network = Network.TestNet
					},
					new
					{
						Base58 = "bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4",
						ExpectedType = typeof(BitcoinWitPubKeyAddress),
						Network = Network.Main
					},
					new
					{
						Base58 = "tb1qw508d6qejxtdg4y5r3zarvary0c5xw7kxpjzsx",
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
					},
					new
					{
						Base58 = "akB4NBW9UuCmHuepksob6yfZs6naHtRCPNy",
						ExpectedType = typeof(BitcoinColoredAddress),
						Network = Network.Main
					}
				};

			foreach (var test in tests)
			{
				if (test.ExpectedType == null)
				{
					Assert.Throws<FormatException>(() => Network.Parse(test.Base58, test.Network ?? Network.RegTest));
				}
				else
				{
					var result = Network.Parse(test.Base58, test.Network);
					Assert.True(test.ExpectedType == result.GetType());
					if (test.Network != null)
						Assert.Equal(test.Network, result.Network);

					foreach (var network in Network.GetNetworks())
					{
						if (network == test.Network)
							break;
						Assert.Throws<FormatException>(() => Network.Parse(test.Base58, network));
					}
				}
			}
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
				new MoneyBag(new AssetMoney(msft, 10), new AssetMoney(goog, 3), new Money(1000L)),
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
				new MoneyBag(new AssetMoney(msft, 10), new AssetMoney(goog, 3), new Money(-1000L)),
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

			var b1_2 = b1 - (b2) + (new Money(10000L));
			Assert.True(
				b1_2.SequenceEqual(new IMoney[] { new AssetMoney(msft, 9), new AssetMoney(goog, 8), new Money(10000L) }));
		}
	}
}
