using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
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
		public void util_ParseMoney()
		{
			Money ret = new Money(0);
			Assert.True(Money.TryParse("0.0", out ret));
			AssertEx.Equal(ret, new Money(0));

			Assert.True(Money.TryParse("12345.6789", out  ret));
			AssertEx.Equal(ret, new Money((Money.COIN / 10000) * 123456789));

			Assert.True(Money.TryParse("100000000.00", out  ret));
			AssertEx.Equal(ret, new Money(Money.COIN * 100000000));
			Assert.True(Money.TryParse("10000000.00", out  ret));
			AssertEx.Equal(ret, new Money(Money.COIN * 10000000));
			Assert.True(Money.TryParse("1000000.00", out  ret));
			AssertEx.Equal(ret, new Money(Money.COIN * 1000000));
			Assert.True(Money.TryParse("100000.00", out ret));
			AssertEx.Equal(ret, new Money(Money.COIN * 100000));
			Assert.True(Money.TryParse("10000.00", out  ret));
			AssertEx.Equal(ret, new Money(Money.COIN * 10000));
			Assert.True(Money.TryParse("1000.00", out  ret));
			AssertEx.Equal(ret, new Money(Money.COIN * 1000));
			Assert.True(Money.TryParse("100.00", out  ret));
			AssertEx.Equal(ret, new Money(Money.COIN * 100));
			Assert.True(Money.TryParse("10.00", out ret));
			AssertEx.Equal(ret, new Money(Money.COIN * 10));
			Assert.True(Money.TryParse("1.00", out  ret));
			AssertEx.Equal(ret, new Money(Money.COIN));
			Assert.True(Money.TryParse("0.1", out  ret));
			AssertEx.Equal(ret, new Money(Money.COIN / 10));
			Assert.True(Money.TryParse("0.01", out  ret));
			AssertEx.Equal(ret, new Money(Money.COIN / 100));
			Assert.True(Money.TryParse("0.001", out  ret));
			AssertEx.Equal(ret, new Money(Money.COIN / 1000));
			Assert.True(Money.TryParse("0.0001", out  ret));
			AssertEx.Equal(ret, new Money(Money.COIN / 10000));
			Assert.True(Money.TryParse("0.00001", out  ret));
			AssertEx.Equal(ret, new Money(Money.COIN / 100000));
			Assert.True(Money.TryParse("0.000001", out  ret));
			AssertEx.Equal(ret, new Money(Money.COIN / 1000000));
			Assert.True(Money.TryParse("0.0000001", out ret));
			AssertEx.Equal(ret, new Money(Money.COIN / 10000000));
			Assert.True(Money.TryParse("0.00000001", out  ret));
			AssertEx.Equal(ret, new Money(Money.COIN / 100000000));

			// Attempted 63 bit overflow should fail
			Assert.True(!Money.TryParse("92233720368.54775808", out  ret));
		}
		[Fact]
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
		public void CanConvertBigIntegerToBytes()
		{
			Assert.Equal<BigInteger>(0, Utils.BytesToBigInteger(new byte[0]));
			Assert.Equal<BigInteger>(0, Utils.BytesToBigInteger(new byte[] { 0 }));
			Assert.Equal<BigInteger>(0, Utils.BytesToBigInteger(new byte[] { 0x80 }));
			Assert.Equal<BigInteger>(1, Utils.BytesToBigInteger(new byte[] { 1 }));
			Assert.Equal<BigInteger>(-1, Utils.BytesToBigInteger(new byte[] { 0x81 }));
			Assert.Equal<BigInteger>(-128, Utils.BytesToBigInteger(new byte[] { 0x80, 0x80 }));
		}
	}
}
