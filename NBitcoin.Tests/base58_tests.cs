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
		[Trait("Core", "Core")]
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

					Key privkey = secret.PrivateKey;
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

					if(exp_addrType == "script")
						Assert.True(addr.GetType() == typeof(BitcoinScriptAddress));
					if(exp_addrType == "pubkey")
						Assert.True(addr.GetType() == typeof(BitcoinAddress));

					Assert.Throws<FormatException>(() => network.CreateBitcoinSecret(exp_base58string));
				}
			}
		}


		// Goal: check that generated keys match test vectors
		[Fact]
		[Trait("Core", "Core")]
		public void base58_keys_valid_gen()
		{
			var tests = TestCase.read_json("data/base58_keys_valid.json");
			Network network = null;

			foreach(var test in tests)
			{
				string strTest = test.ToString();
				if(test.Count < 3) // Allow for extra stuff (useful for comments)
				{
					Assert.False(true, "Bad test: " + strTest);
					continue;
				}
				string exp_base58string = (string)test[0];
				byte[] exp_payload = TestUtils.ParseHex((string)test[1]);
				dynamic metadata = test.GetDynamic(2);
				bool isPrivkey = (bool)metadata.isPrivkey;
				bool isTestnet = (bool)metadata.isTestnet;

				if(isTestnet)
					network = Network.TestNet;
				else
					network = Network.Main;
				if(isPrivkey)
				{
					bool isCompressed = metadata.isCompressed;
					Key key = new Key(exp_payload, fCompressedIn: isCompressed);
					BitcoinSecret secret = network.CreateBitcoinSecret(key);
					Assert.True(secret.ToString() == exp_base58string, "result mismatch: " + strTest);
				}
				else
				{
					string exp_addrType = (string)metadata.addrType;
					TxDestination dest;
					if(exp_addrType == "pubkey")
					{
						dest = new KeyId(new uint160(exp_payload));
					}
					else if(exp_addrType == "script")
					{
						dest = new ScriptId(new uint160(exp_payload));
					}
					else if(exp_addrType == "none")
					{
						dest = new TxDestination(0);
					}
					else
					{
						Assert.True(false, "Bad addrtype: " + strTest);
						continue;
					}
					try
					{
						BitcoinAddress addrOut = network.CreateBitcoinAddress(dest);
						Assert.True(addrOut.ToString() == exp_base58string, "mismatch: " + strTest);
					}
					catch(ArgumentException)
					{
						Assert.True(dest.GetType() == typeof(TxDestination));
					}
				}
			}

			// Visiting a CNoDestination must fail
			TxDestination nodest = new TxDestination();
			Assert.Throws<ArgumentException>(() => network.CreateBitcoinAddress(nodest));
		}

		// Goal: check that base58 parsing code is robust against a variety of corrupted data
		[Fact]
		[Trait("Core", "Core")]
		public void base58_keys_invalid()
		{

			var tests = TestCase.read_json("data/base58_keys_invalid.json"); // Negative testcases

			foreach(var test in tests)
			{
				string strTest = tests.ToString();
				if(test.Count < 1) // Allow for extra stuff (useful for comments)
				{
					Assert.False(true, "Bad test: " + strTest);
					continue;
				}
				string exp_base58string = (string)test[0];

				// must be invalid as public and as private key
				Assert.Throws<FormatException>(() => Network.Main.CreateBitcoinAddress(exp_base58string));
				Assert.Throws<FormatException>(() => Network.Main.CreateBitcoinSecret(exp_base58string));
			}
		}
	}
}
