using Bitcoin.Private.Bitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bitcoin.Private.Bitcoin.Tests
{
	public class base58_tests
	{
		[Fact]
		public void base58_EncodeBase58()
		{
			var tests = TestCase.read_json("Data\\base58_encode_decode.json");
			foreach(var test in tests)
			{
				var strTest = test.ToString();
				if(test.Count < 2) // Allow for extra stuff (useful for comments)
				{
					Assert.False(true, "Bad test: " + strTest);
					continue;
				}
				var sourcedata = Encoders.Hex.DecodeData(test.GetValue<string>(0));
				var base58string = test.GetValue<string>(1);
				Assert.True(
							Encoders.Base58.EncodeData(sourcedata) == base58string,
							strTest);
			}
		}

		[Fact]
		public void base58_DecodeBase58()
		{
			var tests = TestCase.read_json("Data\\base58_encode_decode.json");
			byte[] result = null;
			foreach(var test in tests)
			{
				var strTest = test.ToString();
				if(test.Count < 2) // Allow for extra stuff (useful for comments)
				{
					Assert.False(true, "Bad test: " + strTest);
					continue;
				}
				var expected = Encoders.Hex.DecodeData(test.GetValue<string>(0));
				var base58string = test.GetValue<string>(1);
				result = Encoders.Base58.DecodeData(base58string);
				AssertEx.CollectionEquals(result, expected);
			}

			Assert.Throws<FormatException>(() => Encoders.Base58.DecodeData("invalid"));

			// check that DecodeBase58 skips whitespace, but still fails with unexpected non-whitespace at the end.
			Assert.Throws<FormatException>(() => Encoders.Base58.DecodeData(" \t\n\v\f\r skip \r\f\v\n\t a"));
			result = Encoders.Base58.DecodeData(" \t\n\v\f\r skip \r\f\v\n\t ");
			var expected2 = Encoders.Hex.DecodeData("971a55");
			AssertEx.CollectionEquals(result, expected2);
		}

		[Fact]
		public void base58_keys_valid_parse()
		{
			var tests = TestCase.read_json("Data\\base58_keys_valid.json");
			var o = tests[0].GetDynamic(2);
		}
	}
}
