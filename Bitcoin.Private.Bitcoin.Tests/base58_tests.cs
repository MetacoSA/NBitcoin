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
				var sourcedata = Utils.ParseHex(test.GetValue<string>(0));
				var base58string = test.GetValue<string>(1);
				Assert.True(
							Utils.EncodeBase58(sourcedata) == base58string,
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
				var expected = Utils.ParseHex(test.GetValue<string>(0));
				var base58string = test.GetValue<string>(1);
				Assert.True(Utils.DecodeBase58(base58string, out result), strTest);
				AssertEx.CollectionEquals(result, expected);
			}

			Assert.True(!Utils.DecodeBase58("invalid", out result));

			// check that DecodeBase58 skips whitespace, but still fails with unexpected non-whitespace at the end.
			Assert.True(!Utils.DecodeBase58(" \t\n\v\f\r skip \r\f\v\n\t a", out result));
			Assert.True(Utils.DecodeBase58(" \t\n\v\f\r skip \r\f\v\n\t ", out result));
			var expected2 = Utils.ParseHex("971a55");
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
