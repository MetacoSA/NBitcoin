using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class base58_tests
	{
		[Fact]
		[Trait("Core", "Core")]
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
		[Trait("Core", "Core")]
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
		[Trait("Core","Core")]
		public void base58_keys_valid_parse()
		{
			var tests = TestCase.read_json("Data\\base58_keys_valid.json");
			Network network;
			foreach(var test in tests)
			{
				string strTest = test.ToString();
				if(test.Count < 3) // Allow for extra stuff (useful for comments)
				{
					Assert.True(false, "Bad test " + strTest);
					continue;
				}

				string exp_base58string = (string)test[0];
				byte[] exp_payload = TestUtils.ParseHex((string)test[1]);
				//const Object &metadata = test[2].get_obj();
				bool isPrivkey = (bool)test.GetDynamic(2).isPrivkey;
				bool isTestnet = (bool)test.GetDynamic(2).isTestnet;
				if(isTestnet)
					network = Network.TestNet;
				else
					network = Network.Main;

				if(isPrivkey)
				{
					bool isCompressed = (bool)test.GetDynamic(2).isCompressed;

					// Must be valid private key
					// Note: CBitcoinSecret::SetString tests isValid, whereas CBitcoinAddress does not!
					var secret = network.CreateBitcoinSecret(exp_base58string);
					//If not valid exception would throw

					Key privkey = secret.Key;
					Assert.True(privkey.IsCompressed == isCompressed, "compressed mismatch:" + strTest);
					Assert.True(Utils.ArrayEqual(privkey.ToBytes(), exp_payload), "key mismatch:" + strTest);

					// Private key must be invalid public key
					Assert.Throws<FormatException>(() => network.CreateBitcoinAddress(exp_base58string));
				}
				else
				{
					string exp_addrType = (string)test.GetDynamic(2).addrType; // "script" or "pubkey"
					// Must be valid public key
					var addr = network.CreateBitcoinAddress(exp_base58string);
					Assert.True((addr is BitcoinScriptAddress) == (exp_addrType == "script"), "isScript mismatch" + strTest);
					
					//CTxDestination dest = addr.Get();
					//Assert.True(boost::apply_visitor(TestAddrTypeVisitor(exp_addrType), dest), "addrType mismatch" + strTest);

					Assert.Throws<FormatException>(() => network.CreateBitcoinSecret(exp_base58string));
				}
			}
		}
	}
}
